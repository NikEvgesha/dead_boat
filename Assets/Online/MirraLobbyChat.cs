using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using MirraCloud.Core.Chats.Dto;
using MirraCloud.Core.Chats.Models;
using MirraCloud.Core.Realtime.Protocol;
using UnityEngine;

namespace DeadBoat.Online
{
    // Messages live in Mirra; Photon carries only the public channel identifier.
    public sealed class MirraLobbyChat : MonoBehaviour
    {
        internal const string ChannelProperty = "mc_chat_v1";
        private static readonly SemaphoreSlim channelGate = new(1, 1);
        private LobbyOnlineBootstrap owner;
        private MirraLobbyChatSettings settings;
        private MirraSocialService social;
        private bool working, sending, subscribed, recovering;
        private string room, channel, observedRoom;
        private float nextAttempt, nextSend;
        private int generation;
        private readonly List<ChatMessageDto> messages = new();
        private readonly HashSet<string> deleted = new();
        public IReadOnlyList<ChatMessageDto> Messages => messages;
        public string Status { get; private set; } = "Чат недоступен";
        public bool Ready => subscribed && !working && !recovering && social.Ready &&
            social.Sdk.Chats.ConnectionState == RealtimeConnectionState.Connected && IsCurrent(room);
        public bool Sending => sending;
        public bool CanSend => Ready && !sending && Time.unscaledTime >= nextSend;
        public event Action Changed;

        public void Initialize(LobbyOnlineBootstrap bootstrap)
        {
            owner = bootstrap;
            social = MirraSocialService.Instance;
            settings = Resources.Load<MirraLobbyChatSettings>("OnlineUI/MirraLobbyChatSettings");
            if (settings != null && settings.enabledForPilot)
                gameObject.AddComponent<LobbyChatUI>().Initialize(this, owner);
        }

        private bool IsCurrent(string expected) => this != null && owner != null && owner.IsOnline &&
            !owner.IsInDepartureRoom && !owner.IsBrowsingDepartures && owner.CurrentSessionName == expected;

        private async void Update()
        {
            if (settings == null || !settings.enabledForPilot) return;
            string desired = owner.IsOnline && !owner.IsInDepartureRoom && !owner.IsBrowsingDepartures
                ? owner.CurrentSessionName : null;
            ObserveRoom(desired);
            if (working || Time.unscaledTime < nextAttempt) return;
            if (room == desired && ((subscribed && social.Ready) || desired == null)) return;
            working = true;
            await channelGate.WaitAsync();
            int epoch = generation;
            try
            {
                await DetachAsync();
                epoch = generation;
                if (!IsCurrent(desired) || string.IsNullOrEmpty(desired)) return;
                room = desired;
                Status = "Подключение чата…";
                Changed?.Invoke();
                if (!await social.ConnectAsync() || !IsOperationCurrent(epoch, desired)) return;
                var sdk = social.Sdk;
                var runner = owner.LobbyRunner;
                bool created = false;
                // Declare the key when creating the Photon room so it remains in lobby metadata.
                // Older rooms without this key must not create a different Cloud channel per peer.
                if (!runner.SessionInfo.Properties.TryGetValue(ChannelProperty, out var property))
                {
                    Status = "Чат доступен в новых лобби";
                    return;
                }
                channel = (string)property;
                if (string.IsNullOrEmpty(channel))
                {
                    if (!runner.IsSharedModeMasterClient) return;
                    var create = sdk.Chats.CreateChannelAsync("Dead Boat lobby", settings.templateKey);
                    await create.Task();
                    if (!create.Result.IsSuccess || create.Result.Data == null) throw new InvalidOperationException();
                    channel = create.Result.Data.ChannelId;
                    created = true; // CreateChannel already joins its creator.
                    if (!IsOperationCurrent(epoch, desired) || !runner.IsSharedModeMasterClient) return;
                    if (!runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty> { [ChannelProperty] = channel }))
                        throw new InvalidOperationException("Chat channel property was not accepted by Photon");
                }
                if (!created)
                {
                    var join = sdk.Chats.JoinAsync(channel);
                    await join.Task();
                    if (!join.Result.IsSuccess)
                    {
                        // A reconnect may already be a member. A generic 409 is not proof of membership.
                        if (join.Result.HttpStatusCode != 409) throw new InvalidOperationException();
                        var members = sdk.Chats.GetMembersAsync(channel);
                        await members.Task();
                        string profile = sdk.PlayerAccount.PlayerAccountInfo?.SelectedProfileId;
                        bool member = !string.IsNullOrEmpty(profile) && members.Result.IsSuccess &&
                            Array.Exists(members.Result.Data ?? Array.Empty<ChatMemberDto>(), m => m?.ProfileId == profile);
                        if (!member) throw new InvalidOperationException();
                    }
                    if (!IsOperationCurrent(epoch, desired)) return;
                }
                sdk.Chats.OnMessageReceived += Receive;
                sdk.Chats.OnMessageEdited += Receive;
                sdk.Chats.OnMessageDeleted += Delete;
                sdk.Chats.OnSubscribedChannel += Recover;
                sdk.Chats.OnConnectionStateChanged += ConnectionChanged;
                var connect = sdk.Chats.ConnectAsync();
                await connect.Task();
                if (!IsOperationCurrent(epoch, desired)) return;
                if (!connect.Result.IsSuccess) throw new InvalidOperationException();
                var subscribe = sdk.Chats.SubscribeAsync(channel);
                await subscribe.Task();
                if (!IsOperationCurrent(epoch, desired)) return;
                if (!subscribe.Result.IsSuccess) throw new InvalidOperationException();
                subscribed = true;
                var history = sdk.Chats.GetMessagesAsync(channel, limit: 50);
                await history.Task();
                if (!IsOperationCurrent(epoch, desired)) return;
                if (!history.Result.IsSuccess) throw new InvalidOperationException();
                foreach (var message in history.Result.Data ?? Array.Empty<ChatMessageDto>()) Receive(message);
                Status = "Чат лобби";
            }
            catch (Exception)
            {
                if (IsOperationCurrent(epoch, desired)) Status = "Чат недоступен. Игра продолжается";
            }
            finally
            {
                bool retrySameRoom = IsOperationCurrent(epoch, desired);
                if (!subscribed || !IsOperationCurrent(epoch, desired)) await DetachAsync();
                channelGate.Release(); working = false;
                nextAttempt = retrySameRoom && observedRoom == desired ? Time.unscaledTime + 10 : 0;
                Changed?.Invoke();
            }
        }

        private bool IsOperationCurrent(int epoch, string expected) => epoch == generation && IsCurrent(expected);

        // Invalidate pending callbacks immediately, even while a REST request is in flight.
        private void ObserveRoom(string desired)
        {
            if (observedRoom == desired) return;
            observedRoom = desired;
            Invalidate();
            nextAttempt = 0;
            Status = string.IsNullOrEmpty(desired) ? "Чат недоступен" : "Подключение чата…";
            Changed?.Invoke();
        }

        private void Invalidate()
        {
            generation++;
            subscribed = sending = recovering = false;
            nextSend = 0;
            messages.Clear(); deleted.Clear();
        }

        private void Receive(ChatMessageDto message)
        {
            if ((owner != null && (!IsCurrent(room) || observedRoom != room)) || message == null || message.ChannelId != channel || string.IsNullOrEmpty(message.MessageId) || deleted.Contains(message.MessageId)) return;
            if (message.DeletedAt.HasValue) { Delete(new RealtimeDeletePayload { ChannelId = channel, MessageId = message.MessageId }); return; }
            int index = messages.FindIndex(m => m.MessageId == message.MessageId);
            if (index >= 0 && (messages[index].EditedAt ?? messages[index].CreatedAt) > (message.EditedAt ?? message.CreatedAt)) return;
            if (index >= 0) messages[index] = message; else messages.Add(message);
            messages.Sort((a,b) => a.Number.CompareTo(b.Number));
            while (messages.Count > 50) messages.RemoveAt(0);
            Changed?.Invoke();
        }

        private void Delete(RealtimeDeletePayload payload)
        {
            if ((owner != null && (!IsCurrent(room) || observedRoom != room)) || payload == null || payload.ChannelId != channel || string.IsNullOrEmpty(payload.MessageId)) return;
            if (deleted.Count < 200) deleted.Add(payload.MessageId);
            messages.RemoveAll(m => m.MessageId == payload.MessageId);
            Changed?.Invoke();
        }

        private void ConnectionChanged(RealtimeConnectionState state)
        {
            if (!IsCurrent(room) || observedRoom != room) return;
            if (state != RealtimeConnectionState.Connected) Status = "Переподключение чата…";
            Changed?.Invoke();
        }

        private async void Recover(string subscribedChannel)
        {
            if (!subscribed || working || recovering || subscribedChannel != channel || !IsCurrent(room)) return;
            int epoch = generation;
            recovering = true;
            messages.Clear(); deleted.Clear();
            Status = "Обновление истории…";
            Changed?.Invoke();
            try
            {
                var history = social.Sdk.Chats.GetMessagesAsync(channel, limit: 50);
                await history.Task();
                if (epoch != generation || !IsCurrent(room)) return;
                if (!history.Result.IsSuccess) throw new InvalidOperationException();
                foreach (var message in history.Result.Data ?? Array.Empty<ChatMessageDto>()) Receive(message);
                Status = "Чат лобби";
            }
            catch (Exception)
            {
                if (epoch == generation) { subscribed = false; Status = "Не удалось восстановить историю чата"; }
            }
            finally { if (epoch == generation) { recovering = false; Changed?.Invoke(); } }
        }

        public async Task<bool> SendAsync(string body)
        {
            body = body?.Trim();
            if (!CanSend || string.IsNullOrEmpty(body) || body.Length > 200) return false;
            sending = true;
            int epoch = generation;
            string target = channel;
            Changed?.Invoke();
            try
            {
                // UX preflight only. Mandatory moderation must also be enabled on the Cloud channel.
                var filter = social.Sdk.ProfanityFilter.CheckAsync(body, settings.profanityGroupKey);
                await filter.Task();
                if (epoch != generation || !Ready) return false;
                if (!filter.Result.IsSuccess || filter.Result.Data?.isClean != true)
                { Status = "Сообщение не прошло проверку. Измените текст или попробуйте позже"; return false; }
                var send = social.Sdk.Chats.SendMessageAsync(target, body);
                await send.Task();
                if (epoch != generation || !IsCurrent(room)) return false;
                if (!send.Result.IsSuccess) { Status = "Не удалось отправить сообщение"; return false; }
                Receive(send.Result.Data);
                Status = "Чат лобби";
                owner.StayOnline();
                return true;
            }
            catch (Exception) { if (epoch == generation) Status = "Не удалось отправить сообщение"; return false; }
            finally { if (epoch == generation) { sending = false; nextSend = Time.unscaledTime + 2; Changed?.Invoke(); } }
        }

        private async Task DetachAsync()
        {
            Invalidate();
            var old = channel;
            room = channel = null;
            var chats = social?.Sdk?.Chats;
            if (chats == null) return;
            chats.OnMessageReceived -= Receive; chats.OnMessageEdited -= Receive; chats.OnMessageDeleted -= Delete;
            chats.OnSubscribedChannel -= Recover; chats.OnConnectionStateChanged -= ConnectionChanged;
            try
            {
                if (!string.IsNullOrEmpty(old))
                {
                    var unsub = chats.UnsubscribeAsync(old); await unsub.Task();
                    var leave = chats.LeaveAsync(old); await leave.Task();
                }
                await MirraChatConnection.DisconnectAsync(chats);
            }
            catch (Exception) { /* Best effort during scene teardown; never block gameplay. */ }
        }

        private async void OnDestroy()
        {
            generation++;
            await channelGate.WaitAsync();
            try { await DetachAsync(); }
            finally { channelGate.Release(); }
        }
    }
}
