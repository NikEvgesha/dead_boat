#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MirraCloud;
using MirraCloud.Core;
using MirraCloud.Core.Auth;
using MirraCloud.Core.Chats;
using MirraCloud.Core.Friends;
using MirraCloud.Core.Storage;
using MirraCloud.Core.WebView;
using MirraCloud.Json;
using Plugins.MirraCloud.Core.General.LifeCycle;
using Plugins.MirraCloud.Core.Services.PlayerAccount;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest.Editor
{
    // Explicit real-service test; isolated memory credentials, never the player's SDK/storage.
    public static class MirraFriendsRuntimeProbe
    {
        public static bool Running { get; private set; }
        public static string Result { get; private set; } = "Not run";

        public static void Run()
        {
            if (Running) return;
            if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
                "Assets/Online/SmokeTest/MirraChatWebGLProbe.unity")
                throw new InvalidOperationException("Use Play Mode in isolated chat probe scene");
            _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            Running = true; Result = "Running";
            Client a = null, b = null, restored = null;
            GameObject host = null;
            string channel = null, step = "setup";
            bool relationship = false;
            try
            {
                a = new Client(new MemoryStorage()); b = new Client(new MemoryStorage());
                step = "login A"; await a.Login();
                step = "login B"; await b.Login();
                Require(a.Account.PlayerAccountInfo.Id != b.Account.PlayerAccountInfo.Id, "distinct accounts");
                step = "create test channel";
                var create = a.Chats.CreateChannelAsync("Dead Boat friends self-test", "deadboat-lobby-v1");
                await create.Task(); Require(create.Result.IsSuccess, "create HTTP " + create.Result.HttpStatusCode);
                channel = create.Result.Data.ChannelId;
                step = "join test channel";
                var join = b.Chats.JoinAsync(channel); await join.Task();
                Require(join.Result.IsSuccess, "join HTTP " + join.Result.HttpStatusCode);
                step = "connect chat B";
                var connect = b.Chats.ConnectAsync(); await connect.Task();
                Require(connect.Result.IsSuccess, "chat connect");
                var subscribe = b.Chats.SubscribeAsync(channel); await subscribe.Task();
                Require(subscribe.Result.IsSuccess, "chat subscribe");
                step = "message B";
                var send = b.Chats.SendMessageAsync(channel, "Friends pilot hello"); await send.Task();
                Require(send.Result.IsSuccess, "chat send");
                Require(send.Result.Data.SenderId == b.Account.PlayerAccountInfo.SelectedProfileId, "sender is selected profile");
                host = new GameObject("Memory friends service probe");
                host.SetActive(false);
                var social = host.AddComponent<MirraSocialService>();
                typeof(MirraSocialService).GetField("sdk", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(social, a.Sdk);
                step = "request via game adapter";
                await social.RequestChatFriendAsync(send.Result.Data.SenderId);
                Require(social.Outgoing.Any(r => r.TargetPlayerId == b.Account.PlayerAccountInfo.SelectedProfileId), "outgoing sender mapping; " + social.Status);
                relationship = true;
                Require(!social.CanRequestChatFriend(send.Result.Data.SenderId), "duplicate request disabled");
                Require(!social.CanRequestChatFriend(a.Account.PlayerAccountInfo.SelectedProfileId), "self request disabled");
                step = "incoming B";
                var incoming = b.Friends.GetIncomingAsync(); await incoming.Task();
                Require(incoming.Result.IsSuccess && incoming.Result.Data.Any(r => r.SourcePlayerId == a.Account.PlayerAccountInfo.SelectedProfileId), "incoming request");
                step = "accept B";
                var accept = b.Friends.AcceptAsync(a.Account.PlayerAccountInfo.SelectedProfileId); await accept.Task();
                Require(accept.Result.IsSuccess, "accept HTTP " + accept.Result.HttpStatusCode);
                step = "restore A";
                restored = new Client(a.Storage);
                var restore = restored.Auth.InitializeAsync(); await restore.Task();
                Require(restore.Result.IsSuccess && restored.Auth.IsAuth, "session restore");
                Require(restored.Account.PlayerAccountInfo.Id == a.Account.PlayerAccountInfo.Id, "same account restored");
                step = "persisted friendship";
                var friends = restored.Friends.GetFriendsAsync(); await friends.Task();
                Require(friends.Result.IsSuccess && friends.Result.Data.Any(f => f.PlayerId == b.Account.PlayerAccountInfo.SelectedProfileId), "friends after restore");
                step = "remove B";
                var remove = b.Friends.RemoveFriendAsync(a.Account.PlayerAccountInfo.SelectedProfileId); await remove.Task();
                Require(remove.Result.IsSuccess, "remove HTTP " + remove.Result.HttpStatusCode);
                relationship = false;
                var empty = restored.Friends.GetFriendsAsync(); await empty.Task();
                Require(empty.Result.IsSuccess && !empty.Result.Data.Any(f => f.PlayerId == b.Account.PlayerAccountInfo.SelectedProfileId), "removal propagated");
                Result = "PASS: chat sender to Friends, request/accept, self/duplicate guards, session restore, persisted friends, removal";
            }
            catch (Exception exception) { Result = "FAIL at " + step + ": " + (exception is ProbeFailure ? exception.Message : exception.GetType().Name); }
            finally
            {
                if (relationship && a != null && b != null)
                {
                    // Only these newly-created test identities are involved.
                    try { var revoke = a.Friends.RevokeAsync(b.Account.PlayerAccountInfo.SelectedProfileId); await revoke.Task(); } catch { }
                    try { var remove = b.Friends.RemoveFriendAsync(a.Account.PlayerAccountInfo.SelectedProfileId); await remove.Task(); } catch { }
                }
                if (channel != null)
                {
                    try { var leave = b.Chats.LeaveAsync(channel); await leave.Task(); } catch { }
                    try { var leave = a.Chats.LeaveAsync(channel); await leave.Task(); } catch { }
                }
                if (host != null) UnityEngine.Object.Destroy(host);
                restored?.Dispose(); b?.Dispose(); a?.Dispose();
                Running = false;
                Debug.Log("[Mirra friends runtime probe] " + Result);
            }
        }

        private static void Require(bool condition, string check)
        { if (!condition) throw new ProbeFailure(check); }

        private sealed class ProbeFailure : Exception { public ProbeFailure(string message) : base(message) { } }

        private sealed class Client : IDisposable
        {
            public readonly MemoryStorage Storage;
            public readonly AuthenticationService Auth;
            public readonly PlayerAccountService Account;
            public readonly FriendsService Friends;
            public readonly ChatsService Chats;
            public readonly MirraCloudSDK Sdk;
            public Client(MemoryStorage storage)
            {
                Storage = storage;
                var config = Configuration.Load();
                var logger = new MirraCloud.Core.Logger.Logger();
                var json = new JsonService();
                var runner = CoroutineRunner.CreateInstance();
                var rest = new RestApiClient(new RestApiClientOptions { BaseUrl = config.Url }, runner, json, logger);
                Auth = new AuthenticationService(config, logger, storage, rest, new WebViewService());
                Account = new PlayerAccountService(Auth, rest, config, logger);
                Friends = new FriendsService(config, logger, rest);
                Chats = new ChatsService(config, logger, rest, json, runner, Auth);
                Chats.CloudSdkInitialize();
                Sdk = MirraCloudSDK.Create();
                // Fixture injection only: public SDK service implementations, no package edits.
                foreach (var pair in new Dictionary<string, object> { ["Authentication"] = Auth, ["PlayerAccount"] = Account, ["Friends"] = Friends })
                    typeof(MirraCloudSDK).GetProperty(pair.Key).SetValue(Sdk, pair.Value);
            }
            public async Task Login()
            {
                var login = Auth.LoginGuestAsync(); await login.Task();
                Require(login.Result.IsSuccess && Auth.IsAuth, "login HTTP " + login.Result.HttpStatusCode);
            }
            public void Dispose() { Chats.CloudSdkDispose(); Account.Dispose(); Auth.CloudSdkDispose(); }
        }

        private sealed class MemoryStorage : IStorage
        {
            private readonly Dictionary<string, string> values = new();
            public Task Ready => Task.CompletedTask;
            public bool HasKey(string key) => values.ContainsKey(key);
            public string GetString(string key) => values.TryGetValue(key, out var value) ? value : null;
            public void SaveString(string key, string value) { if (string.IsNullOrEmpty(value)) values.Remove(key); else values[key] = value; }
            public void DeleteKeys(params string[] keys) { foreach (var key in keys) values.Remove(key); }
            public Task FlushAsync() => Task.CompletedTask;
        }
    }
}
#endif
