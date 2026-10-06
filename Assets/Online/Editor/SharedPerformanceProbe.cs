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
        private static bool running;
        private static bool stressRequested;
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
            bool stress = stressRequested;
            stressRequested = false;
            var peers = new List<NetworkRunner>();
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            bool hadPreference = PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1");
            int preference = PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1");
            try
            {
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
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject("Performance transport peer " + i);
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    var runner = go.AddComponent<NetworkRunner>();
                    peers.Add(runner);
                    var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
                    config.PrefabTable = NetworkProjectConfig.Global.PrefabTable;
                    config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                    config.Network.ConnectionTimeout = 60;
                    var result = await runner.StartGame(new StartGameArgs {
                        GameMode = GameMode.Shared, Config = config, SessionName = room,
                        PlayerCount = 4, CustomLobbyName = "river-departures-v6",
                        EnableClientSessionCreation = false,
                        SceneManager = go.AddComponent<LobbySceneManager>(),
                        ObjectProvider = go.AddComponent<NetworkObjectProviderDefault>()
                    });
                    if (!result.Ok) throw new Exception("Peer join: " + result.ShutdownReason);
                    runner.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab"));
                }
                await Until(() => peers.All(p => Replica(p) != null && Replica(p).Phase == 2), "sealed crew");
                foreach (var peer in peers) Replica(peer).RPC_SceneReady();
                await Until(() => SharedRunContext.Playing && BoardController.Instance != null && BoardController.Instance.StartGame, "level loaded");
                foreach (var peer in peers) Replica(peer).RPC_BoatProfile(SharedBoatProfile.Local());
                await Until(() => SharedRunContext.State.BoatInitialized, "boat profiles");
                if (stress) await Stress(peers);
                else
                {
                    BenchmarkAvatars();
                    await Task.Delay(35000);
                }
                Debug.Log(stress
                    ? "[Performance probe] PASS: four Cloud connections, physics/corpses/route stress completed. Transport-only peers; not WebGL acceptance."
                    : "[Performance probe] PASS: four Cloud connections, real Forest scene, 35s capture. Transport-only peers; not WebGL acceptance.");
            }
            catch (Exception exception) { Debug.LogWarning("[Performance probe] FAIL: " + exception); }
            finally
            {
                foreach (var peer in peers)
                {
                    if (peer == null) continue;
                    await peer.Shutdown();
                    if (peer != null) UnityEngine.Object.Destroy(peer.gameObject);
                }
                await SharedRunLifetime.LeaveAsync();
                SharedPerformanceTestSave.Restore();
                NetworkProjectConfig.Global.PeerMode = previousMode;
                if (hadPreference) PlayerPrefs.SetInt("DeadBoat.OnlineMode.v1", preference);
                else PlayerPrefs.DeleteKey("DeadBoat.OnlineMode.v1");
                running = false;
            }
        }

        private static async Task Stress(List<NetworkRunner> peers)
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
            try
            {
                // Inactive dummy has no Awake/Update and never replaces the game's singleton.
                lagging = new GameObject("Stress lagging player");
                lagging.SetActive(false);
                var lagPlayer = lagging.AddComponent<PlayerMovement>();
                lagging.transform.position = PlayerMovement.Instance.transform.position;
                localPlayerField.SetValue(lagAvatar, lagPlayer);
                monitor.BeginStage("baseline");
                await Task.Delay(22000);
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
                    if (i % 10 == 9) await Task.Delay(100);
                }
                var far = UnityEngine.Object.Instantiate(itemPrefab, origin + new Vector3(300, 5, 0), Quaternion.identity);
                spawned.Add(far.gameObject);
                var farIdentity = far.gameObject.AddComponent<WorldSpawnIdentity>();
                farIdentity.Key = "perf:far";
                ulong farId = farIdentity.Id;
                await Task.Delay(2000);
                Debug.Log("[Stress probe] far-item kinematic=" + far.GetComponent<Rigidbody>().isKinematic + " (expected true when no crew nearby)");
                if (!far.GetComponent<Rigidbody>().isKinematic) throw new Exception("Distant free item physics is still enabled");
                lagging.transform.position = far.transform.position + Vector3.up;
                await Task.Delay(1500);
                if (far.GetComponent<Rigidbody>().isKinematic) throw new Exception("Item did not wake near remote crew member");
                lagging.transform.position = origin;
                await Task.Delay(1500);
                if (!far.GetComponent<Rigidbody>().isKinematic) throw new Exception("Item did not sleep after remote crew moved away");
                Debug.Log("[Stress probe] PASS: far body sleeps, wakes near remote crew, sleeps after separation.");
                monitor.BeginStage("100-items");
                await Task.Delay(22000);

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
                await Task.Delay(1500); // Start initializes the ragdoll arrays.
                foreach (var enemy in enemies) if (enemy != null) SharedEnemiesRuntime.ApplyDamage(enemy, 50000);
                await Task.Delay(1500);
                int corpses = UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsSortMode.None)
                    .Count(i => i.Key != null && i.Key.StartsWith("corpse:"));
                if (corpses < 20) throw new Exception("Expected 20 actual ragdoll corpses, got " + corpses);
                Debug.Log("[Stress probe] generated actual corpses=" + corpses);
                monitor.BeginStage("100-items-20-corpses");
                await Task.Delay(22000);

                // Force progress for profiling; this is not a physical sailing test.
                typeof(SharedDepartureState).GetProperty("BoatDistance").SetValue(state, 5000f);
                PlayerMovement.Instance.transform.position = new Vector3(0, 3, 5000) - LobbyNetworkAvatar.Origin;
                monitor.BeginStage("route-5000-lagging");
                await Task.Delay(22000);
                float retained = state.WorldFront - state.WorldRear;
                int before = state.Items.Count;
                Debug.Log($"[Stress probe] separated route span={retained:F0} items={before}");
                lagging.transform.position = PlayerMovement.Instance.transform.position;
                monitor.BeginStage("route-caught-up");
                await Task.Delay(22000);
                if (state.WorldRear < 3900 || state.Items.ContainsKey(farId))
                    throw new Exception("Rear cleanup did not advance after crew caught up");
                Debug.Log($"[Stress probe] PASS: 100 physical items, 20 ragdoll corpses, split/caught-up 5000-unit route; rear={state.WorldRear:F0} items={state.Items.Count}; far-body policy logged separately.");
            }
            finally
            {
                if (lagAvatar != null) localPlayerField.SetValue(lagAvatar, oldPlayer);
                if (lagging != null) UnityEngine.Object.Destroy(lagging);
                foreach (var go in spawned) if (go != null) UnityEngine.Object.Destroy(go);
            }
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
