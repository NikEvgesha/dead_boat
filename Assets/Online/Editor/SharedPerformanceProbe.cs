using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DeadBoat.Online.Editor
{
    // Four real Cloud connections, ONE actual game scene. Transport peers do not
    // simulate three extra game scenes; this is not four-device acceptance.
    public static class SharedPerformanceProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private static bool stressRequested;
        private static bool adaptersRequested;
        private static bool longRequested;
        public static void RunLongRoute() { if (running) return; longRequested = true; RunStress(); }
        public static void RunRegionAdapters() { if (running) return; adaptersRequested = true; Run(); }
        [MenuItem("Tools/Online/Stress Physics And Route (Lobby Play Mode)")]
        public static void RunStress()
        {
            if (running || !EditorApplication.isPlaying) return;
            stressRequested = true;
            Run();
        }
        [MenuItem("Tools/Online/Profile Four Connections (Lobby Play Mode)")]
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true;
            Result = "running";
            bool stress = stressRequested;
            bool longRoute = longRequested; longRequested = false;
            bool adapters = adaptersRequested; adaptersRequested = false;
            stressRequested = false;
            var peers = new List<NetworkRunner>();
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            bool hadPreference = PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1");
            int preference = PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1");
            try
            {
                if (!(SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave))
                    throw new Exception("Prepare isolated save before Play");
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                // LoadingManager may reload Lobby after SDK initialization; do not
                // capture the bootstrap from the outgoing scene.
                await Until(() => LoadingManager.Instance != null &&
                    LoadingManager.Instance.CurrentLocation == Location.Lobby &&
                    UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>() != null, "lobby initialized");
                var lobby = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                if (lobby == null) throw new Exception("Start from Lobby");
                lobby.SelectOnline();
                await Until(() => lobby.IsOnline && !lobby.IsConnecting, "public lobby");
                if (!await lobby.CreateDepartureAsync(0, 4)) throw new Exception(lobby.LastFailure);
                string room = lobby.DepartureSessionName;
                var created = UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None)
                    .First(s => s.Object.HasStateAuthority && s.Phase < 2);
                typeof(SharedDepartureState).GetProperty("Seed").SetValue(created, 27182818);
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject("Performance transport peer " + i);
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    var runner = go.AddComponent<NetworkRunner>();
                    peers.Add(runner);
                    var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig();
                    config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                    var result = await runner.StartGame(new StartGameArgs {
                        GameMode = GameMode.Shared, Config = config, SessionName = room, PlayerUniqueId = 8900 + i,
                        CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"),
                        PlayerCount = 4, CustomLobbyName = LobbyOnlineBootstrap.DepartureDirectoryName,
                        EnableClientSessionCreation = false,
                        SceneManager = go.AddComponent<LobbySceneManager>(),
                        ObjectProvider = go.AddComponent<NetworkObjectProviderDefault>()
                    });
                    if (!result.Ok) throw new Exception("Peer join: " + result.ShutdownReason);
                    runner.SetPlayerObject(runner.LocalPlayer, runner.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab")));
                }
                await Until(() => peers.All(p => Replica(p) != null && Replica(p).Phase == 2), "sealed crew");
                foreach (var peer in peers) Replica(peer).RPC_SceneReady();
                await Until(() => SharedRunContext.Playing && BoardController.Instance != null && BoardController.Instance.StartGame, "level loaded");
                foreach (var peer in peers) Replica(peer).RPC_BoatProfile(SharedBoatProfile.Local());
                await Until(() => SharedRunContext.State.BoatInitialized, "boat profiles");
                if (adapters) await WorldCityStreamingProbe.RunGame();
                else if (stress) await Stress(peers, longRoute);
                else
                {
                    BenchmarkAvatars();
                    await Task.Delay(35000);
                }
                Result = "PASS: " + (adapters ? "city/independent streaming adapters" : stress ? "physics/corpses/route stress" : "four connections profile") + "; one game world";
                if (adapters) Debug.Log("[Region adapter probe] " + Result);
                Debug.Log(adapters ? "[Performance probe] PASS: city/independent adapters, four Cloud connections, one local world."
                    : stress
                    ? "[Performance probe] PASS: four Cloud connections, physics/corpses/route stress completed. Transport-only peers; not WebGL acceptance."
                    : "[Performance probe] PASS: four Cloud connections, real Forest scene, 35s capture. Transport-only peers; not WebGL acceptance.");
            }
            catch (Exception exception) { Result = "FAIL: " + exception; Debug.LogWarning("[Performance probe] " + Result); }
            finally
            {
                foreach (var peer in peers)
                {
                    if (peer == null) continue;
                    await peer.Shutdown();
                    if (peer != null) UnityEngine.Object.Destroy(peer.gameObject);
                }
                await SharedRunLifetime.LeaveAsync();
                // Keep the temporary save active until Edit Mode; never restore owner data in a live level.
                NetworkProjectConfig.Global.PeerMode = previousMode;
                if (hadPreference) PlayerPrefs.SetInt("DeadBoat.OnlineMode.v1", preference);
                else PlayerPrefs.DeleteKey("DeadBoat.OnlineMode.v1");
                running = false;
            }
        }

        private static async Task Stress(List<NetworkRunner> peers, bool longRoute)
        {
            var state = SharedRunContext.State;
            if (!state.Object.HasStateAuthority) throw new Exception("Stress fixture requires local master");
            var monitor = UnityEngine.Object.FindAnyObjectByType<SharedPerformanceMonitor>();
            var spawned = new List<GameObject>();
            GameObject lagging = null;
            var avatar = LobbyNetworkAvatar.All.GetEnumerator();
            LobbyNetworkAvatar lagAvatar = null;
            while (avatar.MoveNext()) if (avatar.Current.Runner == peers[2] && avatar.Current.Object.HasStateAuthority) lagAvatar = avatar.Current;
            avatar.Dispose();
            if (lagAvatar == null) throw new Exception("Lagging avatar missing");
            var localPlayerField = typeof(LobbyNetworkAvatar).GetField("localPlayer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object oldPlayer = localPlayerField.GetValue(lagAvatar);
            var protection = typeof(SharedAdProtection).GetField("until", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            double oldProtection = (double)protection.GetValue(null);
            protection.SetValue(null, Time.realtimeSinceStartupAsDouble + 600);
            stressMemoryLimit = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() + 1400L * 1048576;
            Debug.Log($"[Stress probe] fixture memory guard={stressMemoryLimit / 1048576} MB; local damage suppressed, AI still targets crew.");
            try
            {
                // Inactive dummy has no Awake/Update and never replaces the game's singleton.
                lagging = new GameObject("Stress lagging player");
                lagging.SetActive(false);
                var lagPlayer = lagging.AddComponent<PlayerMovement>();
                lagging.transform.position = PlayerMovement.Instance.transform.position;
                localPlayerField.SetValue(lagAvatar, lagPlayer);
                monitor.BeginStage("baseline");
                await StressDelay(22000);
                var itemPrefab = SharedItemCatalog.Load().entries.Select(e => e.prefab)
                    .FirstOrDefault(p => p != null && p.GetComponent<Rigidbody>() != null && p.GetComponent<FuelItem>() != null);
                if (itemPrefab == null) throw new Exception("Fuel item fixture prefab missing");
                Vector3 origin = PlayerMovement.Instance.transform.position;
                monitor.BeginStage("spawn-items");
                for (int i = 0; i < 100; i++)
                {
                    var item = UnityEngine.Object.Instantiate(itemPrefab,
                        origin + new Vector3(10 + (i % 10) * 1.5f, 6 + i / 10 * 0.15f, (i / 10) * 1.5f), Quaternion.identity);
                    spawned.Add(item.gameObject);
                    item.gameObject.AddComponent<WorldSpawnIdentity>().Key = "perf:item:" + i;
                    if (i % 10 == 9) await StressDelay(100);
                }
                var far = UnityEngine.Object.Instantiate(itemPrefab, origin + new Vector3(300, 5, 0), Quaternion.identity);
                spawned.Add(far.gameObject);
                var farIdentity = far.gameObject.AddComponent<WorldSpawnIdentity>();
                farIdentity.Key = "perf:far";
                ulong farId = farIdentity.Id;
                await StressDelay(2000);
                Debug.Log("[Stress probe] far-item kinematic=" + far.GetComponent<Rigidbody>().isKinematic + " (expected true when no crew nearby)");
                if (!far.GetComponent<Rigidbody>().isKinematic) throw new Exception("Distant free item physics is still enabled");
                lagging.transform.position = far.transform.position + Vector3.up;
                await StressDelay(1500);
                if (far.GetComponent<Rigidbody>().isKinematic) throw new Exception("Item did not wake near remote crew member");
                lagging.transform.position = origin;
                await StressDelay(1500);
                if (!far.GetComponent<Rigidbody>().isKinematic) throw new Exception("Item did not sleep after remote crew moved away");
                Debug.Log("[Stress probe] PASS: far body sleeps, wakes near remote crew, sleeps after separation.");
                monitor.BeginStage("100-items");
                await StressDelay(22000);

                var live = UnityEngine.Object.FindAnyObjectByType<ZombieController>();
                var enemyPrefab = live != null ? PrefabUtility.GetCorrespondingObjectFromSource(live) : null;
                enemyPrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Zombie.prefab")?.GetComponent<ZombieController>();
                if (enemyPrefab == null) throw new Exception("Zombie fixture prefab missing");
                var enemies = new List<ZombieController>();
                monitor.BeginStage("spawn-corpses");
                for (int i = 0; i < 20; i++)
                {
                    var enemy = UnityEngine.Object.Instantiate(enemyPrefab,
                        origin + new Vector3(-10 - i % 5 * 1.5f, 2, i / 5 * 1.5f), Quaternion.identity);
                    spawned.Add(enemy.gameObject);
                    enemy.InitializeLevel(1);
                    enemy.gameObject.AddComponent<WorldSpawnIdentity>().Key = "perf:enemy:" + i;
                    enemies.Add(enemy);
                }
                await StressDelay(1500); // Start initializes the ragdoll arrays.
                foreach (var enemy in enemies) if (enemy != null) SharedEnemiesRuntime.ApplyDamage(enemy, 50000);
                int createdCorpses = UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsSortMode.None)
                    .Count(i => i.Key != null && i.Key.StartsWith("corpse:"));
                RequireCreatedCorpses(createdCorpses);
                await StressDelay(1500);
                int corpses = UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsSortMode.None)
                    .Count(i => i.Key != null && i.Key.StartsWith("corpse:"));
                Debug.Log($"[Stress probe] created actual corpses={createdCorpses}; remaining after physics={corpses}");
                BenchmarkItems();
                monitor.BeginStage("100-items-20-corpses");
                await StressDelay(22000);

                // Force progress for profiling; this is not a physical sailing test.
                typeof(SharedDepartureState).GetProperty("BoatDistance").SetValue(state, 5000f);
                PlayerMovement.Instance.transform.position = new Vector3(0, 3, 5000) - LobbyNetworkAvatar.Origin;
                monitor.BeginStage("route-5000-lagging");
                await StressDelay(22000);
                float retained = state.WorldFront - state.WorldRear;
                int before = state.Items.Count;
                Debug.Log($"[Stress probe] separated route span={retained:F0} items={before}");
                StreamingBudget("split", true);
                await CheckAutomaticReturn();
                lagging.transform.position = PlayerMovement.Instance.transform.position;
                monitor.BeginStage("route-caught-up");
                await StressDelay(22000);
                if (state.WorldRear < 3900 || state.Items.ContainsKey(farId))
                    throw new Exception("Rear cleanup did not advance after crew caught up");
                StreamingBudget("caught-up", false);
                if (longRoute)
                {
                    foreach (int distance in new[] { 10000, 20000, 30000, 40000 })
                    {
                        typeof(SharedDepartureState).GetProperty("BoatDistance").SetValue(state, (float)distance);
                        PlayerMovement.Instance.transform.position = new Vector3(0, 3, distance) - LobbyNetworkAvatar.Origin;
                        lagging.transform.position = PlayerMovement.Instance.transform.position;
                        monitor.BeginStage("long-route-" + distance);
                        await StressDelay(15000);
                        StreamingBudget("long-" + distance, false);
                        int cityRoots = UnityEngine.Object.FindObjectsByType<WorldCityRegion>(FindObjectsSortMode.None).Length;
                        int pages = SharedWorldPage.All(state.Runner).Count();
                        int enemyRecords = SharedWorldPage.All(state.Runner).Sum(p => p.Enemies.Count);
                        Debug.Log($"[Long route] requested={distance} front={state.WorldFront:F0} rear={state.WorldRear:F0} cities={cityRoots} pages={pages} items={state.Items.Count} enemies={enemyRecords} independentSleeping={SharedRunContext.Independent.SleepingCount} membership={SharedRunContext.Membership.RegionCount}/{SharedRunContext.Membership.BindingCount} reservedMB={UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()/1048576.0:F1} monoMB={UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()/1048576.0:F1}");
                        if (state.WorldRear < distance - 1100) throw new Exception("Long-route rear did not catch up");
                    }
                    Debug.Log("[Long route] PASS: automatic schedulers, four Cloud connections, checkpoints10k/20k/30k/40k; memory guard retained; not physical sailing or WebGL acceptance.");
                }
                Debug.Log($"[Stress probe] PASS: 100 physical items, 20 ragdoll corpses, split/caught-up 5000-unit route; rear={state.WorldRear:F0} items={state.Items.Count}; far-body policy logged separately.");
            }
            finally
            {
                protection.SetValue(null, oldProtection);
                if (lagAvatar != null) localPlayerField.SetValue(lagAvatar, oldPlayer);
                if (lagging != null) UnityEngine.Object.Destroy(lagging);
                foreach (var go in spawned) if (go != null) UnityEngine.Object.Destroy(go);
            }
        }

        private static long stressMemoryLimit;
        private static async Task CheckAutomaticReturn()
        {
            var owner = UnityEngine.Object.FindObjectsByType<LocationSpawner>(FindObjectsSortMode.None)
                .First(s => s.Streaming != null && s.Streaming.SleepingCount > 0);
            const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var map = (System.Collections.IDictionary)typeof(WorldRegionStreaming).GetField("sleeping", fields).GetValue(owner.Streaming);
            object asleep = map[map.Keys.Cast<object>().First()];
            var ticket = (WorldRegionResidency.Ticket)asleep.GetType().GetField("Ticket").GetValue(asleep);
            var step = (LocationLayoutStep)typeof(WorldRegionResidency.Ticket).GetField("Step", fields).GetValue(ticket);
            var position = (Vector3)typeof(WorldRegionResidency.Ticket).GetField("Position", fields).GetValue(ticket);
            var items = (List<ulong>)typeof(WorldRegionResidency.Ticket).GetField("Items", fields).GetValue(ticket);
            var enemies = (List<ulong>)typeof(WorldRegionResidency.Ticket).GetField("Enemies", fields).GetValue(ticket);
            var state = SharedRunContext.State;
            var ammo = items.Where(id => state.Items.ContainsKey(id)).ToDictionary(id => id, id => state.Items[id]);
            var hp = enemies.Where(id => state.TryEnemy(id, out _)).ToDictionary(id => id, id => { state.TryEnemy(id, out var e); return e; });
            int before = owner.Streaming.Restored;
            Vector3 original = PlayerMovement.Instance.transform.position;
            try
            {
                // Interest is deliberately longitudinal. Stay off the loot/AI colliders;
                // this is an automatic interest test, not a physical sailing test.
                PlayerMovement.Instance.transform.position = position + new Vector3(300, 10, 0) - LobbyNetworkAvatar.Origin;
                await Until(() => owner.Streaming.Restored > before && owner.GetComponentsInChildren<LocationContentSpawner>()
                    .Any(r => r.SharedGenerationKey == "procedural:" + step.Index), "automatic region return");
                await Task.Delay(250); // Start/rehydration must finish before checking canonical health.
                foreach (var entry in ammo)
                    if (!state.Items.TryGet(entry.Key, out var live) || live.Mode != entry.Value.Mode || live.WeaponAmmo != entry.Value.WeaponAmmo)
                        throw new Exception("Automatic return changed item/ammo " + entry.Key);
                foreach (var entry in hp)
                    if (!state.TryEnemy(entry.Key, out var live) || live.HP != entry.Value.HP)
                        throw new Exception("Automatic return changed enemy health " + entry.Key);
                if (owner.GetComponentsInChildren<LocationContentSpawner>().Count(r => r.SharedGenerationKey == "procedural:" + step.Index) != 1)
                    throw new Exception("Automatic return duplicated location");
                Debug.Log($"[Streaming return] PASS: actual LateUpdate + moving crew avatar restored region={step.Index} items={ammo.Count} enemies={hp.Count}; canonical modes/ammo/HP unchanged; one root; four Cloud connections.");
            }
            finally { PlayerMovement.Instance.transform.position = original; }
        }
        private static void StreamingBudget(string stage, bool requireSuspension)
        {
            var spawners = UnityEngine.Object.FindObjectsByType<LocationSpawner>(FindObjectsSortMode.None);
            int suspended = spawners.Sum(s => s.Streaming?.Suspended ?? 0);
            int restored = spawners.Sum(s => s.Streaming?.Restored ?? 0);
            int sleeping = spawners.Sum(s => s.Streaming?.SleepingCount ?? 0);
            int regions = spawners.Sum(s => s.GetComponentsInChildren<LocationContentSpawner>(true).Length);
            int meshes = spawners.Sum(s => s.GetComponentsInChildren<MeshFilter>(true).Length);
            int refused = spawners.Sum(s => s.Streaming?.Refused ?? 0);
            if (requireSuspension && (suspended == 0 || sleeping == 0))
                throw new Exception("Automatic procedural streaming did not suspend any separated region");
            Debug.Log($"[Streaming budget] stage={stage} owners={spawners.Length} roots={regions} meshes={meshes} sleeping={sleeping} suspended={suspended} restored={restored} refused={refused} archiveRegions={SharedRunContext.Regions.RegionCount} archiveRecords={SharedRunContext.Regions.RecordCount} allocatedMB={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0:F1}");
            foreach (var spawner in spawners) if (spawner.Streaming?.LastRefusal != null)
                Debug.Log("[Streaming refusal] " + spawner.Streaming.LastRefusal);
        }
        private static async Task StressDelay(int milliseconds)
        {
            double end = Time.realtimeSinceStartupAsDouble + milliseconds / 1000d;
            do
            {
                long memory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                if (memory > stressMemoryLimit)
                    throw new Exception("Stress safety stop: allocated memory " + memory / 1048576 + " MB");
                await Task.Delay(Math.Min(250, milliseconds));
            } while (Time.realtimeSinceStartupAsDouble < end);
        }

        private static void RequireCreatedCorpses(int count)
        {
            if (count < 20) throw new Exception("Expected 20 created ragdolls, got " + count);
        }
        private static void BenchmarkItems()
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<SharedItemsRuntime>();
            var method = typeof(SharedItemsRuntime).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var next = typeof(SharedItemsRuntime).GetField("nextSample", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
            {
                next.SetValue(runtime, i % 6 == 0 ? 0d : Time.realtimeSinceStartupAsDouble + 10);
                method.Invoke(runtime, null);
            }
            timer.Stop();
            next.SetValue(runtime, 0d);
            Debug.Log($"[Performance probe] Item loop 200 calls / 34 samples: items={SharedRunContext.State.Items.Count} elapsedMs={timer.Elapsed.TotalMilliseconds:F3}");
        }

        private static void BenchmarkAvatars()
        {
            // Both loops inspect exactly the same valid spawned instances.
            int scanVisits = 0, registryVisits = 0;
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                foreach (var avatar in UnityEngine.Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
                    if (avatar.Object != null && avatar.Object.IsValid) scanVisits++;
            timer.Stop();
            double scanMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            for (int i = 0; i < 1000; i++) foreach (var avatar in LobbyNetworkAvatar.All) registryVisits++;
            timer.Stop();
            if (scanVisits != registryVisits || registryVisits < 4000)
                throw new Exception("Avatar registry mismatch: " + scanVisits + "/" + registryVisits);
            Debug.Log($"[Performance probe] Avatar lookup calls=1000 visits={registryVisits} sceneScanMs={scanMs:F3} registryMs={timer.Elapsed.TotalMilliseconds:F3}");
        }

        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Until(Func<bool> predicate, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 90;
            while (!predicate())
            {
                if (Time.realtimeSinceStartupAsDouble >= end) throw new Exception("Timeout: " + label);
                await Task.Delay(100);
            }
        }
    }
}
