using Fusion;

namespace DeadBoat.Online
{
    public struct SharedAdLease : INetworkStruct
    {
        public long Token;
        public TickTimer Expires;
        public NetworkBool Showing;
    }

    public sealed partial class SharedDepartureState
    {
        public const float AdMaximumSeconds = 180;
        [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, SharedAdLease> Ads => default;
        public bool AdProtected(PlayerRef player) => Ads.TryGet(player, out var lease) &&
            !lease.Expires.ExpiredOrNotRunning(Runner);
        public bool AdShowing(PlayerRef player) => Ads.TryGet(player, out var lease) && lease.Showing &&
            !lease.Expires.ExpiredOrNotRunning(Runner);
        public bool CanAct(PlayerRef player) => Includes(player) && !AdShowing(player);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_AdBegin(long token, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || token == 0) return;
            if (Ads.TryGet(info.Source, out var previous))
            {
                // A duplicate request cannot extend a lease or resurrect a closed one.
                if (previous.Token == token || AdShowing(info.Source)) return;
            }
            var lease = new SharedAdLease { Token = token, Showing = true,
                Expires = TickTimer.CreateFromSeconds(Runner, AdMaximumSeconds) };
            if (Ads.ContainsKey(info.Source)) Ads.Set(info.Source, lease);
            else if (Ads.Count < Ads.Capacity) Ads.Add(info.Source, lease);
            else return;
            if (Driver == info.Source)
            {
                Driver = PlayerRef.None;
                DriverInput = 0;
                DriverInputDeadline = default;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_AdEnd(long token, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || !Ads.TryGet(info.Source, out var lease) ||
                lease.Token != token || !lease.Showing) return;
            lease.Showing = false;
            lease.Expires = TickTimer.CreateFromSeconds(Runner, 1);
            Ads.Set(info.Source, lease);
        }
    }
}
