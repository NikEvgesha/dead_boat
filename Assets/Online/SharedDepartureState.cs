using System;
using System.Linq;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    // MasterClientObject: ownership follows the Shared master without losing the seed.
    public sealed class SharedDepartureState : NetworkBehaviour
    {
        [Networked] public int Seed { get; private set; }
        [Networked] public int GenerationVersion { get; private set; }
        [Networked] public int LevelId { get; private set; }
        [Networked] public int TargetPlayers { get; private set; }
        [Networked] public TickTimer Countdown { get; private set; }
        [Networked] public int Phase { get; private set; } // 0 waiting, 1 countdown, 2 loading
        [Networked] public int CrewCount { get; private set; }
        [Networked, Capacity(4)] public NetworkArray<PlayerRef> Crew => default;

        public override void Spawned() => Runner.MakeDontDestroyOnLoad(gameObject);

        public void Initialize(int levelId, int targetPlayers)
        {
            // Called by the creating master inside OnBeforeSpawned, before attachment.
            Seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
            GenerationVersion = RunRandom.Version;
            LevelId = levelId;
            TargetPlayers = targetPlayers;
        }

        private bool MembershipMatches()
        {
            int count = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                if (!Includes(player)) return false;
                count++;
            }
            return count == CrewCount;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || TargetPlayers == 0 || Phase == 2) return;
            int count = Runner.ActivePlayers.Count();
            if (Phase == 1 && !MembershipMatches())
            {
                Phase = 0;
                Countdown = default;
            }
            if (Phase == 0 && count >= TargetPlayers) StartCountdown();
            if (Phase == 1 && Countdown.Expired(Runner))
            {
                // Closing before the roster snapshot blocks further matchmaking joins.
                Runner.SessionInfo.IsOpen = false;
                Runner.SessionInfo.IsVisible = false;
                var members = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).Take(4).ToArray();
                CrewCount = members.Length;
                for (int i = 0; i < members.Length; i++) Crew.Set(i, members[i]);
                Phase = 2;
            }
        }

        public void LaunchNow()
        {
            if (Object.HasStateAuthority && Phase == 0 && Runner.ActivePlayers.Any())
                StartCountdown();
        }

        private void StartCountdown()
        {
            var members = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).Take(4).ToArray();
            CrewCount = members.Length;
            for (int i = 0; i < members.Length; i++) Crew.Set(i, members[i]);
            Countdown = TickTimer.CreateFromSeconds(Runner, 5);
            Phase = 1;
        }

        public bool Includes(PlayerRef player)
        {
            for (int i = 0; i < CrewCount; i++) if (Crew[i] == player) return true;
            return false;
        }
    }
}
