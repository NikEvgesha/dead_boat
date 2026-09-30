using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class SharedRunLifetime : MonoBehaviour
    {
        private static SharedRunLifetime instance;
        private NetworkRunner runner;
        private bool stopping;

        public void Initialize(NetworkRunner value)
        {
            instance = this;
            runner = value;
            DontDestroyOnLoad(gameObject);
            if (GameLoader.Instance != null) GameLoader.Instance.OnLoadFailed += OnLoadFailed;
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
            if (GameLoader.Instance != null) GameLoader.Instance.OnLoadFailed -= OnLoadFailed;
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

        private void OnGUI()
        {
            if (!SharedRunContext.Active) return;
            GUI.Box(new Rect(12, 12, Mathf.Min(570, Screen.width - 24), 62),
                "Co-op loading preview — interactions/physics are local\n" +
                $"Seed: {SharedRunContext.Seed} · Generator: {RunRandom.Version} · Level: {SharedRunContext.LevelId}");
        }
    }
}
