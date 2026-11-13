using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class SharedLoot265NativeProbe
    {
        // Read-only git show64954ee8:Assets/Resources/Online/SharedItemCatalog.asset,
        // sorted numeric IDs, UTF8 "id:guid:localFileID\n"; all67 original references, not names alone.
        private const string HistoricalHash = "da1ee337d60567db1cf03c426046efac04641f9252db96f77a6d94efedd7a3a6";
        private const string Names = "F01_BrokenSled,F02_CrateScraps,F03_PineCones,F04_DriedWreath,F05_CandleStubs,F06_WrappingPaper,F07_CharcoalSack,F08_StrawBundle,F09_BrokenNutcracker,F10_HempRope,T01_Star,T02_MusicBox,T03_Bell,T04_AuroraCrystal,T05_PorcelainFox,T06_Compass,T07_ShipInBottle,T08_SignetRing,T09_LighthouseGlobe,T10_Coffer";
        [Serializable] private sealed class Row
        {
            public string prefab, tags;
            public int template, repeatedApplications;
            public bool passed, heldExcluded, nonusable;
            public Vector3 rawScale, worldScale, rawVisual, fittedVisual, fittedBox, coldVisual, coldBox, bagVisual, bagBox;
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion, historicalBase = "64954ee8", historicalFingerprint;
            public string scope = "Native EditMode: exact20 assets, catalogs and historical IDs, production fitting and both cold factories, Renderer/BoxCollider, nonuniform rotated parents, registry lifecycle and actual solo/recovery JSON types. No Player Build, Play, physics simulation, networking, UI bag insertion, inventory coordinator, sale/burn RPC, refresh or authority migration. Held exclusion is expected for these nonusable items; no quick-slot/held acceptance is implied.";
            public int catalogCount, allCount, historicalCount, registryBefore, registryAfter, checks;
            public List<Row> rows = new List<Row>();
            public List<string> errors = new List<string>();
        }
        private struct Sizes { public Vector3 visual, box; }
        public static string Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SharedRunContext.Active || SoloRunBranch.Active)
                throw new InvalidOperationException("Requires idle EditMode without an active run.");
            var report = new Report { unityVersion = Application.unityVersion };
            var baseline = Registry(); report.registryBefore = baseline.Length;
            var sourceMeshes = new HashSet<Mesh>();
            Scene preview = default; GameObject root = null;
            try
            {
                var expected = new HashSet<string>(Names.Split(',').Select(n => "Loot203_" + n));
                var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items/Loot203" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
                Must(paths.Length == 20 && expected.SetEquals(paths.Select(Path.GetFileNameWithoutExtension)), report, "Exact20 source prefabs changed.");
                var catalog = SharedItemCatalog.Load();
                var all = AssetDatabase.LoadAssetAtPath<StoreItemsCollection>("Assets/Scripts/Items/Collections/All.asset");
                var held = AssetDatabase.LoadAssetAtPath<LobbyHeldItemCatalog>("Assets/Online/Avatars/HeldItems/LobbyHeldItems.asset");
                Must(catalog != null && all != null && held != null, report, "Required catalogs are missing.");
                report.catalogCount = catalog.entries.Count; report.allCount = all.Items.Count;
                Must(report.catalogCount == 87 && catalog.entries.All(e => e.prefab != null && e.id != 0) &&
                    catalog.entries.Select(e => e.id).Distinct().Count() == 87, report, "Expected87 unique nonzero catalog IDs.");
                var history = catalog.entries.Where(e => !expected.Contains(e.prefab.name)).OrderBy(e => e.id).ToArray();
                report.historicalCount = history.Length;
                var text = new StringBuilder();
                foreach (var entry in history)
                {
                    Must(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(entry.prefab, out string guid, out long localId), report, "Catalog reference has no asset identity.");
                    text.Append(entry.id.ToString(CultureInfo.InvariantCulture)).Append(':').Append(guid).Append(':')
                        .Append(localId.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }
                using (var sha = SHA256.Create()) report.historicalFingerprint = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "").ToLowerInvariant();
                Must(history.Length == 67 && report.historicalFingerprint == HistoricalHash, report, "Original67 catalog IDs/references differ from git64954ee8.");
                Must(paths.Select(p => AssetDatabase.LoadAssetAtPath<PickableItem>(p)?.Data?.Name).All(n => !string.IsNullOrEmpty(n)) &&
                    paths.Select(p => AssetDatabase.LoadAssetAtPath<PickableItem>(p).Data.Name).Distinct().Count() == 20, report, "Loot save names missing or duplicated.");
                preview = EditorSceneManager.NewPreviewScene();
                root = new GameObject("Loot265 native transient matrix") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(root, preview);
                var slot = new GameObject("Production slot").transform; slot.SetParent(root.transform, false);
                var parents = new[] { Parent("Inventory", new Vector3(.3f, .7f, 1.4f), root.transform), Parent("Boat", new Vector3(1.6f, .8f, 1.2f), root.transform) };
                foreach (string path in paths)
                {
                    var row = new Row { prefab = path }; report.rows.Add(row);
                    var clones = new List<GameObject>(); var ownIds = new List<ulong>();
                    try
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<PickableItem>(path); Remember(prefab.gameObject, sourceMeshes);
                        row.template = catalog.Identify(prefab);
                        Must(row.template != 0 && catalog.Find(row.template) == prefab && all.Items.Count(i => i == prefab) == 1, report, path + " catalog/All mismatch");
                        var raw = Object.Instantiate(prefab, root.transform, false); clones.Add(raw.gameObject);
                        var fitted = Object.Instantiate(prefab, root.transform, false); clones.Add(fitted.gameObject);
                        raw.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        fitted.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        raw.CheckComponents(); fitted.CheckComponents();
                        row.nonusable = !raw.Usable && raw.GetComponentInChildren<ActivateItem>(true) == null;
                        row.heldExcluded = held.FindId(raw) == 0 && !held.Entries.Any(e => e.prefabName == prefab.name);
                        row.tags = string.Join(",", raw.Tags.OrderBy(t => t.ToString()));
                        Must(row.nonusable && row.heldExcluded, report, path + " unexpectedly enters quick-slot/held path");
                        Must(prefab.name.StartsWith("Loot203_F", StringComparison.Ordinal)
                            ? raw.GetComponent<FuelItem>() != null && raw.HaveTag(ItemTag.Fuel)
                            : raw.GetComponent<SellableItem>() != null && (raw.HaveTag(ItemTag.Valuable) || raw.HaveTag(ItemTag.Trash)), report, path + " role tags mismatch");
                        var rawSizes = Measure(raw.gameObject); row.rawScale = raw.transform.lossyScale; row.rawVisual = rawSizes.visual;
                        LocationContentSpawner.PlaceItemAtSpawnPoint(fitted.gameObject, new LocationSpawnPoint { spawnTransform = slot, alignVisualBase = true, maxVisualFootprint = .72f });
                        var fittedSizes = Measure(fitted.gameObject); row.worldScale = SharedItemScale.Capture(fitted);
                        row.fittedVisual = fittedSizes.visual; row.fittedBox = fittedSizes.box;
                        float ratio = Mathf.Min(1, .72f / Mathf.Max(rawSizes.visual.x, rawSizes.visual.z));
                        Near(fittedSizes.visual, rawSizes.visual * ratio, report, "production visual fit");
                        Near(fittedSizes.box, rawSizes.box * ratio, report, "production collider fit");
                        Near(SharedItemScale.PresentationMultiplier(fitted), Vector3.one * ratio, report, "presentation ratio");
                        var cold = SharedItemScale.CreateReplica(prefab, Vector3.zero, Quaternion.identity, row.worldScale); clones.Add(cold.gameObject);
                        var bag = SharedItemScale.CreateReplica(prefab, row.worldScale); clones.Add(bag.gameObject);
                        SceneManager.MoveGameObjectToScene(cold.gameObject, preview); SceneManager.MoveGameObjectToScene(bag.gameObject, preview);
                        row.coldVisual = Measure(cold.gameObject).visual; row.coldBox = Measure(cold.gameObject).box;
                        row.bagVisual = Measure(bag.gameObject).visual; row.bagBox = Measure(bag.gameObject).box;
                        Near(row.coldVisual, row.fittedVisual, report, "positioned factory visual"); Near(row.coldBox, row.fittedBox, report, "positioned factory collider");
                        Near(row.bagVisual, row.fittedVisual, report, "bag factory visual"); Near(row.bagBox, row.fittedBox, report, "bag factory collider");
                        foreach (var instance in new[] { cold, bag })
                        {
                            ulong id = WorldSpawnIdentity.IdForKey("test:loot265:" + prefab.name + (instance == cold ? ":cold" : ":bag"));
                            Must(!SharedItemsRuntime.TryFind(id, out _), report, "Fixture registry ID collision");
                            ownIds.Add(id); var identity = instance.gameObject.AddComponent<WorldSpawnIdentity>();
                            identity.ItemTemplate = row.template; identity.InitializeRemote(id, 0);
                            Must(SharedItemsRuntime.TryFind(id, out var found) && found == identity && found.Item == instance, report, "Registry cold identity missing");
                        }
                        foreach (var parent in parents)
                        {
                            cold.transform.SetParent(parent, false); cold.transform.SetLocalPositionAndRotation(Vector3.one, Quaternion.Euler(17, 39, 11));
                            SharedItemScale.Apply(cold.transform, row.worldScale); var stable = Measure(cold.gameObject);
                            for (int i = 0; i < 25; i++)
                            {
                                SharedItemScale.Apply(cold.transform, row.worldScale); row.repeatedApplications++;
                                Near(cold.transform.lossyScale, row.worldScale, report, "parent canonical scale");
                                var size = Measure(cold.gameObject); Near(size.visual, stable.visual, report, "parent renderer drift"); Near(size.box, stable.box, report, "parent collider drift");
                            }
                            cold.transform.SetParent(null, true); cold.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                            SharedItemScale.Apply(cold.transform, row.worldScale); var detached = Measure(cold.gameObject);
                            Near(detached.visual, row.fittedVisual, report, "detached renderer"); Near(detached.box, row.fittedBox, report, "detached collider");
                        }
                        var snapshot = new SharedSoloSnapshot { Seed = 27182818,
                            Items = new[] { new SoloWorldItem { Id = ownIds[0], Template = row.template, Mode = 3, Ammo = -1, WorldScale = SharedItemScale.Capture(cold) } },
                            Bag = new[] { new SharedRecoveryItem { WorldId = ownIds[1], Template = row.template, Ammo = -1, WorldScale = SharedItemScale.Capture(bag) } } };
                        var restored = JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(snapshot));
                        Must(restored.Items[0].Template == row.template && restored.Bag[0].Template == row.template && restored.Items[0].Id == ownIds[0] && restored.Bag[0].WorldId == ownIds[1], report, "JSON identity/template changed");
                        Near(restored.Items[0].WorldScale, row.worldScale, report, "solo JSON scale"); Near(restored.Bag[0].WorldScale, row.worldScale, report, "bag JSON scale");
                        SharedItemScale.Apply(raw.transform, restored.Bag[0].WorldScale);
                        var jsonSize = Measure(raw.gameObject); Near(jsonSize.visual, row.fittedVisual, report, "JSON restored renderer"); Near(jsonSize.box, row.fittedBox, report, "JSON restored collider");
                        row.passed = true;
                    }
                    catch (Exception error) { report.errors.Add(path + ": " + error); }
                    finally
                    {
                        foreach (var clone in clones) if (clone) Dispose(clone, sourceMeshes);
                        foreach (ulong id in ownIds) if (SharedItemsRuntime.TryFind(id, out _)) { row.passed = false; report.errors.Add(path + ": fixture identity survived disposal"); }
                    }
                }
            }
            catch (Exception error) { report.errors.Add(error.ToString()); }
            finally
            {
                if (root) Dispose(root, sourceMeshes);
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                var after = Registry(); report.registryAfter = after.Length;
                if (!baseline.SequenceEqual(after)) report.errors.Add("Registry differs from pre-test snapshot.");
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "Loot265-20261114"); Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "native.json"), JsonUtility.ToJson(report, true));
            }
            if (report.errors.Count != 0) throw new InvalidOperationException("Loot265 native FAIL: " + string.Join("; ", report.errors));
            return "PASS: native20/20, original67IDs, catalogs/tags/held exclusion, factories/fit/colliders,1000 parent reapplications, JSON and registry cleanup; no live-network/UI acceptance";
        }
        private static int[] Registry() { var list = new List<WorldSpawnIdentity>(); SharedItemsRuntime.CopyTo(list); return list.Where(i => i != null).Select(i => i.GetInstanceID()).OrderBy(i => i).ToArray(); }
        private static Transform Parent(string name, Vector3 scale, Transform owner) { var t = new GameObject(name).transform; t.SetParent(owner, false); t.localScale = scale; t.localRotation = Quaternion.Euler(23, 47, 9); return t; }
        private static Sizes Measure(GameObject root)
        {
            Physics.SyncTransforms();
            var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer) && !(r is TrailRenderer) && !(r is LineRenderer)).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible native renderers: " + root.name);
            Bounds b = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) b.Encapsulate(renderer.bounds);
            var box = root.GetComponent<BoxCollider>(); if (!box || !box.enabled || box.bounds.size.sqrMagnitude <= 0) throw new InvalidOperationException("Native BoxCollider unavailable: " + root.name);
            return new Sizes { visual = b.size, box = box.bounds.size };
        }
        private static void Must(bool value, Report report, string message) { if (!value) throw new InvalidOperationException(message); report.checks++; }
        private static void Near(Vector3 actual, Vector3 expected, Report report, string message)
            => Must(Finite(actual) && (actual - expected).sqrMagnitude < .00000036f, report, message + ": " + actual + " versus " + expected);
        private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        private static void Remember(GameObject root, HashSet<Mesh> meshes) { foreach (var m in root.GetComponentsInChildren<MeshFilter>(true)) if (m.sharedMesh) meshes.Add(m.sharedMesh); foreach (var c in root.GetComponentsInChildren<MeshCollider>(true)) if (c.sharedMesh) meshes.Add(c.sharedMesh); }
        private static void Dispose(GameObject root, HashSet<Mesh> sources)
        {
            var meshes = new HashSet<Mesh>(); Remember(root, meshes);
            foreach (var id in root.GetComponentsInChildren<WorldSpawnIdentity>(true))
            {
                typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(id, true);
                // EditMode does not dispatch OnDestroy for this non-ExecuteAlways
                // component. Release the test's explicit registration explicitly.
                SharedItemsRuntime.Unregister(id);
            }
            foreach (var pb in root.GetComponentsInChildren<ProBuilderMesh>(true)) pb.preserveMeshAssetOnDestroy = true;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) filter.sharedMesh = null;
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) collider.sharedMesh = null;
            Object.DestroyImmediate(root);
            foreach (var mesh in meshes) if (mesh && !EditorUtility.IsPersistent(mesh) && !sources.Contains(mesh)) Object.DestroyImmediate(mesh);
        }
    }
}
