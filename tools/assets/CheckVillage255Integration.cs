using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using DeadBoat.Online;
using Object = UnityEngine.Object;

public static class CheckVillage255Integration
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    static List<LocationSpawnItem> Entries(LocationSpawnCollection pool) => (List<LocationSpawnItem>)typeof(LocationSpawnCollection).GetField("spawnItems", Private).GetValue(pool);
    static string Path(Transform t, Transform root) => AnimationUtility.CalculateTransformPath(t, root);
    static string Descriptor(LocationSpawnItem item) => AssetDatabase.GetAssetPath(item.prefab) + "|" + item.chance + "|" + item.HaveMinSpawnDistence + "|" + item.MinSpawnDistence + "|" + item.HaveMaxSpawnDistence + "|" + item.MaxSpawnDistence;
    static List<LocationSpawnItem> ReadBackup(string path)
    {
        var text = File.ReadAllText("Library/Village255/integration-backup/" + path);
        var pattern = @"  - prefab: \{fileID: \d+, guid: ([a-f0-9]+), type: 3\}\r?\n    chance: ([^\r\n]+)\r?\n    HaveMinSpawnDistence: ([01])\r?\n    MinSpawnDistence: ([^\r\n]+)\r?\n    HaveMaxSpawnDistence: ([01])\r?\n    MaxSpawnDistence: ([^\r\n]+)";
        return Regex.Matches(text, pattern).Cast<Match>().Select(m => new LocationSpawnItem
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)).GetComponent<LocationContentSpawner>(),
            chance = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
            HaveMinSpawnDistence = m.Groups[3].Value == "1", MinSpawnDistence = float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture),
            HaveMaxSpawnDistence = m.Groups[5].Value == "1", MaxSpawnDistence = float.Parse(m.Groups[6].Value, CultureInfo.InvariantCulture)
        }).ToList();
    }
    // Reference implementation from the pre-integration pool selection rules.
    sealed class PriorPool
    {
        readonly List<LocationSpawnItem> all;
        List<LocationSpawnItem> unused;
        public PriorPool(List<LocationSpawnItem> entries) { all = entries; unused = new List<LocationSpawnItem>(entries); }
        public LocationContentSpawner Pick(float distance, RunRandom random)
        {
            var actual = all.Where(e => (!e.HaveMinSpawnDistence || e.MinSpawnDistence < distance) && (!e.HaveMaxSpawnDistence || e.MaxSpawnDistence > distance)).ToList();
            if (actual.Count == 0) return null;
            var candidates = actual.Where(unused.Contains).ToList();
            if (candidates.Count == 0) { unused = new List<LocationSpawnItem>(actual); candidates = new List<LocationSpawnItem>(actual); }
            float total = candidates.Sum(e => e.chance);
            var chosen = candidates[0];
            if (total > 0)
            {
                float value = random.Range(0f, total);
                chosen = null;
                foreach (var e in candidates) { if (value < e.chance) { chosen = e; break; } value -= e.chance; }
                chosen ??= candidates[candidates.Count - 1];
            }
            unused.Remove(chosen); return chosen.prefab;
        }
    }
    public static string Pools()
    {
        var report = new StringBuilder(); int comparisons = 0;
        var thresholds = new float[] { 0, 9999, 10000, 10001, 31999, 32000, 32001, 49999, 50000, 50001, 100000 };
        foreach (var path in new[] { "Assets/Scripts/Locations/LocationSpawnCollection.asset", "Assets/Scripts/Locations/LocationSpawnCollectionForest.asset" })
        {
            var source = AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(path);
            var backup = ReadBackup(path);
            var snapshot = (List<LocationSpawnItem>)typeof(LocationSpawnCollection).GetField("_revisionTwoSpawnItems", Private).GetValue(source);
            Check(backup.Count == (path.Contains("Forest") ? 16 : 20), "Backup parsing/count");
            Check(backup.Select(Descriptor).SequenceEqual(snapshot.Select(Descriptor)) && snapshot.All(e => !e.bankAlternativePrefab), "Prior pool snapshot changed: " + path);
            var family = Entries(source).Where(e => e.prefab.name.StartsWith("Village")).ToArray();
            Check(family.Length == 1 && family[0].prefab.name == "Village_1" && family[0].bankAlternativePrefab.name == "Village_2", "Expected one family with both banks");
            foreach (int revision in new[] { 0, 1, 2 })
                foreach (int seed in new[] { 0, 1, -1, int.MinValue, 255 })
                    foreach (float distance in thresholds)
                    {
                        var owned = Object.Instantiate(source);
                        try
                        {
                            owned.ApplyPoolRevision(revision);
                            var expectedEntries = new List<LocationSpawnItem>(backup);
                            int cap = (int)typeof(LocationSpawnCollection).GetField(revision <= 0 ? "_legacyPoolSize" : "_revisionOnePoolSize", Private).GetValue(source);
                            if (revision <= 1 && cap > 0 && cap < expectedEntries.Count) expectedEntries.RemoveRange(cap, expectedEntries.Count - cap);
                            var old = new PriorPool(expectedEntries);
                            var ra = new RunRandom(seed, "old-pool", 0); var rb = new RunRandom(seed, "old-pool", 0);
                            for (int i = 0; i < 40; i++)
                            {
                                Check(old.Pick(distance, ra) == owned.GetRandomSpawnPrefab(distance, rb), "Legacy choice mismatch " + source.name + ":" + revision + ":" + distance + ":" + i);
                                comparisons++;
                            }
                            Check(ra.Value == rb.Value, "Legacy RNG consumption changed");
                        }
                        finally { Object.DestroyImmediate(owned); }
                    }
            var a = Object.Instantiate(source); var b = Object.Instantiate(source);
            try
            {
                int left = 0, right = 0; float z = 32000, otherZ = z;
                for (int i = 0; i < 1800; i++)
                {
                    var first = LocationLayoutPlan.Advance(i, z, 50, 350, a, new RunRandom(255, "layout", i)); z = first.LogicalZ;
                    var replay = LocationLayoutPlan.Advance(i, otherZ, 50, 350, b, new RunRandom(255, "layout", i)); otherZ = replay.LogicalZ;
                    Check(z == otherZ && first.Prefab == replay.Prefab, "Revision-3 replay diverged");
                    if (first.Prefab?.name == "Village_1") left++;
                    if (first.Prefab?.name == "Village_2") right++;
                }
                Check(left > 0 && right > 0, "A river bank never selected");
                report.AppendLine(source.name + ": one village entry; deterministic replay 1800 steps, village left=" + left + ", right=" + right + "; complete old snapshots/revisions 0,1,2 match pre-integration choices and RNG.");
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:LocationSpawnCollection"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid); var pool = AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(path);
            var entries = Entries(pool);
            Check(entries.All(e => e.prefab), "Missing reference in " + path);
            if (path.EndsWith("NY_Locations.asset")) Check(entries.Count == 7 && entries.All(e => !e.prefab.name.StartsWith("Village") && !e.bankAlternativePrefab), "Winter event changed");
            report.AppendLine("POOL " + path + ": " + entries.Count + " entries, village families=" + entries.Count(e => e.prefab.name.StartsWith("Village")));
        }
        report.AppendLine("PASS legacy comparisons=" + comparisons + "; CurrentPoolRevision=" + LocationSpawnCollection.CurrentPoolRevision);
        report.AppendLine(DeadBoat.Online.Editor.AvatarNamesProbe.Validate());
        DeadBoat.Online.Editor.SharedRunValidation.Validate();
        report.AppendLine("PASS shared seeded generation validation across all actual pools.");
        foreach (var name in new[] { "LevelMerged", "LevelForest", "LevelDesert", "LevelWinter", "LevelEvent" })
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
            try
            {
                var spawners = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LocationSpawner>(true)).ToArray();
                foreach (var spawner in spawners) report.AppendLine("MAP " + name + " => " + AssetDatabase.GetAssetPath(spawner.locationCollection));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        File.WriteAllText("Library/Village255/integration-pools.txt", report.ToString());
        return report.ToString();
    }

    public static string Banks()
    {
        var report = new StringBuilder();
        var canonical = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Workshop/Village255/Village_Unified.prefab");
        var original = canonical.GetComponent<LocationContentSpawner>();
        foreach (var name in new[] { "Village_1", "Village_2" })
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/" + name + ".prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                var offset = new Vector3(5000, 0, 5000); root.transform.position += offset;
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(ground, scene);
                ground.transform.position = offset + Vector3.down * .5f; ground.transform.localScale = new Vector3(400, 1, 400);
                var content = root.GetComponent<LocationContentSpawner>();
                var homes = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Village_House_")).OrderBy(t => t.name).ToArray();
                Check(homes.Length == 10 && homes.All(h => h.gameObject.activeInHierarchy), "Wrong homes " + name);
                Check(!root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "dom" || t.name.StartsWith("dom (") || t.name.StartsWith("GunStore") || t.name.StartsWith("Village_NewHouse_")), "Old housing remains " + name);
                Check(content.itemCollection == original.itemCollection && content._enemyPrefabs.SequenceEqual(original._enemyPrefabs), "Changed content definitions " + name);
                Check(content.itemSpawnPoints.Count == 25 && content.enemySpawnPoints.Count == 15, "Changed counts " + name);
                var map = root.transform.Find("PointCenter (1)").localToWorldMatrix * canonical.transform.Find("PointCenter (1)").worldToLocalMatrix;
                for (int i = 0; i < 25; i++)
                {
                    var a = original.itemSpawnPoints[i]; var b = content.itemSpawnPoints[i];
                    Check(Path(a.spawnTransform, canonical.transform) == Path(b.spawnTransform, root.transform) && a.allowedType == b.allowedType && a.allowedSize == b.allowedSize && a.alignVisualBase == b.alignVisualBase && a.maxVisualFootprint == b.maxVisualFootprint, "Changed slot " + name + ":" + i);
                    Check(Vector3.Distance(map.MultiplyPoint3x4(a.spawnTransform.position), b.spawnTransform.position) < .004f, "Wrong bank placement " + name + ":" + i);
                }
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
                Physics.SyncTransforms();
                foreach (var house in homes)
                {
                    var probe = new GameObject("Bank controller probe"); SceneManager.MoveGameObjectToScene(probe, scene);
                    probe.transform.position = house.TransformPoint(new Vector3(0, .55f, -4.6f));
                    var c = probe.AddComponent<CharacterController>(); c.height = source.height; c.radius = source.radius; c.center = source.center;
                    c.stepOffset = source.stepOffset; c.skinWidth = source.skinWidth; c.slopeLimit = source.slopeLimit;
                    Physics.SyncTransforms();
                    for (int i = 0; i < 340 && house.InverseTransformPoint(probe.transform.position).z < 1.3f; i++) c.Move(house.forward * .045f + Vector3.down * .025f);
                    Check(c.isGrounded && house.InverseTransformPoint(probe.transform.position).z > 1.1f, "Blocked entrance " + name + ":" + house.name);
                    Object.DestroyImmediate(probe);
                }
                for (int i = 0; i < 25; i++) Check(scene.GetPhysicsScene().Raycast(content.itemSpawnPoints[i].spawnTransform.position + Vector3.up * .02f, Vector3.down, out var _, .35f, ~0, QueryTriggerInteraction.Ignore), "Unsupported slot " + name + ":" + i);
                report.AppendLine("PASS " + name + ": 10 current homes, 10/10 entrances, old housing absent including inactive children, 25 supported loot slots, 15 guard slots, same content and bank-mapped placements.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        File.WriteAllText("Library/Village255/integration-banks.txt", report.ToString()); return report.ToString();
    }
}
