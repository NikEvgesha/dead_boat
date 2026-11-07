using UnityEngine;

namespace DeadBoat.Online
{
    // Stable wire IDs, independent of catalog ordering. Materials are shared, never cloned per player.
    public sealed class AvatarProfessionVisual : MonoBehaviour
    {
        // Append IDs only: catalog reordering and future professions must not renumber existing peers.
        public const byte LastSkinId = 13;
        public static byte SkinFor(string profession) => profession switch {
            "mechanic" => 1, "hunter" => 2, "yunga" => 3, "trader" => 4,
            "scout" => 5, "berserk" => 6, "vampire" => 7, "gambler" => 8,
            "pyro" => 9, "tank" => 10, "glass_cannon" => 11, "ninja" => 12, "cursed" => 13,
            _ => 0
        };
        private static readonly string[] Professions = { null, "mechanic", "hunter", "yunga", "trader", "scout", "berserk", "vampire", "gambler", "pyro", "tank", "glass_cannon", "ninja", "cursed" };
        public static string ProfessionFor(byte id) => id > 0 && id < Professions.Length ? Professions[id] : null;
        private Renderer[] targets;
        private Material[][] original;
        private byte displayed = byte.MaxValue;
        private static readonly Material[] materials = new Material[LastSkinId + 1];
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
            string profession = ProfessionFor(id);
            Material material = profession != null ? materials[id] ??= Resources.Load<Material>("OnlineUI/AvatarSkins/" + profession) : null;
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
