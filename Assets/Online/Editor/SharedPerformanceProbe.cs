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
        [MenuItem("Tools/Online/Profile Four Connections (Lobby Play Mode)")]
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true;
            var peers = new List<NetworkRunner>();
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            bool hadPreference = PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1");
            int preference = PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1");
            try
            {
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
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
                        PlayerCount = 4, CustomLobbyName = "river-departures-v5",
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
                BenchmarkAvatars();
                await Task.Delay(35000);
                Debug.Log("[Performance probe] PASS: four Cloud connections, real Forest scene, 35s capture. Transport-only peers; not WebGL acceptance.");
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
                NetworkProjectConfig.Global.PeerMode = previousMode;
                if (hadPreference) PlayerPrefs.SetInt("DeadBoat.OnlineMode.v1", preference);
                else PlayerPrefs.DeleteKey("DeadBoat.OnlineMode.v1");
                running = false;
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
