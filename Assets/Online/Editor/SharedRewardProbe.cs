using System;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedRewardProbe
    {
        public static string Result { get; private set; }
        [MenuItem("Tools/Online/Verify Persistent Reward Wallet")]
        public static string Run()
        {
            var wallet = new RunRewardWallet { Gems = 40, Wins = 3 };
            Check(wallet.Grant("room-original", long.MaxValue, 12) == RunRewardResult.Granted, "first win");
            Check(wallet.Gems == 52 && wallet.Wins == 4, "balance and win");
            // Simulate a fresh client, not a static once-per-scene guard.
            wallet = JsonUtility.FromJson<RunRewardWallet>(JsonUtility.ToJson(wallet));
            Check(wallet.Grant("room-original", long.MaxValue, 999) == RunRewardResult.AlreadyGranted,
                "reconnect/relaunch/fork cannot change the original reward");
            wallet.Gems += 5; // A later purchase/reward must survive duplicate delivery.
            Check(wallet.Grant("room-original", long.MaxValue, 12) == RunRewardResult.AlreadyGranted && wallet.Gems == 57,
                "duplicate does not overwrite later balance");
            Check(wallet.Grant("room-original", long.MinValue, 12) == RunRewardResult.Granted, "another crew member");
            Check(wallet.Grant("room-next", long.MaxValue, 0) == RunRewardResult.Granted && wallet.Wins == 6,
                "distinct run and zero-gem victory");
            int count = wallet.Receipts.Count;
            Check(wallet.Grant(null, 1, 2) == RunRewardResult.Invalid &&
                wallet.Grant("room", 0, 2) == RunRewardResult.Invalid &&
                wallet.Grant("room", 1, -2) == RunRewardResult.Invalid && wallet.Receipts.Count == count, "invalid grant");
            wallet.Gems = int.MaxValue;
            Check(wallet.Grant("overflow", 1, 1) == RunRewardResult.Invalid && wallet.Receipts.Count == count,
                "overflow cannot partially commit");
            wallet.Version = 99;
            Check(wallet.Grant("future", 1, 0) == RunRewardResult.Invalid, "unknown version fails closed");
            Check(RunRewardWallet.Key("a:1", 2) != RunRewardWallet.Key("a", 12), "unambiguous identity");
            Result = "PASS: JSON reload, duplicate/fork, later balance, distinct player/run, zero reward, invalid input, overflow/version";
            Debug.Log("[Reward probe] " + Result);
            return Result;
        }
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Reward probe: " + name);
        }
    }
}
