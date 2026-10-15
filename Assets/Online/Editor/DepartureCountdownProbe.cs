using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace DeadBoat.Online.Editor
{
    public static class DepartureCountdownProbe
    {
        public static string Result { get; private set; } = "not run";
        private static readonly List<NetworkRunner> runners = new();
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            var previous = NetworkProjectConfig.Global.PeerMode;
            float scale = Time.timeScale;
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active, "empty isolated scene");
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                await AutoCase();
                await Clear();
                await ManualCase();
                Result = "PASS: automatic countdown, shared tick, leave cancellation/rejoin, duplicate/nonleader launch, batched membership guard, cleared roster, late join cancellation, four-peer refill, leader migration, same seed, fresh manual countdown, closed admission and loading leave abort/UI. Protocol only, no independent WebGL worlds.";
                Debug.Log("[Departure countdown probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Departure countdown probe] " + Result); }
            finally
            {
                await Clear();
                NetworkProjectConfig.Global.PeerMode = previous;
                EditorApplication.update -= clock;
                Time.timeScale = scale;
            }
        }
        private static async Task AutoCase()
        {
            string room = "countdown-auto-" + Guid.NewGuid().ToString("N");
            var a = await Connect(room, 6101, true);
            var state = SpawnState(a, 2);
            var b = await Connect(room, 6102, false);
            await Until(() => state.Phase == 1 && Replica(b)?.Phase == 1, "automatic countdown");
            var remote = Replica(b);
            Require(state.CrewCount == 2 && state.Includes(a.LocalPlayer) && state.Includes(b.LocalPlayer), "actual auto roster");
            Require(state.Countdown.TargetTick == remote.Countdown.TargetTick, "one network deadline");
            int? oldTick = state.Countdown.TargetTick;
            await Task.Delay(500);
            await b.Shutdown();
            await Until(() => state.Phase == 0, "leave cancels countdown");
            Require(!state.Countdown.IsRunning && state.CrewCount == 0 && state.Crew.All(p => p == PlayerRef.None), "cancel clears deadline and roster");
            await Task.Delay(600);
            Require(state.Phase == 0, "no solo launch after leave");
            var replacement = await Connect(room, 6103, false);
            await Until(() => state.Phase == 1 && Replica(replacement)?.Phase == 1, "refill restarts countdown");
            Require(state.Countdown.TargetTick > oldTick && state.CrewCount == 2 && state.Includes(replacement.LocalPlayer), "fresh deadline and roster");
            int? deadline = state.Countdown.TargetTick;
            state.LaunchNow(); state.LaunchNow(); Replica(replacement).LaunchNow();
            Require(state.Countdown.TargetTick == deadline, "duplicate/remote click does not reset timer");
            await Until(() => state.Phase == 2 && Replica(replacement)?.Phase == 2, "auto sealed");
            Require(state.CrewCount == 2 && state.ReadyMask == 0 && state.LoadingDeadline.IsRunning, "loading sealed roster");
            Debug.Log("[Departure countdown probe] auto leave/refill PASS");
        }
        private static async Task ManualCase()
        {
            string room = "countdown-manual-" + Guid.NewGuid().ToString("N");
            var a = await Connect(room, 6201, true);
            var state = SpawnState(a, 4);
            int seed = state.Seed;
            var b = await Connect(room, 6202, false);
            await Until(() => Replica(b) != null, "second state");
            Require(state.Phase == 0, "incomplete crew waits");
            Replica(b).LaunchNow();
            await Task.Delay(150);
            Require(state.Phase == 0, "nonleader cannot launch");
            state.LaunchNow();
            await Until(() => Replica(b).Phase == 1, "manual countdown");
            // Inject coalesced SDK membership events with the same final PlayerRef set.
            // This covers the event guard; actual transport join/leave is tested separately.
            ((IPlayerLeft)state).PlayerLeft(b.LocalPlayer);
            ((IPlayerJoined)state).PlayerJoined(b.LocalPlayer);
            await Until(() => state.Phase == 0, "coalesced events cancel unchanged slot set");
            Require(a.ActivePlayers.Count() == 2 && state.CrewCount == 0, "batched change clears stale roster");
            state.LaunchNow();
            await Until(() => Replica(b).Phase == 1, "manual restart after coalesced events");
            var c = await Connect(room, 6203, false);
            await Until(() => state.Phase == 0 && Replica(b).Phase == 0 && Replica(c)?.Phase == 0, "new arrival cancels old manual roster");
            Require(!state.Countdown.IsRunning && a.ActivePlayers.Count() == 3, "new member retained, old timer cleared");
            var d = await Connect(room, 6204, false);
            await Until(() => state.Phase == 1 && Replica(d)?.Phase == 1, "four-member auto refill");
            Require(state.CrewCount == 4 && state.Includes(c.LocalPlayer) && state.Includes(d.LocalPlayer), "all four included");
            await d.Shutdown();
            await Until(() => state.Phase == 0 && Replica(b).Phase == 0, "fourth exit cancels");
            await a.Shutdown();
            await Until(() => b.IsSharedModeMasterClient && Replica(b) != null && Replica(b).Object.HasStateAuthority && Replica(b).Phase == 0, "new leader");
            var leader = Replica(b);
            Require(leader.Seed == seed && leader.GenerationVersion == RunRandom.Version && leader.LevelId == 0, "migration preserves generation");
            leader.LaunchNow();
            await Until(() => leader.Phase == 1 && Replica(c).Phase == 1, "new leader manual launch");
            Require(leader.CrewCount == 2 && leader.Includes(b.LocalPlayer) && leader.Includes(c.LocalPlayer) &&
                leader.Countdown.TargetTick == Replica(c).Countdown.TargetTick, "surviving crew and shared deadline");
            await Until(() => leader.Phase == 2 && Replica(c).Phase == 2, "new leader sealed");
            // Closing is sent to Photon asynchronously. Check admission after propagation.
            await Task.Delay(300);
            var rejected = await TryConnect(room, 6205, false);
            Require(!rejected.Ok, "sealed room rejects random new entrant");
            await c.Shutdown();
            await Until(() => leader.Phase == 4, "departure aborts after loading member leave");
            leader.LaunchNow();
            Require(leader.Phase == 4, "aborted loading cannot relaunch stale roster");
            CheckAbortedUI(b, leader);
            Debug.Log("[Departure countdown probe] join/refill/master/closed/loading-abort/UI PASS");
        }
        private static void CheckAbortedUI(NetworkRunner runner, SharedDepartureState state)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var avatarProperty = typeof(LobbyOnlineBootstrap).GetProperty("RecoveryAvatarPrefab", BindingFlags.Static | BindingFlags.NonPublic);
            var previousAvatar = avatarProperty.GetValue(null);
            var root = new GameObject("Countdown UI fixture");
            try
            {
                var owner = root.AddComponent<LobbyOnlineBootstrap>(); owner.enabled = false;
                void Set(string name, object value) => typeof(LobbyOnlineBootstrap).GetField(name, flags).SetValue(owner, value);
                Set("runner", runner); Set("connectedOnce", true); Set("inDepartureRoom", true); Set("departureState", state);
                Require(owner.DepartureAborted && !owner.CanLaunchDepartureNow, "aborted launch gate");
                var ui = root.AddComponent<LobbyDepartureUI>(); ui.Open(null, owner);
                var button = (Button)typeof(LobbyDepartureUI).GetField("primary", flags).GetValue(ui);
                var status = (Text)typeof(LobbyDepartureUI).GetField("status", flags).GetValue(ui);
                Require(!button.interactable && status.text.Contains("отменено"), "aborted UI explains outcome and disables launch");
                Set("runner", null); // Fixture destruction must not disconnect the peer being inspected.
            }
            finally { UnityEngine.Object.Destroy(root); avatarProperty.SetValue(null, previousAvatar); }
        }

        private static SharedDepartureState SpawnState(NetworkRunner runner, int target) =>
            runner.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, target)).GetComponent<SharedDepartureState>();
        private static async Task<NetworkRunner> Connect(string room, long id, bool create)
        {
            var result = await TryConnect(room, id, create);
            Require(result.Ok, "connect " + result.ShutdownReason);
            return runners[runners.Count - 1];
        }
        private static async Task<StartGameResult> TryConnect(string room, long id, bool create)
        {
            var root = new GameObject("Countdown isolated peer");
            var runner = root.AddComponent<NetworkRunner>(); runners.Add(runner);
            var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig();
            config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            return await runner.StartGame(new StartGameArgs {
                GameMode = GameMode.Shared, Config = config, PlayerUniqueId = id,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), SessionName = room,
                CustomLobbyName = "deadboat-countdown-probe-v1", PlayerCount = 4,
                EnableClientSessionCreation = create, IsVisible = false,
                SceneManager = root.AddComponent<LobbySceneManager>(), ObjectProvider = root.AddComponent<NetworkObjectProviderDefault>()
            });
        }
        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Clear()
        {
            foreach (var runner in runners.ToArray()) if (runner != null) try { await runner.Shutdown(); } catch (Exception) { }
            runners.Clear();
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> condition, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 30;
            while (!condition()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(50); }
        }
    }
}
