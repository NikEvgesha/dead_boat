using System.Threading.Tasks;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest
{
    // Used by the isolated scene and, with a build-only define, the playable draft.
    public sealed class MirraCloudConnectionSmokeTest : MonoBehaviour
    {
        private string status = "Ready";
        private string errorDetails;
        private bool connecting;

#if DEADBOAT_MIRRA_DIAGNOSTICS && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallInPlayableDraft()
        {
            var overlay = new GameObject("Mirra Cloud Draft Diagnostic");
            DontDestroyOnLoad(overlay);
            overlay.AddComponent<MirraCloudConnectionSmokeTest>();
        }
#endif

        private void Start()
        {
            Application.runInBackground = true;
            _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (connecting) return;
            connecting = true;
            try
            {
                var service = DeadBoat.Online.MirraSocialService.Instance;
                await service.ConnectAsync();
                status = service.Status;
                errorDetails = null;
            }
            finally { connecting = false; }
        }
        private void OnGUI()
        {
#if DEADBOAT_MIRRA_DIAGNOSTICS && !UNITY_EDITOR
            var width = Mathf.Min(Screen.width - 20, 360);
            var x = Mathf.Max(10, Screen.width - width - 10);
            var y = Mathf.Max(10, Screen.height - 130);
            GUILayout.BeginArea(new Rect(x, y, width, 120), GUI.skin.box);
#else
            var width = Mathf.Min(Screen.width - 40, 540);
            GUILayout.BeginArea(new Rect(20, 20, width, 180), GUI.skin.box);
#endif
            GUILayout.Label("Mirra Cloud - Yandex Games connection test");
            GUILayout.Label($"Status: {status}");
            if (!string.IsNullOrEmpty(errorDetails))
                GUILayout.Label(errorDetails);
            if (!connecting && GUILayout.Button("Retry"))
                _ = ConnectAsync();
            GUILayout.EndArea();
        }

    }
}
