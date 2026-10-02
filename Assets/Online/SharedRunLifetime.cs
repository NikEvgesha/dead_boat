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

        public void Initialize(NetworkRunner value)
        {
            instance = this;
            runner = value;
            DontDestroyOnLoad(gameObject);
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
            bool connected = runner != null && runner.IsConnectedToServer;
            int players = connected ? runner.ActivePlayers.Count() : 0;
            Debug.Log($"[Shared run] Scene loaded; connected={connected}; players={players}; seed={SharedRunContext.Seed}; avatars={FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None).Length}");
        }

        private void Update()
        {
            if (stopping || connectionLost || runner == null || runner.IsConnectedToServer) return;
            connectionLost = true;
            Debug.LogWarning("[Shared run] Connection lost after transfer to run lifetime.");
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
