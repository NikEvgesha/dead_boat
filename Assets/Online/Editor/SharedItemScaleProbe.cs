using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Native construction/serialization check only: no Play, RPC, scene saves or physics steps.
    public static class SharedItemScaleProbe
    {
        [Serializable]
        private sealed class Measurement
        {
            public string label;
            public Vector3 worldScale, visualSize, boxSize;
        }

        [Serializable]
        private sealed class Report
        {
            public string unityVersion, prefab;
            public string scope = "Native EditMode production fitting, scale helper, Renderer/BoxCollider bounds and JSON contracts. No Play, Player Build, physics simulation, live networking, recovery coordinator or user saves. Parent tests preserve world-axis scale lengths; a nonuniform rotated hierarchy can introduce shear, so its AABB is not compared to an unparented object at another orientation.";
            public int catalogId, checks, repeatApplications;
            public List<string> passed = new List<string>();
            public List<string> errors = new List<string>();
            public List<Measurement> measurements = new List<Measurement>();
            public string fusionBudgetScope = "SDK word counts are serialized state capacity, not useful traffic per second, snapshot-history memory or measured WebGL/native heap. Legacy struct counts are calculated from the previous field contracts, not a measurement of the old binary. Full prefab NetworkObject.GetWordCount is reported separately from individual behaviours, including the SDK's additional object words; external native allocator/history overhead is not measured.";
            public List<StructBudget> structBudgets = new List<StructBudget>();
            public List<BehaviourBudget> behaviourBudgets = new List<BehaviourBudget>();
            public List<ObjectBudget> objectBudgets = new List<ObjectBudget>();
        }

        [Serializable]
        private sealed class StructBudget
        {
            public string type;
            public int words, bytes, legacyCalculatedWords, legacyCalculatedBytes, additionalBytes;
        }

        [Serializable]
        private sealed class BehaviourBudget
        {
            public string type;
            public int words, bytes;
        }

        [Serializable]
        private sealed class ObjectBudget
        {
            public string prefab;
            public int words, bytes, behaviourWords, additionalObjectWords, additionalObjectBytes, allocationHeadroomBytes;
        }

        private const float Tolerance = .0003f;
        public static string Result { get; private set; } = "not run";

        public static void RunBatch() => Run();

        public static string Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SharedRunContext.Active || SoloRunBranch.Active)
                throw new InvalidOperationException("Requires idle EditMode without an active run.");

            var report = new Report { unityVersion = Application.unityVersion };
            var sources = new HashSet<Mesh>();
            var clones = new List<GameObject>();
            Scene preview = default;
            GameObject staging = null;
            try
            {
                string[] paths = AssetDatabase.FindAssets("Loot203_T10_Coffer t:Prefab")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => Path.GetFileNameWithoutExtension(p) == "Loot203_T10_Coffer").ToArray();
                Require(paths.Length == 1, report, "Expected exactly one coffer asset.");
                report.prefab = paths[0];
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(report.prefab);
                Require(source != null && source.GetComponent<PickableItem>() != null, report, "Coffer prefab missing PickableItem.");
                var catalog = SharedItemCatalog.Load();
                Require(catalog != null, report, "Shared item catalog missing.");
                report.catalogId = catalog.Identify(source.GetComponent<PickableItem>());
                Require(report.catalogId != 0 && catalog.Find(report.catalogId) == source.GetComponent<PickableItem>(), report,
                    "Coffer catalog identity does not resolve to the production prefab.");
                RememberMeshes(source, sources);

                preview = EditorSceneManager.NewPreviewScene();
                staging = new GameObject("Item scale264 transient fixture") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(staging, preview);
                var slot = new GameObject("Production coffer slot");
                slot.transform.SetParent(staging.transform, false);
                GameObject NewClone()
                {
                    var clone = Object.Instantiate(source, staging.transform, false);
                    clones.Add(clone);
                    clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    return clone;
                }

                var raw = NewClone();
                var fitted = NewClone();
                var rawMeasurement = Measure(raw, "raw catalog clone");
                report.measurements.Add(rawMeasurement);
                LocationContentSpawner.PlaceItemAtSpawnPoint(fitted, new LocationSpawnPoint
                {
                    spawnTransform = slot.transform, alignVisualBase = true, maxVisualFootprint = .72f
                });
                var fittedMeasurement = Measure(fitted, "production fitted coffer");
                report.measurements.Add(fittedMeasurement);
                Vector3 canonical = SharedItemScale.Capture(fitted.transform);

                Check("production fitting changes native visual and collision size", report, () =>
                {
                    Near(rawMeasurement.worldScale, Vector3.one * 2, report, "raw root scale");
                    Near(Mathf.Max(rawMeasurement.visualSize.x, rawMeasurement.visualSize.z), 1.016f, report, "raw width");
                    Near(Mathf.Max(fittedMeasurement.visualSize.x, fittedMeasurement.visualSize.z), .72f, report, "fitted width");
                    Near(canonical, Vector3.one * (2 * .72f / 1.016f), report, "fitted root scale");
                    float ratio = canonical.x / rawMeasurement.worldScale.x;
                    Near(fittedMeasurement.boxSize, rawMeasurement.boxSize * ratio, report, "fitted BoxCollider must scale with the visual");
                });

                Check("raw catalog reconstruction applies absolute canonical scale", report, () =>
                {
                    var receiver = NewClone();
                    SharedItemScale.Apply(receiver.transform, canonical);
                    receiver.transform.SetPositionAndRotation(fitted.transform.position, fitted.transform.rotation);
                    var observed = Measure(receiver, "raw clone after canonical apply");
                    report.measurements.Add(observed);
                    Near(observed.worldScale, canonical, report, "receiver scale");
                    Near(observed.visualSize, fittedMeasurement.visualSize, report, "receiver native visual size");
                    Near(observed.boxSize, fittedMeasurement.boxSize, report, "receiver native collider size");
                });

                Check("both production cold replica factories preserve native dimensions and pose", report, () =>
                {
                    var prefab = source.GetComponent<PickableItem>();
                    Vector3 position = new Vector3(4, 5, 6);
                    Quaternion rotation = Quaternion.Euler(13, 37, 7);
                    fitted.transform.SetPositionAndRotation(position, rotation);
                    var expectedPose = Measure(fitted, "fitted factory reference at requested pose");
                    var positioned = SharedItemScale.CreateReplica(prefab, position, rotation, canonical);
                    clones.Add(positioned.gameObject);
                    SceneManager.MoveGameObjectToScene(positioned.gameObject, preview);
                    var positionedMeasurement = Measure(positioned.gameObject, "production positioned factory");
                    report.measurements.Add(positionedMeasurement);
                    Near(positioned.transform.position, position, report, "positioned factory world position");
                    Near(Quaternion.Angle(positioned.transform.rotation, rotation), 0, report, "positioned factory rotation");
                    Near(positionedMeasurement.worldScale, canonical, report, "positioned factory scale");
                    Near(positionedMeasurement.visualSize, expectedPose.visualSize, report, "positioned factory renderer");
                    Near(positionedMeasurement.boxSize, expectedPose.boxSize, report, "positioned factory collider");

                    var unpositioned = SharedItemScale.CreateReplica(prefab, canonical);
                    clones.Add(unpositioned.gameObject);
                    SceneManager.MoveGameObjectToScene(unpositioned.gameObject, preview);
                    // Compare at the original orientation; the other overload must keep the raw prefab pose.
                    Near(Quaternion.Angle(unpositioned.transform.rotation, prefab.transform.rotation), 0, report, "unpositioned factory prefab rotation");
                    var unpositionedMeasurement = Measure(unpositioned.gameObject, "production unpositioned factory");
                    report.measurements.Add(unpositionedMeasurement);
                    Near(unpositionedMeasurement.worldScale, canonical, report, "unpositioned factory scale");
                    Near(unpositionedMeasurement.visualSize, fittedMeasurement.visualSize, report, "unpositioned factory renderer");
                    Near(unpositionedMeasurement.boxSize, fittedMeasurement.boxSize, report, "unpositioned factory collider");
                    fitted.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                });

                Check("remote held presentation multiplier is relative to the raw catalog size", report, () =>
                {
                    Near(SharedItemScale.PresentationMultiplier(raw.GetComponent<PickableItem>()), Vector3.one, report, "raw presentation multiplier");
                    var expected = new Vector3(canonical.x / rawMeasurement.worldScale.x,
                        canonical.y / rawMeasurement.worldScale.y, canonical.z / rawMeasurement.worldScale.z);
                    Near(SharedItemScale.PresentationMultiplier(fitted.GetComponent<PickableItem>()), expected, report, "fitted presentation multiplier");
                    Near(SharedItemScale.PresentationMultiplier(null), Vector3.one, report, "missing item presentation multiplier");
                });

                Check("rotated nonuniform inventory/hand/boat and detach stay idempotent", report, () =>
                {
                    var parents = new[]
                    {
                        Parent("Scaled inventory", new Vector3(.3f, .7f, 1.4f), new Vector3(11, 32, 7), staging.transform),
                        Parent("Rotated hand", new Vector3(1.7f, .4f, 1.2f), new Vector3(27, 53, 19), staging.transform),
                        Parent("Boat", new Vector3(.8f, 1.3f, 1.6f), new Vector3(8, 121, 3), staging.transform)
                    };
                    foreach (var parent in parents)
                    {
                        // false reproduces network attached reconstruction; local rotation is deliberately nontrivial.
                        fitted.transform.SetParent(parent, false);
                        fitted.transform.SetLocalPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(21, 38, 17));
                        SharedItemScale.Apply(fitted.transform, canonical);
                        var expected = Measure(fitted, parent.name + " initial apply");
                        Near(expected.worldScale, canonical, report, parent.name + " world scale");
                        for (int i = 0; i < 25; i++)
                        {
                            SharedItemScale.Apply(fitted.transform, canonical);
                            report.repeatApplications++;
                            var repeated = Measure(fitted, parent.name);
                            Near(repeated.worldScale, canonical, report, parent.name + " repeated scale");
                            Near(repeated.visualSize, expected.visualSize, report, parent.name + " renderer drift");
                            Near(repeated.boxSize, expected.boxSize, report, parent.name + " collider drift");
                        }
                        report.measurements.Add(Measure(fitted, parent.name + " after25"));

                        // true reproduces bag/hand/detach worldPositionStays transitions.
                        fitted.transform.SetParent(null, true);
                        fitted.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        SharedItemScale.Apply(fitted.transform, canonical);
                        var detached = Measure(fitted, parent.name + " detached");
                        report.measurements.Add(detached);
                        Near(detached.worldScale, canonical, report, "detached scale");
                        Near(detached.visualSize, fittedMeasurement.visualSize, report, "detached renderer size");
                        Near(detached.boxSize, fittedMeasurement.boxSize, report, "detached collider size");
                    }
                });

                Check("world and bag scale fields survive solo JSON contract", report, () =>
                {
                    var record = new SharedItemRecord { Template = report.catalogId, WeaponAmmo = 7, Mode = 3, WorldScale = canonical };
                    var snapshot = new SharedSoloSnapshot
                    {
                        Seed = 27182818,
                        Items = new[] { new SoloWorldItem { Id = 264, Template = record.Template, Ammo = record.WeaponAmmo,
                            Mode = record.Mode, WorldScale = record.WorldScale } },
                        Bag = new[] { new SharedRecoveryItem { WorldId = 265, Template = record.Template, Ammo = 5,
                            Equipped = 1, WorldScale = record.WorldScale } }
                    };
                    var restored = JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(snapshot));
                    Require(restored != null && restored.Items.Length == 1 && restored.Bag.Length == 1, report, "JSON collections changed");
                    Near(restored.Items[0].WorldScale, canonical, report, "solo JSON scale");
                    Near(restored.Bag[0].WorldScale, canonical, report, "bag JSON scale");
                    Require(restored.Items[0].Ammo == 7 && restored.Items[0].Mode == 3 && restored.Bag[0].Ammo == 5 &&
                        restored.Bag[0].Equipped == 1, report, "JSON item fields changed");
                    foreach (Vector3 scale in new[] { restored.Items[0].WorldScale, restored.Bag[0].WorldScale })
                    {
                        var restoredClone = NewClone();
                        SharedItemScale.Apply(restoredClone.transform, scale);
                        var observed = Measure(restoredClone, "JSON reconstructed coffer");
                        Near(observed.visualSize, fittedMeasurement.visualSize, report, "JSON native renderer size");
                        Near(observed.boxSize, fittedMeasurement.boxSize, report, "JSON native collider size");
                    }
                    var legacy = JsonUtility.FromJson<SharedSoloSnapshot>("{\"Seed\":27182818,\"Items\":[{\"Id\":264,\"Template\":1}],\"Bag\":[{\"Template\":1}]}");
                    Near(legacy.Items[0].WorldScale, Vector3.zero, report, "legacy missing solo scale");
                    Near(legacy.Bag[0].WorldScale, Vector3.zero, report, "legacy missing bag scale");
                });

                Check("legacy zero and malformed scale leave existing native size unchanged", report, () =>
                {
                    var instance = NewClone();
                    var before = Measure(instance, "legacy raw instance");
                    var parent = instance.transform.parent;
                    Vector3 localScale = instance.transform.localScale;
                    foreach (Vector3 invalid in new[]
                    {
                        Vector3.zero, new Vector3(1, 0, 1), new Vector3(float.NaN, 1, 1),
                        new Vector3(1, float.PositiveInfinity, 1), new Vector3(1, 1, float.NegativeInfinity)
                    })
                    {
                        SharedItemScale.Apply(instance.transform, invalid);
                        Near(instance.transform.localScale, localScale, report, "invalid input local scale changed");
                        Require(instance.transform.parent == parent, report, "invalid input changed parent");
                        var unchanged = Measure(instance, "legacy/malformed unchanged");
                        Near(unchanged.visualSize, before.visualSize, report, "invalid input renderer changed");
                        Near(unchanged.boxSize, before.boxSize, report, "invalid input collider changed");
                    }
                });

                Check("Fusion struct and full prefab state budgets", report, () => CheckFusionBudgets(report));
                Check("nameplate protocol settings copy includes itemscale1 without mutating SDK globals", report,
                    () => Require(AvatarNamesProbe.Validate().StartsWith("PASS", StringComparison.Ordinal), report, "AvatarNamesProbe.Validate failed"));
            }
            catch (Exception error) { report.errors.Add(error.ToString()); }
            finally
            {
                try
                {
                    foreach (var clone in clones) if (clone) DisposeClone(clone, sources);
                    if (staging) DisposeClone(staging, sources);
                    if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                }
                catch (Exception error) { report.errors.Add("Cleanup: " + error); }
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "ItemScale264-20261114");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "native-scale-probe.json"), JsonUtility.ToJson(report, true));
                Result = report.errors.Count == 0 ? "PASS: native fitting, canonical scale, parents, idempotence, JSON and legacy input" : "FAIL: " + string.Join("; ", report.errors);
                Debug.Log("[Item scale264] " + Result + "; checks=" + report.checks + "; repeatApplications=" + report.repeatApplications);
            }
            if (report.errors.Count != 0) throw new InvalidOperationException(Result);
            return Result;
        }

        private static Transform Parent(string name, Vector3 scale, Vector3 euler, Transform owner)
        {
            var parent = new GameObject(name).transform;
            parent.SetParent(owner, false); parent.localScale = scale; parent.localRotation = Quaternion.Euler(euler);
            return parent;
        }

        private static void CheckFusionBudgets(Report report)
        {
            void Struct(string name, int words, int legacyCalculatedWords, int additionalWords)
            {
                var budget = new StructBudget { type = name, words = words, bytes = words * 4,
                    legacyCalculatedWords = legacyCalculatedWords, legacyCalculatedBytes = legacyCalculatedWords * 4,
                    additionalBytes = (words - legacyCalculatedWords) * 4 };
                report.structBudgets.Add(budget);
                Require(words > 0 && budget.additionalBytes == additionalWords * 4, report, name + " unexpected state word count");
            }
            Struct(nameof(SharedItemRecord), NetworkStructUtils.GetWordCount<SharedItemRecord>(), 21, 4); // scale3 + seller price1
            Struct(nameof(SharedRecoveryItem), NetworkStructUtils.GetWordCount<SharedRecoveryItem>(), 5, 3);
            foreach (Type type in new[] { typeof(SharedWorldPage), typeof(SharedDepartureState), typeof(LobbyNetworkAvatar) })
            {
                int words = NetworkBehaviourUtils.GetStaticWordCount(type);
                report.behaviourBudgets.Add(new BehaviourBudget { type = type.FullName, words = words, bytes = words * 4 });
                Require(words > 0 && words * 4 < 32768, report, type.Name + " behaviour reaches Fusion 32KiB ceiling");
            }
            foreach (string path in new[]
            {
                "Assets/Resources/Online/SharedWorldPage.prefab",
                "Assets/Resources/Online/SharedDepartureState.prefab",
                "Assets/Online/Avatars/LobbyNetworkAvatar.prefab"
            })
            {
                var obj = path.Contains("/Resources/")
                    ? Resources.Load<NetworkObject>(Path.GetFileNameWithoutExtension(path) == "SharedWorldPage" ? "Online/SharedWorldPage" : "Online/SharedDepartureState")
                    : AssetDatabase.LoadAssetAtPath<NetworkObject>(path);
                Require(obj != null && obj.NetworkedBehaviours != null, report, "Baked NetworkObject missing: " + path);
                int behaviourWords = 0;
                foreach (var behaviour in obj.NetworkedBehaviours)
                {
                    Require(behaviour != null, report, "Missing baked network behaviour: " + path);
                    behaviourWords += NetworkBehaviourUtils.GetWordCount(behaviour);
                }
                int words = NetworkObject.GetWordCount(obj);
                var budget = new ObjectBudget { prefab = path, words = words, bytes = words * 4,
                    behaviourWords = behaviourWords, additionalObjectWords = words - behaviourWords,
                    additionalObjectBytes = (words - behaviourWords) * 4, allocationHeadroomBytes = 32768 - words * 4 };
                report.objectBudgets.Add(budget);
                Require(words > 0 && words >= behaviourWords && budget.bytes < 32768, report,
                    "Full NetworkObject reaches Fusion 32KiB ceiling or has invalid baked state: " + path);
            }
        }

        private static Measurement Measure(GameObject root, string label)
        {
            Physics.SyncTransforms(); // Native bounds refresh only; never simulate physics.
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer) && !(r is TrailRenderer) && !(r is LineRenderer)).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException(label + ": no active native renderer");
            Bounds visual = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) visual.Encapsulate(renderer.bounds);
            var box = root.GetComponent<BoxCollider>();
            if (box == null || !box.enabled || box.bounds.size.sqrMagnitude <= 0)
                throw new InvalidOperationException(label + ": native root BoxCollider bounds unavailable");
            return new Measurement { label = label, worldScale = root.transform.lossyScale, visualSize = visual.size, boxSize = box.bounds.size };
        }

        private static void Check(string label, Report report, Action action)
        {
            try { action(); report.passed.Add(label); }
            catch (Exception error) { report.errors.Add(label + ": " + error); }
        }

        private static void Require(bool value, Report report, string label)
        {
            if (!value) throw new InvalidOperationException(label);
            report.checks++;
        }

        private static void Near(Vector3 actual, Vector3 expected, Report report, string label)
        {
            Near(actual.x, expected.x, report, label + ".x");
            Near(actual.y, expected.y, report, label + ".y");
            Near(actual.z, expected.z, report, label + ".z");
        }

        private static void Near(float actual, float expected, Report report, string label)
            => Require(!float.IsNaN(actual) && !float.IsInfinity(actual) && Mathf.Abs(actual - expected) <= Tolerance,
                report, label + ": " + actual + " versus " + expected);

        private static void RememberMeshes(GameObject root, HashSet<Mesh> meshes)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh) meshes.Add(filter.sharedMesh);
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) if (collider.sharedMesh) meshes.Add(collider.sharedMesh);
        }

        private static void DisposeClone(GameObject root, HashSet<Mesh> sources)
        {
            var meshes = new HashSet<Mesh>(); RememberMeshes(root, meshes);
            foreach (var builder in root.GetComponentsInChildren<ProBuilderMesh>(true)) builder.preserveMeshAssetOnDestroy = true;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) filter.sharedMesh = null;
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) collider.sharedMesh = null;
            Object.DestroyImmediate(root);
            foreach (var mesh in meshes) if (mesh && !EditorUtility.IsPersistent(mesh) && !sources.Contains(mesh)) Object.DestroyImmediate(mesh);
        }
    }
}
