using System;
using System.Linq;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    // MasterClientObject: ownership follows the Shared master without losing the seed.
    public sealed partial class SharedDepartureState : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [Networked] public int Seed { get; private set; }
        [Networked] public int GenerationVersion { get; private set; }
        [Networked] public int LevelId { get; private set; }
        [Networked] public int TargetPlayers { get; private set; }
        [Networked] public TickTimer Countdown { get; private set; }
        [Networked] public int Phase { get; private set; } // 0 waiting, 1 countdown, 2 loading, 3 playing, 4 aborted
        [Networked] public int ReadyMask { get; private set; }
        [Networked] public TickTimer LoadingDeadline { get; private set; }
        [Networked] public int CrewCount { get; private set; }
        [Networked, Capacity(4)] public NetworkArray<PlayerRef> Crew => default;

        private bool heldRunAuthority;
        private bool membershipChanged;
        private int? changedCountdownTick;
        void IPlayerJoined.PlayerJoined(PlayerRef player) => NoteMembershipChange();
        void IPlayerLeft.PlayerLeft(PlayerRef player) => NoteMembershipChange();
        private void NoteMembershipChange()
        {
            // A leave and join can be delivered between two ticks, even reusing a slot.
            // Track the event as well as comparing the current PlayerRef set.
            if (Object != null && Object.IsValid && Phase == 1)
            {
                membershipChanged = true;
                changedCountdownTick = Countdown.TargetTick;
            }
        }
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
            // A proxy must not carry an old round's callback into a later master takeover.
            if (Phase != 1 || changedCountdownTick != Countdown.TargetTick) membershipChanged = false;
            bool gainedRunAuthority = Object.HasStateAuthority && !heldRunAuthority;
            heldRunAuthority = Object.HasStateAuthority;
            if (Object.HasStateAuthority && Phase == 3)
            {
                bool playingCrew = false;
                foreach (var player in Runner.ActivePlayers)
                    if (Includes(player) && Runner.TryGetPlayerObject(player, out var avatar) && avatar != null && avatar.IsValid)
                        playingCrew = true;
                if (!playingCrew) { Phase = 4; Runner.SessionInfo.IsOpen = false; return; }
                // Explicitly publish admission on takeover. The new master's local
                // SessionInfo cache is not confirmation of the current server property.
                if (gainedRunAuthority || Runner.SessionInfo.IsOpen == RunWon)
                    Runner.SessionInfo.IsOpen = !RunWon;
                TickBoat();
                TickRearFire();
            }
            if (!Object.HasStateAuthority || TargetPlayers == 0 || Phase >= 3) return;
            if (Phase == 2)
            {
                // The sealed crew must reach the scene together. Never turn a failed
                // shared launch into independent solo runs.
                if (!MembershipMatches() || LoadingDeadline.Expired(Runner))
                    Phase = 4;
                else if (ReadyMask == (1 << CrewCount) - 1)
                    Phase = 3;
                return;
            }
            int count = Runner.ActivePlayers.Count();
            if (Phase == 1 && (membershipChanged || !MembershipMatches()))
                CancelCountdown();
            if (Phase == 0 && count >= TargetPlayers) StartCountdown();
            if (Phase == 1 && Countdown.Expired(Runner))
            {
                // Closing before the roster snapshot blocks further matchmaking joins.
                Runner.SessionInfo.IsOpen = false;
                Runner.SessionInfo.IsVisible = false;
                var members = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).Take(4).ToArray();
                CrewCount = members.Length;
                for (int i = 0; i < members.Length; i++) Crew.Set(i, members[i]);
                ReadyMask = 0;
                LoadingDeadline = TickTimer.CreateFromSeconds(Runner, 180);
                Phase = 2;
            }
        }

        public void LaunchNow()
        {
            if (Object.HasStateAuthority && Phase == 0 && Runner.ActivePlayers.Any())
                StartCountdown();
        }

        private void CancelCountdown()
        {
            Phase = 0;
            Countdown = default;
            CrewCount = ReadyMask = 0;
            LoadingDeadline = default;
            for (int i = 0; i < Crew.Length; i++) Crew.Set(i, PlayerRef.None);
            membershipChanged = false;
        }

        private void StartCountdown()
        {
            membershipChanged = false;
            var members = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).Take(4).ToArray();
            CrewCount = members.Length;
            for (int i = 0; i < members.Length; i++) Crew.Set(i, members[i]);
            Countdown = TickTimer.CreateFromSeconds(Runner, 5);
            Phase = 1;
        }

        public bool Includes(PlayerRef player)
        {
            for (int i = 0; i < CrewCount; i++)
                if (Crew[i] == player) return Phase != 3 || (AbandonedMask & (1 << i)) == 0;
            return false;
        }

        public bool IsReady(PlayerRef player)
        {
            for (int i = 0; i < CrewCount; i++)
                if (Crew[i] == player) return (ReadyMask & (1 << i)) != 0;
            return false;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SceneReady(RpcInfo info = default)
        {
            if (Phase != 2) return;
            for (int i = 0; i < CrewCount; i++)
                if (Crew[i] == info.Source)
                {
                    ReadyMask |= 1 << i;
                    return;
                }
        }
    }
}
