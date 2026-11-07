using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedVictoryPresentationProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Run()
        {
            Require(!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer, "idle Editor");
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var active = typeof(SharedRunContext).GetField("<Active>k__BackingField", flags);
            var state = typeof(SharedRunContext).GetField("<State>k__BackingField", flags);
            object oldActive = active.GetValue(null), oldState = state.GetValue(null);
            var root = new GameObject("Temporary shared victory presentation");
            try
            {
                active.SetValue(null, true); state.SetValue(null, null);
                var ui = root.AddComponent<EndGameUIManager>();
                var enemies = root.AddComponent<SharedEnemiesRuntime>();
                typeof(EndGameUIManager).GetField("currentState", Private).SetValue(ui, EndGameState.Faint);
                // No UI managers/providers are installed: an early Win must return before touching any.
                ui.ShowEndGameUI(EndGameState.Win);
                Require(ui.GetState() == EndGameState.Faint && !SharedVictoryRewards.CanPresentVictory,
                    "unconfirmed shared Win preserves existing UI and cannot touch gameplay/reward providers");
                var uiCallback = (Action<EndGameState>)Delegate.CreateDelegate(typeof(Action<EndGameState>), ui,
                    typeof(EndGameUIManager).GetMethod("ShowEndGameUIAction", Private));
                var enemyCallback = (Action<EndGameState>)Delegate.CreateDelegate(typeof(Action<EndGameState>), enemies,
                    typeof(SharedEnemiesRuntime).GetMethod("OnEndGame", Private));
                var shown = typeof(SharedEnemiesRuntime).GetField("victoryShown", Private);
                foreach (bool uiFirst in new[] { true, false })
                {
                    shown.SetValue(enemies, true);
                    Action<EndGameState> handlers = uiFirst ? uiCallback + enemyCallback : enemyCallback + uiCallback;
                    handlers(EndGameState.Win);
                    Require(ui.GetState() == EndGameState.Faint && !(bool)shown.GetValue(enemies),
                        "both subscriber orders leave presentation pending before verdict");
                }
                Require(SharedVictoryRewards.Apply(20), "unconfirmed shared reward stays handled, never legacy solo fallback");
                active.SetValue(null, false);
                Require(SharedVictoryRewards.CanPresentVictory && !SharedVictoryRewards.Apply(20), "ordinary solo routing unchanged");
                enemyCallback(EndGameState.Win);
                Require((bool)shown.GetValue(enemies), "ordinary solo notification unchanged");
                typeof(EndGameUIManager).GetField("currentState", Private).SetValue(ui, EndGameState.Win);
                ui.ShowEndGameUI(EndGameState.Win);
                Require(ui.GetState() == EndGameState.Win, "ordinary solo duplicate UI returns without a reward provider");
                ui.ShowEndGameUI(EndGameState.Lose);
                uiCallback(EndGameState.Faint);
                Require(ui.GetState() == EndGameState.Win, "late death/faint cannot replace a presented victory");
                var faintTimer = (System.Collections.IEnumerator)typeof(EndGameUIManager).GetMethod("FaintStateTimer", Private).Invoke(ui, null);
                Require(!faintTimer.MoveNext() && ui.GetState() == EndGameState.Win, "obsolete faint timer cannot replace Win with Lose");
                typeof(EndGameUIManager).GetField("currentState", Private).SetValue(ui, EndGameState.None);
                var resultTimer = (System.Collections.IEnumerator)typeof(EndGameUIManager).GetMethod("WinLoseStateTimer", Private).Invoke(ui, null);
                Require(!resultTimer.MoveNext(), "obsolete result timer cannot exit from None");

                var wallet = new RunRewardWallet { Gems = 40, Wins = 2 };
                for (int player = 1; player <= 4; player++)
                    Require(wallet.Grant("shared-victory", player, 20) == RunRewardResult.Granted, "four independent receipts");
                wallet = JsonUtility.FromJson<RunRewardWallet>(JsonUtility.ToJson(wallet));
                for (int player = 4; player >= 1; player--)
                    Require(wallet.Grant("shared-victory", player, 999) == RunRewardResult.AlreadyGranted, "reload/fork/exit order cannot regrant or change amount");
                Require(wallet.Gems == 120 && wallet.Wins == 6 && wallet.Receipts.Count == 4, "independent four-player result");
                Debug.Log("[Victory presentation] PASS: pre-verdict direct/UI-event guards, both subscriber orders, shared no-fallback, solo duplicate routing, Win terminal against late death/faint, obsolete timers; four receipt keys + JSON repeat. No live Photon/master transfer/full UI/boss/account save/Play/Build");
                Debug.Log("[Victory presentation] Wallet regression: " + SharedRewardProbe.Run());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                active.SetValue(null, oldActive); state.SetValue(null, oldState);
            }
        }
    }
}
