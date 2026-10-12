using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeadBoat.Online
{
    [Serializable]
    public struct SoloWorldItem
    {
        public ulong Id;
        public int Template, Ammo, Mode;
        public Vector3 Position, BoatPosition;
        public Quaternion Rotation, BoatRotation;
        public bool Prunable;
    }
    [Serializable]
    public struct SoloWorldEnemy
    {
        public ulong Id;
        public int HP, MaxHP;
        public int Level;
        public ulong Template;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Prunable;
    }
    [Serializable]
    public sealed class SharedSoloSnapshot
    {
        public int Seed, Level, NightSequence, Generator = RunRandom.Version;
        public string SourceRun;
        public long PlayerId;
        public float Distance, Origin, Fuel, Rear;
        public SharedPersonalData Personal;
        public SharedRecoveryItem[] Bag;
        public SoloWorldItem[] Items;
        public SoloWorldEnemy[] Enemies;
        public static SharedSoloSnapshot Capture(SharedDepartureState state)
        {
            var local = state.Runner.LocalPlayer;
            state.Personal.TryGet(local, out var data);
            var bag = new List<SharedRecoveryItem>();
            var ids = new HashSet<ulong>();
            int slot = state.CrewSlot(local);
            for (int i = 0; i < data.ItemCount; i++)
                if (state.PersonalItems.TryGet(slot * SharedDepartureState.RecoverySlots + i, out var item))
                {
                    if (item.WorldId != 0 && (!state.Items.TryGet(item.WorldId, out var current) || current.Owner != local ||
                        (current.Mode != 1 && current.Mode != 2))) continue;
                    if (item.WorldId != 0 && state.Items.TryGet(item.WorldId, out var latest)) item.Ammo = latest.WeaponAmmo;
                    bag.Add(item); ids.Add(item.WorldId);
                }
            var world = new List<SoloWorldItem>();
            foreach (var entry in state.Items)
            {
                var item = entry.Value;
                if (item.Owner == local && (item.Mode == 1 || item.Mode == 2) && ids.Add(entry.Key))
                    bag.Add(new SharedRecoveryItem { WorldId = entry.Key, Template = item.Template, Ammo = item.WeaponAmmo });
                world.Add(new SoloWorldItem
                {
                    Id = entry.Key, Template = item.Template, Ammo = item.WeaponAmmo,
                    Mode = item.Mode == 1 || item.Mode == 2 ? 4 : item.Mode,
                    Position = item.Position, Rotation = item.Rotation,
                    BoatPosition = item.BoatLocalPosition, BoatRotation = item.BoatLocalRotation, Prunable = item.Prunable
                });
            }
            var enemies = new List<SoloWorldEnemy>();
            foreach (var page in SharedWorldPage.All(state.Runner))
                foreach (var entry in page.Enemies) enemies.Add(new SoloWorldEnemy
                {
                    Id = entry.Key, HP = entry.Value.HP, MaxHP = entry.Value.MaxHP,
                    Template = entry.Value.Template, Level = entry.Value.Level,
                    Position = entry.Value.Position, Rotation = entry.Value.Rotation, Prunable = entry.Value.Prunable
                });
            return new SharedSoloSnapshot
            {
                Seed = state.Seed, Level = state.LevelId, SourceRun = state.Runner.SessionInfo.Name, PlayerId = SharedRunRecovery.LocalId,
                Distance = state.BoatDistance, Origin = LobbyNetworkAvatar.Origin.z,
                Fuel = state.BoatFuel, Rear = state.WorldRear, Personal = data, NightSequence = state.NightSpawnSequence,
                Bag = bag.ToArray(), Items = world.ToArray(), Enemies = enemies.ToArray()
            };
        }
    }
    // Only an explicit offline fork persists a world. Online return tickets do not.
    public sealed class SoloRunBranch : MonoBehaviour
    {
        private const string SaveKey = "DeadBoat.SoloBranch.v1";
        private static SoloRunBranch instance;
        private SharedSoloSnapshot snapshot;
        private readonly Dictionary<ulong, SoloWorldItem> items = new();
        private readonly Dictionary<ulong, SoloWorldEnemy> enemies = new();
        private readonly Dictionary<ulong, int> applied = new();
        private readonly List<WorldSpawnIdentity> identities = new();
        private double nextSave;
        private bool needsInventory;
        private bool waitingScene;
        public static bool Active => instance != null && instance.snapshot != null;
        public static int Seed => Active ? instance.snapshot.Seed : 0;
        public static string SourceRun => Active ? instance.snapshot.SourceRun : null;
        public static long SourcePlayerId => Active ? instance.snapshot.PlayerId : 0;
        public static float Rear => Active ? instance.snapshot.Rear : 0;
        public static bool ClosesReturn(string room, long playerId)
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            try
            {
                var value = JsonUtility.FromJson<SharedSoloSnapshot>(PlayerPrefs.GetString(SaveKey));
                return value != null && value.SourceRun == room && value.PlayerId == playerId;
            }
            catch (Exception) { return false; }
        }
        public static void Store(SharedSoloSnapshot value)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(value));
            PlayerPrefs.Save();
        }
        public static void RestoreStartup()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            try
            {
                var value = JsonUtility.FromJson<SharedSoloSnapshot>(PlayerPrefs.GetString(SaveKey));
                if (value == null || value.Generator != RunRandom.Version)
                { Clear(); return; }
                Create(value);
                instance.needsInventory = true;
                value.Personal.SeedSave(SaveManager.Instance.PersistentProvider, value.Level, value.Distance, value.Origin, value.Fuel);
            }
            catch (Exception) { Clear(); }
        }
        private static void Create(SharedSoloSnapshot value)
        {
            if (instance != null) Destroy(instance.gameObject);
            var root = new GameObject("Solo run branch");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<SoloRunBranch>();
            instance.snapshot = value;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += instance.RegisterAuthored;
            foreach (var item in value.Items ?? Array.Empty<SoloWorldItem>()) instance.items[item.Id] = item;
            foreach (var enemy in value.Enemies ?? Array.Empty<SoloWorldEnemy>()) instance.enemies[enemy.Id] = enemy;
        }
        public static void Begin(SharedSoloSnapshot value)
        {
            Create(value);
            instance.needsInventory = instance.waitingScene = true;
            var provider = SaveManager.Instance.PersistentProvider;
            value.Personal.SeedSave(provider, value.Level, value.Distance, value.Origin, value.Fuel);
            provider.SaveInventory(new List<PickableItem>());
            // The outgoing boat trigger must not reparent the preserved player
            // into the scene during the loader's first yielded frame.
            if (GameManager.Instance != null) GameManager.Instance.isEndGame = true;
            PlayerInput.Instance.SitTrain(false);
            PlayerManager.Instance.transform.SetParent(null);
            DontDestroyOnLoad(PlayerManager.Instance.gameObject);
            provider.SaveProgress();
            Store(value);
            Action loaded = null;
            loaded = () =>
            {
                if (instance != null) instance.waitingScene = false;
                GameLoader.Instance.OnSceneLoaded -= loaded;
            };
            GameLoader.Instance.OnSceneLoaded += loaded;
            if (LevelManager.Instance.TryGetLevel(value.Level, out var level))
                LoadingManager.Instance.LoadLocation(Location.Game, level.Scene, false);
        }
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            if (instance != null) Destroy(instance.gameObject);
            instance = null;
        }
        private void ApplyWorld()
        {
            ApplyWorldInternal();
        }
        public static void Suspend()
        {
            if (instance != null) Destroy(instance.gameObject);
            instance = null;
        }
        private void RegisterAuthored(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
            => SharedRunLifetime.RegisterAuthoredItems(scene);
        private void OnDestroy() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= RegisterAuthored;
        private void ApplyWorldInternal()
        {
            foreach (var record in items.Values)
            {
                if (record.Mode == 4 || record.Template == 0 || SharedItemsRuntime.TryFind(record.Id, out _) ||
                    (record.Prunable && record.Mode != 3 && record.Position.z < snapshot.Rear)) continue;
                var prefab = SharedItemCatalog.Load()?.Find(record.Template);
                if (prefab == null) continue;
                var item = Instantiate(prefab, record.Position - Vector3.forward * snapshot.Origin, record.Rotation);
                var id = item.gameObject.AddComponent<WorldSpawnIdentity>();
                id.RecoveryReplica = true;
                id.InitializeRemote(record.Id, 0);
            }
            foreach (var record in enemies.Values)
            {
                if (record.HP <= 0 || record.Template == 0 || SharedItemsRuntime.TryFind(record.Id, out _) ||
                    !SharedEnemiesRuntime.TryTemplate(record.Template, out var prefab)) continue;
                var enemy = Instantiate(prefab, record.Position - Vector3.forward * snapshot.Origin, record.Rotation);
                enemy.InitializeLevel(record.Level);
                var id = enemy.gameObject.AddComponent<WorldSpawnIdentity>();
                id.RecoveryReplica = true;
                id.InitializeRemote(record.Id, record.Template);
            }
            SharedItemsRuntime.CopyTo(identities);
            foreach (var identity in identities)
            {
                if (identity == null) continue;
                if (applied.TryGetValue(identity.Id, out int previousInstance) && previousInstance == identity.GetInstanceID()) continue;
                applied[identity.Id] = identity.GetInstanceID();
                if (identity.Item != null && items.TryGetValue(identity.Id, out var item))
                {
                    if (item.Mode == 4) { Destroy(identity.gameObject); continue; }
                    var pickable = identity.Item;
                    pickable.gameObject.SetActive(true);
                    pickable.ApplySharedWorldState(item.Mode);
                    pickable.ApplySharedPhysics(item.Mode != 3);
                    identity.Weapon?.ApplySharedAmmo(item.Ammo);
                    if (item.Mode == 3)
                    {
                        identity.transform.SetParent(BoardController.Instance.transform, false);
                        identity.transform.SetLocalPositionAndRotation(item.BoatPosition, item.BoatRotation);
                    }
                    else identity.transform.SetPositionAndRotation(item.Position - Vector3.forward * snapshot.Origin, item.Rotation);
                }
                if (identity.Enemy != null)
                {
                    if (enemies.TryGetValue(identity.Id, out var enemy))
                    {
                        identity.transform.SetPositionAndRotation(enemy.Position - Vector3.forward * snapshot.Origin, enemy.Rotation);
                        identity.Enemy.ApplySharedHealth(enemy.HP, enemy.MaxHP);
                    }
                    if (identity.Agent != null && !identity.Enemy.IsDead) identity.Agent.enabled = true;
                }
            }
        }
        private void Update()
        {
            if (waitingScene || snapshot == null || LoadingManager.Instance == null || LoadingManager.Instance.CurrentLocation != Location.Game ||
                BoardController.Instance == null || !BoardController.Instance.StartGame || SharedRunContext.Active) return;
            if (needsInventory)
            {
                needsInventory = false;
                snapshot.Personal.SeedSave(SaveManager.Instance.PersistentProvider, snapshot.Level, snapshot.Distance, snapshot.Origin, snapshot.Fuel);
                LevelStatManager.Instance.Stats = snapshot.Personal.Cards;
                RestoreInventoryAfterLoad();
                var save = SaveManager.Instance.PersistentProvider;
                PlayerAmmoManager.Instance.RestoreRecovery(save.LoadAmmo(WeaponType.Pistol),
                    save.LoadAmmo(WeaponType.Rifle), save.LoadAmmo(WeaponType.Shotgun));
                PlayerStatsManager.Instance.LoadExp(snapshot.Personal.Experience, snapshot.Personal.Level);
                PlayerStatsManager.Instance.RestoreRecoveryHealth(save.LoadPlayerHealth());
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, save.LoadGameCoin() - CurrencyManager.Instance.Coins);
            }
            ApplyWorld();
            if (Time.realtimeSinceStartupAsDouble < nextSave) return;
            nextSave = Time.realtimeSinceStartupAsDouble + 5;
            snapshot.Distance = BoardController.Instance.TotalDistanceTraveled;
            snapshot.Fuel = BoardController.Instance.currentFuel;
            snapshot.Origin = FixCoordinate.Instance.BoardAddPos;
            snapshot.Rear = snapshot.Distance - 1000;
            // The standard provider stores solo stats. This small sidecar preserves
            // the original generation and changed object identities.
            var bag = new List<SharedRecoveryItem>();
            foreach (var item in Inventory.Instance.GetInventoryList())
                bag.Add(new SharedRecoveryItem
                {
                    Template = SharedItemCatalog.Load()?.Identify(item) ?? 0,
                    Ammo = item.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo ?? -1,
                    Equipped = item == Inventory.Instance.GetComponent<ActiveItemManager>()?.Active ? 1 : 0
                });
            snapshot.Bag = bag.ToArray();
            snapshot.Personal = SharedPersonalData.Capture();
            SharedItemsRuntime.CopyTo(identities);
            foreach (var id in identities)
            {
                if (id == null) continue;
                if (id.Item != null) items[id.Id] = new SoloWorldItem
                {
                    Id = id.Id, Template = SharedItemCatalog.Load()?.Identify(id.Item) ?? 0,
                    Position = id.transform.position + Vector3.forward * snapshot.Origin,
                    Rotation = id.transform.rotation, Prunable = id.Prunable,
                    Mode = id.Item.Status == ItemStatus.InInventory || id.Item.Grabbed ? 4 : id.Item.Attached ? 3 : 0,
                    BoatPosition = BoardController.Instance.transform.InverseTransformPoint(id.transform.position),
                    BoatRotation = Quaternion.Inverse(BoardController.Instance.transform.rotation) * id.transform.rotation,
                    Ammo = id.Weapon?.CurrentAmmo ?? -1
                };
                if (id.Enemy != null) enemies[id.Id] = new SoloWorldEnemy
                {
                    Id = id.Id, HP = id.Enemy.SharedHP, MaxHP = id.Enemy.SharedMaxHP, Prunable = id.Prunable,
                    Template = id.TemplateId, Level = id.Enemy is LevelledEnemy levelled ? levelled.mobLevel : 1,
                    Position = id.transform.position + Vector3.forward * snapshot.Origin, Rotation = id.transform.rotation
                };
            }
            foreach (var key in items.Where(p => p.Value.Prunable && p.Value.Mode != 3 && p.Value.Position.z < snapshot.Rear).Select(p => p.Key).ToArray()) items.Remove(key);
            foreach (var key in enemies.Where(p => p.Value.Prunable && p.Value.Position.z < snapshot.Rear).Select(p => p.Key).ToArray()) enemies.Remove(key);
            snapshot.Items = items.Values.ToArray(); snapshot.Enemies = enemies.Values.ToArray();
            SaveManager.Instance.PersistentProvider.SaveInventory(Inventory.Instance.GetInventoryList());
            SaveManager.Instance.SaveGameProgress(Mathf.RoundToInt(snapshot.Distance), Inventory.Instance.GetInventoryList(), snapshot.Level);
            SaveManager.Instance.FlushProgress();
            Store(snapshot);
        }
        public static void RestoreInventoryAfterLoad()
        {
            if (!Active) return;
            SharedRecoveryInventory.Restore(instance.snapshot.Bag, false);
        }

        public static void RememberDestroyed(WorldSpawnIdentity identity)
        {
            if (!Active || instance.waitingScene || identity == null) return;
            if (!SharedItemsRuntime.TryFind(identity.Id, out var registered) || registered != identity) return;
            instance.applied.Remove(identity.Id);
            if (identity.Item != null)
            {
                instance.items.TryGetValue(identity.Id, out var item);
                item.Id = identity.Id; item.Mode = 4; item.Prunable = identity.Prunable;
                item.Position = identity.transform.position + Vector3.forward * instance.snapshot.Origin;
                instance.items[identity.Id] = item;
            }
            if (identity.Enemy != null)
            {
                instance.enemies.TryGetValue(identity.Id, out var enemy);
                enemy.Id = identity.Id; enemy.HP = 0; enemy.Prunable = identity.Prunable;
                enemy.Position = identity.transform.position + Vector3.forward * instance.snapshot.Origin;
                instance.enemies[identity.Id] = enemy;
            }
        }
        public static int NextNightSpawn() => Active ? ++instance.snapshot.NightSequence : 0;
        private void OnApplicationPause(bool paused) { if (paused) { nextSave = 0; Update(); } }
        private void OnApplicationQuit() { nextSave = 0; Update(); }
    }
}
