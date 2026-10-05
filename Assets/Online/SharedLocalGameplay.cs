using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedLocalGameplay
    {
        public static bool Blocked => SharedRunContext.Active && (!SharedRunContext.Playing ||
            SharedAdProtection.IsShowing || (PlayerStatsManager.Instance != null &&
            (PlayerStatsManager.Instance.Health <= 0 || !PlayerStatsManager.Instance.InGame)) || (PauseManager.Instance != null && PauseManager.Instance.IsPaused));
    }

    public static class SharedAdProtection
    {
        private static bool showing;
        private static double deadline;
        public static bool IsShowing => showing && Time.realtimeSinceStartupAsDouble < deadline;
        private static double until;
        public static bool Protected => IsShowing || Time.realtimeSinceStartupAsDouble < until;
        public static void Begin() { showing = true; deadline = Time.realtimeSinceStartupAsDouble + 180; }
        public static void End()
        {
            if (!showing) return;
            showing = false;
            until = Time.realtimeSinceStartupAsDouble + 1;
        }
    }
}
