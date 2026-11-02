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
            // Include every prefab reachable by procedural item collections, even
            // when stored outside Items (for example legacy egg prefabs).
            var prefabGuids = new System.Collections.Generic.SortedSet<string>(
                AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items" }), StringComparer.Ordinal);
            foreach (string collectionGuid in AssetDatabase.FindAssets("t:LocationItemSpawnCollection"))
            {
                var collection = AssetDatabase.LoadAssetAtPath<LocationItemSpawnCollection>(AssetDatabase.GUIDToAssetPath(collectionGuid));
                if (collection.spawnEntries == null) continue;
                foreach (var entry in collection.spawnEntries)
                {
                    if (entry?.itemPrefab == null) continue;
                    string itemPath = AssetDatabase.GetAssetPath(entry.itemPrefab);
                    if (string.IsNullOrEmpty(itemPath) || !itemPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Spawn item must reference a prefab asset: " + collection.name);
                    prefabGuids.Add(AssetDatabase.AssetPathToGUID(itemPath));
                }
            }
            var staged = new System.Collections.Generic.List<SharedItemCatalog.Entry>();
            var names = new System.Collections.Generic.HashSet<string>();
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (string guid in prefabGuids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var item = prefab.GetComponent<PickableItem>();
                if (item == null || prefab.GetComponent<EnemyCore>() != null) continue;
                uint hash = 2166136261;
                foreach (char c in guid) hash = unchecked((hash ^ c) * 16777619);
                int id = (int)(hash & 0x7fffffff);
                if (id == 0 || !names.Add(prefab.name) || !ids.Add(id)) throw new InvalidOperationException("Shared item catalog collision: " + prefab.name);
                staged.Add(new SharedItemCatalog.Entry { id = id, prefab = item });
            }
            staged.Sort((a, b) => a.id.CompareTo(b.id));
            catalog.entries = staged; // Commit only a complete, collision-free catalog.
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("[Shared items] Catalog prepared: " + catalog.entries.Count);
        }
    }
}
