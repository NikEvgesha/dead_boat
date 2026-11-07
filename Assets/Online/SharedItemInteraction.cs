using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedItemInteraction
    {
        // Recovery populates the bag while loading/paused; these are not player actions.
        public static bool Blocked => !SharedRecoveryInventory.Applying &&
            (SharedAdProtection.IsShowing || SharedLocalGameplay.Blocked);

        public static Vector3 ReleasePosition(Vector3 candidate, Vector3 actor)
        {
            // A claim can be acknowledged while an ad/menu blocks its local action.
            // After a long boat trip release near the owner, within the existing 20m RPC limit.
            if (float.IsNaN(candidate.x) || float.IsNaN(candidate.y) || float.IsNaN(candidate.z) ||
                float.IsInfinity(candidate.x) || float.IsInfinity(candidate.y) || float.IsInfinity(candidate.z) ||
                (candidate - actor).sqrMagnitude > 400)
                return actor + Vector3.up;
            return candidate;
        }
    }
}
