using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedLocalGameplay
    {
        public static bool Blocked => SharedRunContext.Active && (!SharedRunContext.Playing ||
            SharedAdProtection.IsShowing || (PlayerStatsManager.Instance != null &&
            (PlayerStatsManager.Instance.Health <= 0 || !PlayerStatsManager.Instance.InGame)) || (PauseManager.Instance != null && PauseManager.Instance.IsPaused));
    }

}
