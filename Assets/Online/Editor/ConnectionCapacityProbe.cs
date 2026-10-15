using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class ConnectionCapacityProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Result { get; private set; } = "not run";
        public static string Validate()
        {
            Require(LobbyConnectionMessages.Classify("MaxCcuReached", null, false) == LobbyConnectionFailure.Capacity, "CCU enum");
            Require(LobbyConnectionMessages.Classify("GameIsFull", null, false) == LobbyConnectionFailure.RoomFull, "room differs from global CCU");
            Require(LobbyConnectionMessages.Classify("", "room full", false) == LobbyConnectionFailure.RoomFull, "room text");
            Require(LobbyConnectionMessages.Classify("ConnectionTimeout", null, false) == LobbyConnectionFailure.Timeout, "timeout");
            Require(LobbyConnectionMessages.Classify("", "anything", true) == LobbyConnectionFailure.NoNetwork, "reachability");
            foreach (var value in new[] { LobbyConnectionFailure.Capacity, LobbyConnectionFailure.NoNetwork, LobbyConnectionFailure.Timeout })
            {
                Require(!LobbyConnectionMessages.CanFallbackAfterInvite(value), "bounded retry policy " + value);
                Require(!string.IsNullOrEmpty(LobbyConnectionMessages.Describe(value, false)) &&
                    !string.IsNullOrEmpty(LobbyConnectionMessages.Describe(value, true)), "localized failure");
            }
            Require(LobbyConnectionMessages.CanFallbackAfterInvite(LobbyConnectionFailure.RoomFull), "full room may fallback to public lobby");
            return "PASS: CCU/room/network/timeout distinctions, RU/EN messages, bounded invite retry policy";
        }
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            NetworkRunner host = null;
            GameObject root = null;
            var configMode = NetworkProjectConfig.Global.PeerMode;
            var avatarProperty = typeof(LobbyOnlineBootstrap).GetProperty("RecoveryAvatarPrefab", BindingFlags.Static | BindingFlags.NonPublic);
            var previousAvatar = avatarProperty.GetValue(null);
            float scale = Time.timeScale;
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active, "empty isolated scene");
                Validate();
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var hostRoot = new GameObject("Capacity one-slot hidden host");
                host = hostRoot.AddComponent<NetworkRunner>();
                var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig(); config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                string room = "capacity-probe-" + Guid.NewGuid().ToString("N");
                var result = await host.StartGame(new StartGameArgs {
                    GameMode = GameMode.Shared, Config = config, PlayerUniqueId = 7101,
                    CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), SessionName = room,
                    CustomLobbyName = "capacity-probe-v1", PlayerCount = 1, IsVisible = false,
                    SceneManager = hostRoot.AddComponent<LobbySceneManager>(), ObjectProvider = hostRoot.AddComponent<NetworkObjectProviderDefault>()
                });
                Require(result.Ok, "one-slot host");
                root = new GameObject("Capacity disabled bootstrap");
                var owner = root.AddComponent<LobbyOnlineBootstrap>(); owner.enabled = false;
                Set(owner, "mode", LobbyOnlineMode.Online);
                Set(owner, "avatarPrefab", AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab"));
                bool joined = await owner.JoinFriendLobbyAsync(room, "eu", false, false);
                Require(!joined && owner.LastFriendJoinFailure == LobbyConnectionFailure.RoomFull, "real full-room refusal classified");
                await Until(() => owner.IsOnline && !owner.IsConnecting, "public fallback after room-only refusal");
                await Stop(owner);
                // Global CCU cannot be forced without filling the account. Inject the same
                // service reason into the production failure handler, never the SDK.
                Set(owner, "reconnectRequested", true);
                typeof(LobbyOnlineBootstrap).GetMethod("RecordFriendFailure", Private).Invoke(owner, new object[] { "MaxCcuReached", null });
                Require(owner.Failure == LobbyConnectionFailure.Capacity && owner.LastFriendJoinFailure == LobbyConnectionFailure.Capacity &&
                    !(bool)Get(owner, "reconnectRequested"), "CCU records reason and clears queued retry");
                var ui = root.AddComponent<LobbyOnlineModeUI>(); ui.Initialize(owner, true);
                typeof(LobbyOnlineModeUI).GetMethod("Refresh", Private).Invoke(ui, null);
                var retry = (Button)typeof(LobbyOnlineModeUI).GetField("retryButton", Private).GetValue(ui);
                var offline = (Button)typeof(LobbyOnlineModeUI).GetField("offlineButton", Private).GetValue(ui);
                var text = (Text)typeof(LobbyOnlineModeUI).GetField("statusText", Private).GetValue(ui);
                Require(text.text.Contains("нет мест") && retry.interactable && retry.gameObject.activeSelf && offline.interactable,
                    "CCU visible, manual retry/solo available");
                var departure = root.AddComponent<LobbyDepartureUI>();
                typeof(LobbyDepartureUI).GetField("online", Private).SetValue(departure, owner);
                var departureMessage = (string)typeof(LobbyDepartureUI).GetMethod("FailureMessage", Private).Invoke(departure, new object[] { "generic" });
                Require(departureMessage.Contains("нет мест"), "departure failure does not hide CCU");
                var recovery = SharedRunRecovery.Instance;
                typeof(SharedRunRecovery).GetField("ticket", Private).SetValue(recovery, new SharedReturnTicket { Room = "capacity-fixture", PlayerId = 1 });
                typeof(SharedRunRecovery).GetMethod("HandleJoinRejected", Private).Invoke(recovery, new object[] { ShutdownReason.MaxCcuReached });
                Require(recovery.CanRetry && recovery.Message.Contains("нет мест"), "CCU does not invalidate live return ticket");
                SharedRunRecovery.Forget();
                typeof(LobbyOnlineBootstrap).GetMethod("ResetFriendJoinFailure", Private).Invoke(owner, null);
                Require(owner.LastFriendJoinFailure == LobbyConnectionFailure.None, "new invite clears stale transport error");
                Require(await owner.JoinFriendLobbyAsync("capacity-retry-" + Guid.NewGuid().ToString("N"), "eu", true, true), "manual retry after injected CCU");
                Require(owner.IsOnline && owner.Failure == LobbyConnectionFailure.None, "successful retry clears failure");
                Result = "PASS: real full-room refusal/public fallback, injected MaxCcuReached, no queued retry, mode/departure UI, retained recovery ticket, stale error reset and real hidden reconnect. No 100-client saturation or Mirra API test.";
                Debug.Log("[Capacity probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Capacity probe] " + Result); }
            finally
            {
                if (root != null)
                {
                    var owner = root.GetComponent<LobbyOnlineBootstrap>();
                    if (owner != null) try { await Stop(owner); } catch (Exception) { }
                    UnityEngine.Object.Destroy(root);
                }
                if (host != null) try { await host.Shutdown(); } catch (Exception) { }
                avatarProperty.SetValue(null, previousAvatar);
                NetworkProjectConfig.Global.PeerMode = configMode;
                EditorApplication.update -= clock; Time.timeScale = scale;
            }
        }
        private static Task Stop(LobbyOnlineBootstrap owner) => (Task)typeof(LobbyOnlineBootstrap).GetMethod("StopRunnerAsync", Private).Invoke(owner, null);
        private static object Get(object owner, string name) => owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> check, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 45;
            while (!check()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(100); }
        }
    }
}
