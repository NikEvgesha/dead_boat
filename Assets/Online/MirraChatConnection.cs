using System;
using System.Threading.Tasks;
using MirraCloud.Core.Chats;
using MirraCloud.Core.Realtime.Protocol;
using UnityEngine;

namespace DeadBoat.Online
{
    internal static class MirraChatConnection
    {
        // SDK 0.10.0 can finish its inner WebGL close synchronously before attaching
        // the completion callback. Use the public connection state as evidence of close.
        internal static async Task DisconnectAsync(ChatsService chats)
        {
            var operation = chats.DisconnectAsync();
            float deadline = Time.realtimeSinceStartup + 5;
            while (!operation.IsDone && chats.ConnectionState != RealtimeConnectionState.Disconnected)
            {
                if (Time.realtimeSinceStartup >= deadline) throw new TimeoutException("Chat disconnect timed out");
                await Task.Yield();
            }
            if (chats.ConnectionState == RealtimeConnectionState.Disconnected) return;
            await operation.Task();
            if (!operation.Result.IsSuccess) throw new InvalidOperationException("Chat disconnect failed");
        }
    }
}
