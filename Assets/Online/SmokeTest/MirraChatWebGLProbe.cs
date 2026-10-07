#if UNITY_EDITOR || DEADBOAT_MIRRA_CHAT_PROBE
using System;
using System.Collections.Generic;
using MirraCloud.Core.Chats.Dto;
using UnityEngine;

namespace DeadBoat.Online.SmokeTest
{
    // Isolated diagnostic scene only. Never attached to the game or enabled in normal builds.
    public sealed class MirraChatWebGLProbe : MonoBehaviour
    {
        private string channel = "", peer = "A", status = "Idle";
        private bool busy, attached;
        private int sequence;
        private float nextSend;
        private readonly List<string> events = new();
        private MirraSocialService social;

        private void Start() { social = MirraSocialService.Instance; }

        private void Receive(ChatMessageDto message)
        {
            if (message?.ChannelId != channel) return;
            events.Add(message.Number + ": " + message.Body);
            if (events.Count > 20) events.RemoveAt(0);
            Debug.Log("[Mirra browser probe] received=" + message.Number);
        }

        private async void Attach(bool create)
        {
            if (busy || attached) return;
            busy = true;
            string step = "login";
            try
            {
                if (!await social.ConnectAsync()) throw new InvalidOperationException();
                var sdk = social.Sdk;
                if (create)
                {
                    step = "create";
                    var op = sdk.Chats.CreateChannelAsync("Dead Boat WebGL self-test", "deadboat-lobby-v1");
                    await op.Task();
                    if (!op.Result.IsSuccess) throw new InvalidOperationException();
                    channel = op.Result.Data.ChannelId;
                    Debug.Log("[Mirra browser probe] channel=" + channel);
                }
                else
                {
                    step = "join";
                    var op = sdk.Chats.JoinAsync(channel.Trim());
                    await op.Task();
                    if (!op.Result.IsSuccess)
                    {
                        var members = sdk.Chats.GetMembersAsync(channel.Trim());
                        await members.Task();
                        var profile = sdk.PlayerAccount.PlayerAccountInfo?.SelectedProfileId;
                        if (op.Result.HttpStatusCode != 409 || !members.Result.IsSuccess ||
                            !Array.Exists(members.Result.Data ?? Array.Empty<MirraCloud.Core.Chats.Dto.ChatMemberDto>(), m => m.ProfileId == profile))
                            throw new InvalidOperationException();
                    }
                    channel = channel.Trim();
                }
                step = "connect";
                var connection = sdk.Chats.ConnectAsync(); await connection.Task();
                if (!connection.Result.IsSuccess) throw new InvalidOperationException();
                sdk.Chats.OnMessageReceived -= Receive;
                sdk.Chats.OnMessageReceived += Receive;
                step = "subscribe";
                var sub = sdk.Chats.SubscribeAsync(channel); await sub.Task();
                if (!sub.Result.IsSuccess) throw new InvalidOperationException();
                attached = true; status = "Connected " + peer;
                Debug.Log("[Mirra browser probe] connected=" + peer);
            }
            catch (Exception) { status = "Failed: " + step; Debug.Log("[Mirra browser probe] failed=" + step); }
            finally { busy = false; }
        }

        private async void Send()
        {
            if (busy || !attached || Time.unscaledTime < nextSend) return;
            busy = true;
            try
            {
                var op = social.Sdk.Chats.SendMessageAsync(channel, "WebGL probe " + peer + " " + ++sequence);
                await op.Task();
                status = op.Result.IsSuccess ? "Sent " + peer + " " + sequence : "Send failed";
            }
            catch (Exception) { status = "Send failed"; }
            finally { busy = false; nextSend = Time.unscaledTime + 2; }
        }

        private async void History()
        {
            if (busy || !attached) return;
            busy = true;
            try
            {
                var op = social.Sdk.Chats.GetMessagesAsync(channel, limit: 20); await op.Task();
                status = op.Result.IsSuccess ? "History: " + (op.Result.Data?.Length ?? 0) : "History failed";
                Debug.Log("[Mirra browser probe] " + status);
            }
            catch (Exception) { status = "History failed"; }
            finally { busy = false; }
        }

        private async void Detach()
        {
            if (busy || !attached) return;
            busy = true;
            try
            {
                social.Sdk.Chats.OnMessageReceived -= Receive;
                var u = social.Sdk.Chats.UnsubscribeAsync(channel); await u.Task();
                var l = social.Sdk.Chats.LeaveAsync(channel); await l.Task();
                await MirraChatConnection.DisconnectAsync(social.Sdk.Chats);
                attached = false; status = "Disconnected";
            }
            catch (Exception) { status = "Disconnect failed"; }
            finally { busy = false; }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 700, 650), GUI.skin.box);
            GUILayout.Label("Mirra WebGL chat probe — separate test channel only");
            GUILayout.Label(status);
            GUI.enabled = !busy && !attached;
            peer = GUILayout.TextField(peer, 8);
            channel = GUILayout.TextField(channel, 100);
            if (GUILayout.Button("Create test channel (A)")) Attach(true);
            if (GUILayout.Button("Join test channel (B)")) Attach(false);
            GUI.enabled = !busy && attached;
            if (GUILayout.Button("Send benign probe")) Send();
            if (GUILayout.Button("Read history")) History();
            if (GUILayout.Button("Leave and disconnect")) Detach();
            GUI.enabled = true;
            foreach (var entry in events) GUILayout.Label(entry);
            GUILayout.EndArea();
        }
    }
}
#endif
