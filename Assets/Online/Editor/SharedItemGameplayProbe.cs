using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
namespace DeadBoat.Online.Editor
{
    public static class SharedItemGameplayProbe
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
                var root = new GameObject("Item gameplay transport peer");
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
                await (Task)typeof(SharedGameplayProbe).GetMethod("CityItemsCase",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { state, Replica(peer) });
                await Proxies(state, Replica(peer));
                await SharedItemAmmoGameplayProbe.Check(state, Replica(peer));
                await SharedBoatReturnGameplayProbe.Check(state);
                await SharedRearFireGameplayProbe.Check(state);
                Result = "PASS: city pickup/drop/purchase, driver anchor, remote carry interpolation between audits, inventory hide/drop, boat attachment, item/ammo transfer/recovery, rewarded return, rear fire";
                Debug.Log("[Item gameplay probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Item gameplay probe] " + Result); }
            finally
            {
                if (peer != null) await peer.Shutdown();
                NetworkProjectConfig.Global.PeerMode = mode;
            }
        }
        private static async Task Proxies(SharedDepartureState state, SharedDepartureState remote)
        {
            var identity = UnityEngine.Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsSortMode.None)
                .First(i => i.Key.StartsWith("authored:") && i.Item != null && i.Enemy == null && state.Items.TryGet(i.Id, out var r) &&
                    r.Mode == 0 && i.Item.GetComponent<AmmoItem>() == null && i.Item.GetComponent<EggCollectibleItem>() == null);
            PlayerMovement.Instance.transform.position = identity.transform.position + Vector3.up;
            await Task.Delay(350);
            remote.RPC_ClaimItem(identity.Id);
            await Until(() => state.Items[identity.Id].Owner == remote.Runner.LocalPlayer, "remote claim");
            Vector3 position = identity.transform.position + LobbyNetworkAvatar.Origin + Vector3.right;
            remote.RPC_ItemState(identity.Id, 1, position, Quaternion.identity);
            await Until(() => Vector3.Distance(identity.transform.position + LobbyNetworkAvatar.Origin, position) < .05f, "remote carry visual");
            var runtime = UnityEngine.Object.FindAnyObjectByType<SharedItemsRuntime>();
            var next = typeof(SharedItemsRuntime).GetField("nextSample", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            await Task.Delay(200); // Ensure the visible proxy was classified by an audit.
            next.SetValue(runtime, Time.realtimeSinceStartupAsDouble + 5);
            try
            {
                position += Vector3.right;
                remote.RPC_ItemState(identity.Id, 1, position, Quaternion.identity);
                await Until(() => state.Items[identity.Id].Position == position, "new carry snapshot");
                await Until(() => Vector3.Distance(identity.transform.position + LobbyNetworkAvatar.Origin, position) < .05f,
                    "frame interpolation without full audit");
                Require(identity.GetComponent<Rigidbody>() == null || identity.GetComponent<Rigidbody>().isKinematic,
                    "remote carry has no local simulation");
                remote.RPC_ItemState(identity.Id, 2, position, Quaternion.identity);
                await Until(() => !identity.gameObject.activeSelf, "inventory mode invalidates frame proxy immediately");
            }
            finally { next.SetValue(runtime, 0d); }
            remote.RPC_ItemState(identity.Id, 0, position, Quaternion.identity);
            await Until(() => identity.gameObject.activeSelf && state.Items[identity.Id].Owner == PlayerRef.None, "drop restores world visual");
            Debug.Log("[Item proxy probe] PASS: frame interpolation, remote inventory hide and drop.");
            remote.RPC_ClaimItem(identity.Id);
            await Until(() => state.Items[identity.Id].Owner == remote.Runner.LocalPlayer, "claim for attachment");
            var boat = BoardController.Instance;
            PlayerMovement.Instance.transform.position = boat.GetComponentInChildren<DriverSeatTrigger>().driverSeatTransform.position;
            await Until(() => AvatarNear(state, remote.Runner.LocalPlayer, PlayerMovement.Instance.transform.position + LobbyNetworkAvatar.Origin), "carrier reaches boat");
            position = PlayerMovement.Instance.transform.position + LobbyNetworkAvatar.Origin + Vector3.right * 2;
            remote.RPC_ItemState(identity.Id, 3, position, Quaternion.identity);
            await Until(() => state.Items[identity.Id].Mode == 3 && identity.transform.parent == boat.transform &&
                Vector3.Distance(identity.transform.localPosition, state.Items[identity.Id].BoatLocalPosition) < .01f, "attach pose");
            var local = state.Items[identity.Id].BoatLocalPosition;
            typeof(SharedDepartureState).GetProperty("BoatDistance").SetValue(state, state.BoatDistance + 5);
            await Task.Delay(350);
            Require(identity.transform.parent == boat.transform &&
                Vector3.Distance(identity.transform.localPosition, local) < .01f,
                "attached item follows parent between audits: delta=" + Vector3.Distance(identity.transform.localPosition, local) +
                " mode=" + state.Items[identity.Id].Mode);
        }
        private static bool AvatarNear(SharedDepartureState state, PlayerRef player, Vector3 point)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == state.Runner && avatar.Object.StateAuthority == player &&
                    Vector3.Distance(avatar.LogicalPosition, point) < 1.5f) return true;
            return false;
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
