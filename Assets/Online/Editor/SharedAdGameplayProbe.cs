using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
namespace DeadBoat.Online.Editor
{
    public static class SharedAdGameplayProbe
    {
        public static string Result { get; private set; } = "not run";
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
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
                var root = new GameObject("Ad gameplay transport peer");
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
                await SharedGameplayProbe.CheckAds(state);
                Result = "PASS: real level, rewarded revival once, authority ACK before provider, shared clock, interstitial damage rejection, grace and expiry";
                Debug.Log("[Ad gameplay probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Ad gameplay probe] " + Result); }
            finally
            {
                if (peer != null) await peer.Shutdown();
                NetworkProjectConfig.Global.PeerMode = mode;
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
