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
        public bool PhysicsNearby { get; set; } = true;
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

        private void OnDestroy() => SharedItemsRuntime.Unregister(this);
    }
}
