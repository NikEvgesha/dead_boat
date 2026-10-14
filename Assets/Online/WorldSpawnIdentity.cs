using UnityEngine;

namespace DeadBoat.Online
{
    // FNV-1a is stable across platforms; hash collisions are detected in the local registry.
    public sealed class WorldSpawnIdentity : MonoBehaviour
    {
        private string key;
        public PickableItem Item { get; private set; }
        public EnemyCore Enemy { get; private set; }
        public RangedWeaponController Weapon { get; private set; }
        public Animator Animator { get; private set; }
        public UnityEngine.AI.NavMeshAgent Agent { get; private set; }
        private bool cached;
        private bool proxyInitialized, proxyAIStopped, animationApplied;
        private int proxyHP, proxyMaxHP, proxyAnimation;
        private float proxyAnimationTime;
        private void OnEnable() => ResetEnemyProxy();
        internal void ResetEnemyProxy() { proxyInitialized = false; proxyAIStopped = false; animationApplied = false; }
        internal void SuspendEnemyProxyAI(bool audit)
        {
            if (Enemy == null) return;
            if (!proxyAIStopped || audit)
            {
                Enemy.StopAllCoroutines();
                if (Agent != null && Agent.enabled) Agent.enabled = false;
                proxyAIStopped = true;
            }
        }
        internal void ApplyEnemyProxy(SharedEnemyRecord record, bool audit)
        {
            if (Enemy == null) return;
            SuspendEnemyProxyAI(audit);
            if (!proxyInitialized || proxyHP != record.HP || proxyMaxHP != record.MaxHP || Enemy.SharedHP != record.HP)
                Enemy.ApplySharedHealth(record.HP, record.MaxHP);
            proxyHP = record.HP; proxyMaxHP = record.MaxHP;
            proxyInitialized = true;
            if (Enemy is TentaclePart tentacle && tentacle.Active != record.Active) tentacle.Active = record.Active;
            if (Animator == null || !Animator.isActiveAndEnabled || record.Animation == 0)
            {
                animationApplied = false;
                return;
            }
            // Let Unity advance the clip between snapshots instead of restarting
            // the same normalized time every rendered frame.
            if (!animationApplied || proxyAnimation != record.Animation || proxyAnimationTime != record.AnimationTime)
            {
                Animator.Play(record.Animation, 0, record.AnimationTime);
                proxyAnimation = record.Animation; proxyAnimationTime = record.AnimationTime;
                animationApplied = true;
            }
        }
        public bool PhysicsNearby { get; set; } = true;
        public bool RecoveryReplica { get; set; }
        private Rigidbody[] corpseBodies;
        public void ApplyCorpsePhysics(bool simulate)
        {
            if (key == null || !key.StartsWith("corpse:", System.StringComparison.Ordinal)) return;
            corpseBodies ??= GetComponentsInChildren<Rigidbody>(true);
            foreach (var body in corpseBodies)
                if (body != null && body.isKinematic == simulate) body.isKinematic = !simulate;
        }
        private void Awake() => CacheComponents();
        private void CacheComponents()
        {
            if (cached) return;
            cached = true;
            Item = GetComponent<PickableItem>();
            Enemy = GetComponent<EnemyCore>();
            Weapon = GetComponentInChildren<RangedWeaponController>(true);
            Animator = GetComponent<Animator>();
            Agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }
        public ulong Id { get; private set; }
        public bool Prunable => key != null && !key.StartsWith("scene:") && !key.StartsWith("boss:") && !key.StartsWith("test:");
        public int ItemTemplate { get; set; }
        public ulong TemplateId { get; set; }
        public static string StablePath(Transform part)
        {
            string path = "";
            var location = part.GetComponentInParent<LocationContentSpawner>();
            var anchor = part.GetComponentInParent<WorldGenerationAnchor>();
            while (part != null && (location == null || part != location.transform) && (anchor == null || part != anchor.transform))
            {
                path = part.name + "[" + part.GetSiblingIndex() + "]/" + path;
                part = part.parent;
            }
            return location != null ? location.SharedGenerationKey + "/" + path : anchor != null ? anchor.Key + "/" + path : "scene:" + path;
        }
        public void InitializeRemote(ulong id, ulong template)
        {
            CacheComponents();
            key = "remote:" + id;
            Id = id;
            TemplateId = template;
            SharedItemsRuntime.Register(this);
        }
        public string Key
        {
            get => key;
            set
            {
                CacheComponents();
                key = value;
                ulong hash = 14695981039346656037UL;
                foreach (byte b in System.Text.Encoding.UTF8.GetBytes(value)) hash = (hash ^ b) * 1099511628211UL;
                Id = hash;
                SharedItemsRuntime.Register(this);
            }
        }

        private void OnDestroy()
        {
            SoloRunBranch.RememberDestroyed(this);
            SharedItemsRuntime.Unregister(this);
        }
    }

    public sealed class WorldGenerationAnchor : MonoBehaviour { public string Key; }
}
