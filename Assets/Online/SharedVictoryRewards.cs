using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedVictoryRewards
    {
        public static bool CanPresentVictory
        {
            get
            {
                if (!SharedRunContext.Active) return true;
                var state = SharedRunContext.State;
                return SharedRunContext.Playing && state != null && state.RunWon && state.Includes(state.Runner.LocalPlayer);
            }
        }

        // Returns true for ALL shared-origin runs, including a rejected grant:
        // callers must never fall through to the legacy solo reward path.
        public static bool Apply(int reward)
        {
            string run;
            long player;
            if (SharedRunContext.Active)
            {
                if (!SharedRunContext.Playing) return true;
                var state = SharedRunContext.State;
                if (state == null || state.Object == null || !state.Object.IsValid) return true;
                // Granting a reward must never manufacture the authoritative result.
                if (!state.RunWon || !state.Includes(state.Runner.LocalPlayer)) return true;
                run = state.Runner.SessionInfo.Name;
                player = SharedRunRecovery.LocalId;
            }
            else if (SoloRunBranch.Active)
            {
                run = SoloRunBranch.SourceRun;
                player = SoloRunBranch.SourcePlayerId;
            }
            else return false;

            var save = SaveManager.Instance.PersistentProvider;
            RunRewardResult result;
            try { result = save.GrantRunReward(run, player, reward); }
            catch (System.Exception e)
            {
                Debug.LogError("[Co-op reward] Grant failed: " + e.GetType().Name);
                return true;
            }
            if (result != RunRewardResult.Granted && result != RunRewardResult.AlreadyGranted)
            {
                Debug.LogError("[Co-op reward] Persistent grant unavailable: " + result);
                return true;
            }
            int gems = save.LoadGems();
            SharedRunContext.Save?.SaveGems(gems);
            CurrencyManager.Instance.RefreshPersistentGems(gems);
            // Flush the persistent provider explicitly; the online save is temporary.
            save.SaveProgress();
            if (result == RunRewardResult.Granted && !(save is SharedRunPreviewSave) && LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.SaveScore(LBName.gems.ToString(), gems);
                LeaderboardManager.Instance.SaveScore(LBName.wins.ToString(), save.LoadWins());
            }
            return true;
        }
    }
}
