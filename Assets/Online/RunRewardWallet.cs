using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeadBoat.Online
{
    public enum RunRewardResult { Unsupported, NotReady, Granted, AlreadyGranted, Invalid, RequiresServer }

    // Balance and receipts travel in ONE platform-save value. This is client
    // idempotency, not a server verdict or a cross-device compare-and-swap.
    [Serializable]
    public sealed class RunRewardWallet
    {
        public int Version = 1;
        public int Gems;
        public int Wins;
        public List<string> Receipts = new();

        public static string Key(string run, long player)
        {
            if (string.IsNullOrEmpty(run) || run.Length > 256 || player == 0) return null;
            return run.Length.ToString(CultureInfo.InvariantCulture) + ":" + run + ":" +
                player.ToString(CultureInfo.InvariantCulture);
        }

        public RunRewardResult Grant(string run, long player, int reward)
        {
            string key = Key(run, player);
            if (Version != 1 || key == null || reward < 0 || Gems < 0 || Wins < 0)
                return RunRewardResult.Invalid;
            if (Receipts == null) Receipts = new List<string>();
            if (Receipts.Contains(key)) return RunRewardResult.AlreadyGranted;
            // Validate before modifying any part of the wallet.
            if ((long)Gems + reward > int.MaxValue || Wins == int.MaxValue) return RunRewardResult.Invalid;
            Gems += reward;
            Wins++;
            Receipts.Add(key); // Do not prune: old forks must remain idempotent.
            return RunRewardResult.Granted;
        }
    }
}
