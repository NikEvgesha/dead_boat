using System;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class LobbyIdleProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Result { get; private set; } = "not run";

        public static string Validate()
        {
            var timer = new LobbyIdleClock();
            Require(timer.Remaining(100, 300) == 300, "uninitialized allowance");
            timer.Touch(100);
            Require(timer.Remaining(370, 300) == 30 && timer.Remaining(399.9, 300) == 1 &&
                timer.Remaining(400, 300) == 0, "warning and expiration boundaries");
            for (int i = 1; i <= 10; i++) timer.Observe(100 + i * 200, true, false);
            Require(timer.Remaining(2100, 300) == 300, "held input renews allowance");
            timer.Observe(2400, false, true);
            timer.Observe(9000, false, false);
            Require(timer.Remaining(9000, 300) == 300, "first frame after suspended gap");
            Require(timer.Remaining(9299.9, 300) == 1 && timer.Remaining(9300, 300) == 0, "timeout resumes after suspension");
            timer.Observe(50, false, false);
            Require(timer.Remaining(50, 300) == 300, "clock reset does not trigger false expiry");
            return "PASS: monotonic idle allowance, warning/ceil boundary, held input, suspension/resume gap, clock reset";
        }

        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            NetworkRunner first = null, second = null;
            GameObject root = null;
            float scale = Time.timeScale;
            var mode = NetworkProjectConfig.Global.PeerMode;
            var avatarProperty = typeof(LobbyOnlineBootstrap).GetProperty("RecoveryAvatarPrefab", BindingFlags.Static | BindingFlags.NonPublic);
            var previousAvatar = avatarProperty.GetValue(null);
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active, "isolated empty scene required");
                Validate();
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                string room = "idle-probe-" + Guid.NewGuid().ToString("N");
                first = await Connect(room, 5701, true);
                second = await Connect(room, 5702, false);
                root = new GameObject("Idle probe disabled bootstrap");
                var owner = root.AddComponent<LobbyOnlineBootstrap>();
                owner.enabled = false; // No scene bootstrap, real input or automatic transitions in this fixture.
                Set(owner, "runner", first); Set(owner, "connectedOnce", true); Set(owner, "mode", LobbyOnlineMode.Online);
                var timer = (LobbyIdleClock)Get(owner, "idleClock");
                var ui = root.AddComponent<LobbyOnlineModeUI>(); ui.Initialize(owner, false);
                timer.Touch(Time.realtimeSinceStartupAsDouble - 280);
                Require(owner.IdleWarning, "warning begins");
                typeof(LobbyOnlineModeUI).GetMethod("Refresh", Private).Invoke(ui, null);
                var stay = (Button)typeof(LobbyOnlineModeUI).GetField("stayButton", Private).GetValue(ui);
                Require(stay.gameObject.activeSelf && stay.interactable, "stay button available");
                stay.onClick.Invoke();
                Require(!owner.IdleWarning && owner.IdleSecondsRemaining >= 299, "stay button renews allowance");
                timer.Touch(Time.realtimeSinceStartupAsDouble - 301);
                foreach (string flag in new[] { "connecting", "stopping", "departureTransition", "leaving", "startingSoloRun", "launchingDeparture" })
                {
                    Set(owner, flag, true);
                    await Disconnect(owner);
                    Require(first.IsConnectedToServer && !owner.IdleWarning, "protected " + flag);
                    Set(owner, flag, false);
                }
                SharedAdProtection.Begin();
                await Disconnect(owner);
                Require(first.IsConnectedToServer && !owner.IdleWarning, "ad excludes idle shutdown");
                SharedAdProtection.End();
                typeof(LobbyOnlineBootstrap).GetMethod("OnApplicationFocus", Private).Invoke(owner, new object[] { true });
                Require(owner.IdleSecondsRemaining >= 299, "foreground resume renews allowance");
                var state = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 4)).GetComponent<SharedDepartureState>();
                Set(owner, "inDepartureRoom", true); Set(owner, "departureState", state);
                state.LaunchNow();
                Require(state.Phase == 1, "countdown started");
                timer.Touch(Time.realtimeSinceStartupAsDouble - 301);
                await Disconnect(owner);
                Require(first.IsConnectedToServer && !owner.IdleWarning, "countdown protected");
                await Until(() => state.Phase == 2, "loading phase");
                await Disconnect(owner);
                Require(first.IsConnectedToServer && !owner.IdleWarning, "loading barrier protected");
                typeof(SharedDepartureState).GetProperty("Phase").SetValue(state, 4);
                Require(owner.IdleWarning, "aborted crew does not suppress idle forever");
                Set(owner, "inDepartureRoom", false); Set(owner, "departureState", null);
                Set(owner, "browsingDepartures", true);
                await Task.WhenAll(Disconnect(owner), Disconnect(owner));
                Require(owner.IdleDisconnected && !owner.IsOnline && !owner.IsBrowsingDepartures &&
                    Get(owner, "runner") == null && owner.Mode == LobbyOnlineMode.Online, "idle stops transport once, preserves preference");
                await Until(() => second.SessionInfo.PlayerCount == 1, "remote sees idle peer leave");
                typeof(LobbyOnlineModeUI).GetMethod("Refresh", Private).Invoke(ui, null);
                var retry = (Button)typeof(LobbyOnlineModeUI).GetField("retryButton", Private).GetValue(ui);
                Require(retry.gameObject.activeSelf && retry.interactable, "offline reconnect button available");
                // Use a hidden fixture lobby, not the public matchmaking pool.
                Set(owner, "avatarPrefab", AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab"));
                Require(await owner.JoinFriendLobbyAsync("idle-rejoin-" + Guid.NewGuid().ToString("N"), "eu", true, true), "reconnect after idle");
                Require(owner.IsOnline && !owner.IdleDisconnected && owner.IdleSecondsRemaining >= 299, "fresh online allowance");
                Result = "PASS: clock/UI stay, transition/ad/countdown/loading guards, focus resume, aborted crew eligibility, idempotent real shutdown, remote room leave, preserved mode, reconnect UI and hidden lobby reconnect. Two Cloud peers, no WebGL input test.";
                Debug.Log("[Lobby idle probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Lobby idle probe] " + Result); }
            finally
            {
                SharedAdProtection.End();
                if (root != null)
                {
                    var owner = root.GetComponent<LobbyOnlineBootstrap>();
                    if (owner != null) try { await (Task)typeof(LobbyOnlineBootstrap).GetMethod("StopRunnerAsync", Private).Invoke(owner, null); } catch (Exception) { }
                    UnityEngine.Object.Destroy(root);
                }
                if (first != null) try { await first.Shutdown(); } catch (Exception) { }
                if (second != null) try { await second.Shutdown(); } catch (Exception) { }
                avatarProperty.SetValue(null, previousAvatar);
                NetworkProjectConfig.Global.PeerMode = mode;
                EditorApplication.update -= clock;
                Time.timeScale = scale;
            }
        }
        private static async Task<NetworkRunner> Connect(string room, long id, bool create) =>
            await (Task<NetworkRunner>)typeof(SharedAdProtocolProbe).GetMethod("Connect", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { room, id, create });
        private static Task Disconnect(LobbyOnlineBootstrap owner) =>
            (Task)typeof(LobbyOnlineBootstrap).GetMethod("DisconnectForIdleAsync", Private).Invoke(owner, null);
        private static object Get(object owner, string field) => owner.GetType().GetField(field, Private).GetValue(owner);
        private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> condition, string message)
        {
            double end = Time.realtimeSinceStartupAsDouble + 25;
            while (!condition()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + message); await Task.Delay(50); }
        }
    }
}
