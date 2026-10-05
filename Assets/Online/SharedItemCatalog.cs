using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class SharedItemCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry { public int id; public PickableItem prefab; }
        public List<Entry> entries = new();
        private static SharedItemCatalog instance;
        public static SharedItemCatalog Load() => instance != null ? instance : instance = Resources.Load<SharedItemCatalog>("Online/SharedItemCatalog");
        public PickableItem Find(int id) => entries.Find(e => e.id == id)?.prefab;
        public int Identify(PickableItem item)
        {
            if (item == null) return 0;
            string name = item.name.Replace("(Clone)", "").TrimEnd();
            return entries.Find(e => e.prefab != null && e.prefab.name == name)?.id ?? 0;
        }
    }
}
