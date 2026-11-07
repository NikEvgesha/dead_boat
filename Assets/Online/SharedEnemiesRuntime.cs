using UnityEngine;
using UnityEngine.AI;

namespace DeadBoat.Online
{
    public sealed class SharedEnemiesRuntime : MonoBehaviour
    {
        private static readonly EnemyAuthorityEpoch authorityEpoch = new();
        private static readonly System.Collections.Generic.List<WorldSpawnIdentity> authoritySnapshot = new();
        private static bool applyingDamage;
        public static bool ApplyingDamage => applyingDamage;
        private readonly System.Collections.Generic.List<WorldSpawnIdentity> snapshot = new();
        private double nextSample;
        private bool victoryShown;
        private bool wasAuthority;
        private int observedRevision = -1;
        private SharedDepartureState observedState;
        internal static readonly Unity.Profiling.ProfilerMarker UpdateMarker = new("DeadBoat.SharedEnemies");
        private static readonly System.Collections.Generic.Dictionary<ulong, ZombieController> templates = new();
        private static readonly System.Collections.Generic.Dictionary<DragonBossController,
            System.Collections.Generic.HashSet<Fusion.PlayerRef>> diveVictims = new();
        public static void Clear() { templates.Clear(); diveVictims.Clear(); authorityEpoch.Invalidate(); authoritySnapshot.Clear(); }
        public static bool TryTemplate(ulong id, out ZombieController prefab) => templates.TryGetValue(id, out prefab);
        public static int DiveMask(DragonBossController dragon)
        {
            if (!diveVictims.TryGetValue(dragon, out var hit) || !SharedRunContext.Playing) return 0;
            int mask = 0;
            var state = SharedRunContext.State;
            for (int i = 0; i < state.CrewCount; i++) if (hit.Contains(state.Crew[i])) mask |= 1 << i;
            return mask;
        }
        public static void RestoreDiveMask(DragonBossController dragon, int mask)
        {
            if (!SharedRunContext.Playing) return;
            if (!diveVictims.TryGetValue(dragon, out var hit)) diveVictims[dragon] = hit = new();
            hit.Clear();
            var state = SharedRunContext.State;
            for (int i = 0; i < state.CrewCount; i++) if ((mask & (1 << i)) != 0) hit.Add(state.Crew[i]);
        }
        public static void StartDive(DragonBossController dragon) => diveVictims[dragon] = new();
        public static void DiveAttack(DragonBossController dragon)
        {
            if (!SharedRunContext.Playing || !Authority) return;
            if (!diveVictims.TryGetValue(dragon, out var hit)) diveVictims[dragon] = hit = new();
            var state = SharedRunContext.State;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (AvailableTarget(state, avatar) &&
                    Vector3.Distance(avatar.LogicalPosition - LobbyNetworkAvatar.Origin + Vector3.up,
                        dragon.transform.position) <= 3 && hit.Add(avatar.Object.StateAuthority))
                    state.DamagePlayer(avatar.Object.StateAuthority, (int)dragon.diveDamage);
        }
        public static ulong RegisterTemplate(ZombieController prefab)
        {
            ulong id = 14695981039346656037UL;
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(prefab.name)) id = (id ^ b) * 1099511628211UL;
            templates[id] = prefab;
            return id;
        }
        public static bool Authority => !SharedRunContext.Active || EnsureAuthority(SharedRunContext.State);

        // Entry used by AI, RPCs, burn and publishing. Root authority alone is not readiness.
        internal static bool EnsureAuthority(SharedDepartureState state)
        {
            if (!SharedRunContext.Playing)
            {
                authorityEpoch.Invalidate();
                return false;
            }
            // A late RPC from another run must not invalidate this run's prepared state.
            if (state == null || state != SharedRunContext.State) return false;
            if (!state.Object.HasStateAuthority) { authorityEpoch.Invalidate(); return false; }
            int owner = state.Object.StateAuthority.RawEncoded;
            if (authorityEpoch.IsReady(state, owner)) return true;
            return PrepareAuthoritySlow(state, owner);
        }
        // Keep captured delegates off the steady-state per-actor Update path.
        private static bool PrepareAuthoritySlow(SharedDepartureState state, int owner) =>
            authorityEpoch.TryPrepare(state, owner, () => RestoreAuthorityWorld(state), () => ResumeAuthorityWorld(state));

        private static bool RestoreAuthorityWorld(SharedDepartureState state)
        {
            // Shared world pages transfer independently of the departure root.
            if (SharedWorldPage.Get(state.Runner, 0) == null) return false;
            foreach (var page in SharedWorldPage.All(state.Runner))
                if (page.Enemies.Count > 0 && !page.Object.HasStateAuthority) return false;
            SharedItemsRuntime.CopyEnemiesTo(authoritySnapshot);
            for (int i = 0; i < authoritySnapshot.Count; i++)
            {
                var identity = authoritySnapshot[i];
                if (identity == null || identity.Enemy == null || !state.TryEnemy(identity.Id, out var record)) continue;
                identity.ResetEnemyProxy();
                identity.SuspendEnemyProxyAI(true);
                identity.transform.SetPositionAndRotation(record.Position - LobbyNetworkAvatar.Origin, record.Rotation);
                identity.ApplyEnemyProxy(record, false);
                if (identity.Enemy is DragonBossController dragon && state.DragonBrain.Id == identity.Id)
                    dragon.ApplySharedBrain(state.DragonBrain);
            }
            return true;
        }

        private static void ResumeAuthorityWorld(SharedDepartureState state)
        {
            foreach (var boss in Object.FindObjectsByType<TentacleBoss>(FindObjectsSortMode.None))
            {
                var parts = boss.GetComponentsInChildren<TentaclePart>(true);
                bool known = parts.Length > 0, active = false;
                int alive = 0;
                foreach (var part in parts)
                {
                    var identity = part.GetComponent<WorldSpawnIdentity>();
                    if (identity == null || !state.TryEnemy(identity.Id, out var record)) { known = false; break; }
                    if (record.HP <= 0) continue;
                    alive++;
                    if (part.IsDead) { known = false; break; }
                    if (part.Active) active = true;
                }
                // Fresh spawns without canonical records keep their ordinary StartBossFight path.
                if (!known) continue;
                if (alive == 0) { state.MarkWon(); continue; }
                if (active)
                {
                    foreach (var part in parts) if (!part.IsDead && part.Active) part.SetActive();
                }
                else boss.ChangeTentacle(null);
            }
        }

        public static bool RequestBurn(EnemyCore enemy, float duration, float damage)
        {
            if (!SharedRunContext.Active) return false;
            if (SharedLocalGameplay.Blocked) return true;
            var identity = enemy.GetComponent<WorldSpawnIdentity>();
            if (SharedRunContext.Playing && identity != null)
                SharedRunContext.State.RPC_EnemyBurn(identity.Id, duration, damage);
            return true;
        }

        public static bool RequestDamage(EnemyCore enemy, int damage)
        {
            if (!SharedRunContext.Active || applyingDamage) return false;
            if (SharedLocalGameplay.Blocked) return true;
            var identity = enemy.GetComponent<WorldSpawnIdentity>();
            if (SharedRunContext.Playing && identity != null)
                SharedRunContext.State.RPC_EnemyDamage(identity.Id, damage);
            return true;
        }

        public static void ApplyDamage(EnemyCore enemy, int damage)
        {
            applyingDamage = true;
            try { enemy.TakeDamage(damage); }
            finally { applyingDamage = false; }
        }

        // Ignore abandoned, invalid, dead and ad-protected representations.
        // This is also used before spending a dragon dive's per-victim hit mask.
        private static bool AvailableTarget(SharedDepartureState state, LobbyNetworkAvatar avatar) =>
            state != null && avatar != null && avatar.Object != null && avatar.Object.IsValid &&
            avatar.Runner == state.Runner && state.Includes(avatar.Object.StateAuthority) &&
            avatar.Health > 0 && !state.AdProtected(avatar.Object.StateAuthority);

        public static Transform Target(Transform from, Transform fallback)
        {
            if (!SharedRunContext.Active) return fallback;
            if (!SharedRunContext.Playing) return null;
            var state = SharedRunContext.State;
            Transform nearest = null;
            float distance = float.MaxValue;
            foreach (var avatar in LobbyNetworkAvatar.All)
            {
                if (!AvailableTarget(state, avatar)) continue;
                var target = avatar.Object.HasStateAuthority && PlayerMovement.Instance != null
                    ? PlayerMovement.Instance.transform : avatar.WorldTransform;
                if (target == null) continue;
                float candidate = (target.position - from.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = target; }
            }
            return nearest;
        }

        public static bool CanMeleeHit(Transform from, Transform target, float range)
        {
            if (target == null) return false;
            Vector3 origin = from.position + Vector3.up * 1.5f;
            Vector3 delta = target.position + Vector3.up * 1.5f - origin;
            float distance = delta.magnitude;
            if (distance > range) return false;
            if (distance < 0.001f) return true;
            // Remote avatars have no physics collider. Test obstacles up to the target.
            if (!Physics.Raycast(origin, delta / distance, out var hit, Mathf.Max(0, distance - .3f))) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        public static bool MeleeAttack(Transform from, Transform target, float range, int damage)
        {
            if (!SharedRunContext.Active) return false;
            if (!Authority || !CanMeleeHit(from, target, range)) return true;
            var state = SharedRunContext.State;
            foreach (var avatar in LobbyNetworkAvatar.All)
            {
                if (!AvailableTarget(state, avatar)) continue;
                var candidate = avatar.Object.HasStateAuthority && PlayerMovement.Instance != null
                    ? PlayerMovement.Instance.transform : avatar.WorldTransform;
                if (candidate != target) continue;
                state.DamagePlayer(avatar.Object.StateAuthority, damage);
                break;
            }
            return true;
        }

        public static bool AreaAttack(Vector3 center, float radius, int damage)
        {
            if (!SharedRunContext.Active) return false;
            if (!Authority) return true;
            var state = SharedRunContext.State;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (AvailableTarget(state, avatar) &&
                    (avatar.LogicalPosition - LobbyNetworkAvatar.Origin - center).sqrMagnitude <= radius * radius)
                    state.DamagePlayer(avatar.Object.StateAuthority, damage);
            return true;
        }

        public static bool BoxAttack(Vector3 center, Vector3 size, Quaternion rotation, int damage)
        {
            if (!SharedRunContext.Active) return false;
            if (!Authority) return true;
            var state = SharedRunContext.State;
            foreach (var avatar in LobbyNetworkAvatar.All)
            {
                if (!AvailableTarget(state, avatar)) continue;
                Vector3 point = Quaternion.Inverse(rotation) *
                    (avatar.LogicalPosition - LobbyNetworkAvatar.Origin + Vector3.up - center);
                if (Mathf.Abs(point.x) <= size.x / 2 + 0.3f && Mathf.Abs(point.y) <= size.y / 2 + 1 &&
                    Mathf.Abs(point.z) <= size.z / 2 + 0.3f)
                    state.DamagePlayer(avatar.Object.StateAuthority, damage);
            }
            return true;
        }

        // Materialize only a living, absent actor. Never use ApplySharedHealth here:
        // its damage/death side effects belong to subsequent gameplay snapshots.
        // The supplied record must be the latest canonical value, not an archive copy.
        public static bool TryCreateReplica(ZombieController prefab, ulong id, SharedEnemyRecord record,
            out WorldSpawnIdentity identity)
        {
            identity = null;
            if (prefab == null || id == 0 || record.Template == 0 || record.Level < 1 || record.Level > 10 ||
                record.MaxHP <= 0 || record.HP <= 0 || record.HP > record.MaxHP ||
                !Finite(record.Position.x) || !Finite(record.Position.y) || !Finite(record.Position.z) ||
                !Finite(record.Rotation.x) || !Finite(record.Rotation.y) || !Finite(record.Rotation.z) ||
                !Finite(record.Rotation.w) || Quaternion.Dot(record.Rotation, record.Rotation) < .000001f ||
                SharedItemsRuntime.TryFind(id, out _) ||
                SharedRunContext.Regions?.AllowsEnemyReplica(id, record) == false || SharedRunContext.Independent?.AllowsEnemy(id) == false) return false;
            var enemy = Instantiate(prefab, record.Position - LobbyNetworkAvatar.Origin, record.Rotation);
            if (!enemy.TryInitializeReplica(record.Level, record.HP, record.MaxHP))
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
                return false;
            }
            identity = enemy.gameObject.AddComponent<WorldSpawnIdentity>();
            identity.InitializeRemote(id, record.Template);
            return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void Awake() => EndGameUIManager.EndGame += OnEndGame;
        private void OnDestroy() => EndGameUIManager.EndGame -= OnEndGame;
        private void OnEndGame(EndGameState outcome)
        {
            if (outcome != EndGameState.Win) return;
            // A local pre-result callback on a replica must not suppress the
            // eventual shared verdict (and its persistent reward).
            // If this event precedes the shared verdict, LateUpdate must dispatch it
            // again after MarkWon, regardless of subscriber ordering (including master).
            victoryShown = SharedVictoryRewards.CanPresentVictory;
            if (SharedRunContext.Playing && Authority) SharedRunContext.State.MarkWon();
        }

        private void LateUpdate()
        {
            using var measured = UpdateMarker.Auto();
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null) { authorityEpoch.Invalidate(); return; }
            bool authority = EnsureAuthority(state);
            if (state.Object.HasStateAuthority && !authority) return;
            if (state.RunWon && !victoryShown)
            {
                victoryShown = true;
                EndGameUIManager.EndGame?.Invoke(EndGameState.Win);
            }
            bool sample = Time.realtimeSinceStartupAsDouble >= nextSample;
            if (sample) nextSample = Time.realtimeSinceStartupAsDouble + 0.1;
            bool roleChanged = authority != wasAuthority || observedState != state;
            bool audit = sample || roleChanged || observedRevision != SharedItemsRuntime.EnemyRevision;
            wasAuthority = authority;
            observedState = state;
            if (!authority && audit)
            {
                foreach (var page in SharedWorldPage.All(state.Runner))
                {
                    foreach (var entry in page.Enemies)
                    {
                        var record = entry.Value;
                        if (record.Template == 0 || record.HP <= 0 || SharedItemsRuntime.TryFind(entry.Key, out _) ||
                            !templates.TryGetValue(record.Template, out var prefab)) continue;
                        if (SharedRunContext.Regions?.AllowsEnemyReplica(entry.Key, record) == false || SharedRunContext.Independent?.AllowsEnemy(entry.Key) == false) continue;
                        TryCreateReplica(prefab, entry.Key, record, out _);
                    }
                }
            }
            if (audit)
            {
                SharedItemsRuntime.CopyEnemiesTo(snapshot);
                observedRevision = SharedItemsRuntime.EnemyRevision;
            }
            if (authority && !audit) return;
            foreach (var identity in snapshot)
            {
                if (identity == null) continue;
                var enemy = identity.Enemy;
                if (enemy == null) continue;
                if (roleChanged) identity.ResetEnemyProxy();
                if (authority)
                {
                    identity.ResetEnemyProxy();
                    state.SampleEnemy(identity, enemy);
                    continue;
                }
                if (!state.TryEnemy(identity.Id, out var record)) { identity.SuspendEnemyProxyAI(audit); continue; }
                Vector3 target = record.Position - LobbyNetworkAvatar.Origin;
                if (enemy.transform.position != target || enemy.transform.rotation != record.Rotation)
                {
                    var position = Vector3.Lerp(enemy.transform.position, target, 1 - Mathf.Exp(-20 * Time.unscaledDeltaTime));
                    if ((position - target).sqrMagnitude < .00000001f) position = target;
                    enemy.transform.SetPositionAndRotation(position, record.Rotation);
                }
                identity.ApplyEnemyProxy(record, audit);
                if (enemy is DragonBossController dragon && state.DragonBrain.Id == identity.Id)
                    dragon.ApplySharedBrain(state.DragonBrain);
            }
        }
    }
}
