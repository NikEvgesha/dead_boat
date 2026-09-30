using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public enum LobbyOnlineMode { Unselected, Online, Offline }
    public enum LobbyConnectionFailure { None, NoNetwork, Capacity, Timeout, Other }

    public sealed class LobbyOnlineBootstrap : MonoBehaviour
    {
        private const string MatchmakingLobbyName = "river-public-lobby-v2";
        private const string DepartureLobbyName = "river-departures-v1";
        private const string ModePreferenceKey = "DeadBoat.OnlineMode.v1";

        [SerializeField] private NetworkObject avatarPrefab;
        [SerializeField, Min(1)] private int lobbyCapacity = 10;
        [SerializeField, Min(60)] private int idleDisconnectSeconds = 300;
        [SerializeField, Min(5)] private int idleWarningSeconds = 30;

        private NetworkRunner runner;
        private string status = "Waiting for player";
        private bool leaving;
        private bool connectedOnce;
        private bool connecting;
        private bool stopping;
        private bool reconnectRequested;
        private string lastFailure;
        private int connectionRevision;
        private LobbyOnlineMode mode;
        private LobbyConnectionFailure failure;
        private DateTime lastActivityUtc;
        private bool idleDisconnected;
        private bool startingSoloRun;
        private bool departureTransition;
        private bool browsingDepartures;
        private bool inDepartureRoom;
        private LobbySessionBrowser departureBrowser;
        private string departureSessionName;
        private int departureLevelId = -1;
        private int departureTargetPlayers;
#if UNITY_EDITOR || DEADBOAT_ONLINE_DIAGNOSTICS
        private bool diagnosticsLogged;
        private GUIStyle diagnosticsLabelStyle;
        private GUIStyle diagnosticsButtonStyle;
#endif

        public string Status => status;
        public string LastFailure => lastFailure;
        public LobbyOnlineMode Mode => mode;
        public LobbyConnectionFailure Failure => failure;
        public bool IsConnecting => mode == LobbyOnlineMode.Online && (connecting || stopping || departureTransition);
        public bool IsOnline => connectedOnce && runner != null && runner.IsConnectedToServer;
        public bool IsBrowsingDepartures => browsingDepartures;
        public bool IsInDepartureRoom => inDepartureRoom && IsOnline;
        public string DepartureSessionName => departureSessionName;
        public int DepartureLevelId => departureLevelId;
        public int DepartureTargetPlayers => departureTargetPlayers;
        public int DeparturePlayerCount => IsInDepartureRoom ? runner.ActivePlayers.Count() : 0;
        public bool IdleDisconnected => idleDisconnected;
        public int IdleSecondsRemaining => IsOnline || browsingDepartures
            ? Mathf.Max(0, idleDisconnectSeconds - (int)(DateTime.UtcNow - lastActivityUtc).TotalSeconds)
            : 0;
        public bool IdleWarning => (IsOnline || browsingDepartures) && IdleSecondsRemaining <= idleWarningSeconds;
        public string CurrentSessionName => runner != null && runner.SessionInfo.IsValid
            ? runner.SessionInfo.Name
            : null;

        private void Awake()
        {
            Debug.Log("[Lobby online] Bootstrap active.");
        }

        private IEnumerator Start()
        {
            while (!leaving && (PlayerMovement.Instance == null ||
                   LoadingManager.Instance == null ||
                   LoadingManager.Instance.CurrentLocation != Location.Lobby))
                yield return null;

            if (leaving)
                yield break;

            bool hasPreference = PlayerPrefs.HasKey(ModePreferenceKey);
            mode = hasPreference && PlayerPrefs.GetInt(ModePreferenceKey) == 1
                ? LobbyOnlineMode.Online
                : hasPreference ? LobbyOnlineMode.Offline : LobbyOnlineMode.Unselected;
            gameObject.AddComponent<LobbyOnlineModeUI>().Initialize(this, !hasPreference);
            if (mode == LobbyOnlineMode.Online)
                _ = ConnectAsync();
            else
                status = mode == LobbyOnlineMode.Offline ? "Solo mode: selected" : "Choose online or solo";
        }

        private void Update()
        {
            if (browsingDepartures && departureBrowser != null && departureBrowser.Disconnected)
            {
                browsingDepartures = false;
                departureBrowser = null;
                lastFailure = "Departure directory disconnected";
                failure = LobbyConnectionFailure.Other;
                status = "Solo mode: disconnected";
                _ = StopRunnerAsync();
            }

            if (IsOnline || browsingDepartures)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
                    Input.touchCount > 0 || Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f ||
                    Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f)
                    StayOnline();

                // Ads and pause overlays must not consume the idle allowance.
                if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                    StayOnline();
                else if (IdleSecondsRemaining == 0)
                    _ = DisconnectForIdleAsync();
            }

            if (connectedOnce && !leaving && mode == LobbyOnlineMode.Online &&
                runner != null && !runner.IsConnectedToServer)
            {
                connectedOnce = false;
                status = "Solo mode: disconnected";
                lastFailure = "Connection lost";
                failure = LobbyConnectionFailure.Other;
                browsingDepartures = false;
                inDepartureRoom = false;
                _ = StopRunnerAsync();
            }
        }

        public void SelectOnline()
        {
            if (leaving || startingSoloRun || departureTransition || browsingDepartures || inDepartureRoom)
                return;

            mode = LobbyOnlineMode.Online;
            idleDisconnected = false;
            StayOnline();
            PlayerPrefs.SetInt(ModePreferenceKey, 1);
            PlayerPrefs.Save();
            if (connecting || stopping)
                reconnectRequested = true;
            else
                RetryConnection();
        }

        public void SelectOffline()
        {
            if (leaving || startingSoloRun || departureTransition || browsingDepartures || inDepartureRoom)
                return;

            mode = LobbyOnlineMode.Offline;
            PlayerPrefs.SetInt(ModePreferenceKey, 0);
            PlayerPrefs.Save();
            ++connectionRevision;
            reconnectRequested = false;
            connectedOnce = false;
            browsingDepartures = false;
            inDepartureRoom = false;
            departureBrowser = null;
            idleDisconnected = false;
            failure = LobbyConnectionFailure.None;
            lastFailure = null;
            status = "Solo mode: selected";
            _ = StopRunnerAsync();
        }

        public void RetryConnection()
        {
            if (mode == LobbyOnlineMode.Online && !leaving && !startingSoloRun &&
                !connecting && !stopping && runner == null)
                _ = ConnectAsync();
        }

        // The selected mode is kept: returning to the lobby may reconnect automatically.
        public async Task DisconnectForSoloRunAsync()
        {
            startingSoloRun = true;
            ++connectionRevision;
            reconnectRequested = false;
            connectedOnce = false;
            browsingDepartures = false;
            inDepartureRoom = false;
            status = "Solo run";
            var modeUI = GetComponent<LobbyOnlineModeUI>();
            if (modeUI != null)
                Destroy(modeUI);
            await StopRunnerAsync();
        }

        public List<DepartureListing> GetAvailableDepartures()
        {
            var listings = new List<DepartureListing>();
            if (!browsingDepartures || departureBrowser == null)
                return listings;

            foreach (var session in departureBrowser.LatestSessions)
                if (DepartureListing.TryFromSession(session, out var listing))
                    listings.Add(listing);
            return listings;
        }

        public async Task<bool> OpenDepartureBrowserAsync()
        {
            if (leaving || startingSoloRun || departureTransition || connecting ||
                mode != LobbyOnlineMode.Online || browsingDepartures || inDepartureRoom)
                return false;

            departureTransition = true;
            int revision = ++connectionRevision;
            connectedOnce = false;
            status = "Finding departures";
            try
            {
                await StopRunnerAsync();
                if (leaving || revision != connectionRevision || mode != LobbyOnlineMode.Online)
                    return false;

                departureBrowser = CreateRunner();
                var joinTask = runner.JoinSessionLobby(SessionLobby.Custom, DepartureLobbyName);
                if (await Task.WhenAny(joinTask, Task.Delay(TimeSpan.FromSeconds(12))) != joinTask)
                {
                    RecordTimeout(revision);
                    await StopRunnerAsync();
                    return false;
                }

                var result = await joinTask;
                if (leaving || revision != connectionRevision || mode != LobbyOnlineMode.Online)
                {
                    await StopRunnerAsync();
                    return false;
                }
                if (!result.Ok)
                {
                    lastFailure = result.ShutdownReason.ToString();
                    failure = ClassifyFailure(lastFailure, result.ErrorMessage);
                    status = "Solo mode";
                    await StopRunnerAsync();
                    return false;
                }

                browsingDepartures = true;
                StayOnline();
                status = "Departure browser";
                return true;
            }
            catch (Exception exception)
            {
                lastFailure = exception.GetType().Name;
                failure = ClassifyFailure(lastFailure, exception.Message);
                Debug.LogWarning($"[Lobby online] Departure browser failed: {exception.Message}");
                await StopRunnerAsync();
                return false;
            }
            finally
            {
                departureTransition = false;
            }
        }

        public async Task<bool> JoinDepartureAsync(DepartureListing listing)
        {
            if (!browsingDepartures || departureTransition || listing == null || runner == null)
                return false;

            // The listing may have changed while the player was deciding.
            if (!GetAvailableDepartures().Exists(item => item.SessionName == listing.SessionName))
                return false;

            return await StartDepartureAsync(listing.SessionName, listing.LevelId,
                listing.TargetPlayers, false);
        }

        public async Task<bool> CreateDepartureAsync(int levelId, int targetPlayers)
        {
            if (!browsingDepartures || departureTransition || targetPlayers < 2 || targetPlayers > 4 ||
                LevelManager.Instance == null ||
                !LevelManager.Instance.TryGetLevel(levelId, out var level) || !level.Unlocked)
                return false;

            return await StartDepartureAsync("trip-" + Guid.NewGuid().ToString("N"),
                levelId, targetPlayers, true);
        }

        private async Task<bool> StartDepartureAsync(string sessionName, int levelId, int targetPlayers, bool create)
        {
            departureTransition = true;
            int revision = connectionRevision;
            browsingDepartures = false;
            status = create ? "Creating departure" : "Joining departure";
            try
            {
                var startTask = runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    CustomLobbyName = DepartureLobbyName,
                    SessionName = sessionName,
                    EnableClientSessionCreation = create,
                    PlayerCount = targetPlayers,
                    SessionProperties = create ? new Dictionary<string, SessionProperty>
                    {
                        { "d", 1 }, { "l", levelId }, { "t", targetPlayers }
                    } : null,
                    IsOpen = true,
                    IsVisible = true,
                    SceneManager = runner.GetComponent<LobbySceneManager>(),
                    ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>()
                });
                if (await Task.WhenAny(startTask, Task.Delay(TimeSpan.FromSeconds(20))) != startTask)
                {
                    RecordTimeout(revision);
                    await StopRunnerAsync();
                    return false;
                }

                var result = await startTask;
                if (leaving || revision != connectionRevision || mode != LobbyOnlineMode.Online)
                {
                    await StopRunnerAsync();
                    return false;
                }
                if (!result.Ok)
                {
                    lastFailure = result.ShutdownReason.ToString();
                    failure = ClassifyFailure(lastFailure, result.ErrorMessage);
                    status = "Solo mode";
                    await StopRunnerAsync();
                    return false;
                }

                inDepartureRoom = true;
                connectedOnce = true;
                departureSessionName = sessionName;
                departureLevelId = levelId;
                departureTargetPlayers = targetPlayers;
                departureBrowser = null;
                lastFailure = null;
                failure = LobbyConnectionFailure.None;
                status = "Waiting for crew";
                StayOnline();
                return true;
            }
            catch (Exception exception)
            {
                lastFailure = exception.GetType().Name;
                failure = ClassifyFailure(lastFailure, exception.Message);
                Debug.LogWarning($"[Lobby online] Departure start failed: {exception.Message}");
                await StopRunnerAsync();
                return false;
            }
            finally
            {
                departureTransition = false;
            }
        }

        public async Task LeaveDepartureForSoloSelectionAsync()
        {
            if (departureTransition)
                return;

            ++connectionRevision;
            connectedOnce = false;
            browsingDepartures = false;
            inDepartureRoom = false;
            departureBrowser = null;
            status = "Solo map selection";
            await StopRunnerAsync();
        }

        public async Task ReturnToVisualLobbyAsync()
        {
            if (leaving || startingSoloRun || departureTransition)
                return;

            departureTransition = true;
            ++connectionRevision;
            connectedOnce = false;
            browsingDepartures = false;
            inDepartureRoom = false;
            departureBrowser = null;
            departureSessionName = null;
            departureLevelId = -1;
            try
            {
                await StopRunnerAsync();
            }
            finally
            {
                departureTransition = false;
            }

            if (mode == LobbyOnlineMode.Online && !leaving)
                await ConnectAsync();
        }

        private LobbySessionBrowser CreateRunner()
        {
            var runnerObject = new GameObject("Lobby Photon Runner");
            runner = runnerObject.AddComponent<NetworkRunner>();
            runnerObject.AddComponent<LobbySceneManager>();
            runnerObject.AddComponent<NetworkObjectProviderDefault>();
            runnerObject.AddComponent<LobbyAvatarSpawner>().AvatarPrefab = avatarPrefab;
            var browser = runnerObject.AddComponent<LobbySessionBrowser>();
            runner.AddCallbacks(browser);
            return browser;
        }

        public void StayOnline()
        {
            lastActivityUtc = DateTime.UtcNow;
        }

        private async Task DisconnectForIdleAsync()
        {
            if (idleDisconnected || (!IsOnline && !browsingDepartures) || leaving)
                return;

            idleDisconnected = true;
            ++connectionRevision;
            connectedOnce = false;
            browsingDepartures = false;
            inDepartureRoom = false;
            departureBrowser = null;
            status = "Solo mode: idle timeout";
            await StopRunnerAsync();
        }

        private async Task ConnectAsync()
        {
            if (leaving || startingSoloRun || mode != LobbyOnlineMode.Online ||
                connecting || stopping || runner != null)
                return;

            int attempt = ++connectionRevision;
            connecting = true;
            lastFailure = null;
            failure = LobbyConnectionFailure.None;
            try
            {
                if (avatarPrefab == null)
                {
                    status = "Solo mode";
                    lastFailure = "Avatar prefab missing";
                    failure = LobbyConnectionFailure.Other;
                    Debug.LogError("[Lobby online] Avatar prefab is missing.");
                    return;
                }

                for (int roomAttempt = 0; roomAttempt < 3; roomAttempt++)
                {
                    status = "Finding online lobby";
                    Debug.Log($"[Lobby online] Looking for a room with capacity {lobbyCapacity}; attempt={roomAttempt + 1}.");
                    var runnerObject = new GameObject("Lobby Photon Runner");
                    runner = runnerObject.AddComponent<NetworkRunner>();
                    var sceneManager = runnerObject.AddComponent<LobbySceneManager>();
                    var objectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>();
                    var spawner = runnerObject.AddComponent<LobbyAvatarSpawner>();
                    spawner.AvatarPrefab = avatarPrefab;
                    var browser = runnerObject.AddComponent<LobbySessionBrowser>();
                    runner.AddCallbacks(browser);

                    var joinTask = runner.JoinSessionLobby(SessionLobby.Custom, MatchmakingLobbyName);
                    if (await Task.WhenAny(joinTask, Task.Delay(TimeSpan.FromSeconds(12))) != joinTask)
                    {
                        RecordTimeout(attempt);
                        await StopRunnerAsync();
                        return;
                    }

                    var joinResult = await joinTask;
                    if (leaving || attempt != connectionRevision || mode != LobbyOnlineMode.Online)
                    {
                        await StopRunnerAsync();
                        return;
                    }
                    if (!joinResult.Ok)
                    {
                        status = "Solo mode";
                        lastFailure = joinResult.ShutdownReason.ToString();
                        failure = ClassifyFailure(lastFailure, joinResult.ErrorMessage);
                        await StopRunnerAsync();
                        return;
                    }

                    SessionInfo selected = null;
                    if (await Task.WhenAny(browser.FirstList, Task.Delay(TimeSpan.FromSeconds(2))) == browser.FirstList)
                        selected = SelectRoom(await browser.FirstList, lobbyCapacity);
                    else
                        Debug.LogWarning("[Lobby online] Room list was delayed; using Photon FillRoom matchmaking.");

                    if (leaving || attempt != connectionRevision || mode != LobbyOnlineMode.Online)
                    {
                        await StopRunnerAsync();
                        return;
                    }

                    status = "Connecting to lobby";
                    var startTask = runner.StartGame(new StartGameArgs
                    {
                        GameMode = GameMode.Shared,
                        CustomLobbyName = MatchmakingLobbyName,
                        SessionName = selected?.Name,
                        MatchmakingMode = Photon.Realtime.MatchmakingMode.FillRoom,
                        EnableClientSessionCreation = selected == null,
                        PlayerCount = lobbyCapacity,
                        SessionProperties = new Dictionary<string, SessionProperty>
                        {
                            { "cap", lobbyCapacity }
                        },
                        IsOpen = true,
                        IsVisible = true,
                        SceneManager = sceneManager,
                        ObjectProvider = objectProvider
                    });

                    if (await Task.WhenAny(startTask, Task.Delay(TimeSpan.FromSeconds(20))) != startTask)
                    {
                        RecordTimeout(attempt);
                        await StopRunnerAsync();
                        return;
                    }

                    var result = await startTask;
                    if (leaving || attempt != connectionRevision || mode != LobbyOnlineMode.Online)
                    {
                        await StopRunnerAsync();
                        return;
                    }

                    if (result.Ok)
                    {
                        status = "Online lobby";
                        connectedOnce = true;
                        lastFailure = null;
                        failure = LobbyConnectionFailure.None;
                        idleDisconnected = false;
                        StayOnline();
                        Debug.Log($"[Lobby online] Joined session={CurrentSessionName}; players={runner.ActivePlayers.Count()}.");
                        return;
                    }

                    lastFailure = result.ShutdownReason.ToString();
                    Debug.LogWarning($"[Lobby online] {result.ShutdownReason}: {result.ErrorMessage}");
                    bool retryRoomRace = selected != null && roomAttempt < 2 && IsRoomRace(lastFailure);
                    await StopRunnerAsync();
                    if (retryRoomRace)
                        continue;

                    status = "Solo mode";
                    failure = ClassifyFailure(lastFailure, result.ErrorMessage);
                    return;
                }
            }
            catch (Exception exception)
            {
                if (attempt == connectionRevision && mode == LobbyOnlineMode.Online)
                {
                    status = "Solo mode";
                    lastFailure = exception.GetType().Name;
                    failure = ClassifyFailure(lastFailure, exception.Message);
                }
                Debug.LogWarning($"[Lobby online] Connection failed: {exception.Message}");
                await StopRunnerAsync();
            }
            finally
            {
                connecting = false;
                TryPendingReconnect();
            }
        }

        private void RecordTimeout(int attempt)
        {
            if (attempt != connectionRevision || mode != LobbyOnlineMode.Online)
                return;

            status = "Solo mode: no connection";
            lastFailure = "Connection timed out";
            failure = Application.internetReachability == NetworkReachability.NotReachable
                ? LobbyConnectionFailure.NoNetwork : LobbyConnectionFailure.Timeout;
        }

        private static bool IsRoomRace(string reason)
        {
            string value = reason.ToLowerInvariant();
            return value.Contains("gameisfull") || value.Contains("gameclosed") ||
                   value.Contains("gamenotfound") || value.Contains("roomisfull") ||
                   value.Contains("roomclosed");
        }

        private static SessionInfo SelectRoom(List<SessionInfo> sessions, int capacity)
        {
            if (sessions == null)
                return null;

            int smallestPopulation = int.MaxValue;
            var best = new List<SessionInfo>();
            foreach (var session in sessions)
            {
                if (!session.IsOpen || !session.IsVisible || session.MaxPlayers != capacity ||
                    session.PlayerCount >= capacity || session.Properties == null ||
                    !session.Properties.TryGetValue("cap", out var property) ||
                    !property.IsInt || (int)property.PropertyValue != capacity)
                    continue;

                if (session.PlayerCount < smallestPopulation)
                {
                    smallestPopulation = session.PlayerCount;
                    best.Clear();
                }
                if (session.PlayerCount == smallestPopulation)
                    best.Add(session);
            }

            return best.Count == 0 ? null : best[UnityEngine.Random.Range(0, best.Count)];
        }

        private void TryPendingReconnect()
        {
            if (!reconnectRequested || leaving || startingSoloRun || mode != LobbyOnlineMode.Online ||
                connecting || stopping || departureTransition || browsingDepartures || inDepartureRoom || runner != null)
                return;

            reconnectRequested = false;
            _ = ConnectAsync();
        }

        private static LobbyConnectionFailure ClassifyFailure(string reason, string message)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return LobbyConnectionFailure.NoNetwork;

            string details = (reason + " " + message).ToLowerInvariant();
            if (details.Contains("maxccu") || details.Contains("ccu") ||
                details.Contains("full") || details.Contains("capacity"))
                return LobbyConnectionFailure.Capacity;
            if (details.Contains("timeout") || details.Contains("timed out"))
                return LobbyConnectionFailure.Timeout;
            return LobbyConnectionFailure.Other;
        }

        private async Task StopRunnerAsync()
        {
            var currentRunner = runner;
            runner = null;
            if (currentRunner == null)
                return;

            stopping = true;
            try
            {
                var shutdown = currentRunner.Shutdown();
                if (await Task.WhenAny(shutdown, Task.Delay(TimeSpan.FromSeconds(5))) == shutdown)
                    await shutdown;
                else
                    Debug.LogWarning("[Lobby online] Shutdown timed out; destroying runner.");
            }
            catch (Exception exception) { Debug.LogWarning($"[Lobby online] Shutdown: {exception.Message}"); }
            finally
            {
                stopping = false;
                Destroy(currentRunner.gameObject);
                TryPendingReconnect();
            }
        }

#if UNITY_EDITOR || DEADBOAT_ONLINE_DIAGNOSTICS
        private void OnGUI()
        {
            if (leaving || startingSoloRun)
                return;

            if (!diagnosticsLogged)
            {
                diagnosticsLogged = true;
                Debug.Log("[Lobby online] Diagnostics overlay active.");
            }

            if (diagnosticsLabelStyle == null)
            {
                diagnosticsLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    wordWrap = true
                };
                diagnosticsLabelStyle.normal.textColor = Color.white;
                diagnosticsButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            }

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = -10000;
            var panel = new Rect(16, Mathf.Max(16, Screen.height - 220),
                Mathf.Min(500, Screen.width - 32), 204);
            GUI.color = new Color(0.04f, 0.07f, 0.11f, 0.92f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(panel.x + 12, panel.y + 8, panel.width - 24, panel.height - 16));
            GUILayout.Label($"Photon lobby: {status}", diagnosticsLabelStyle);
            if (runner != null && runner.IsConnectedToServer && runner.SessionInfo.IsValid)
            {
                GUILayout.Label($"Region: {runner.SessionInfo.Region}", diagnosticsLabelStyle);
                GUILayout.Label($"Room: {runner.SessionInfo.Name}", diagnosticsLabelStyle);
                GUILayout.Label($"Players: {runner.ActivePlayers.Count()}/{lobbyCapacity}", diagnosticsLabelStyle);
            }
            if (!string.IsNullOrEmpty(lastFailure))
                GUILayout.Label($"Reason: {lastFailure}", diagnosticsLabelStyle);
            if (!connecting && !stopping && runner == null &&
                GUILayout.Button("Reconnect", diagnosticsButtonStyle, GUILayout.Height(36)))
                RetryConnection();
            GUILayout.EndArea();
            GUI.depth = previousDepth;
            GUI.color = previousColor;
        }
#endif

        private void OnDestroy()
        {
            leaving = true;
            ++connectionRevision;
            _ = StopRunnerAsync();
        }

    }
}
