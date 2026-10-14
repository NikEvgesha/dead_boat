using Fusion;

namespace DeadBoat.Online
{
    public sealed partial class SharedDepartureState
    {
        public const float RearFireDistance = 1000;
        public const int RearFireDamage = 10;
        [Networked, Capacity(4)] public NetworkArray<TickTimer> RearFireTimers => default;
        public float RearFireZ => BoatDistance - RearFireDistance;
        public bool InRearFire(float logicalZ) => BoatInitialized && Phase == 3 && !RunWon && logicalZ < RearFireZ;

        private void TickRearFire()
        {
            for (int slot = 0; slot < CrewCount; slot++)
            {
                LobbyNetworkAvatar target = null;
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Runner == Runner && avatar.Object.StateAuthority == Crew[slot]) { target = avatar; break; }
                bool present = false;
                foreach (var player in Runner.ActivePlayers) if (player == Crew[slot]) present = true;
                if (!present || target == null || target.Health <= 0 || !InRearFire(target.LogicalPosition.z) || AdProtected(Crew[slot]))
                {
                    RearFireTimers.Set(slot, default);
                    continue;
                }
                var timer = RearFireTimers[slot];
                if (timer.Expired(Runner)) target.RPC_ReceiveRearFireDamage();
                // At most one hit per tick, no accumulated damage burst after a stall.
                if (timer.ExpiredOrNotRunning(Runner))
                    RearFireTimers.Set(slot, TickTimer.CreateFromSeconds(Runner, 1));
            }
        }
    }
}
