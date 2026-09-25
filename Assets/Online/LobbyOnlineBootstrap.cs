using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class LobbyOnlineBootstrap : MonoBehaviour
    {
        private const string MatchmakingLobbyName = "river-public-lobby-v1";

        [SerializeField] private NetworkObject avatarPrefab;
        [SerializeField, Min(1)] private int lobbyCapacity = 10;

        private NetworkRunner runner;
        private string status = "Ожидание игрока";
        private bool leaving;
        private bool connectedOnce;

        public string Status => status;
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
                _ = StopRunnerAsync();
            }
        }

        private async Task ConnectAsync()
        {
            if (avatarPrefab == null)
            {
                status = "Одиночный режим";
                Debug.LogError("[Lobby online] Avatar prefab is missing.");
                return;
            }

            status = "Подключение к лобби";
            Debug.Log($"[Lobby online] Looking for a room with capacity {lobbyCapacity}.");
            var runnerObject = new GameObject("Lobby Photon Runner");
            runner = runnerObject.AddComponent<NetworkRunner>();
            var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>();
            var spawner = runnerObject.AddComponent<LobbyAvatarSpawner>();
            spawner.AvatarPrefab = avatarPrefab;

            try
            {
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
                    Debug.LogWarning($"[Lobby online] {result.ShutdownReason}: {result.ErrorMessage}");
                    await StopRunnerAsync();
                }
            }
            catch (Exception exception)
            {
                status = "Одиночный режим";
                Debug.LogWarning($"[Lobby online] Connection failed: {exception.Message}");
                await StopRunnerAsync();
            }
        }

        private async Task StopRunnerAsync()
        {
            var currentRunner = runner;
            runner = null;
            if (currentRunner == null)
                return;

            try { await currentRunner.Shutdown(); }
            catch (Exception exception) { Debug.LogWarning($"[Lobby online] Shutdown: {exception.Message}"); }

            if (currentRunner != null)
                Destroy(currentRunner.gameObject);
        }

        private void OnDestroy()
        {
            leaving = true;
            _ = StopRunnerAsync();
        }

    }
}
