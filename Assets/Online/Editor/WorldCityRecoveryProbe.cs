using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Real Cloud peers, one inactive local city fixture. UI/ads/owner saves never run.
    public static class WorldCityRecoveryProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool running;
        public static string Result { get; private set; } = "not run";
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true; Result = "running";
            float scale = Time.timeScale;
            var peerMode = NetworkProjectConfig.Global.PeerMode;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active && FixCoordinate.Instance == null,
                    "empty isolated Play scene required");
                Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == "Assets/Online/SmokeTest/RecoveryProtocolProbe.unity",
                    "prepared recovery fixture scene required");
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                CheckBudget();
                await Case(false);
                await Case(true);
                await Case(true, true);
                Result = "PASS: city authority handoff and fresh Photon reconnect; both dropped-item load orders, origin500, seeded stock, latest HP/ammo, consumed/dead, no duplicates";
                Debug.Log("[City recovery] " + Result);
            }
            catch (Exception error) { Result = "FAIL: " + error; Debug.LogWarning("[City recovery] " + Result); }
            finally { Time.timeScale = scale; NetworkProjectConfig.Global.PeerMode = peerMode; running = false; }
        }
        private static async Task Case(bool cold, bool formerMaster = false)
        {
            NetworkRunner first = null, second = null, newcomer = null;
            Fixture fixture = null;
            var coordinateRoot = new GameObject("City recovery origin"); coordinateRoot.SetActive(false);
            var coordinate = coordinateRoot.AddComponent<FixCoordinate>(); coordinate.enabled = false;
            FixCoordinate.Instance = coordinate;
            try
            {
                Time.timeScale = 0;
                fixture = new Fixture(); // Asset IO must finish before live Cloud peers start ticking.
                string room = "city-probe-" + Guid.NewGuid().ToString("N");
                string user = Guid.NewGuid().ToString("N");
                first = SharedReadyBarrierProbe.NewRunner(); await Start(first, room, user + "-a", 51001);
                var authority = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 2)).GetComponent<SharedDepartureState>();
                Set(authority, "Seed", 27182818);
                second = SharedReadyBarrierProbe.NewRunner(); await Start(second, room, user + "-b", 51002);
                await Until(() => Valid(authority) && authority.Phase == 2 && Replica(second) != null && Replica(second).Phase == 2, "sealed crew");
                var remote = Replica(second);
                SpawnAvatar(first); SpawnAvatar(second);
                authority.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => authority.Phase == 3 && remote.Phase == 3, "ready crew");
                Set(authority, "WorldFront", 10000f);
                await Until(() => remote.WorldFront >= 10000, "city frontier replication");
                SharedRunContext.Begin(remote.Seed, remote.LevelId, fixture.Owner.gameObject, null, remote);
                fixture.Spawn();
                var actors = fixture.City.GetComponentsInChildren<WorldSpawnIdentity>(true);
                foreach (var actor in actors)
                    if (actor.Item != null) authority.RegisterItem(actor); else if (actor.Enemy != null) authority.SampleEnemy(actor, actor.Enemy);
                Require(actors.Count(a => a.Item != null) == 3 && actors.Count(a => a.Enemy != null) == 2, "controlled city content");
                var gun = actors.First(a => a.Weapon != null);
                var plain = actors.Where(a => a.Item != null && a.Weapon == null).ToArray();
                var defenders = actors.Where(a => a.Enemy != null).ToArray();
                ulong gunId = gun.Id, usedId = plain[0].Id, movedId = plain[1].Id, deadId = defenders[0].Id, hurtId = defenders[1].Id;
                var stock = fixture.Stock();
                int meshes = fixture.City.GetComponentsInChildren<MeshFilter>(true).Length;
                Require(meshes > 1000 && stock.Count > 0, "actual city geometry and shops");
                var ammo = authority.Items[gunId]; ammo.WeaponAmmo = 2; authority.Items.Set(gunId, ammo); gun.Weapon.ApplySharedAmmo(2);
                var used = authority.Items[usedId]; used.Mode = 4; authority.Items.Set(usedId, used);
                authority.MarkEnemyDead(deadId);
                Wound(authority, hurtId, 17);
                plain[1].transform.SetParent(fixture.Owner.transform, true);
                var moved = authority.Items[movedId]; moved.Dropped = true; moved.Position += Vector3.right * 300;
                authority.Items.Set(movedId, moved); plain[1].transform.position = moved.Position;
                // A published purchase snapshot; actual spending/ads were checked by RunGame.
                ulong boughtId = WorldSpawnIdentity.IdForKey("dropped:city-recovery-purchase");
                var bought = ammo; bought.Position += Vector3.right * 500; bought.Dropped = true;
                authority.Items.Add(boughtId, bought);
                await Until(() => remote.Items.TryGet(gunId, out var w) && w.WeaponAmmo == 2 &&
                    remote.Items.TryGet(usedId, out var u) && u.Mode == 4 && remote.Items.TryGet(movedId, out var d) && d.Dropped &&
                    remote.Items.ContainsKey(boughtId) && remote.TryEnemy(deadId, out var e) && e.HP == 0 &&
                    remote.TryEnemy(hurtId, out var h) && h.HP == 17, "city replication before sleep");
                Require(WorldReplicaRemoval.TryRemoveLocalTree(plain[0].gameObject, out var reason), reason);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(defenders[0].gameObject, out reason), reason);
                await Task.Delay(100);
                Require(fixture.Stream.Audit(fixture.Active, new[] { 0f }, 0), fixture.Stream.LastRefusal);
                await Task.Delay(100);
                Require(fixture.Active.Count == 0 && fixture.Stream.SleepingCount == 1, "whole city asleep");
                SetOrigin(coordinate, 500);
                plain[1].transform.position = moved.Position - LobbyNetworkAvatar.Origin;
                if (!cold || formerMaster)
                {
                    await first.Shutdown();
                    await Until(() => Valid(remote) && remote.Object.HasStateAuthority &&
                        SharedWorldPage.All(second).All(p => p.Object.HasStateAuthority), "city master/page takeover");
                    authority = remote;
                    await Task.Delay(350); // Let takeover consume the final incoming snapshot before fixture writes.
                    Require(SharedRunContext.Playing && fixture.Stream.SleepingCount == 1, "survivor lost city ticket");
                }
                ammo = authority.Items[gunId]; ammo.WeaponAmmo = 1; authority.Items.Set(gunId, ammo);
                bought = authority.Items[boughtId]; bought.WeaponAmmo = 1; authority.Items.Set(boughtId, bought);
                Wound(authority, hurtId, 13);
                await Until(() => remote.Items[gunId].WeaponAmmo == 1 && remote.TryEnemy(hurtId, out var h) && h.HP == 13, "latest city changes");
                await Task.Delay(200);
                Require(remote.Items[gunId].WeaponAmmo == 1 && remote.TryEnemy(hurtId, out var stable) && stable.HP == 13,
                    "fixture writes did not survive network ticks before restore");
                if (cold)
                {
                    Require(WorldReplicaRemoval.TryRemoveLocalTree(plain[1].gameObject, out reason), reason);
                    var oldStream = fixture.Stream; var oldActive = fixture.Active;
                    SharedRunContext.End(); Time.timeScale = 0;
                    if (!formerMaster) await second.Shutdown();
                    fixture.Dispose(); fixture = null;
                    await Task.Delay(100);
                    Require(Valid(authority) && authority.Phase == 3, "live crew ended on client exit");
                    newcomer = SharedReadyBarrierProbe.NewRunner();
                    await Start(newcomer, room, user + (formerMaster ? "-a" : "-b"), formerMaster ? 51001 : 51002, false);
                    await Until(() => Replica(newcomer) != null && Replica(newcomer).Phase == 3, "fresh city peer state");
                    remote = Replica(newcomer); SpawnAvatar(newcomer);
                    await Until(() => remote.WorldFront >= 10000 && remote.Items.TryGet(gunId, out var w) && w.WeaponAmmo == 1 &&
                        remote.Items.TryGet(boughtId, out var b) && b.WeaponAmmo == 1 && remote.TryEnemy(hurtId, out var h) && h.HP == 13,
                        "fresh canonical city pages");
                    fixture = new Fixture();
                    SharedRunContext.Begin(remote.Seed, remote.LevelId, fixture.Owner.gameObject, null, remote);
                    Require(SharedRunContext.Regions.RegionCount == 0 && SharedRunContext.Membership.BindingCount == 0, "cold local history survived");
                    Require(!oldStream.Audit(oldActive, new[] { 10000f }, 0) && oldStream.SleepingCount == 0, "stale city ticket accepted");
                    fixture.Spawn(); // City BEFORE dropped-item discovery: no old ticket or names.
                }
                else
                {
                    Require(fixture.Stream.Audit(fixture.Active, new[] { 10000f }, 0), fixture.Stream.LastRefusal);
                    fixture.Quiet();
                }
                await Task.Delay(100);
                Check(fixture, stock, gunId, usedId, deadId, hurtId, movedId, false);
                fixture.ReconcileDrops();
                CheckBought(boughtId);
                if (cold)
                {
                    // Fresh caches again, reverse order: dropped-item discovery BEFORE city.
                    fixture.RemoveReplicas(); SharedRunContext.End(); Time.timeScale = 0;
                    fixture.Dispose(); await Task.Delay(100); fixture = new Fixture();
                    SharedRunContext.Begin(remote.Seed, remote.LevelId, fixture.Owner.gameObject, null, remote);
                    fixture.ReconcileDrops(); fixture.Spawn(); await Task.Delay(100);
                    Check(fixture, stock, gunId, usedId, deadId, hurtId, movedId, false); CheckBought(boughtId);
                }
                Require(fixture.Stream.Audit(fixture.Active, new[] { 0f }, 0), fixture.Stream.LastRefusal);
                await Task.Delay(100); authority.MarkEnemyDead(hurtId);
                await Until(() => remote.TryEnemy(hurtId, out var h) && h.HP == 0, "death while city asleep");
                Require(fixture.Stream.Audit(fixture.Active, new[] { 10000f }, 0, false), fixture.Stream.LastRefusal);
                fixture.Quiet(); await Task.Delay(100);
                Check(fixture, stock, gunId, usedId, deadId, hurtId, movedId, true); CheckBought(boughtId);
                if (!cold) await CheckCityQueue(fixture.Owner);
                string scenario = formerMaster ? "former master reconnect after takeover" : cold ? "cold reconnect + both load orders" : "master takeover";
                Debug.Log($"[City recovery] {scenario} PASS: meshes={meshes}, stores={stock.Count}; origin500, stable stock, ammo1/HP13, consumed/dead absent, detached drop outside city, purchase snapshot unique, death during second sleep.");
            }
            finally
            {
                fixture?.RemoveReplicas(); SharedRunContext.End(); fixture?.Dispose();
                FixCoordinate.Instance = null; Object.Destroy(coordinateRoot);
                if (newcomer != null && newcomer.IsRunning) await newcomer.Shutdown();
                if (second != null && second.IsRunning) await second.Shutdown();
                if (first != null && first.IsRunning) await first.Shutdown();
                Time.timeScale = 0;
            }
        }
        private static void Check(Fixture f, Dictionary<string, string> stock, ulong gun, ulong used, ulong dead, ulong hurt, ulong moved, bool killed)
        {
            Require(f.Active.Count == 1 && f.City.transform.position.z == 9500, "city count/origin changed");
            Require(stock.OrderBy(x => x.Key).SequenceEqual(f.Stock().OrderBy(x => x.Key)), "recovered city stock changed");
            Require(!SharedItemsRuntime.TryFind(used, out _) && !SharedItemsRuntime.TryFind(dead, out _), "consumed/dead city content resurrected");
            Require(SharedItemsRuntime.TryFind(gun, out var w) && w.Weapon.CurrentAmmo == 1, "recovered city ammo reset");
            Require(killed ? !SharedItemsRuntime.TryFind(hurt, out _) : SharedItemsRuntime.TryFind(hurt, out var e) && e.Enemy.SharedHP == 13, "recovered defender HP/death reset");
            Require(SharedItemsRuntime.TryFind(moved, out var item) && !item.transform.IsChildOf(f.City.transform), "detached city item was lost or reattached");
            foreach (ulong id in killed ? new[] { gun, moved } : new[] { gun, moved, hurt }) One(id);
            var state = SharedRunContext.State;
            Require(state.Items[used].Mode == 4 && state.TryEnemy(dead, out var tombstone) && tombstone.HP == 0,
                "city replay overwrote canonical tombstones");
            bool hasLive = state.TryEnemy(hurt, out var live);
            Require(state.Items[gun].WeaponAmmo == 1 && hasLive && live.HP == (killed ? 0 : 13),
                $"city replay canonical ammo={state.Items[gun].WeaponAmmo}, enemyPresent={hasLive}, HP={live.HP}, expectedHP={(killed ? 0 : 13)}");
            Require((w.transform.position + LobbyNetworkAvatar.Origin - state.Items[gun].Position).sqrMagnitude < .001f, "logical city item pose changed");
            Require(SharedRunContext.Membership.TryCopy(f.City.GetComponent<WorldCityRegion>().Index, new(), new(), out _), "recovered city membership incomplete");
        }
        private static void CheckBought(ulong id)
        { Require(SharedItemsRuntime.TryFind(id, out var item) && item.Weapon.CurrentAmmo == 1, "published purchase ammo lost"); One(id); }
        private static void One(ulong id) => Require(Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(i => i.Id == id) == 1, "duplicate city actor " + id);
        private static void Wound(SharedDepartureState state, ulong id, int hp)
        { foreach (var page in SharedWorldPage.All(state.Runner)) if (page.Object.HasStateAuthority && page.Enemies.TryGet(id, out var e)) { e.HP = hp; page.Enemies.Set(id, e); } }
        private static void Set(object target, string property, object value) => target.GetType().GetProperty(property).SetValue(target, value);
        private static void SetOrigin(FixCoordinate coordinate, float z)
        { typeof(FixCoordinate).GetField("_boardAddPos", Private).SetValue(coordinate, z); typeof(FixCoordinate).GetField("_playerAddPos", Private).SetValue(coordinate, z); }
        private static bool Valid(SharedDepartureState state) => state != null && state.Object != null && state.Object.IsValid;
        private static SharedDepartureState Replica(NetworkRunner runner) => Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static void SpawnAvatar(NetworkRunner runner) => runner.SetPlayerObject(runner.LocalPlayer, runner.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab")));
        private static async Task Start(NetworkRunner runner, string room, string user, int unique, bool create = true)
        {
            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = NetworkProjectConfig.Global.PrefabTable; config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            var result = await runner.StartGame(new StartGameArgs { GameMode = GameMode.Shared, Config = config, SessionName = room,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), AuthValues = new Photon.Realtime.AuthenticationValues(user),
                PlayerUniqueId = unique, EnableClientSessionCreation = create, CustomLobbyName = "deadboat-city-probe-v1", PlayerCount = 2, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(), ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>() });
            Require(result.Ok, "city connect " + result.ShutdownReason);
        }
        private static async Task Until(Func<bool> condition, string label)
        { double end = Time.realtimeSinceStartupAsDouble + 30; while (!condition()) { Require(Time.realtimeSinceStartupAsDouble < end, label); await Task.Delay(50); } }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

        private static void CheckBudget()
        {
            var budget = new WorldStreamingBudget();
            int[] counts = new int[3];
            for (int frame = 0; frame < 120; frame++)
            {
                int granted = 0;
                for (int n = 0; n < 3; n++)
                {
                    int lane = (n + frame / 3) % 3;
                    if (budget.TryAcquire(frame, lane)) { counts[lane]++; granted++; }
                    Require(!budget.TryAcquire(frame, lane), "duplicate audit in one frame");
                }
                Require(granted == 1, "automatic budget exceeded or starved");
            }
            Require(counts.All(n => n == 40) && !budget.TryAcquire(-1, 0) && !budget.TryAcquire(120, 3), "lane fairness/invalid input");
            Require(new WorldStreamingBudget().TryAcquire(0, 0), "fresh run inherited old frame budget");
            Debug.Log("[Streaming budget] PASS: 120 frames, one audit each, 40 grants per lane independent of caller order; duplicate/invalid denied, fresh context clean.");
        }
        private static async Task CheckCityQueue(CitySpawner owner)
        {
            var source = new GameObject("Empty queue fixture prefab"); source.SetActive(false);
            var first = Object.Instantiate(source, owner.transform); first.transform.position = new Vector3(0, 0, 30000) - LobbyNetworkAvatar.Origin;
            var second = Object.Instantiate(source, owner.transform); second.transform.position = new Vector3(0, 0, 40000) - LobbyNetworkAvatar.Origin;
            var list = new List<GameObject> { first, second };
            var stream = new WorldCityStreaming(owner);
            var shop = first.AddComponent<StorePoint>(); shop.enabled = false;
            try
            {
                foreach (var root in list)
                {
                    var region = root.AddComponent<WorldCityRegion>();
                    region.Initialize(owner, source, (root.transform.position + LobbyNetworkAvatar.Origin).z);
                    var start = (System.Collections.IEnumerator)typeof(WorldCityRegion).GetMethod("Start", Private).Invoke(region, null);
                    while (start.MoveNext()) { }
                }
                typeof(StorePoint).GetField("rewardPending", Private).SetValue(shop, true);
                Require(!stream.Audit(list, new[] { 0f }, 0) && stream.LastRefusal == "purchase in progress", "pending shop guard");
                Require(stream.Audit(list, new[] { 0f }, 0) && list.Count == 1 && list[0] == first, "blocked first city starved second");
                await Task.Delay(100);
                Require(second == null && stream.SleepingCount == 1, "second city not removed");
                Require(stream.Audit(list, new[] { 40000f }, 0) && list.Count == 2, "queued city not restored");
                Debug.Log("[City queue] PASS: pending purchase keeps first city alive; next audit suspends second, then restores it.");
            }
            finally
            {
                typeof(StorePoint).GetField("rewardPending", Private).SetValue(shop, false);
                foreach (var root in list) if (root != null) Object.Destroy(root);
                Object.Destroy(source);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject staging;
            private readonly LocationBalanceProfile balance;
            public readonly CitySpawner Owner;
            public List<GameObject> Active => (List<GameObject>)typeof(CitySpawner).GetField("spawnedCities", Private).GetValue(Owner);
            public GameObject City => Active.Single();
            public WorldCityStreaming Stream => Owner.Streaming;
            public Fixture()
            {
                var parent = new GameObject("Inactive city recovery world"); parent.SetActive(false);
                Owner = parent.AddComponent<CitySpawner>(); Owner.enabled = false;
                var asset = AssetDatabase.LoadAssetAtPath<CitySpawner>("Assets/Prefabs/Locations/Spawner/CitySpawner.prefab");
                staging = new GameObject("Fresh city recovery template"); staging.SetActive(false);
                var template = Object.Instantiate(asset.cityPrefab, staging.transform);
                var plain = SharedItemCatalog.Load().entries.First(e => e.prefab != null && !e.prefab.Usable && e.prefab.GetComponent<AmmoItem>() == null && e.prefab.GetComponent<EggCollectibleItem>() == null).prefab;
                var gun = SharedItemCatalog.Load().entries.First(e => e.prefab != null && e.prefab.GetComponentInChildren<RangedWeaponController>(true) is RangedWeaponController w && new SerializedObject(w).FindProperty("maxAmmo").intValue >= 3).prefab;
                Object.Instantiate(plain, template.transform); Object.Instantiate(plain, template.transform); Object.Instantiate(gun, template.transform);
                var contentRoot = new GameObject("City recovery defenders"); contentRoot.transform.SetParent(template.transform, false);
                var content = contentRoot.AddComponent<LocationContentSpawner>(); balance = ScriptableObject.CreateInstance<LocationBalanceProfile>();
                typeof(LocationContentSpawner).GetField("_balanceProfile", Private).SetValue(content, balance);
                content._enemyPrefabs = new() { AssetDatabase.LoadAssetAtPath<ZombieController>("Assets/Prefabs/Enemy/Zombie.prefab") }; content.enemySpawnPoints = new();
                for (int i = 0; i < 2; i++) { var point = new GameObject("City defender " + i); point.transform.SetParent(contentRoot.transform, false); point.transform.localPosition = Vector3.right * (i * 4); content.enemySpawnPoints.Add(point.transform); }
                Owner.cityPrefab = template;
                typeof(CitySpawner).GetField("nextSpawnDistance", Private).SetValue(Owner, 10000f);
                typeof(CitySpawner).GetField("spawnCount", Private).SetValue(Owner, 1);
            }
            public void Spawn()
            {
                // Uses the ordinary CitySpawner replay route, not the former sleep ticket.
                typeof(CitySpawner).GetMethod("Update", Private).Invoke(Owner, null);
                foreach (var content in City.GetComponentsInChildren<LocationContentSpawner>(true)) typeof(LocationContentSpawner).GetMethod("Start", Private).Invoke(content, null);
                Quiet();
                var region = City.GetComponent<WorldCityRegion>();
                var start = (System.Collections.IEnumerator)typeof(WorldCityRegion).GetMethod("Start", Private).Invoke(region, null);
                while (start.MoveNext()) { } // child generation above replaces the ordinary one-frame wait
                Quiet();
            }
            public Dictionary<string, string> Stock()
            {
                var result = new Dictionary<string, string>();
                foreach (var store in City.GetComponentsInChildren<ItemStore>(true))
                {
                    var parent = (Transform)typeof(ItemStore).GetField("_pointsParent", Private).GetValue(store);
                    var points = parent.GetComponentsInChildren<StorePoint>(true);
                    var random = SharedRunContext.Random("store:" + WorldSpawnIdentity.StablePath(store.transform), 0);
                    var stock = (PickableItem[])typeof(ItemStore).GetMethod("SelectAssortment", Private).Invoke(store, new object[] { points, random });
                    for (int i = 0; i < points.Length; i++)
                    { var prefab = points[i].StaticItem ? (PickableItem)typeof(StorePoint).GetField("_itemPrefab", Private).GetValue(points[i]) : stock[i];
                        result.Add(WorldSpawnIdentity.StablePath(points[i].transform), prefab == null ? "empty" : prefab.name + ":" + (prefab.GetComponent<StoreItem>()?.CoinPrice ?? 0)); }
                }
                return result;
            }
            public void Quiet()
            {
                foreach (var city in Active) QuietTree(city);
                var items = new List<WorldSpawnIdentity>(); SharedItemsRuntime.CopyTo(items);
                foreach (var item in items) if (item != null) QuietTree(item.gameObject);
            }
            private static void QuietTree(GameObject root)
            {
                // Inactive fixtures do not receive Unity Awake. Supply only the
                // dormant corpse cache that Zombie.Awake normally initializes.
                foreach (var zombie in root.GetComponentsInChildren<ZombieController>(true))
                    if (typeof(ZombieController).GetField("pickable", Private).GetValue(zombie) == null)
                        typeof(ZombieController).GetField("pickable", Private).SetValue(zombie, zombie.GetComponentInChildren<PickableItem>(true));
                foreach (var b in root.GetComponentsInChildren<MonoBehaviour>(true)) if (b != null && b is not WorldSpawnIdentity) b.enabled = false;
                foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                foreach (var id in root.GetComponentsInChildren<WorldSpawnIdentity>(true)) id.PhysicsNearby = false;
            }
            public void ReconcileDrops()
            {
                var runtime = Owner.GetComponent<SharedItemsRuntime>() ?? Owner.gameObject.AddComponent<SharedItemsRuntime>(); runtime.enabled = false;
                typeof(SharedItemsRuntime).GetMethod("LateUpdate", Private).Invoke(runtime, null);
                var all = new List<WorldSpawnIdentity>(); SharedItemsRuntime.CopyTo(all);
                foreach (var id in all) if (id != null) QuietTree(id.gameObject);
            }
            public void RemoveReplicas()
            {
                // These identities belong exclusively to this empty test world.
                var all = new List<WorldSpawnIdentity>(); SharedItemsRuntime.CopyTo(all);
                var enemies = new List<WorldSpawnIdentity>();
                typeof(SharedItemsRuntime).GetMethod("CopyEnemiesTo", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { enemies });
                all.AddRange(enemies);
                foreach (var id in all) if (id != null)
                { typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private).SetValue(id, true); SharedItemsRuntime.Unregister(id); Object.Destroy(id.gameObject); }
            }
            public void Dispose() { Object.Destroy(Owner.gameObject); Object.Destroy(staging); Object.Destroy(balance); }
        }
    }
}
