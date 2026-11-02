using UnityEngine;

namespace DeadBoat.Online
{
    // Path planning cadence only: targeting, melee checks and damage still run each frame.
    public struct SharedEnemyPathSchedule
    {
        private bool initialized;
        private int target;
        private Vector3 goal, origin;
        private float next;

        public bool ShouldRefresh(int targetId, Vector3 position, Vector3 worldOrigin, float now)
        {
            if (initialized && target == targetId && origin == worldOrigin &&
                now < next && (position - goal).sqrMagnitude < 4f) return false;
            initialized = true;
            target = targetId; goal = position; origin = worldOrigin;
            next = now + 0.2f;
            return true;
        }

        public void Reset() => initialized = false;
    }
}
