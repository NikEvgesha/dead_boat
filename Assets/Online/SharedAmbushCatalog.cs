using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DeadBoat.Online
{
    // A small cold-restore fallback. Loaded only after the live template cache misses,
    // not from the boot scene or an automatic initialization hook.
    public sealed class SharedAmbushCatalog : ScriptableObject
    {
        public const string ResourcePath = "Online/AmbushEnemyCatalog";
        [Serializable] public sealed class Entry { public ulong id; public ZombieController prefab; }
        public List<Entry> entries = new List<Entry>();
        private static SharedAmbushCatalog instance;
        private static bool attempted;

        public static ulong TemplateId(ZombieController prefab) => prefab != null
            ? WorldSpawnIdentity.IdForKey(prefab.name) : 0;

        // The current scripted templates inherit LevelledEnemy.InitializeLevel
        // unchanged. Reserve the same max HP before any actor exists.
        public static int InitialHealth(ZombieController prefab, int level) => prefab == null ? 0 :
            Mathf.RoundToInt(Mathf.Lerp(prefab.minHP, prefab.maxHP, (Mathf.Clamp(level, 1, 10) - 1f) / 9f));

        // The sole guard-key formula shared by authority, membership and solo restore.
        public static ulong GuardId(ulong actionId, int ordinal)
        {
            if (actionId == 0 || ordinal < 0) throw new ArgumentOutOfRangeException();
            return WorldSpawnIdentity.IdForKey("ambush:" + actionId.ToString(CultureInfo.InvariantCulture) +
                ":guard:" + ordinal.ToString(CultureInfo.InvariantCulture));
        }

        public static bool TryFind(ulong id, out ZombieController prefab)
        {
            prefab = null;
            if (id == 0) return false;
            if (!attempted)
            {
                attempted = true;
                instance = Resources.Load<SharedAmbushCatalog>(ResourcePath);
            }
            if (instance == null || instance.entries == null) return false;
            foreach (var entry in instance.entries)
                if (entry != null && entry.id == id && entry.prefab != null && TemplateId(entry.prefab) == id)
                { prefab = entry.prefab; return true; }
            return false;
        }

        public static void ResetLookup() { instance = null; attempted = false; }
    }
}
