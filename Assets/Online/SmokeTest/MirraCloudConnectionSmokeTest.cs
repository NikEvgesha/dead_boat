using System;
using System.Threading.Tasks;
using MirraCloud;
using MirraCloud.Core;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest
{
    // Used by the isolated scene and, with a build-only define, the playable draft.
    public sealed class MirraCloudConnectionSmokeTest : MonoBehaviour
    {
        private MirraCloudSDK sdk;
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
            if (connecting)
                return;

            connecting = true;
            status = "Connecting to Mirra Cloud...";
            errorDetails = null;

            try
            {
                var configuration = Configuration.Load();
                if (string.IsNullOrWhiteSpace(configuration.ProjectId) ||
                    string.IsNullOrWhiteSpace(configuration.BranchId) ||
                    string.IsNullOrWhiteSpace(configuration.PlatformKey))
                {
                    status = "Missing local Mirra Cloud configuration";
                    return;
                }

                sdk ??= MirraCloudSDK.Create();
                sdk.Initialize();

                var login = sdk.Authentication.LoginGuestAsync();
                await login.Task();

                if (login.Result.IsSuccess)
                {
                    status = "Guest login OK";
                    Debug.Log("[Mirra Cloud smoke test] Guest login OK");
                }
                else
                {
                    var error = login.Result.Error;
                    status = $"Login failed: HTTP {login.Result.HttpStatusCode} {error?.Message}";
                    string serverCode = error?.Errors != null && error.Errors.Count > 0
                        ? error.Errors[0].Code : null;
                    errorDetails = $"Type: {error?.Type}; network: {error?.NetworkResult}; " +
                        $"code: {(string.IsNullOrEmpty(serverCode) ? "none" : serverCode)}";
                    Debug.LogWarning($"[Mirra Cloud smoke test] {status}; {errorDetails}");
                }
            }
            catch (Exception exception)
            {
                status = $"Exception: {exception.Message}";
                Debug.LogException(exception);
            }
            finally
            {
                connecting = false;
            }
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

        private void OnDestroy()
        {
            sdk?.Dispose();
        }
    }
}
