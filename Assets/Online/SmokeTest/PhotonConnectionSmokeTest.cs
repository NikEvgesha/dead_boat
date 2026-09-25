using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest
{
    // Isolated WebGL test scene. It does not modify the game's normal loading flow.
    public sealed class PhotonConnectionSmokeTest : MonoBehaviour
    {
        private const string SessionName = "river-yandex-csp-424778";
        private NetworkRunner runner;
        private string status = "Starting";
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
            status = "Connecting to Photon...";

            if (runner != null)
            {
                await runner.Shutdown();
                if (runner != null)
                    Destroy(runner.gameObject);
            }

            var runnerObject = new GameObject("Photon Smoke Test Runner");
            runner = runnerObject.AddComponent<NetworkRunner>();
            var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>();

            try
            {
                var result = await runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = SessionName,
                    PlayerCount = 10,
                    IsOpen = true,
                    IsVisible = true,
                    SceneManager = sceneManager,
                    ObjectProvider = objectProvider
                });

                status = result.Ok
                    ? "Connected"
                    : $"Failed: {result.ShutdownReason} {result.ErrorMessage}";
                Debug.Log($"[Photon smoke test] {status}");
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
            const int width = 500;
            GUILayout.BeginArea(new Rect(20, 20, width, 250), GUI.skin.box);
            GUILayout.Label("Photon Fusion Shared - Yandex Games connection test");
            GUILayout.Label($"Status: {status}");
            GUILayout.Label($"Session: {SessionName}");

            if (runner != null && runner.IsConnectedToServer)
            {
                GUILayout.Label($"Player count: {runner.ActivePlayers.Count()}/10");
                GUILayout.Label($"Local player: {runner.LocalPlayer}");
            }

            if (!connecting && (runner == null || !runner.IsConnectedToServer) && GUILayout.Button("Retry"))
                _ = ConnectAsync();

            GUILayout.EndArea();
        }
    }
}
