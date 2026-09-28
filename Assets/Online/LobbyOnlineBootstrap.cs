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
        private string status = "Ожидание игрока";
        private bool leaving;
        private bool connectedOnce;
        private bool connecting;
        private bool stopping;
        private string lastFailure;

        public string Status => status;
        public string LastFailure => lastFailure;
        public string CurrentSessionName => runner != null && runner.SessionInfo.IsValid
            ? runner.SessionInfo.Name
            : null;

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
                status = "Одиночный режим: связь потеряна";
                lastFailure = "Соединение потеряно";
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
                    status = "Одиночный режим";
                    lastFailure = "Отсутствует префаб аватара";
                    Debug.LogError("[Lobby online] Avatar prefab is missing.");
                    return;
                }

                status = "Подключение к лобби";
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
                    status = "Одиночный режим: нет связи";
                    lastFailure = "Тайм-аут подключения (20 с)";
                    await StopRunnerAsync();
                    return;
                }

                var result = await startTask;
                if (leaving)
                    return;

                status = result.Ok ? "Онлайн-лобби" : "Одиночный режим";
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
                status = "Одиночный режим";
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

            GUILayout.BeginArea(new Rect(16, 16, 390, 180), GUI.skin.box);
            GUILayout.Label($"Photon lobby: {status}");
            if (runner != null && runner.IsConnectedToServer && runner.SessionInfo.IsValid)
            {
                GUILayout.Label($"Регион: {runner.SessionInfo.Region}");
                GUILayout.Label($"Комната: {runner.SessionInfo.Name}");
                GUILayout.Label($"Игроки: {runner.ActivePlayers.Count()}/{lobbyCapacity}");
            }
            if (!string.IsNullOrEmpty(lastFailure))
                GUILayout.Label($"Причина: {lastFailure}");
            if (!connecting && !stopping && runner == null && GUILayout.Button("Повторить подключение"))
                RetryConnection();
            GUILayout.EndArea();
        }
#endif

        private void OnDestroy()
        {
            leaving = true;
            _ = StopRunnerAsync();
        }

    }
}
