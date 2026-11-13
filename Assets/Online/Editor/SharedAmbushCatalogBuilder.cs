using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // Explicit native invocation only. This class never runs on import/startup.
    public static class SharedAmbushCatalogBuilder
    {
        public const string AssetPath = "Assets/Resources/Online/AmbushEnemyCatalog.asset";
        private static readonly string[] Sources =
        {
            "Assets/Prefabs/Enemy/just_zombie.prefab",
            "Assets/Prefabs/Enemy/dragon.prefab",
            "Assets/Events/NewYear/NY_just_zombie.prefab"
        };

        public static string Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepare ambush catalog in idle Edit Mode.");
            var entries = new List<SharedAmbushCatalog.Entry>();
            var ids = new HashSet<ulong>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in Sources)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var prefab = asset != null ? asset.GetComponent<ZombieController>() : null;
                ulong id = SharedAmbushCatalog.TemplateId(prefab);
                if (prefab == null || id == 0 || !ids.Add(id) || !names.Add(prefab.name))
                    throw new InvalidOperationException("Invalid or duplicate production ambush prefab: " + path);
                entries.Add(new SharedAmbushCatalog.Entry { id = id, prefab = prefab });
            }
            entries.Sort((a, b) => a.id.CompareTo(b.id));
            // Resolve/validate every source before changing the asset.
            var catalog = AssetDatabase.LoadAssetAtPath<SharedAmbushCatalog>(AssetPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Online"))
                    throw new InvalidOperationException("Existing Online Resources folder required.");
                catalog = ScriptableObject.CreateInstance<SharedAmbushCatalog>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }
            catalog.entries = entries;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            SharedAmbushCatalog.ResetLookup();
            string result = "PASS: 3 production ambush templates in " + AssetPath;
            Debug.Log("[Ambush catalog266] " + result);
            return result;
        }
    }
}
