using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using static DeadBoat.Online.Editor.SharedRecoveryProbe;

namespace DeadBoat.Online.Editor
{
    public static class RunEventCalendarProbe
    {
        private static readonly DateTimeOffset Start = DateTimeOffset.FromUnixTimeSeconds(0);
        private static readonly DateTimeOffset End = Start.AddHours(1);
        private static readonly RunEventContent Winter = RunEventCalendar.FirstWinterTheme;
        private static ScheduledRunEvent Window(DateTimeOffset? start, DateTimeOffset? end) =>
            new(3, Winter, "LevelEvent", start, end, crewReady: true);

        [MenuItem("Tools/New Year/Test event calendar")]
        public static void CheckRules()
        {
            var levels = AssetDatabase.LoadAssetAtPath<LevelList>("Assets/Scripts/Lobby/Levels.asset");
            Require(levels.Levels.Count >= 4 && levels.Levels[0].Scene == "LevelForest" && levels.Levels[1].Scene == "LevelDesert"
                && levels.Levels[2].Scene == "LevelWinter" && levels.Levels[3].Scene == "LevelEvent", "stable level IDs");
            var time = Start;
            var window = Window(Start, End);
            var later = new RunEventContent { EventId = Winter.EventId, ThemeRevision = Winter.ThemeRevision + 1 };
            var next = new ScheduledRunEvent(3, later, "FixtureNextTheme", End, End.AddHours(1), crewReady: true);
            var calendar = new RunEventCalendar(() => time, window, next);
            Require(!Window(null, null).IsOpen(Start) && !Window(Start, null).IsOpen(Start) && !Window(null, End).IsOpen(Start) &&
                !Window(End, Start).IsOpen(Start) && !Window(Start, Start).IsOpen(Start) &&
                !Window(Start.ToOffset(TimeSpan.FromHours(3)), End).IsOpen(Start), "invalid or missing UTC window");
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                foreach (string culture in new[] { "ru-RU", "en-US" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    foreach (int offset in new[] { -8, 0, 3, 9 })
                    {
                        time = Start.ToOffset(TimeSpan.FromHours(offset)).AddTicks(-1);
                        Require(!calendar.TryAdmit(3, out _), "before start");
                        time = time.AddTicks(1);
                        Require(calendar.IsOpenFor(3, Winter) && calendar.IsOpenFor(3, Winter), "inclusive start, repeated admission");
                        time = End.ToOffset(TimeSpan.FromHours(offset)).AddTicks(-1);
                        Require(calendar.IsOpenFor(3, Winter), "last tick");
                        time = time.AddTicks(1);
                        Require(!calendar.IsOpenFor(3, Winter) && calendar.IsOpenFor(3, later) &&
                            calendar.TryResolve(3, Winter, out var scene) && scene == "LevelEvent", "exclusive end, accepted theme");
                    }
                }
            }
            finally { CultureInfo.CurrentCulture = previousCulture; }
            Require(!new RunEventCalendar(() => Start, window, window).TryAdmit(3, out _), "overlap fails closed");
            time = Start.AddTicks(-1);
            var soloTheme = new ScheduledRunEvent(3, Winter, "LevelEvent", Start, End, crewReady: false);
            var timeline = new RunEventCalendar(() => time, next, soloTheme);
            Require(timeline.GetWindow(3, out var first) == EventWindow.Upcoming && first.Content.Matches(Winter),
                "nearest upcoming window in an unordered registry");
            time = Start;
            Require(timeline.IsOpenFor(3, Winter) && !timeline.IsOpenFor(3, Winter, forCrew: true) &&
                timeline.TryResolve(3, Winter, out _), "unreleased crew, accepted recovery still available");
            time = End;
            Require(timeline.IsOpenFor(3, later, forCrew: true) && !timeline.IsOpenFor(3, Winter), "new theme replaces old selection");
            for (int level = 0; level < 3; level++)
                Require(calendar.IsOpenFor(level, default) && !calendar.TryResolve(level, Winter, out _), "ordinary levels");
            RunEventContent legacy = default;
            Require(!calendar.TryResolve(3, legacy, out _) && calendar.TryResolveSaved(3, ref legacy, out _) &&
                legacy.Matches(Winter), "old saved winter run only");
            Require(!calendar.TryResolve(3, new RunEventContent { EventId = 99, ThemeRevision = 1 }, out _), "unsupported content");
            Debug.Log("[Event calendar] PASS UTC boundaries, cultures/offsets, invalid dates, overlap, old saves, stable maps, pinned theme.");
        }

        // Isolated RecoveryProtocolProbe scene, no game managers. Only these private rooms use the fixture clock.
        public static async Task CheckNetwork()
        {
            Require(Application.isPlaying && LoadingManager.Instance == null && !SharedRunContext.Active, "isolated Play Mode");
            var now = Start;
            var production = RunEventCalendar.ReplaceForEditorTest(new RunEventCalendar(() => now, Window(Start, End)));
            NetworkRunner first = null, second = null, returned = null, directory = null;
            try
            {
                string prefix = "calendar-" + Guid.NewGuid().ToString("N");
                string expiringRoom = prefix + "-expires";
                first = NewRunner();
                var args = Args(first, expiringRoom, 26201, true);
                args.IsVisible = true;
                args.CustomLobbyName = prefix;
                args.SessionProperties = DepartureListing.PropertiesFor(3, 2, Winter);
                Require((await first.StartGame(args)).Ok, "isolated visible event room");
                var waiting = Spawn(first);
                directory = NewRunner();
                var browser = directory.gameObject.AddComponent<LobbySessionBrowser>();
                directory.AddCallbacks(browser);
                Require((await directory.JoinSessionLobby(SessionLobby.Custom, prefix,
                    customAppSettings: SharedRunRecovery.Settings("eu"))).Ok, "isolated Photon directory");
                SessionInfo listing = null;
                await Until(() => (listing = browser.LatestSessions.FirstOrDefault(s => s.Name == expiringRoom)) != null &&
                    DepartureListing.TryFromSession(listing, out _), "real session properties visible in directory");
                Require(DepartureListing.TryFromSession(listing, out var cached) && cached.LevelId == 3 && cached.Content.Matches(Winter),
                    "directory shows selected winter content");
                waiting.LaunchNow();
                await Until(() => waiting.Phase == 1, "event countdown started");
                now = End;
                Require(!DepartureListing.TryFromSession(listing, out _) && !RunEventCalendar.Current.IsOpenFor(cached.LevelId, cached.Content),
                    "previously visible listing rejected before the next Photon directory callback");
                await Until(() => waiting.Phase == 4, "expired countdown canceled");
                Require(!waiting.Countdown.IsRunning && waiting.CrewCount == 0, "expired countdown cleared its roster");
                await Until(() => !first.SessionInfo.IsOpen && !first.SessionInfo.IsVisible, "Photon acknowledged closed admission");
                await first.Shutdown(); first = null;
                await directory.Shutdown(); directory = null;
                Debug.Log("[Event calendar] PASS stale directory listing, expiry during Photon countdown.");

                now = Start;
                string room = prefix + "-accepted";
                first = await Connect(room, 26201, true);
                var state = Spawn(first);
                SpawnAvatar(first);
                second = await Connect(room, 26202, false);
                SpawnAvatar(second);
                await Until(() => state.Phase == 2 && Replica(second)?.Phase == 2, "accepted before event end");
                var remote = Replica(second);
                now = End;
                state.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => state.Phase == 3 && remote.Phase == 3, "both ready after end");
                var ticket = JsonUtility.FromJson<SharedReturnTicket>(JsonUtility.ToJson(SharedReturnTicket.ForRun(state, "probe", 26201)));
                Require(ticket.Matches(remote) && ticket.EventContent.Matches(Winter) &&
                    ticket.LocationPoolRevision == LocationSpawnCollection.CurrentPoolRevision, "saved return identity");
                var fork = JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(SharedSoloSnapshot.Capture(remote)));
                Require(fork.EventContent.Matches(Winter) && fork.Level == ticket.Level && fork.Seed == ticket.Seed &&
                    fork.LocationPoolRevision == ticket.LocationPoolRevision && fork.Generator == ticket.Generator &&
                    RunEventCalendar.Current.TryResolveSaved(fork.Level, ref fork.EventContent, out var scene) && scene == "LevelEvent",
                    "offline fork keeps accepted generation and scene after end");
                ticket.EventContent.ThemeRevision++;
                Require(!ticket.Matches(remote), "mismatched return ticket");
                ticket.EventContent = Winter;
                await first.Shutdown(); first = null;
                await Until(() => remote.Object.HasStateAuthority && remote.Phase == 3 && second.SessionInfo.IsOpen, "takeover after end");
                returned = await Connect(room, 26201, false);
                await Until(() => Replica(returned)?.Phase == 3 && Replica(returned).HasOtherPlayingCrew(returned.LocalPlayer),
                    "former leader received live crew");
                var restored = Replica(returned);
                Require(ticket.Matches(restored) && restored.CanReturn(returned.LocalPlayer), "return after end");
                await second.Shutdown(); second = null;
                await Until(() => restored.Phase == 4, "last gameplay participant left");
                Require(!restored.CanReturn(returned.LocalPlayer), "return cannot revive finished room");
                Debug.Log("[Event calendar] PASS sealed crew, fork JSON, stable recovery identity, master takeover, cold return, last crew exit.");
            }
            finally
            {
                RunEventCalendar.ReplaceForEditorTest(production);
                foreach (var runner in new[] { directory, returned, second, first })
                    if (runner != null) await runner.Shutdown();
            }
        }

        public static async Task CheckGameplay()
        {
            await Until(() => LoadingManager.Instance?.CurrentLocation == Location.Lobby, "isolated lobby ready");
            var save = SaveManager.Instance.PersistentProvider as SharedRunPreviewSave;
            Require(save != null && !SharedRunContext.Active && !SoloRunBranch.Active, "isolated solo profile");
            var now = End;
            var production = RunEventCalendar.ReplaceForEditorTest(new RunEventCalendar(() => now, Window(Start, End)));
            try
            {
                var point = UnityEngine.Object.FindFirstObjectByType<GameStartPoint>();
                point.SendMessage("ShowCanvas");
                var picker = UnityEngine.Object.FindFirstObjectByType<LevelMapUI>();
                Require(picker != null && LevelManager.Instance.TryGetLevel(3, out _), "live level picker");
                var level = LevelManager.Instance.GetLevel(3);
                EventLevelCard EventRow() => picker.GetComponentInChildren<EventLevelCard>(true);
                await Until(() => EventRow() != null && !EventRow().gameObject.activeSelf, "closed event hidden in solo picker");
                var eventRow = EventRow().gameObject;
                save.SaveDistance(321);
                save.SaveLevelId(0);
                int before = save.LoadDistance();
                LoadingManager.Instance.LoadLocation(Location.Game, "LevelEvent", false);
                now = Start;
                await Until(() => eventRow.activeSelf, "event opens in existing picker");
                picker.SelectLevel(level);
                now = End; // The real click coroutine is waiting to display the loading screen.
                var transition = typeof(LevelMapUI).GetField("_transitionStarted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                await Until(() => !(bool)transition.GetValue(picker) && !eventRow.activeSelf, "stale solo click rejected");
                Require(LoadingManager.Instance.CurrentLocation == Location.Lobby && save.LoadDistance() == before, "old save preserved");

                var lobby = UnityEngine.Object.FindFirstObjectByType<LobbyOnlineBootstrap>();
                var fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var stop = new TaskCompletionSource<bool>();
                typeof(LobbyOnlineBootstrap).GetField("stopTask", fields).SetValue(lobby, stop.Task);
                now = Start;
                await Until(() => eventRow.activeSelf, "picker ready during pending shutdown");
                picker.SelectLevel(level);
                try
                {
                    await Until(() => (bool)typeof(LobbyOnlineBootstrap).GetField("startingSoloRun", fields).GetValue(lobby),
                        "solo click reached pending transport shutdown");
                    now = End;
                }
                finally { stop.TrySetResult(true); }
                await Until(() => !(bool)transition.GetValue(picker) && !eventRow.activeSelf, "expiry while disconnecting");
                Require(save.LoadDistance() == before && LoadingManager.Instance.CurrentLocation == Location.Lobby, "canceled solo preserved checkpoint");
                now = Start;
                await Until(() => eventRow.activeSelf, "reopened picker");
                EventLevelCardProbe.Click(eventRow.GetComponentInChildren<UnityEngine.UI.Button>(), 0);
                await Until(() => LoadingManager.Instance.CurrentRunEvent.Matches(Winter), "solo accepted");
                now = End;
                await WaitForEventBoat();
                Require(save.LoadRunEvent().Matches(Winter) && save.LoadDistance() < before, "fresh solo checkpoint stores admitted theme");
                Debug.Log("[Event calendar] PASS real picker closes/reopens, stale click preserves save, accepted solo loads winter after end.");

                var beforeBoat = BoardController.Instance;
                var fork = new SharedSoloSnapshot
                {
                    SourceRun = "calendar-fixture", Seed = 262, Level = 3, PlayerId = 26201,
                    LocationPoolRevision = LocationSpawnCollection.CurrentPoolRevision, EventContent = Winter,
                    Fuel = 50, Distance = 100, Personal = new SharedPersonalData { Health = 80 },
                    Items = Array.Empty<SoloWorldItem>(), Enemies = Array.Empty<SoloWorldEnemy>(), Bag = Array.Empty<SharedRecoveryItem>()
                };
                // Model a transport that has just died with a previously captured fallback. CheckNetwork exercises actual transport loss.
                var recovery = SharedRunRecovery.Instance;
                var privateField = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(SharedRunRecovery).GetField("fallback", privateField).SetValue(recovery, fork);
                typeof(SharedRunRecovery).GetField("lost", privateField).SetValue(recovery, true);
                await recovery.ContinueSoloAsync();
                await Until(() => BoardController.Instance != beforeBoat && SoloRunBranch.ReadyForWorldActions, "ContinueSolo loaded", 120);
                await WaitForEventBoat();
                Require(!SharedRunRecovery.HasPending && SoloRunBranch.Seed == 262 && save.LoadRunEvent().Matches(Winter), "fork persisted");

                // Clear in-memory branch and run the normal startup path with the persisted checkpoint.
                GameManager.Instance.isEndGame = true;
                PlayerManager.Instance.transform.SetParent(null);
                UnityEngine.Object.DontDestroyOnLoad(PlayerManager.Instance.gameObject);
                SoloRunBranch.Suspend();
                LoadingManager.Instance.LoadLocation(Location.Lobby, null, false);
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Lobby, "back to lobby with saved fork");
                LoadingManager.Instance.SendMessage("StartGame");
                await WaitForEventBoat();
                Require(SoloRunBranch.Active && SoloRunBranch.Seed == 262 && save.LoadLevelId() == 3 &&
                    save.LoadRunEvent().Matches(Winter) && !SharedRunRecovery.HasPending, "startup restores closed event fork");
                Debug.Log("[Event calendar] PASS ContinueSolo from simulated loss, in-memory branch cleared, normal startup recovered winter/seed without Photon.");
            }
            finally { RunEventCalendar.ReplaceForEditorTest(production); }
        }

        private static Task WaitForEventBoat() => Until(() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "LevelEvent" &&
            LoadingManager.Instance.CurrentLocation == Location.Game && BoardController.Instance != null && BoardController.Instance.StartGame,
            "winter scene and boat ready", 120);

        private static SharedDepartureState Spawn(NetworkRunner runner) => runner.Spawn(
            Resources.Load<NetworkObject>("Online/SharedDepartureState"),
            onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(3, 2, Winter))
            .GetComponent<SharedDepartureState>();

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
