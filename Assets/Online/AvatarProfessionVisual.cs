using UnityEngine;

namespace DeadBoat.Online
{
    // Stable wire IDs, independent of catalog ordering. Materials are shared, never cloned per player.
    public sealed class AvatarProfessionVisual : MonoBehaviour
    {
        public static byte SkinFor(string profession) => profession == "mechanic" ? (byte)1 : profession == "hunter" ? (byte)2 : (byte)0;
        private Renderer[] targets;
        private Material[][] original;
        private byte displayed = byte.MaxValue;
        private static Material mechanic, hunter;
        public byte Displayed => displayed;
        public void Initialize(Transform model)
        {
            targets = model.GetComponentsInChildren<Renderer>(true);
            original = new Material[targets.Length][];
            for (int i = 0; i < targets.Length; i++) original[i] = targets[i].sharedMaterials;
        }
        public void Apply(byte id)
        {
            if (targets == null || displayed == id) return;
            displayed = id;
            Material material = id == 1 ? mechanic ??= Resources.Load<Material>("OnlineUI/AvatarSkins/mechanic")
                : id == 2 ? hunter ??= Resources.Load<Material>("OnlineUI/AvatarSkins/hunter") : null;
            for (int i = 0; i < targets.Length; i++)
            {
                if (!targets[i]) continue;
                if (!material) { targets[i].sharedMaterials = original[i]; continue; }
                var slots = new Material[original[i].Length];
                for (int j = 0; j < slots.Length; j++) slots[j] = material;
                targets[i].sharedMaterials = slots;
            }
        }
    }
}
