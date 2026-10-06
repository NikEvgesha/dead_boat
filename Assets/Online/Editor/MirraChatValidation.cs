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
                for (int i = 3; i < 70; i++)
                    add(new ChatMessageDto { ChannelId = "probe", MessageId = "m" + i, Number = i, CreatedAt = date });
                Require(chat.Messages.Count == 50 && chat.Messages[0].Number == 20 && chat.Messages[49].Number == 69, "bounded recent history");
                Debug.Log("[Mirra chat validation] PASS: ordering, deduplication, edit/delete races, isolation, 50-message cap.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException("Mirra chat validation failed: " + check);
        }
    }
}
#endif
