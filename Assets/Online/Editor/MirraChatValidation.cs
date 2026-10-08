#if UNITY_EDITOR
using System;
using System.Reflection;
using MirraCloud.Core.Chats.Dto;
using MirraCloud.Core.Chats.Models;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // No HTTP or real messages: verifies races between live events and a history response.
    public static class MirraChatValidation
    {
        [MenuItem("Tools/Online/Validate Mirra Chat Buffer")]
        public static void Run()
        {
            var root = EditorUtility.CreateGameObjectWithHideFlags("Chat buffer validation", HideFlags.HideAndDontSave, typeof(MirraLobbyChat));
            try
            {
                var chat = root.GetComponent<MirraLobbyChat>();
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(MirraLobbyChat).GetField("channel", flags).SetValue(chat, "probe");
                var receive = typeof(MirraLobbyChat).GetMethod("Receive", flags);
                var delete = typeof(MirraLobbyChat).GetMethod("Delete", flags);
                var date = new DateTime(2026, 10, 6);
                Action<ChatMessageDto> add = message => receive.Invoke(chat, new object[] { message });
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "two", Number = 2, Body = "edited", CreatedAt = date, EditedAt = date.AddSeconds(2) });
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "one", Number = 1, CreatedAt = date });
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "two", Number = 2, Body = "old", CreatedAt = date });
                Require(chat.Messages.Count == 2 && chat.Messages[0].Number == 1 && chat.Messages[1].Body == "edited", "order / duplicate / stale edit");
                delete.Invoke(chat, new object[] { new RealtimeDeletePayload { ChannelId = "probe", MessageId = "two" } });
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "two", Number = 2, CreatedAt = date });
                Require(chat.Messages.Count == 1, "deleted message must not reappear from stale history");
                add(new ChatMessageDto { ChannelId = "other", MessageId = "alien", Number = 3 });
                Require(chat.Messages.Count == 1, "channel isolation");
                typeof(MirraLobbyChat).GetField("recovering", flags).SetValue(chat, true);
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "live", Number = 3, Body = "new edit", CreatedAt = date, EditedAt = date.AddSeconds(3) });
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "live", Number = 3, Body = "stale live", CreatedAt = date });
                delete.Invoke(chat, new object[] { new RealtimeDeletePayload { ChannelId = "probe", MessageId = "one" } });
                typeof(MirraLobbyChat).GetMethod("ApplyRecoveredHistory", flags).Invoke(chat, new object[] { new[] {
                    new ChatMessageDto { ChannelId = "probe", MessageId = "one", Number = 1, CreatedAt = date },
                    new ChatMessageDto { ChannelId = "probe", MessageId = "two", Number = 2, CreatedAt = date },
                    new ChatMessageDto { ChannelId = "probe", MessageId = "live", Number = 3, Body = "old snapshot", CreatedAt = date }
                } });
                Require(chat.Messages.Count == 1 && chat.Messages[0].Body == "new edit", "history reconciliation preserves live edit/deletes and rejects stale event");
                typeof(MirraLobbyChat).GetField("recovering", flags).SetValue(chat, false);
                for (int i = 3; i < 70; i++)
                    add(new ChatMessageDto { ChannelId = "probe", MessageId = "m" + i, Number = i, CreatedAt = date });
                Require(chat.Messages.Count == 50 && chat.Messages[0].Number == 20 && chat.Messages[49].Number == 69, "bounded recent history");
                var type = typeof(MirraLobbyChat);
                var observe = type.GetMethod("ObserveRoom", flags);
                var generation = type.GetField("generation", flags);
                type.GetField("room", flags).SetValue(chat, "lobby-A");
                type.GetField("observedRoom", flags).SetValue(chat, "lobby-A");
                type.GetField("working", flags).SetValue(chat, true);
                type.GetField("subscribed", flags).SetValue(chat, true);
                type.GetField("sending", flags).SetValue(chat, true);
                type.GetField("recovering", flags).SetValue(chat, true);
                type.GetField("nextAttempt", flags).SetValue(chat, Time.unscaledTime + 100);
                int before = (int)generation.GetValue(chat);
                // Departure/offline while requests are pending: no retained messages or busy flags.
                observe.Invoke(chat, new object[] { null });
                Require(chat.Messages.Count == 0 && (int)generation.GetValue(chat) == before + 1, "immediate departure invalidation");
                Require(!chat.Sending && !(bool)type.GetField("recovering", flags).GetValue(chat) &&
                    !(bool)type.GetField("subscribed", flags).GetValue(chat), "pending send/history invalidation");
                Require((float)type.GetField("nextAttempt", flags).GetValue(chat) == 0, "transition bypasses retry cooldown");
                observe.Invoke(chat, new object[] { "lobby-A" });
                Require((int)generation.GetValue(chat) == before + 2, "same room return rejects old operation epoch");
                observe.Invoke(chat, new object[] { "lobby-A" });
                Require((int)generation.GetValue(chat) == before + 2, "stable room does not repeatedly invalidate");
                observe.Invoke(chat, new object[] { "lobby-B" });
                type.GetField("channel", flags).SetValue(chat, "new-channel");
                add(new ChatMessageDto { ChannelId = "probe", MessageId = "late", Number = 70 });
                Require(chat.Messages.Count == 0, "late old-channel message after room switch");
                ValidateLocalVisibility();
                ValidateOfflineUI(chat);
                Debug.Log("[Mirra chat validation] PASS: buffer races/cap; departure and return epochs, pending flags, retry cooldown, old-channel isolation. No HTTP.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ValidateLocalVisibility()
        {
            var local = new ChatLocalVisibility();
            Require(!local.SetHidden(null, true) && !local.SetHidden("", true), "empty sender rejected");
            for (int i = 0; i < 64; i++) Require(local.SetHidden("local-" + i, true), "bounded local hide entries");
            Require(!local.SetHidden("overflow", true) && local.HiddenSenders.Count == 64, "local preference memory cap");
            Require(local.SetHidden("local-0", true), "repeated hide is idempotent at cap");
            Require(local.SetHidden("local-0", false) && !local.IsHidden("local-0") && local.SetHidden("overflow", true), "unhide releases capacity");
            Require(new ChatLocalVisibility().HiddenSenders.Count == 0, "independent local preference isolation");
        }

        private static void ValidateOfflineUI(MirraLobbyChat chat)
        {
            var host = new GameObject("Chat UI validation");
            host.hideFlags = HideFlags.HideAndDontSave;
            host.SetActive(false); // No game Start/Update or Photon connection.
            GameObject canvas = null;
            try
            {
                var bootstrap = host.AddComponent<LobbyOnlineBootstrap>();
                var ui = host.AddComponent<LobbyChatUI>();
                var social = host.AddComponent<MirraSocialService>();
                ui.Initialize(chat, bootstrap, social);
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var type = typeof(LobbyChatUI);
                canvas = (GameObject)type.GetField("root", flags).GetValue(ui);
                var panel = (GameObject)type.GetField("panel", flags).GetValue(ui);
                var input = (UnityEngine.UI.InputField)type.GetField("input", flags).GetValue(ui);
                var history = (UnityEngine.UI.Text)type.GetField("history", flags).GetValue(ui);
                var friend = (UnityEngine.UI.Button)type.GetField("friend", flags).GetValue(ui);
                Require(input.characterLimit == 200 && !input.textComponent.supportRichText && !history.supportRichText,
                    "UI length and plain text");
                var preference = new ChatLocalVisibility();
                type.GetField("localVisibility", flags).SetValue(ui, preference);
                panel.SetActive(true);
                var receive = typeof(MirraLobbyChat).GetMethod("Receive", flags);
                receive.Invoke(chat, new object[] { new ChatMessageDto { ChannelId = "new-channel", SenderId = "sender-A", MessageId = "visible", Number = 1, Body = "visible-body" } });
                receive.Invoke(chat, new object[] { new ChatMessageDto { ChannelId = "new-channel", SenderId = "sender-B", MessageId = "hidden", Number = 2, Body = "hidden-body" } });
                type.GetMethod("Refresh", flags).Invoke(ui, null);
                Require(!friend.interactable, "unavailable social session cannot request from chat");
                type.GetMethod("RequestFriend", flags).Invoke(ui, null);
                Require(!social.Busy, "disabled chat friend action does not start HTTP");
                type.GetField("selectedAuthor", flags).SetValue(ui, "sender-B");
                type.GetMethod("ToggleVisibility", flags).Invoke(ui, null);
                Require(history.text.Contains("visible-body") && !history.text.Contains("hidden-body") && chat.Messages.Count == 2,
                    "local hide filters UI without changing transport history");
                ((System.Collections.IList)typeof(MirraLobbyChat).GetField("messages", flags).GetValue(chat)).Clear();
                type.GetMethod("Refresh", flags).Invoke(ui, null);
                Require((string)type.GetField("selectedAuthor", flags).GetValue(ui) == "sender-B", "hidden author selectable after buffer eviction");
                type.GetMethod("ToggleVisibility", flags).Invoke(ui, null);
                receive.Invoke(chat, new object[] { new ChatMessageDto { ChannelId = "new-channel", SenderId = "sender-B", MessageId = "restored", Number = 3, Body = "restored-body" } });
                Require(history.text.Contains("restored-body") && !preference.IsHidden("sender-B"), "unhide restores presentation");
                type.GetMethod("Update", flags).Invoke(ui, null);
                Require(!panel.activeSelf && !canvas.activeSelf, "offline transition closes and hides chat UI");
                type.GetMethod("SetOpen", flags).Invoke(ui, new object[] { true });
                Require(!panel.activeSelf, "offline player cannot open chat");
                Debug.Log("[Mirra chat UI validation] PASS: 200 characters/plain text, local hide/show and buffer eviction, bounded preferences, offline closes/rejects open. No gameplay or HTTP.");
            }
            finally
            {
                // OnDestroy normally owns this canvas; detach it for immediate EditMode cleanup.
                var ui = host.GetComponent<LobbyChatUI>();
                if (ui != null) typeof(LobbyChatUI).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ui, null);
                UnityEngine.Object.DestroyImmediate(host);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException("Mirra chat validation failed: " + check);
        }
    }
}
#endif
