using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed partial class SharedDepartureState
    {
        // Survives Shared authority migration. Reliable per-sender ordering allows a high-water mark.
        [Networked, Capacity(4)] public NetworkArray<int> WeaponSequences => default;

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_WeaponDamage(ulong id, int damage, int sequence, RpcInfo info = default)
        {
            int slot = CrewSlot(info.Source);
            if (Phase != 3 || slot < 0 || !CanAct(info.Source) || damage <= 0 || damage > 10000 ||
                sequence <= WeaponSequences[slot]) return;
            WeaponSequences.Set(slot, sequence);
            if (!TryEnemy(id, out var record) || record.HP <= 0 || !record.Active ||
                !SharedItemsRuntime.TryFind(id, out var identity)) return;
            LobbyNetworkAvatar source = null;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == Runner && avatar.Object != null && avatar.Object.IsValid &&
                    avatar.Object.StateAuthority == info.Source) source = avatar;
            var enemy = identity.GetComponent<EnemyCore>();
            if (enemy == null || enemy.IsDead || source == null || source.Health <= 0 ||
                Vector3.Distance(source.LogicalPosition, record.Position) > 200) return;
            int before = Mathf.Max(0, enemy.SharedHP);
            SharedEnemiesRuntime.ApplyDamage(enemy, damage);
            int removed = Mathf.Clamp(before - enemy.SharedHP, 0, before);
            SampleEnemy(identity, enemy);
            if (removed > 0) source.RPC_ConfirmWeaponDamage(sequence, removed);
        }
    }
}
