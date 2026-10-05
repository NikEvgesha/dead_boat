using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // One real game scene plus a transport-only second peer. This exercises the
    // real loader and boss prefabs; it is not two-device WebGL acceptance.
    public static class SharedGameplayProbe
    {
        private static bool running;
        [MenuItem("Tools/Online/Test Shared Gameplay Integration (Lobby Play Mode)")]
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true;
            NetworkRunner peer = null;
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            bool hadPreference = PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1");
            int preference = PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1");
            try
            {
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                await Until(() => UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>() != null &&
                    LoadingManager.Instance != null && LoadingManager.Instance.CurrentLocation == Location.Lobby, "lobby ready");
                var lobby = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                lobby.SelectOnline();
                await Until(() => lobby.IsOnline && !lobby.IsConnecting, "visual lobby connection");
                if (!await lobby.CreateDepartureAsync(0, 2)) throw new Exception("Create departure: " + lobby.LastFailure);
                var go = new GameObject("Gameplay integration second peer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                peer = go.AddComponent<NetworkRunner>();
                var sceneManager = go.AddComponent<LobbySceneManager>();
                var provider = go.AddComponent<NetworkObjectProviderDefault>();
                var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
                config.PrefabTable = NetworkProjectConfig.Global.PrefabTable;
                config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var result = await peer.StartGame(new StartGameArgs {
                    GameMode = GameMode.Shared, Config = config, SessionName = lobby.DepartureSessionName,
                    PlayerCount = 2, CustomLobbyName = "river-departures-v5",
                    EnableClientSessionCreation = false, SceneManager = sceneManager, ObjectProvider = provider
                });
                if (!result.Ok) throw new Exception("Second peer: " + result.ShutdownReason);
                peer.Spawn(UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab"),
                    inputAuthority: peer.LocalPlayer);
                await Until(() => Replica(peer) != null && Replica(peer).Phase == 2, "sealed crew");
                Replica(peer).RPC_SceneReady();
                await Until(() => SharedRunContext.Playing && BoardController.Instance != null && BoardController.Instance.StartGame,
                    "real level ready");
                var state = SharedRunContext.State;
                Replica(peer).RPC_BoatProfile(SharedBoatProfile.Local());
                await Until(() => state.BoatInitialized && Camera.main != null && state.Items.Count > 0, "boat and pickups initialized");
                Debug.Log("[Gameplay probe] Level ready: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                    "; items=" + state.Items.Count + "; crew=" + state.CrewCount);

                var item = UnityEngine.Object.FindObjectsByType<PickableItem>(FindObjectsSortMode.None)
                    .FirstOrDefault(p => p.GetComponent<WorldSpawnIdentity>() != null && p.GetComponent<EnemyCore>() == null &&
                        p.GetComponent<AmmoItem>() == null && p.GetComponent<EggCollectibleItem>() == null);
                if (item == null) throw new Exception("No test pickup in real level");
                var identity = item.GetComponent<WorldSpawnIdentity>();
                PlayerMovement.Instance.transform.position = item.transform.position + Vector3.up;
                await Task.Delay(300);
                Inventory.Instance.AddItem(item);
                await Until(() => state.Items.TryGet(identity.Id, out var r) && r.Owner == state.Runner.LocalPlayer && r.Mode == 2,
                    "real inventory pickup");
                Debug.Log("[Gameplay probe] Real pickup reached local inventory and shared snapshot.");

                await AdsCase(state);

                // Fast-forward only this isolated preview to exercise the real end-of-route boss.
                Set(state, "BoatDistance", BoardController.Instance.SharedEndDistance);
                Set(state, "BoatFinished", (NetworkBool)true);
                await Until(() => UnityEngine.Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None)
                    .Any(e => e.GetComponent<WorldSpawnIdentity>()?.Key?.StartsWith("boss:") == true), "boss spawned");
                double end = Time.realtimeSinceStartupAsDouble + 35;
                while (!state.RunWon && Time.realtimeSinceStartupAsDouble < end)
                {
                    foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None))
                    {
                        if (enemy == null) continue;
                        var id = enemy.GetComponent<WorldSpawnIdentity>();
                        if (id == null || !id.Key.StartsWith("boss:") || enemy.IsDead ||
                            !state.TryEnemy(id.Id, out var r) || !r.Active) continue;
                        PlayerMovement.Instance.transform.position = enemy.transform.position + Vector3.up;
                        await Task.Delay(150);
                        if (id != null) state.RPC_EnemyDamage(id.Id, 5000);
                    }
                    await Task.Delay(250);
                }
                await Until(() => state.RunWon && Replica(peer).RunWon, "real shared boss victory");
                Debug.Log("[Gameplay probe] PASS: real scene load, crew barrier, boat initialization, inventory pickup, real boss and shared victory.");
            }
            catch (Exception e) { Debug.LogWarning("[Gameplay probe] FAIL: " + e); }
            finally
            {
                if (peer != null) await peer.Shutdown();
                NetworkProjectConfig.Global.PeerMode = previousMode;
                if (hadPreference) PlayerPrefs.SetInt("DeadBoat.OnlineMode.v1", preference);
                else PlayerPrefs.DeleteKey("DeadBoat.OnlineMode.v1");
                running = false;
            }
        }
        private static async Task AdsCase(SharedDepartureState state)
        {
            var manager = AdsManager.Instance;
            var field = typeof(AdsManager).GetField("adsProviders",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var original = field.GetValue(manager);
            var go = new GameObject("Fake ads callback probe");
            var provider = go.AddComponent<SharedProbeAdsProvider>();
            try
            {
                field.SetValue(manager, new System.Collections.Generic.List<AdsProvider> { provider });
                var stats = PlayerStatsManager.Instance;
                stats.TakeDamage(100000);
                var ui = UnityEngine.Object.FindAnyObjectByType<EndGameUIManager>();
                if (stats.Health != 0 || ui.GetState() != EndGameState.Faint) throw new Exception("Personal death state");
                typeof(EndGameUIManager).GetMethod("ShowAdAndRevive",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(ui, null);
                if (!SharedAdProtection.IsShowing || !SharedLocalGameplay.Blocked || provider.requests != 1)
                    throw new Exception("Ad protection/input gate");
                await Task.Delay(300);
                if (Time.timeScale != 1 || state.Phase != 3) throw new Exception("Ad paused shared world");
                provider.CompleteTwice();
                if (stats.Health != stats.MaxHealth || ui.GetState() != EndGameState.None || !SharedAdProtection.Protected)
                    throw new Exception("Revival or post-ad protection");
                await Task.Delay(1200);
                if (SharedAdProtection.Protected) throw new Exception("Post-ad protection expires");
                Debug.Log("[Ads probe] PASS: personal death, ad input gate, shared world advances, duplicate callback, revival, one-second protection.");
            }
            finally
            {
                SharedAdProtection.End();
                field.SetValue(manager, original);
                UnityEngine.Object.Destroy(go);
            }
        }

        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static void Set(SharedDepartureState state, string name, object value) => typeof(SharedDepartureState)
            .GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .SetValue(state, value);
        private static async Task Until(Func<bool> check, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 60;
            while (!check())
            {
                if (Time.realtimeSinceStartupAsDouble >= end) throw new Exception("Timeout: " + label);
                EditorApplication.isPaused = false;
                await Task.Delay(100);
            }
        }
    }
}
