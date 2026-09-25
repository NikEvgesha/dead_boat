using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class LobbyHeldItemCatalogBuilder
    {
        private const string VisualFolder = "Assets/Online/Avatars/HeldItems";
        private const string CatalogPath = VisualFolder + "/LobbyHeldItems.asset";
        private const string AvatarPath = "Assets/Online/Avatars/LobbyNetworkAvatar.prefab";

        [MenuItem("Tools/Online/Rebuild Held Item Visuals")]
        public static void Rebuild()
        {
            if (!AssetDatabase.IsValidFolder(VisualFolder))
                AssetDatabase.CreateFolder("Assets/Online/Avatars", "HeldItems");

            var catalog = AssetDatabase.LoadAssetAtPath<LobbyHeldItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LobbyHeldItemCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var previous = catalog.Entries.ToDictionary(entry => entry.prefabName);
            int nextId = catalog.Entries.Count == 0 ? 1 : catalog.Entries.Max(entry => entry.id) + 1;
            var entries = new List<LobbyHeldItemCatalog.Entry>();
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal);

            foreach (string path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var item = prefab.GetComponent<PickableItem>();
                if (item == null || prefab.GetComponent<ActivateItem>() == null)
                    continue;

                var visual = new SerializedObject(item).FindProperty("_visualObj")?.objectReferenceValue as GameObject;
                if (visual == null)
                {
                    Debug.LogWarning($"[Lobby items] No visual on {path}");
                    continue;
                }

                GameObject visualRoot = new GameObject(prefab.name + " Held Visual");
                try
                {
                    var chain = new Stack<Transform>();
                    for (Transform current = visual.transform; current != null && current != prefab.transform;
                         current = current.parent)
                        chain.Push(current);

                    Transform destination = visualRoot.transform;
                    while (chain.Count > 1)
                        destination = CopyNode(chain.Pop(), destination, false);
                    CopyNode(chain.Pop(), destination, true);

                    string visualPath = VisualFolder + "/" + prefab.name + "Visual.prefab";
                    GameObject visualAsset = PrefabUtility.SaveAsPrefabAsset(visualRoot, visualPath);
                    if (!previous.TryGetValue(prefab.name, out var entry))
                    {
                        if (nextId > byte.MaxValue)
                            throw new InvalidOperationException("Too many held item visuals for a byte ID.");
                        entry = new LobbyHeldItemCatalog.Entry
                        {
                            id = (byte)nextId++,
                            prefabName = prefab.name,
                            localEuler = new Vector3(65f, 0f, 0f)
                        };
                        float longest = 0f;
                        foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
                        {
                            Vector3 size = renderer.bounds.size;
                            longest = Mathf.Max(longest, size.x, size.y, size.z);
                        }
                        entry.scale = longest > 1.05f ? 1.05f / longest : 1f;
                    }

                    entry.visualPrefab = visualAsset;
                    entry.pose = prefab.name == "Rifle" || prefab.name == "Shotgun"
                        ? LobbyHeldItemCatalog.HoldPose.TwoHanded
                        : LobbyHeldItemCatalog.HoldPose.OneHanded;
                    entries.Add(entry);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(visualRoot);
                }
            }

            catalog.SetEntries(entries.OrderBy(entry => entry.id).ToList());
            EditorUtility.SetDirty(catalog);
            var avatar = PrefabUtility.LoadPrefabContents(AvatarPath);
            try
            {
                var networkAvatar = avatar.GetComponent<LobbyNetworkAvatar>();
                var serialized = new SerializedObject(networkAvatar);
                serialized.FindProperty("itemCatalog").objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(avatar, AvatarPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(avatar);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Lobby items] Built {entries.Count} held item visuals.");
        }

        private static Transform CopyNode(Transform source, Transform parent, bool recursive)
        {
            var copy = new GameObject(source.name);
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = source.localPosition;
            copy.transform.localRotation = source.localRotation;
            copy.transform.localScale = source.localScale;

            var meshFilter = source.GetComponent<MeshFilter>();
            if (meshFilter != null)
                copy.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;

            var meshRenderer = source.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var destinationRenderer = copy.AddComponent<MeshRenderer>();
                destinationRenderer.sharedMaterials = meshRenderer.sharedMaterials;
                destinationRenderer.shadowCastingMode = meshRenderer.shadowCastingMode;
            }

            var skinned = source.GetComponent<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                copy.AddComponent<MeshFilter>().sharedMesh = skinned.sharedMesh;
                copy.AddComponent<MeshRenderer>().sharedMaterials = skinned.sharedMaterials;
            }

            if (recursive)
            {
                foreach (Transform child in source)
                    CopyNode(child, copy.transform, true);
            }

            return copy.transform;
        }
    }
}
