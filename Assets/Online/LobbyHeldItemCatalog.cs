using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    [CreateAssetMenu(menuName = "Dead Boat/Online/Held Item Catalog")]
    public sealed class LobbyHeldItemCatalog : ScriptableObject
    {
        public enum HoldPose : byte
        {
            OneHanded,
            TwoHanded
        }

        [Serializable]
        public sealed class Entry
        {
            public byte id;
            public string prefabName;
            public GameObject visualPrefab;
            public HoldPose pose;
            public Vector3 localPosition;
            public Vector3 localEuler = new Vector3(65f, 0f, 0f);
            public float scale = 1f;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public byte FindId(PickableItem item)
        {
            if (item == null)
                return 0;

            string itemName = item.gameObject.name;
            const string cloneSuffix = "(Clone)";
            if (itemName.EndsWith(cloneSuffix, StringComparison.Ordinal))
                itemName = itemName.Substring(0, itemName.Length - cloneSuffix.Length).TrimEnd();

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].prefabName, itemName, StringComparison.Ordinal))
                    return entries[i].id;
            }

            return 0;
        }

        public Entry Find(byte id)
        {
            if (id == 0)
                return null;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == id)
                    return entries[i];
            }

            return null;
        }

#if UNITY_EDITOR
        public void SetEntries(List<Entry> newEntries)
        {
            entries = newEntries;
        }
#endif
    }
}
