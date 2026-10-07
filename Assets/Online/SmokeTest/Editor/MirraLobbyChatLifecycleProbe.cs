#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using MirraCloud.Core;
using MirraCloud.Core.Realtime.Protocol;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest.Editor
{
    // Explicit diagnostic only: real private Photon rooms and Mirra test messages.
    // Run in the isolated chat probe scene, never in a player's active game.
    public static class MirraLobbyChatLifecycleProbe
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        public static bool Running { get; private set; }
        public static string Result { get; private set; } = "Not run";

        public static void Run()
        {
            if (Running) return;
            if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
                "Assets/Online/SmokeTest/MirraChatWebGLProbe.unity")
                throw new InvalidOperationException("Use Play Mode in the isolated chat probe scene");
            if (EditorApplication.isPaused) throw new InvalidOperationException("Resume Play Mode before running network diagnostics");
            _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            Running = true;
            Result = "Running";
            GameObject host = null;
            NetworkRunner runner = null;
            MirraLobbyChat chat = null;
            MirraLobbyChatSettings settings = null;
            string step = "setup";
            try
            {
                host = new GameObject("Private chat lifecycle probe");
                host.SetActive(false); // Bootstrap Start must not start the game flow.
                var owner = host.AddComponent<LobbyOnlineBootstrap>();
                chat = host.AddComponent<MirraLobbyChat>();
                chat.Initialize(owner);
                settings = UnityEngine.Object.Instantiate(Resources.Load<MirraLobbyChatSettings>("OnlineUI/MirraLobbyChatSettings"));
                settings.enabledForPilot = true; // In-memory copy only; public pilot stays disabled.
                Set(chat, "settings", settings);
                var ui = host.AddComponent<LobbyChatUI>();
                ui.Initialize(chat, owner);
                void Pump()
                {
                    typeof(MirraLobbyChat).GetMethod("Update", Fields).Invoke(chat, null);
                    typeof(LobbyChatUI).GetMethod("Update", Fields).Invoke(ui, null);
                }
                async Task Wait(Func<bool> condition)
                {
                    float deadline = Time.realtimeSinceStartup + 30;
                    do
                    {
                        Pump();
                        if (condition()) return;
                        if (Time.realtimeSinceStartup >= deadline) throw new TimeoutException();
                        await Task.Delay(50);
                    } while (EditorApplication.isPlaying);
                    throw new InvalidOperationException("Play Mode stopped");
                }

                step = "Photon room A";
                runner = await CreateRunner();
                Set(owner, "runner", runner);
                Set(owner, "connectedOnce", true);
                step = "initial chat attach";
                await Wait(() => chat.Ready);
                string channelA = (string)Get(chat, "channel");
                Require(!string.IsNullOrEmpty(channelA), "channel published");
                Require(((GameObject)Get(ui, "root")).activeSelf, "online UI visible");
                step = "send";
                Require(await chat.SendAsync("Editor lifecycle probe"), "send acknowledgement");
                Require(chat.Messages.Count == 1, "own acknowledgement in buffer");

                step = "SDK disconnect and resubscribe";
                var social = MirraSocialService.Instance;
                var sdk = (MirraCloudSDK)Get(social, "sdk");
                await (Task)typeof(MirraLobbyChat).Assembly.GetType("DeadBoat.Online.MirraChatConnection")
                    .GetMethod("DisconnectAsync", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { sdk.Chats });
                Require(!chat.Ready && sdk.Chats.ConnectionState == RealtimeConnectionState.Disconnected, "disconnected blocks send");
                bool recoveringObserved = false;
                chat.Changed += () => recoveringObserved |= chat.Status == "Обновление истории…";
                var connect = sdk.Chats.ConnectAsync(); await connect.Task();
                Require(connect.Result.IsSuccess, "SDK reconnect");
                var subscribe = sdk.Chats.SubscribeAsync(channelA); await subscribe.Task();
                Require(subscribe.Result.IsSuccess, "SDK resubscribe");
                await Wait(() => chat.Ready && recoveringObserved && chat.Messages.Count == 1);

                step = "departure browser";
                Set(owner, "browsingDepartures", true);
                Pump();
                Require(!chat.Ready && chat.Messages.Count == 0 && !((GameObject)Get(ui, "root")).activeSelf, "immediate departure/UI isolation");
                await Wait(() => !(bool)Get(chat, "working") && Get(chat, "channel") == null);
                Require(sdk.Chats.ConnectionState == RealtimeConnectionState.Disconnected, "departure socket cleanup");

                step = "departure room";
                Set(owner, "browsingDepartures", false);
                Set(owner, "inDepartureRoom", true);
                Pump();
                Require(!chat.Ready && Get(chat, "channel") == null, "departure room has no lobby chat");

                step = "return to same Photon lobby";
                Set(owner, "inDepartureRoom", false);
                await Wait(() => chat.Ready);
                Debug.Log("[Mirra lifecycle probe] return channelMatch=" + ((string)Get(chat, "channel") == channelA) + "; historyCount=" + chat.Messages.Count);
                Require((string)Get(chat, "channel") == channelA && chat.Messages.Count == 1, "same channel/history restored");

                step = "offline cleanup";
                Set(owner, "connectedOnce", false);
                await Wait(() => !(bool)Get(chat, "working") && Get(chat, "channel") == null);
                await runner.Shutdown();
                UnityEngine.Object.Destroy(runner.gameObject);
                runner = null;

                step = "Photon room B";
                runner = await CreateRunner();
                Set(owner, "runner", runner);
                Set(owner, "connectedOnce", true);
                await Wait(() => chat.Ready);
                Require((string)Get(chat, "channel") != channelA && chat.Messages.Count == 0, "different lobby/channel/history isolation");

                step = "legacy room guard";
                Set(owner, "connectedOnce", false);
                await Wait(() => !(bool)Get(chat, "working") && Get(chat, "channel") == null);
                await runner.Shutdown();
                UnityEngine.Object.Destroy(runner.gameObject);
                runner = await CreateRunner(false);
                Set(owner, "runner", runner);
                Set(owner, "connectedOnce", true);
                await Wait(() => chat.Status == "Чат доступен в новых лобби" && !(bool)Get(chat, "working"));
                Require(!chat.Ready && Get(chat, "channel") == null && chat.Messages.Count == 0, "legacy room must not create a divergent channel");
                Result = "PASS";
                Debug.Log("[Mirra lifecycle probe] PASS: real private Photon rooms, SDK send/reconnect/history, departure cleanup, same-room return, different-room isolation, legacy room guard, online/offline UI visibility. Single Editor client; no level scene transition.");
            }
            catch (Exception exception)
            {
                Result = "FAIL at " + step + ": " + exception.GetType().Name;
                Debug.LogWarning("[Mirra lifecycle probe] " + Result);
            }
            finally
            {
                if (chat != null) await (Task)typeof(MirraLobbyChat).GetMethod("DetachAsync", Fields).Invoke(chat, null);
                if (runner != null) { await runner.Shutdown(); UnityEngine.Object.Destroy(runner.gameObject); }
                if (host != null) UnityEngine.Object.Destroy(host);
                if (settings != null) UnityEngine.Object.Destroy(settings);
                Running = false;
                Debug.Log("[Mirra lifecycle probe] cleanup complete");
            }
        }

        private static async Task<NetworkRunner> CreateRunner(bool declareChatKey = true)
        {
            var root = new GameObject("Private chat Photon runner");
            var runner = root.AddComponent<NetworkRunner>();
            // Match the game lobby's scene adapter; Fusion's default initializes unused Addressables.
            var sceneManager = root.AddComponent<LobbySceneManager>();
            root.AddComponent<NetworkObjectProviderDefault>();
            try
            {
                var start = runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = "chat-probe-" + Guid.NewGuid().ToString("N"),
                    CustomLobbyName = "deadboat-private-chat-probe",
                    SceneManager = sceneManager,
                    PlayerCount = 1, IsVisible = false, IsOpen = false,
                    SessionProperties = declareChatKey
                        ? new System.Collections.Generic.Dictionary<string, SessionProperty> { ["mc_chat_v1"] = "" }
                        : null
                });
                if (await Task.WhenAny(start, Task.Delay(20000)) != start) throw new TimeoutException();
                var result = await start;
                if (!result.Ok)
                {
                    Debug.LogWarning("[Mirra lifecycle probe] Photon start reason=" + result.ShutdownReason);
                    throw new InvalidOperationException("Private Photon room failed");
                }
                return runner;
            }
            catch { await runner.Shutdown(); UnityEngine.Object.Destroy(root); throw; }
        }

        private static object Get(object target, string field) => target.GetType().GetField(field, Fields).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
        private static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException(check);
        }
    }
}
#endif
