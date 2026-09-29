using System;
using System.Threading.Tasks;
using MirraCloud;
using MirraCloud.Core;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest
{
    // Isolated WebGL check; never added to the normal game's scene list.
    public sealed class MirraCloudConnectionSmokeTest : MonoBehaviour
    {
        private MirraCloudSDK sdk;
        private string status = "Ready";
        private bool connecting;

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
                    Debug.LogWarning($"[Mirra Cloud smoke test] {status}");
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
            var width = Mathf.Min(Screen.width - 40, 540);
            GUILayout.BeginArea(new Rect(20, 20, width, 180), GUI.skin.box);
            GUILayout.Label("Mirra Cloud - Yandex Games connection test");
            GUILayout.Label($"Status: {status}");
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
