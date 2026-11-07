using System;

namespace DeadBoat.Online
{
    // Local readiness only; never serialized or used as a network authority grant.
    internal sealed class EnemyAuthorityEpoch
    {
        private object context;
        private int owner;
        private bool ready, preparing;
        public long Generation { get; private set; }
        public bool IsReady(object value, int authority) => ready && ReferenceEquals(context, value) && owner == authority;
        public bool IsCurrent(long generation) => ready && Generation == generation;
        public void Invalidate()
        {
            if (context == null && !ready && !preparing) return;
            context = null; ready = false; Generation++;
        }
        public bool TryPrepare(object value, int authority, Func<bool> restore, Action resume)
        {
            if (value == null) { Invalidate(); return false; }
            if (IsReady(value, authority)) return true;
            if (preparing)
            {
                if (!ReferenceEquals(context, value) || owner != authority) Invalidate();
                return false;
            }
            context = value; owner = authority; ready = false; preparing = true;
            long attempt = ++Generation;
            try
            {
                if (!restore() || Generation != attempt) return false;
                // Resume may query the same gate (e.g. starting an attack coroutine).
                ready = true;
                resume();
                return Generation == attempt && ready;
            }
            catch { ready = false; throw; }
            finally { preparing = false; }
        }
    }
}
