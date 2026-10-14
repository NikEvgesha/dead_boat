using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedAdProtection
    {
        private static bool showing;
        private static double deadline;
        private static double until;
        private static long token;
        private static SharedDepartureState run;
        public static bool IsShowing => showing && Time.realtimeSinceStartupAsDouble < deadline;
        public static bool Protected => IsShowing || Time.realtimeSinceStartupAsDouble < until;
        public static void Begin()
        {
            showing = true;
            deadline = Time.realtimeSinceStartupAsDouble + SharedDepartureState.AdMaximumSeconds;
        }
        public static async Task<bool> PrepareAsync()
        {
            Begin();
            run = null;
            token = 0;
            if (!SharedRunContext.Active) return true;
            if (!SharedRunContext.Playing) return false;
            var expected = SharedRunContext.State;
            run = expected;
            token = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
            if (token == 0) token = 1;
            long expectedToken = token;
            expected.RPC_AdBegin(expectedToken);
            double end = Time.realtimeSinceStartupAsDouble + 5;
            while (showing && token == expectedToken && SharedRunContext.Playing && SharedRunContext.State == expected &&
                Time.realtimeSinceStartupAsDouble < end)
            {
                if (expected.Ads.TryGet(expected.Runner.LocalPlayer, out var lease) &&
                    lease.Token == expectedToken && expected.AdShowing(expected.Runner.LocalPlayer)) return true;
                await Awaitable.NextFrameAsync();
            }
            return false;
        }
        public static void End()
        {
            if (!showing) return;
            showing = false;
            until = Time.realtimeSinceStartupAsDouble + 1;
            var previous = run;
            var previousToken = token;
            run = null;
            token = 0;
            if (previous != null && previous.Object != null && previous.Object.IsValid &&
                previous.Runner.IsRunning && previous.Runner.IsConnectedToServer && previousToken != 0)
                previous.RPC_AdEnd(previousToken);
        }
    }
}
