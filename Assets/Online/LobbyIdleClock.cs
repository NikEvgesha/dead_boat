using System;

namespace DeadBoat.Online
{
    // Unscaled monotonic time; no dependence on wall-clock changes or Time.timeScale.
    public sealed class LobbyIdleClock
    {
        private double lastActivity;
        private bool initialized, wasSuspended;

        public void Touch(double now)
        {
            lastActivity = now;
            initialized = true;
        }

        public void Observe(double now, bool activity, bool suspended)
        {
            if (!initialized || activity || suspended || wasSuspended || now < lastActivity)
                Touch(now);
            wasSuspended = suspended;
        }

        public int Remaining(double now, int timeout)
        {
            timeout = Math.Max(1, timeout);
            if (!initialized) return timeout;
            double elapsed = Math.Max(0, now - lastActivity);
            return (int)Math.Max(0, Math.Ceiling(timeout - elapsed));
        }
    }
}
