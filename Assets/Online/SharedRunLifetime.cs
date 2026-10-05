using System.Threading.Tasks;
using System.Linq;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class SharedRunLifetime : MonoBehaviour
    {
        private static SharedRunLifetime instance;
        private NetworkRunner runner;
        private bool stopping;
        private GameLoader loader;
        private bool connectionLost;
        private bool sceneLoaded;
        private bool returningToLobby;
        private double lastFrameTime;
        private PauseManager waitingPause;
        private double nextReadyReport;

        private void ReleaseWaitingPause()
        {
            if (waitingPause != null) waitingPause.SetPause(false);
            waitingPause = null;
        }

        public void Initialize(NetworkRunner value)
        {
            instance = this;
            runner = value;
            lastFrameTime = Time.realtimeSinceStartupAsDouble;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(value.gameObject);
            loader = GameLoader.Instance;
            if (loader != null)
            {
                loader.OnLoadFailed += OnLoadFailed;
                loader.OnSceneLoaded += OnSceneLoaded;
            }
        }

        public static async Task LeaveAsync()
        {
            var current = instance;
            if (current == null || current.stopping) return;
            current.stopping = true;
            current.ReleaseWaitingPause();
            try
            {
                if (current.runner != null) await current.runner.Shutdown();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Shared run] Shutdown failed: " + exception.Message);
            }
            finally
            {
                SharedRunContext.End();
                if (current != null) Destroy(current.gameObject);
                instance = null;
            }
        }

        private void OnDestroy()
        {
            ReleaseWaitingPause();
            if (loader != null)
            {
                loader.OnLoadFailed -= OnLoadFailed;
                loader.OnSceneLoaded -= OnSceneLoaded;
            }
            if (instance != this) return;
            SharedRunContext.End();
            instance = null;
        }

        private async void OnLoadFailed(string message)
        {
            Debug.LogError("[Shared run] Level load failed: " + message);
            await LeaveAsync();
            LoadingManager.Instance.LoadLocation(Location.Lobby, withAds: false);
        }

        private void OnSceneLoaded()
        {
            sceneLoaded = true;
            if (!SharedRunContext.Playing && waitingPause == null)
            {
                waitingPause = PauseManager.Instance;
                if (waitingPause != null) waitingPause.SetPause(true);
            }
            bool connected = runner != null && runner.IsConnectedToServer;
            int players = connected ? runner.ActivePlayers.Count() : 0;
            Debug.Log($"[Shared run] Scene loaded; connected={connected}; players={players}; seed={SharedRunContext.Seed}; avatars={FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None).Length}; player={PlayerManager.Instance != null}; camera={Camera.main != null}");
            if (!connected) ReturnAfterDisconnect();
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double gap = now - lastFrameTime;
            lastFrameTime = now;
            if (gap > 5)
                Debug.LogWarning($"[Shared run] Main-thread frame gap={gap:F1}s; loaded={sceneLoaded}; focused={Application.isFocused}");
            if (stopping || returningToLobby) return;
            if (runner != null && runner.IsConnectedToServer)
            {
                if (!sceneLoaded) return;
                var state = SharedRunContext.State;
                if (state == null || state.Object == null || !state.Object.IsValid || state.Phase == 4)
                {
                    Debug.LogWarning("[Shared run] Crew loading aborted or state unavailable.");
                    ReturnAfterDisconnect();
                    return;
                }
                if (SharedRunContext.Playing)
                {
                    ReleaseWaitingPause();
                    return;
                }
                // Acknowledgements are idempotent and retried across authority changes.
                if (state.Phase == 2 && !state.IsReady(runner.LocalPlayer) && now >= nextReadyReport)
                {
                    nextReadyReport = now + 1;
                    state.RPC_SceneReady();
                }
                return;
            }
            if (!connectionLost)
            {
                connectionLost = true;
                Debug.LogWarning("[Shared run] Connection lost after transfer to run lifetime.");
            }
            // Wait for the current async scene load to finish before requesting another.
            if (sceneLoaded) ReturnAfterDisconnect();
        }

        private async void ReturnAfterDisconnect()
        {
            if (returningToLobby || stopping) return;
            returningToLobby = true;
            ReleaseWaitingPause();
            Debug.LogWarning("[Shared run] Returning to lobby after network loss; solo fallback is disabled.");
            // The normal exit preserves the first-person player and resets the
            // transient save before LeaveAsync. A bare scene load loses the player
            // and leaves persistent UI without its camera.
            if (GameManager.Instance != null && !GameManager.Instance.isEndGame &&
                PlayerManager.Instance != null)
            {
                GameManager.Instance.EndGame(true, false);
                return;
            }
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.transform.SetParent(null);
                DontDestroyOnLoad(PlayerManager.Instance.gameObject);
            }
            if (ControlManager.Instance != null) ControlManager.Instance.MoveActive = false;
            await LeaveAsync();
            if (ControlManager.Instance != null) ControlManager.Instance.MoveActive = true;
            if (LoadingManager.Instance != null)
                LoadingManager.Instance.LoadLocation(Location.Lobby, withAds: false);
        }

        private void OnGUI()
        {
            if (!SharedRunContext.Active) return;
            bool connected = runner != null && runner.IsConnectedToServer;
            GUI.Box(new Rect(12, 12, Mathf.Min(570, Screen.width - 24), 82),
                (connected ? $"Co-op preview · connected · players: {runner.ActivePlayers.Count()}"
                    : "Co-op connection lost — return to lobby") + "\n" +
                $"Seed: {SharedRunContext.Seed} · Generator: {RunRandom.Version} · Level: {SharedRunContext.LevelId}" +
                (sceneLoaded && !SharedRunContext.Playing ? "\nОжидаем загрузку экипажа…" : ""));
        }
    }
}
