using UnityEngine;

namespace DeadBoat.Online
{
    // FNV-1a is stable across platforms; hash collisions are detected in the local registry.
    public sealed class WorldSpawnIdentity : MonoBehaviour
    {
        private string key;
        public ulong Id { get; private set; }
        public bool Prunable => key != null && !key.StartsWith("scene:") && !key.StartsWith("boss:") && !key.StartsWith("test:");
        public int ItemTemplate { get; set; }
        public ulong TemplateId { get; set; }
        public void InitializeRemote(ulong id, ulong template)
        {
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
