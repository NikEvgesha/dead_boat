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
        public bool TryEnemy(ulong id, out SharedEnemyRecord enemy)
        {
            var page = EnemyPage(id);
            if (page != null) return page.Enemies.TryGet(id, out enemy);
            enemy = default;
            return false;
        }

        public void SampleEnemy(WorldSpawnIdentity identity, EnemyCore enemy)
        {
            var page = EnemyPage(identity.Id) ?? (Object.HasStateAuthority ? AvailablePage(true) : null);
            if (page == null || !page.Object.HasStateAuthority || enemy.SharedMaxHP <= 0) return;
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
            if (page.Enemies.TryGet(identity.Id, out var previous))
            {
                record.HomeZ = previous.HomeZ; record.Prunable = previous.Prunable;
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
            enemy.HP = 0;
            page.Enemies.Set(id, enemy);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_EnemyBurn(ulong id, float duration, float damage, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || duration <= 0 || duration > 120 ||
                damage <= 0 || damage > 5000 || float.IsNaN(duration) || float.IsNaN(damage) ||
                !TryEnemy(id, out var record) || record.HP <= 0 || !record.Active) return;
            LobbyNetworkAvatar source = null;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == Runner && avatar.Object.StateAuthority == info.Source) source = avatar;
            if (source == null || Vector3.Distance(source.LogicalPosition, record.Position) > 200) return;
            record.BurnRemaining = duration; record.BurnDamage = damage; record.BurnAccumulator = 0;
            EnemyPage(id).Enemies.Set(id, record);
        }

        private void TickEnemyEffects()
        {
            foreach (var page in SharedWorldPage.All(Runner))
            {
                if (!page.Object.HasStateAuthority) continue;
                foreach (var entry in page.Enemies)
                {
                    var record = entry.Value;
                    if (record.HP <= 0 || record.BurnRemaining <= 0) continue;
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
            if (Phase != 3 || !Includes(info.Source) || damage <= 0 || damage > 5000 ||
                !TryEnemy(id, out var record) || record.HP <= 0 || !record.Active ||
                !SharedItemsRuntime.TryFind(id, out var identity)) return;
            var enemy = identity.GetComponent<EnemyCore>();
            LobbyNetworkAvatar source = null;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == Runner && avatar.Object.StateAuthority == info.Source) source = avatar;
            if (enemy == null || source == null || Vector3.Distance(source.LogicalPosition, record.Position) > 200) return;
            SharedEnemiesRuntime.ApplyDamage(enemy, damage);
            SampleEnemy(identity, enemy);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayerDamage(PlayerRef player, int damage)
        {
            if (player == Runner.LocalPlayer && PlayerStatsManager.Instance != null)
                PlayerStatsManager.Instance.TakeDamage(damage);
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
