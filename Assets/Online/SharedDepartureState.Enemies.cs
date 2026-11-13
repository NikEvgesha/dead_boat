using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public struct SharedEnemyRecord : INetworkStruct
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public int HP, MaxHP, Animation;
        public float AnimationTime;
        public NetworkBool Active;
        public ulong Template;
        public int Level;
        public float BurnRemaining, BurnDamage, BurnAccumulator;
        public float HomeZ;
        public NetworkBool Prunable;
        // Reserved ambush slots are canonical before their actor is released.
        public NetworkBool PendingSpawn;
        public ulong ActionId;
    }

    public sealed partial class SharedDepartureState
    {
        [Networked] public NetworkBool RunWon { get; private set; }
        [Networked] public int NightSpawnSequence { get; private set; }
        public int NextNightSpawn() => ++NightSpawnSequence;
        public void MarkWon() { if (Object.HasStateAuthority && Phase == 3) RunWon = true; }

        private readonly System.Collections.Generic.Dictionary<ulong, SharedWorldPage> enemyLocations = new();

        private SharedWorldPage EnemyPage(ulong id)
        {
            if (enemyLocations.TryGetValue(id, out var cached) && cached != null && cached.Object != null &&
                cached.Object.IsValid && cached.Enemies.ContainsKey(id)) return cached;
            enemyLocations.Remove(id);
            foreach (var p in SharedWorldPage.All(Runner)) if (p.Enemies.ContainsKey(id))
            {
                if (enemyLocations.Count >= 4096) enemyLocations.Clear();
                enemyLocations[id] = p;
                return p;
            }
            return null;
        }
        private bool HasEnemyAuthority(ulong id)
        {
            var page = EnemyPage(id);
            return page != null && page.Object.HasStateAuthority && !SharedEnemiesRuntime.IsEnemyRecoveryPending(id);
        }

        public bool TryEnemy(ulong id, out SharedEnemyRecord enemy)
        {
            var page = EnemyPage(id);
            if (page != null) return page.Enemies.TryGet(id, out enemy);
            enemy = default;
            return false;
        }

        public void SampleEnemy(WorldSpawnIdentity identity, EnemyCore enemy)
        {
            if (!SharedEnemiesRuntime.EnsureAuthority(this)) return;
            // Do not publish stale local HP over a deferred boss's known canonical record.
            // A truly absent record may still use the ordinary initial registration path.
            if (SharedEnemiesRuntime.IsEnemyRecoveryPending(identity.Id) && TryEnemy(identity.Id, out _)) return;
            var page = EnemyPage(identity.Id) ?? (Object.HasStateAuthority ? AvailablePage(true) : null);
            if (page == null || !page.Object.HasStateAuthority || enemy.SharedMaxHP <= 0) return;
            bool existing = page.Enemies.TryGet(identity.Id, out var previous);
            if (existing && previous.PendingSpawn && (enemy.IsDead || enemy.SharedHP <= 0 ||
                identity.TemplateId != previous.Template ||
                (enemy is LevelledEnemy reservedLevel && reservedLevel.mobLevel != previous.Level) ||
                enemy.SharedMaxHP != previous.MaxHP)) return;
            if (enemy is DragonBossController dragon) DragonBrain = dragon.CaptureSharedBrain(identity.Id);
            var animator = identity.Animator;
            var animation = animator != null && animator.isActiveAndEnabled ? animator.GetCurrentAnimatorStateInfo(0) : default;
            var record = new SharedEnemyRecord
            {
                HomeZ = identity.transform.position.z + LobbyNetworkAvatar.Origin.z, Prunable = identity.Prunable,
                Position = enemy.transform.position + LobbyNetworkAvatar.Origin,
                Rotation = enemy.transform.rotation, HP = enemy.SharedHP, MaxHP = enemy.SharedMaxHP,
                Animation = animation.fullPathHash, AnimationTime = animation.normalizedTime,
                Active = !(enemy is TentaclePart tentacle) || tentacle.Active
                ,Template = identity.TemplateId, Level = enemy is LevelledEnemy levelled ? levelled.mobLevel : 1
            };
            if (existing)
            {
                record.HomeZ = previous.HomeZ; record.Prunable = previous.Prunable;
                record.ActionId = previous.ActionId;
                // Only the initialized living actor above can turn a reservation into a release.
                record.PendingSpawn = false;
                record.BurnRemaining = previous.BurnRemaining; record.BurnDamage = previous.BurnDamage;
                record.BurnAccumulator = previous.BurnAccumulator;
                page.Enemies.Set(identity.Id, record);
            }
            else if (page.Enemies.Count < page.Enemies.Capacity) page.Enemies.Add(identity.Id, record);
            else Debug.LogWarning("[Shared enemies] Snapshot page full; enemy not registered.");
        }

        public void MarkEnemyDead(ulong id)
        {
            var page = EnemyPage(id);
            if (page == null || !page.Object.HasStateAuthority || !page.Enemies.TryGet(id, out var enemy)) return;
            if (enemy.PendingSpawn || (SharedEnemiesRuntime.IsEnemyRecoveryPending(id) && enemy.HP > 0)) return;
            enemy.HP = 0;
            page.Enemies.Set(id, enemy);
        }

        private LobbyNetworkAvatar LivingCombatAvatar(PlayerRef player)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar != null && avatar.Object != null && avatar.Object.IsValid && avatar.Runner == Runner &&
                    avatar.Object.StateAuthority == player && avatar.Health > 0) return avatar;
            return null;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_EnemyBurn(ulong id, float duration, float damage, RpcInfo info = default)
        {
            if (Phase != 3 || !CanAct(info.Source) || !SharedEnemiesRuntime.EnsureAuthority(this) || duration <= 0 || duration > 120 ||
                damage <= 0 || damage > 5000 || float.IsNaN(duration) || float.IsNaN(damage) ||
                !TryEnemy(id, out var record) || record.PendingSpawn || !HasEnemyAuthority(id) || record.HP <= 0 || !record.Active) return;
            var source = LivingCombatAvatar(info.Source);
            if (source == null || Vector3.Distance(source.LogicalPosition, record.Position) > 200) return;
            record.BurnRemaining = duration; record.BurnDamage = damage; record.BurnAccumulator = 0;
            EnemyPage(id).Enemies.Set(id, record);
        }

        private void TickEnemyEffects()
        {
            if (!SharedEnemiesRuntime.EnsureAuthority(this)) return;
            foreach (var page in SharedWorldPage.All(Runner))
            {
                if (!page.Object.HasStateAuthority) continue;
                foreach (var entry in page.Enemies)
                {
                    var record = entry.Value;
                    if (SharedEnemiesRuntime.IsEnemyRecoveryPending(entry.Key)) continue;
                    if (record.PendingSpawn || record.HP <= 0 || record.BurnRemaining <= 0) continue;
                    float delta = Mathf.Min(Runner.DeltaTime, record.BurnRemaining);
                    record.BurnRemaining -= delta;
                    record.BurnAccumulator += delta * record.BurnDamage;
                    int damage = Mathf.FloorToInt(record.BurnAccumulator);
                    if (damage > 0 && SharedItemsRuntime.TryFind(entry.Key, out var identity) && identity != null)
                    {
                        var enemy = identity.GetComponent<EnemyCore>();
                        if (enemy != null)
                        {
                            record.BurnAccumulator -= damage;
                            SharedEnemiesRuntime.ApplyDamage(enemy, damage);
                            record.HP = enemy.SharedHP;
                        }
                    }
                    page.Enemies.Set(entry.Key, record);
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_EnemyDamage(ulong id, int damage, RpcInfo info = default)
        {
            if (Phase != 3 || !CanAct(info.Source) || !SharedEnemiesRuntime.EnsureAuthority(this) || damage <= 0 || damage > 5000 ||
                !TryEnemy(id, out var record) || record.PendingSpawn || !HasEnemyAuthority(id) || record.HP <= 0 || !record.Active ||
                !SharedItemsRuntime.TryFind(id, out var identity)) return;
            var enemy = identity.GetComponent<EnemyCore>();
            var source = LivingCombatAvatar(info.Source);
            if (enemy == null || enemy.IsDead || source == null || Vector3.Distance(source.LogicalPosition, record.Position) > 200) return;
            SharedEnemiesRuntime.ApplyDamage(enemy, damage);
            SampleEnemy(identity, enemy);
        }

        // Health belongs to this player's avatar owner, never to the boat/master.
        public void DamagePlayer(PlayerRef player, int damage)
        {
            if (!Object.HasStateAuthority || Phase != 3 || !Includes(player) || AdProtected(player) || damage <= 0 ||
                !SharedEnemiesRuntime.EnsureAuthority(this)) return;
            var avatar = LivingCombatAvatar(player);
            if (avatar != null) avatar.RPC_ReceiveDamage(damage);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ShowFireball(Vector3 origin, Vector3 target)
        {
            if (Object.HasStateAuthority) return;
            var dragon = FindAnyObjectByType<DragonBossController>();
            if (dragon == null || dragon.fireProjectile == null) return;
            var projectile = Instantiate(dragon.fireProjectile, origin - LobbyNetworkAvatar.Origin, Quaternion.identity);
            projectile.SetTarget(target - LobbyNetworkAvatar.Origin, false);
        }
    }
}
