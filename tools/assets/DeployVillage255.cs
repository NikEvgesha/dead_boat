// Unity MCP script-execute, class DeployVillage255, method Deploy.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class DeployVillage255
{
    const string Canonical = "Assets/Prefabs/Workshop/Village255/Village_Unified.prefab";
    const string Enemy = "Assets/Prefabs/Locations/Enemy/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static LocationSpawnItem Copy(LocationSpawnItem e) => new LocationSpawnItem
    {
        prefab = e.prefab, bankAlternativePrefab = e.bankAlternativePrefab, chance = e.chance,
        HaveMinSpawnDistence = e.HaveMinSpawnDistence, MinSpawnDistence = e.MinSpawnDistence,
        HaveMaxSpawnDistence = e.HaveMaxSpawnDistence, MaxSpawnDistence = e.MaxSpawnDistence
    };
    public static string Deploy()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode required");
        Check(LocationSpawnCollection.CurrentPoolRevision == 3, "Compile approved pool revision first");
        var log = new List<string>();
        var canonical = AssetDatabase.LoadAssetAtPath<GameObject>(Canonical);
        var origin = canonical.transform.Find("PointCenter (1)");
        var matrices = new Matrix4x4[2]; var rotations = new Quaternion[2]; var oldIds = new long[2];
        var paths = new[] { Enemy + "Village_1.prefab", Enemy + "Village_2.prefab" };
        for (int i = 0; i < 2; i++)
        {
            var old = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            var center = old.transform.Find("PointCenter (1)");
            matrices[i] = center.localToWorldMatrix * origin.worldToLocalMatrix;
            rotations[i] = center.rotation * Quaternion.Inverse(origin.rotation);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(old.GetComponent<LocationContentSpawner>(), out string _, out oldIds[i]);
        }
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            for (int bank = 0; bank < 2; bank++)
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(canonical, preview);
                try
                {
                    // Leave the ten nested house prefabs linked to the common current templates.
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                    root.name = "Village_" + (bank + 1);
                    foreach (Transform child in root.transform)
                    {
                        child.position = matrices[bank].MultiplyPoint3x4(child.position);
                        child.rotation = rotations[bank] * child.rotation;
                    }
                    var p1 = root.transform.Find("Point1"); var p2 = root.transform.Find("Point2");
                    var min = Vector3.Min(p1.position, p2.position); var max = Vector3.Max(p1.position, p2.position);
                    p1.position = min; p2.position = max;
                    Check(PrefabUtility.SaveAsPrefabAsset(root, paths[bank]), "Save failed " + paths[bank]);
                }
                finally { Object.DestroyImmediate(root); }
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(paths[bank]).GetComponent<LocationContentSpawner>();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved, out string _, out long id);
                if (id != oldIds[bank])
                {
                    File.Copy("Library/Village255/integration-backup/" + paths[bank], paths[bank], true);
                    AssetDatabase.ImportAsset(paths[bank], ImportAssetOptions.ForceUpdate);
                    throw new Exception("Root component identifier changed; restored original asset " + paths[bank]);
                }
                log.Add("Saved " + paths[bank] + ": component fileID retained=" + id + ", houses=10, loot=25, guards=15");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }

        var left = AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]).GetComponent<LocationContentSpawner>();
        var right = AssetDatabase.LoadAssetAtPath<GameObject>(paths[1]).GetComponent<LocationContentSpawner>();
        var poolField = typeof(LocationSpawnCollection).GetField("spawnItems", Private);
        var snapshotField = typeof(LocationSpawnCollection).GetField("_revisionTwoSpawnItems", Private);
        foreach (var path in new[] { "Assets/Scripts/Locations/LocationSpawnCollection.asset", "Assets/Scripts/Locations/LocationSpawnCollectionForest.asset" })
        {
            var pool = AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(path);
            Check(!(((List<LocationSpawnItem>)snapshotField.GetValue(pool))?.Count > 0), "Pool already migrated: " + path);
            var old = (List<LocationSpawnItem>)poolField.GetValue(pool);
            Check(old.All(e => e.prefab), "Missing original prefab reference " + path);
            var villages = old.Where(e => e.prefab == left || e.prefab == right).ToArray();
            Check(villages.Length == 2, "Expected both previous village entries " + path);
            var a = villages[0]; var b = villages[1];
            float loA = a.HaveMinSpawnDistence ? a.MinSpawnDistence : float.NegativeInfinity;
            float loB = b.HaveMinSpawnDistence ? b.MinSpawnDistence : float.NegativeInfinity;
            float hiA = a.HaveMaxSpawnDistence ? a.MaxSpawnDistence : float.PositiveInfinity;
            float hiB = b.HaveMaxSpawnDistence ? b.MaxSpawnDistence : float.PositiveInfinity;
            bool sameRange = loA == loB && hiA == hiB;
            // The base map used the right bank at 10..50km and the left after
            // 50km. Keep that combined distance range and weight, now with both
            // placements available. Forest used both banks in the same range.
            Check(sameRange || (a.chance == b.chance && (hiA == loB || hiB == loA)), "Cannot consolidate non-contiguous/differently weighted bank ranges");
            snapshotField.SetValue(pool, old.Select(Copy).ToList());
            var unified = Copy(a); unified.prefab = left; unified.bankAlternativePrefab = right;
            unified.chance = sameRange ? a.chance + b.chance : a.chance;
            float lower = Mathf.Min(loA, loB), upper = Mathf.Max(hiA, hiB);
            unified.HaveMinSpawnDistence = !float.IsNegativeInfinity(lower); unified.MinSpawnDistence = unified.HaveMinSpawnDistence ? lower : 0;
            unified.HaveMaxSpawnDistence = !float.IsPositiveInfinity(upper); unified.MaxSpawnDistence = unified.HaveMaxSpawnDistence ? upper : 0;
            var next = new List<LocationSpawnItem>(); bool added = false;
            foreach (var entry in old)
                if (entry.prefab == left || entry.prefab == right) { if (!added) { next.Add(unified); added = true; } }
                else next.Add(entry);
            poolField.SetValue(pool, next);
            typeof(LocationSpawnCollection).GetMethod("OnEnable", Private).Invoke(pool, null);
            EditorUtility.SetDirty(pool); AssetDatabase.SaveAssetIfDirty(pool);
            log.Add(path + ": " + old.Count + " -> " + next.Count + " entries, one village family, weight=" + unified.chance + ", both banks, full revision-2 snapshot retained");
        }

        var workshop = SceneManager.GetSceneByPath("Assets/Scenes/VillageWorkshop.unity");
        Check(workshop.isLoaded, "Village workshop must remain open");
        var stage = workshop.GetRootGameObjects().Single();
        var walk = stage.GetComponent<VillageWalkPreview>();
        if (!walk) walk = stage.AddComponent<VillageWalkPreview>();
        walk.leftBankPrefab = left.gameObject; walk.rightBankPrefab = right.gameObject;
        walk.authoredLocation = stage.GetComponentInChildren<LocationContentSpawner>().gameObject;
        walk.ground = stage.transform.Find("Workshop_Ground"); walk.overview = stage.GetComponentInChildren<Camera>();
        walk.controllerTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
        foreach (var behaviour in walk.authoredLocation.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!behaviour) continue;
            behaviour.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
        }
        EditorUtility.SetDirty(walk);
        Check(EditorSceneManager.SaveScene(workshop), "Cannot save workshop");
        log.Add("Saved standalone Play Mode walk with two bank buttons; no persistent game save or enemy AI is started.");
        File.WriteAllLines("Library/Village255/deployment.txt", log);
        return string.Join("\n", log);
    }
}
