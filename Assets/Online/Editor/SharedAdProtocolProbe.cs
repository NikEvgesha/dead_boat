using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedAdProtocolProbe
    {
        public static string Result { get; private set; } = "not run";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            NetworkRunner first = null, second = null;
            try
            {
                Require(!SharedRunContext.Active && LoadingManager.Instance == null, "isolated empty scene required");
                await Lifecycle();
                string room = "ad-test-" + Guid.NewGuid().ToString("N");
                first = await Connect(room, 1001, true); SpawnAvatar(first);
                var state = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, o) => o.GetComponent<SharedDepartureState>().Initialize(0, 2))
                    .GetComponent<SharedDepartureState>();
                second = await Connect(room, 1002, false); SpawnAvatar(second);
                await Until(() => state.Phase == 2 && Replica(second)?.Phase == 2, "crew");
                var remote = Replica(second); var player = second.LocalPlayer;
                state.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => state.Phase == 3 && remote.Phase == 3, "playing");
                typeof(SharedDepartureState).GetProperty("Driver").SetValue(state, player);
                remote.RPC_AdBegin(901);
                await Until(() => remote.AdShowing(player), "confirmed ad protection");
                Require(state.Driver == PlayerRef.None && !state.CanAct(player) && state.AdProtected(player), "driver release and action gate");
                var expiry = state.Ads[player].Expires;
                remote.RPC_AdBegin(901); remote.RPC_AdBegin(902); remote.RPC_AdEnd(902);
                await Task.Delay(350);
                Require(state.Ads[player].Token == 901 && state.Ads[player].Expires.Equals(expiry) && state.AdShowing(player), "duplicate begin and stale close rejected");
                int template = SharedItemCatalog.Load().entries.First(e => e.prefab != null).id;
                state.Items.Add(9001, new SharedItemRecord { Template = template, Rotation = Quaternion.identity });
                remote.RPC_ClaimItem(9001);
                await Task.Delay(350);
                Require(state.Items[9001].Owner == PlayerRef.None, "watching player cannot claim");
                state.RPC_ClaimItem(9001);
                await Until(() => state.Items[9001].Owner == first.LocalPlayer, "other player still claims");
                await first.Shutdown(); first = null;
                await Until(() => remote.Object.HasStateAuthority, "takeover");
                Require(remote.AdShowing(player) && remote.Ads[player].Token == 901 && remote.Ads[player].Expires.Equals(expiry), "lease survives takeover without extension");
                remote.RPC_AdEnd(901);
                await Until(() => !remote.AdShowing(player), "close");
                Require(remote.AdProtected(player) && remote.CanAct(player), "one second grace allows play");
                expiry = remote.Ads[player].Expires;
                remote.RPC_AdEnd(901);
                Require(remote.Ads[player].Expires.Equals(expiry), "duplicate close cannot extend grace");
                await Task.Delay(1200);
                Require(!remote.AdProtected(player), "grace expires");
                remote.RPC_AdBegin(902);
                await Until(() => remote.AdShowing(player), "next ad");
                var lease = remote.Ads[player]; lease.Expires = TickTimer.CreateFromSeconds(second, .1f);
                remote.Ads.Set(player, lease);
                await Task.Delay(300);
                Require(!remote.AdProtected(player) && remote.CanAct(player), "missing close expires on network clock");
                remote.RPC_AdBegin(903); remote.RPC_AdEnd(902);
                await Until(() => remote.Ads[player].Token == 903, "new token");
                Require(remote.AdShowing(player), "late callback cannot close next ad");
                remote.RPC_AdEnd(903);
                Result = "PASS: callback lifecycle, confirmed lease, driver release, authority action gates, other player continues, takeover, grace, bounded expiry, stale tokens";
                Debug.Log("[Ad protocol probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Ad protocol probe] " + Result); }
            finally
            {
                foreach (var runner in new[] { first, second }) if (runner != null) try { await runner.Shutdown(); } catch (Exception) { }
            }
        }
        private static async Task Lifecycle()
        {
            var manager = AdsManager.Instance;
            var providers = typeof(AdsManager).GetField("adsProviders", Private);
            var original = providers.GetValue(manager);
            var go = new GameObject("Ad callback lifecycle probe");
            var provider = go.AddComponent<SharedProbeAdsProvider>();
            try
            {
                providers.SetValue(manager, new System.Collections.Generic.List<AdsProvider> { provider });
                int completed = 0; bool success = false;
                manager.ShowRewardedAd("probe", value => { completed++; success = value; });
                Require(provider.requests == 1 && SharedAdProtection.IsShowing, "local protection before provider");
                var stale = provider.callback;
                provider.CompleteTwice();
                Require(completed == 1 && success && !SharedAdProtection.IsShowing && SharedAdProtection.Protected, "exactly one success and grace");
                provider.throwOnShow = true;
                manager.ShowRewardedAd("probe", value => { completed++; success = value; });
                Require(completed == 2 && !success && !SharedAdProtection.IsShowing, "exception releases guard");
                provider.throwOnShow = false;
                manager.ShowInterstitialAd(value => { completed++; success = value; });
                stale(true);
                Require(completed == 2 && SharedAdProtection.IsShowing, "late callback does not finish new request");
                typeof(AdsManager).GetField("deadline", Private).SetValue(manager, Time.realtimeSinceStartupAsDouble - 1);
                await Until(() => completed == 3, "watchdog");
                Require(!success && !SharedAdProtection.IsShowing, "missing callback timeout");
                provider.ready = false;
                manager.ShowRewardedAd("probe", value => { completed++; success = value; });
                Require(completed == 4 && !success, "not ready completes false");
                await Task.Delay(1200);
                Require(!SharedAdProtection.Protected, "local grace expires");
            }
            finally
            {
                SharedAdProtection.End(); providers.SetValue(manager, original);
                UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(manager.gameObject);
            }
        }
        private static NetworkRunner NewRunner()
        {
            var root = new GameObject("Ad test runner");
            root.AddComponent<LobbySceneManager>(); root.AddComponent<NetworkObjectProviderDefault>();
            return root.AddComponent<NetworkRunner>();
        }
        private static StartGameArgs Args(NetworkRunner runner, string room, long id, bool create)
        {
            var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig();
            config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            return new StartGameArgs
            {
                GameMode = GameMode.Shared, Config = config, PlayerUniqueId = id,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), SessionName = room,
                CustomLobbyName = "deadboat-ad-probe-v1", PlayerCount = 2,
                EnableClientSessionCreation = create, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(), ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>()
            };
        }
        private static async Task<NetworkRunner> Connect(string room, long id, bool create)
        {
            var runner = NewRunner(); var result = await runner.StartGame(Args(runner, room, id, create));
            Require(result.Ok, "connect: " + result.ShutdownReason); return runner;
        }
        private static void SpawnAvatar(NetworkRunner runner)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
            runner.SetPlayerObject(runner.LocalPlayer, runner.Spawn(prefab));
        }
        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Until(Func<bool> test, string label)
        {
            double until = Time.realtimeSinceStartupAsDouble + 25;
            while (!test()) { Require(Time.realtimeSinceStartupAsDouble < until, "timeout: " + label); await Task.Delay(50); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
