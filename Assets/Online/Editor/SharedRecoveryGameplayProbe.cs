using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // Real loader + one local game world and one transport peer. The save provider
    // must be isolated with SharedPerformanceTestSave before entering Play Mode.
    public static class SharedRecoveryGameplayProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        [MenuItem("Tools/Online/Test Recovery In Real Level (isolated Lobby Play Mode)")]
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true; Result = "running";
            NetworkRunner peer = null;
            var mode = NetworkProjectConfig.Global.PeerMode;
            try
            {
                Require(SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave, "isolated save required");
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                await Until(() => LoadingManager.Instance?.CurrentLocation == Location.Lobby, "lobby");
                var lobby = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                lobby.SelectOnline();
                await Until(() => lobby.IsOnline && !lobby.IsConnecting, "online lobby");
                Require(await lobby.CreateDepartureAsync(0, 2), "create departure");
                var root = new GameObject("Recovery game transport peer");
                UnityEngine.Object.DontDestroyOnLoad(root);
                peer = root.AddComponent<NetworkRunner>();
                var scenes = root.AddComponent<LobbySceneManager>();
                var provider = root.AddComponent<NetworkObjectProviderDefault>();
                var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig();
                config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var start = await peer.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared, Config = config, PlayerUniqueId = 7702,
                    CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"),
                    SessionName = lobby.DepartureSessionName, CustomLobbyName = LobbyOnlineBootstrap.DepartureDirectoryName,
                    EnableClientSessionCreation = false, SceneManager = scenes, ObjectProvider = provider
                });
                Require(start.Ok, "second peer");
                peer.SetPlayerObject(peer.LocalPlayer, peer.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab")));
                await Until(() => Replica(peer)?.Phase == 2, "sealed crew");
                Replica(peer).RPC_SceneReady();
                await Until(() => SharedRunContext.Playing && BoardController.Instance != null && BoardController.Instance.StartGame, "real level");
                Replica(peer).RPC_BoatProfile(SharedBoatProfile.Local());
                var state = SharedRunContext.State;
                await Until(() => state.BoatInitialized && state.Items.Count > 0, "world snapshot");
                // Keep combat out of the state restoration test.
                typeof(SharedAdProtection).GetField("until", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .SetValue(null, Time.realtimeSinceStartupAsDouble + 300);
                var weaponPrefab = SharedItemCatalog.Load().entries.Select(e => e.prefab)
                    .First(p => p != null && p.GetComponentInChildren<RangedWeaponController>(true)?.GetWeaponType() == WeaponType.Pistol);
                var weapon = UnityEngine.Object.Instantiate(weaponPrefab);
                weapon.GetComponentInChildren<RangedWeaponController>(true).ApplySharedAmmo(3);
                Require(Inventory.Instance.AddItem(weapon), "personal weapon");
                PlayerAmmoManager.Instance.RestoreRecovery(31, 17, 9);
                PlayerStatsManager.Instance.LoadExp(17, 1);
                PlayerStatsManager.Instance.RestoreRecoveryHealth(80);
                var gold = UnityEngine.Object.FindObjectsByType<PickableItem>(FindObjectsSortMode.None).Where(p =>
                    p.GetComponent<WorldSpawnIdentity>() != null && p.GetComponent<EnemyCore>() == null &&
                    p.GetComponent<AmmoItem>() == null && p.GetComponent<EggCollectibleItem>() == null &&
                    !p.Usable && state.Items.TryGet(p.GetComponent<WorldSpawnIdentity>().Id, out var item) && item.Mode == 0)
                    .OrderByDescending(p => p.GetComponent<FuelItem>() != null).FirstOrDefault();
                Require(gold != null, "city pickup");
                ulong goldId = gold.GetComponent<WorldSpawnIdentity>().Id;
                PlayerMovement.Instance.transform.position = gold.transform.position + Vector3.up;
                await Task.Delay(400);
                Inventory.Instance.AddItem(gold);
                await Until(() => state.Items[goldId].Owner == state.Runner.LocalPlayer && state.Items[goldId].Mode == 2, "gold ownership");
                Debug.Log("[Recovery game probe] pickup confirmed.");
                int expectedCount = Inventory.Instance.GetInventoryList().Count;
                await Until(() => state.Personal.TryGet(state.Runner.LocalPlayer, out var data) && data.ItemCount == expectedCount &&
                    data.Pistol == 31 && data.Health == 80, "personal confirmed");
                await Task.Delay(2300);
                Debug.Log("[Recovery game probe] personal snapshot confirmed; disconnecting.");
                var oldRef = state.Runner.LocalPlayer;
                await state.Runner.Shutdown();
                await Until(() => SharedRunRecovery.Instance.Lost, "lost dialog");
                Require(SharedRunRecovery.Instance.CanSolo && SharedLocalGameplay.Blocked, "frozen choice and snapshot");
                await SharedRunRecovery.Instance.ResumeAsync();
                Require(SharedRunContext.Playing && SharedRunContext.State.Runner.LocalPlayer == oldRef, "actual reconnect");
                await Task.Delay(600);
                Require(Inventory.Instance.GetInventoryList().Count == expectedCount, "restored bag count");
                Require(PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 31, "personal reserve restored");
                Require(Mathf.Abs(PlayerStatsManager.Instance.Health - 80) < .01f, "health restored");
                Require(PlayerStatsManager.Instance.Experience().Exp == 17 && PlayerStatsManager.Instance.Experience().CurrentLevel == 1, "experience restored");
                Require(Inventory.Instance.GetInventoryList().Any(p => p.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo == 3), "weapon magazine restored");
                Require(UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(i => i.Id == goldId) == 1, "no duplicate city gold");
                Debug.Log("[Recovery game probe] reconnect PASS: real reload, gold, bag, magazine, reserve, health, avatar.");
                await Task.Delay(2500);
                var before = BoardController.Instance;
                await SharedRunContext.State.Runner.Shutdown();
                await Until(() => SharedRunRecovery.Instance.Lost && SharedRunRecovery.Instance.CanSolo, "second disconnect");
                await SharedRunRecovery.Instance.ContinueSoloAsync();
                await Until(() => SoloRunBranch.Active && !SharedRunContext.Active && BoardController.Instance != before &&
                    BoardController.Instance != null && BoardController.Instance.StartGame && Inventory.Instance.GetInventoryList().Count == expectedCount, "solo reload");
                Require(!SharedRunRecovery.HasPending && !SharedRunRecovery.Instance.CanRetry, "solo closes return");
                Require(PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 31, "solo reserve");
                Require(PlayerStatsManager.Instance.Experience().Exp == 17 && PlayerStatsManager.Instance.Experience().CurrentLevel == 1, "solo experience");
                Require(Inventory.Instance.GetInventoryList().Any(p => p.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo == 3), "solo magazine");
                Require(Replica(peer).Phase == 3 && peer.IsConnectedToServer, "original session remains with peer");
                Require(PlayerPrefs.HasKey("DeadBoat.SoloBranch.v1") && SaveManager.Instance.LoadGameProgress().Item1 >= 0, "solo save exists");
                var victim = UnityEngine.Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None)
                    .FirstOrDefault(e => !e.IsDead && e.GetComponent<WorldSpawnIdentity>() != null);
                Require(victim != null, "solo corpse victim");
                string corpseKey = "corpse:" + victim.GetComponent<WorldSpawnIdentity>().Id;
                victim.TakeDamage(999999);
                await Until(() => UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Any(i => i.Key == corpseKey), "solo corpse stable identity");
                var corpse = UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .First(i => i.Key == corpseKey);
                ulong corpseId = corpse.Id;
                Require(SharedItemCatalog.Load().Identify(corpse.Item) != 0, "corpse catalog template");
                UnityEngine.Object.Destroy(corpse.gameObject);
                await Until(() => JsonUtility.FromJson<SharedSoloSnapshot>(PlayerPrefs.GetString("DeadBoat.SoloBranch.v1"))
                    .Items.Any(i => i.Id == corpseId && i.Mode == 4), "consumed corpse saved");
                Result = "PASS";
                Debug.Log("[Recovery game probe] PASS: real reconnect/reload and separate saved solo fork. One local world, not two independent WebGL worlds.");
            }
            catch (Exception e) { Result = "FAIL: " + e.Message; Debug.LogError("[Recovery game probe] " + e); }
            finally
            {
                if (peer != null) try { await peer.Shutdown(); } catch (Exception) { }
                NetworkProjectConfig.Global.PeerMode = mode;
                SharedAdProtection.End();
                running = false;
            }
        }
        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Until(Func<bool> test, string label)
        {
            double until = Time.realtimeSinceStartupAsDouble + 120;
            while (!test()) { Require(Time.realtimeSinceStartupAsDouble < until, "timeout: " + label); await Task.Delay(100); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
