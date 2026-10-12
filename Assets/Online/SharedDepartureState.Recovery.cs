using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed partial class SharedDepartureState
    {
        public const int RecoverySlots = 32;
        [Networked, Capacity(4)] public NetworkDictionary<PlayerRef, SharedPersonalData> Personal => default;
        [Networked, Capacity(128)] public NetworkDictionary<int, SharedRecoveryItem> PersonalItems => default;
        [Networked] public int AbandonedMask { get; private set; }
        private readonly Dictionary<PlayerRef, StagedPersonal> stagedPersonal = new();
        private sealed class StagedPersonal
        {
            public int Revision;
            public uint Mask;
            public readonly SharedRecoveryItem[] Items = new SharedRecoveryItem[RecoverySlots];
        }

        public int CrewSlot(PlayerRef player)
        {
            for (int i = 0; i < CrewCount; i++) if (Crew[i] == player) return i;
            return -1;
        }

        public bool CanReturn(PlayerRef player)
        {
            int slot = CrewSlot(player);
            return Phase == 3 && !RunWon && slot >= 0 && (AbandonedMask & (1 << slot)) == 0;
        }

        public bool HasOtherPlayingCrew(PlayerRef except)
        {
            foreach (var player in Runner.ActivePlayers)
                if (player != except && Includes(player) && Runner.TryGetPlayerObject(player, out var avatar) &&
                    avatar != null && avatar.IsValid) return true;
            return false;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RecoveryItem(int revision, int slot, SharedRecoveryItem value, RpcInfo info = default)
        {
            if (!CanReturn(info.Source) || revision < 1 || slot < 0 || slot >= RecoverySlots ||
                value.Template == 0 || SharedItemCatalog.Load()?.Find(value.Template) == null) return;
            if (!stagedPersonal.TryGetValue(info.Source, out var stage) || stage.Revision != revision)
            {
                if (stage != null && stage.Revision > revision) return;
                stagedPersonal[info.Source] = stage = new StagedPersonal { Revision = revision };
            }
            stage.Items[slot] = value;
            stage.Mask |= 1u << slot;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RecoveryCommit(SharedPersonalData data, RpcInfo info = default)
        {
            int crewSlot = CrewSlot(info.Source);
            if (!CanReturn(info.Source) || data.Revision < 1 || data.ItemCount < 0 ||
                data.ItemCount > RecoverySlots || float.IsNaN(data.Health) || float.IsInfinity(data.Health)) return;
            if (Personal.TryGet(info.Source, out var previous) && previous.Revision >= data.Revision) return;
            stagedPersonal.TryGetValue(info.Source, out var stage);
            uint expected = data.ItemCount == 32 ? uint.MaxValue : (1u << data.ItemCount) - 1;
            if (data.ItemCount > 0 && (stage == null || stage.Revision != data.Revision || stage.Mask != expected)) return;
            // Apply the complete inventory + header in one callback. An incomplete
            // transmission never replaces the last confirmed snapshot.
            for (int i = 0; i < RecoverySlots; i++)
            {
                int key = crewSlot * RecoverySlots + i;
                if (i < data.ItemCount)
                {
                    if (PersonalItems.ContainsKey(key)) PersonalItems.Set(key, stage.Items[i]);
                    else PersonalItems.Add(key, stage.Items[i]);
                }
                else PersonalItems.Remove(key);
            }
            if (Personal.ContainsKey(info.Source)) Personal.Set(info.Source, data);
            else Personal.Add(info.Source, data);
            stagedPersonal.Remove(info.Source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_AbandonRun(RpcInfo info = default)
        {
            int slot = CrewSlot(info.Source);
            if (slot >= 0) AbandonedMask |= 1 << slot;
        }
    }
}
