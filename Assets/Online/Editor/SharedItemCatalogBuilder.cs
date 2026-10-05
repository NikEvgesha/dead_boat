using System;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedItemCatalogBuilder
    {
        public static void Prepare()
        {
            const string path = "Assets/Resources/Online/SharedItemCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<SharedItemCatalog>(path);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<SharedItemCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            catalog.entries.Clear();
            var names = new System.Collections.Generic.HashSet<string>();
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var item = prefab.GetComponent<PickableItem>();
                if (item == null || prefab.GetComponent<EnemyCore>() != null) continue;
                uint hash = 2166136261;
                foreach (char c in guid) hash = unchecked((hash ^ c) * 16777619);
                int id = (int)(hash & 0x7fffffff);
                if (id == 0 || !names.Add(prefab.name) || !ids.Add(id)) throw new InvalidOperationException("Shared item catalog collision: " + prefab.name);
                catalog.entries.Add(new SharedItemCatalog.Entry { id = id, prefab = item });
            }
            catalog.entries.Sort((a, b) => a.id.CompareTo(b.id));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Shared items] Catalog prepared: " + catalog.entries.Count);
        }
    }
}
