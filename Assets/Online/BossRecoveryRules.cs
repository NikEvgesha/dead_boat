namespace DeadBoat.Online
{
    internal enum TentacleRecoveryAction { Wait, Victory, ResumeActive, SelectNext }

    // Decisions use canonical evidence; local presentation alone cannot award victory.
    internal static class BossRecoveryRules
    {
        public static bool CanonicalDefeat(bool finalBoss, int canonicalHP, int canonicalMaximum) =>
            finalBoss && canonicalMaximum > 0 && canonicalHP <= 0;
        public static bool RecoverInactiveDragon(bool finalBoss, int canonicalHP, int canonicalMaximum, bool active) =>
            CanonicalDefeat(finalBoss, canonicalHP, canonicalMaximum) && !active;
    }

    internal struct TentacleRecoveryEvidence
    {
        private int parts, living;
        private bool unknown, active;
        public void Observe(bool known, int hp, int maximum, bool localDead, bool localActive)
        {
            parts++;
            if (!known || maximum <= 0 || hp > maximum) { unknown = true; return; }
            if (hp <= 0) return;
            living++;
            if (localDead) unknown = true;
            if (localActive) active = true;
        }
        public TentacleRecoveryAction Action => parts == 0 || unknown ? TentacleRecoveryAction.Wait :
            living == 0 ? TentacleRecoveryAction.Victory : active ? TentacleRecoveryAction.ResumeActive : TentacleRecoveryAction.SelectNext;
    }
}
