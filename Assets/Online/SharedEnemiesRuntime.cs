using UnityEngine;
using UnityEngine.AI;

namespace DeadBoat.Online
{
    public sealed class SharedEnemiesRuntime : MonoBehaviour
    {
        private static bool applyingDamage;
        public static bool ApplyingDamage => applyingDamage;
        private readonly System.Collections.Generic.List<WorldSpawnIdentity> snapshot = new();
        private double nextSample;
        private bool victoryShown;
        private bool wasAuthority;
        private static readonly System.Collections.Generic.Dictionary<ulong, ZombieController> templates = new();
        private static readonly System.Collections.Generic.Dictionary<DragonBossController,
            System.Collections.Generic.HashSet<Fusion.PlayerRef>> diveVictims = new();
        public static void Clear() { templates.Clear(); diveVictims.Clear(); }
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
            foreach (var avatar in Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
                if (avatar.Runner == state.Runner && avatar.Health > 0 &&
                    Vector3.Distance(avatar.transform.position - LobbyNetworkAvatar.Origin + Vector3.up,
                        dragon.transform.position) <= 3 && hit.Add(avatar.Object.StateAuthority))
                    state.RPC_PlayerDamage(avatar.Object.StateAuthority, (int)dragon.diveDamage);
        }
        public static ulong RegisterTemplate(ZombieController prefab)
        {
            ulong id = 14695981039346656037UL;
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(prefab.name)) id = (id ^ b) * 1099511628211UL;
            templates[id] = prefab;
            return id;
        }
        public static bool Authority => !SharedRunContext.Active || (SharedRunContext.Playing &&
            SharedRunContext.State.Object.HasStateAuthority);

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

        public static Transform Target(Transform from, Transform fallback)
        {
            if (!SharedRunContext.Active) return fallback;
            Transform nearest = fallback;
            float distance = float.MaxValue;
            foreach (var avatar in Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Runner != SharedRunContext.State.Runner || avatar.Health <= 0) continue;
                var target = avatar.Object.HasStateAuthority && PlayerMovement.Instance != null
                    ? PlayerMovement.Instance.transform : avatar.WorldTransform;
                float candidate = (target.position - from.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = target; }
            }
            return nearest;
        }

        public static bool AreaAttack(Vector3 center, float radius, int damage)
        {
            if (!SharedRunContext.Active) return false;
            if (!Authority) return true;
            var state = SharedRunContext.State;
            foreach (var avatar in Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
                if (avatar.Runner == state.Runner && avatar.Health > 0 &&
                    (avatar.transform.position - LobbyNetworkAvatar.Origin - center).sqrMagnitude <= radius * radius)
                    state.RPC_PlayerDamage(avatar.Object.StateAuthority, damage);
            return true;
        }

        public static bool BoxAttack(Vector3 center, Vector3 size, Quaternion rotation, int damage)
        {
            if (!SharedRunContext.Active) return false;
            if (!Authority) return true;
            var state = SharedRunContext.State;
            foreach (var avatar in Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Runner != state.Runner || avatar.Health <= 0) continue;
                Vector3 point = Quaternion.Inverse(rotation) *
                    (avatar.transform.position - LobbyNetworkAvatar.Origin + Vector3.up - center);
                if (Mathf.Abs(point.x) <= size.x / 2 + 0.3f && Mathf.Abs(point.y) <= size.y / 2 + 1 &&
                    Mathf.Abs(point.z) <= size.z / 2 + 0.3f)
                    state.RPC_PlayerDamage(avatar.Object.StateAuthority, damage);
            }
            return true;
        }

        private void Awake() => EndGameUIManager.EndGame += OnEndGame;
        private void OnDestroy() => EndGameUIManager.EndGame -= OnEndGame;
        private void OnEndGame(EndGameState outcome)
        {
            if (outcome != EndGameState.Win) return;
            victoryShown = true;
            if (SharedRunContext.Playing && Authority) SharedRunContext.State.MarkWon();
        }

        private void LateUpdate()
        {
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null) return;
            if (state.RunWon && !victoryShown)
            {
                victoryShown = true;
                EndGameUIManager.EndGame?.Invoke(EndGameState.Win);
            }
            bool sample = Time.realtimeSinceStartupAsDouble >= nextSample;
            if (sample) nextSample = Time.realtimeSinceStartupAsDouble + 0.1;
            if (Authority && !wasAuthority)
            {
                bool active = false;
                int alive = 0;
                foreach (var tentacle in Object.FindObjectsByType<TentaclePart>(FindObjectsSortMode.None))
                    if (!tentacle.IsDead)
                    {
                        alive++;
                        if (tentacle.Active) { tentacle.SetActive(); active = true; }
                    }
                if (!active && alive > 0) Object.FindAnyObjectByType<TentacleBoss>()?.ChangeTentacle(null);
            }
            wasAuthority = Authority;
            if (!Authority)
            {
                foreach (var page in SharedWorldPage.All(state.Runner))
                {
                    foreach (var entry in page.Enemies)
                    {
                        var record = entry.Value;
                        if (record.Template == 0 || record.HP <= 0 || SharedItemsRuntime.TryFind(entry.Key, out _) ||
                            !templates.TryGetValue(record.Template, out var prefab)) continue;
                        var enemy = Instantiate(prefab, record.Position - LobbyNetworkAvatar.Origin, record.Rotation);
                        enemy.InitializeLevel(record.Level);
                        enemy.gameObject.AddComponent<WorldSpawnIdentity>().InitializeRemote(entry.Key, record.Template);
                    }
                }
            }
            SharedItemsRuntime.CopyTo(snapshot);
            foreach (var identity in snapshot)
            {
                if (identity == null) continue;
                var enemy = identity.GetComponent<EnemyCore>();
                if (enemy == null) continue;
                if (Authority)
                {
                    if (sample) state.SampleEnemy(identity, enemy);
                    continue;
                }
                enemy.StopAllCoroutines();
                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                if (!state.TryEnemy(identity.Id, out var record)) continue;
                enemy.transform.SetPositionAndRotation(Vector3.Lerp(enemy.transform.position,
                    record.Position - LobbyNetworkAvatar.Origin, 1 - Mathf.Exp(-20 * Time.unscaledDeltaTime)), record.Rotation);
                if (enemy is TentaclePart tentacle) tentacle.Active = record.Active;
                enemy.ApplySharedHealth(record.HP, record.MaxHP);
                if (enemy is DragonBossController dragon && state.DragonBrain.Id == identity.Id)
                    dragon.ApplySharedBrain(state.DragonBrain);
                var animator = enemy.GetComponent<Animator>();
                if (animator != null && animator.isActiveAndEnabled && record.Animation != 0)
                    animator.Play(record.Animation, 0, record.AnimationTime);
            }
        }
    }
}
