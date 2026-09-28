using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class LobbyOnlineBootstrap : MonoBehaviour
    {
        private const string MatchmakingLobbyName = "river-public-lobby-v2";

        [SerializeField] private NetworkObject avatarPrefab;
        [SerializeField, Min(1)] private int lobbyCapacity = 10;

        private NetworkRunner runner;
        private string status = "Waiting for player";
        private bool leaving;
        private bool connectedOnce;
        private bool connecting;
        private bool stopping;
        private string lastFailure;
#if UNITY_EDITOR || DEADBOAT_ONLINE_DIAGNOSTICS
        private bool diagnosticsLogged;
        private GUIStyle diagnosticsLabelStyle;
        private GUIStyle diagnosticsButtonStyle;
#endif

        public string Status => status;
        public string LastFailure => lastFailure;
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

            _ = ConnectAsync();
        }

        private void Update()
        {
            if (connectedOnce && !leaving && runner != null && !runner.IsConnectedToServer)
            {
                connectedOnce = false;
                status = "Solo mode: disconnected";
                lastFailure = "Connection lost";
                _ = StopRunnerAsync();
            }
        }

        public void RetryConnection()
        {
            if (!leaving && !connecting && !stopping && runner == null)
                _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (leaving || connecting || stopping || runner != null)
                return;

            connecting = true;
            lastFailure = null;
            try
            {
                if (avatarPrefab == null)
                {
                    status = "Solo mode";
                    lastFailure = "Avatar prefab missing";
                    Debug.LogError("[Lobby online] Avatar prefab is missing.");
                    return;
                }

                status = "Connecting to lobby";
                Debug.Log($"[Lobby online] Looking for a room with capacity {lobbyCapacity}.");
                var runnerObject = new GameObject("Lobby Photon Runner");
                runner = runnerObject.AddComponent<NetworkRunner>();
                var sceneManager = runnerObject.AddComponent<LobbySceneManager>();
                var objectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>();
                var spawner = runnerObject.AddComponent<LobbyAvatarSpawner>();
                spawner.AvatarPrefab = avatarPrefab;

                var startTask = runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    CustomLobbyName = MatchmakingLobbyName,
                    MatchmakingMode = Photon.Realtime.MatchmakingMode.SerialMatching,
                    EnableClientSessionCreation = true,
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
                    status = "Solo mode: no connection";
                    lastFailure = "Connection timed out (20 s)";
                    await StopRunnerAsync();
                    return;
                }

                var result = await startTask;
                if (leaving)
                    return;

                status = result.Ok ? "Online lobby" : "Solo mode";
                connectedOnce = result.Ok;
                Debug.Log($"[Lobby online] StartGame: {result.Ok}; {result.ShutdownReason}; session={CurrentSessionName}");
                if (!result.Ok)
                {
                    lastFailure = result.ShutdownReason.ToString();
                    Debug.LogWarning($"[Lobby online] {result.ShutdownReason}: {result.ErrorMessage}");
                    await StopRunnerAsync();
                }
            }
            catch (Exception exception)
            {
                status = "Solo mode";
                lastFailure = exception.GetType().Name;
                Debug.LogWarning($"[Lobby online] Connection failed: {exception.Message}");
                await StopRunnerAsync();
            }
            finally
            {
                connecting = false;
            }
        }

        private async Task StopRunnerAsync()
        {
            var currentRunner = runner;
            runner = null;
            if (currentRunner == null)
                return;

            stopping = true;
            try { await currentRunner.Shutdown(); }
            catch (Exception exception) { Debug.LogWarning($"[Lobby online] Shutdown: {exception.Message}"); }
            finally
            {
                stopping = false;
                Destroy(currentRunner.gameObject);
            }
        }

#if UNITY_EDITOR || DEADBOAT_ONLINE_DIAGNOSTICS
        private void OnGUI()
        {
            if (leaving)
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
            _ = StopRunnerAsync();
        }

    }
}
