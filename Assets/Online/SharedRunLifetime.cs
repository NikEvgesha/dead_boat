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

        public void Initialize(NetworkRunner value)
        {
            instance = this;
            runner = value;
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
            bool connected = runner != null && runner.IsConnectedToServer;
            int players = connected ? runner.ActivePlayers.Count() : 0;
            Debug.Log($"[Shared run] Scene loaded; connected={connected}; players={players}; seed={SharedRunContext.Seed}; avatars={FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None).Length}");
            if (!connected) ReturnAfterDisconnect();
        }

        private void Update()
        {
            if (stopping || returningToLobby || (runner != null && runner.IsConnectedToServer)) return;
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
            Debug.LogWarning("[Shared run] Returning to lobby after network loss; solo fallback is disabled.");
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
            GUI.Box(new Rect(12, 12, Mathf.Min(570, Screen.width - 24), 62),
                (connected ? $"Co-op preview · connected · players: {runner.ActivePlayers.Count()}"
                    : "Co-op connection lost — return to lobby") + "\n" +
                $"Seed: {SharedRunContext.Seed} · Generator: {RunRandom.Version} · Level: {SharedRunContext.LevelId}");
        }
    }
}
