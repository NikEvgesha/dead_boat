using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ProBuilder;

namespace DeadBoat.EditorChecks
{
    // Run only in an already imported Editor, via -executeMethod. No Play/Build/save operations.
    public static class IncomingContentAudit
    {
        private const string EventScene = "Assets/Scenes/LevelEvent.unity";
        private const string LevelsPath = "Assets/Scripts/Lobby/Levels.asset";

        [Serializable]
        private sealed class AssetResult
        {
            public string path;
            public string kind;
            public bool loaded;
            public int roots;
            public int transforms;
            public int missingScripts;
            public int meshFilters;
            public int activeMeshFilters;
            public int meshColliders;
            public int enabledActiveMeshColliders;
            public int proceduralPendingReferences;
        }

        [Serializable]
        private sealed class ProceduralPending
        {
            public string prefab;
            public string hierarchy;
            public int vertices;
            public int faces;
            public bool filterPending;
            public bool colliderPending;
        }

        [Serializable]
        private sealed class GameplayFinding
        {
            public string task = "DB-ON-264";
            public string scope = "Edit Mode construction only: fitted production placement versus a raw catalogue clone. This is not a Photon receiver, recovery or physics test; mismatch alone does not prove a current network bug.";
            public string prefab;
            public bool measured;
            public bool rawVsFittedMismatch;
            public Vector3 rawRootScale;
            public Vector3 fittedRootScale;
            public Vector3 rawBoundsSize;
            public Vector3 fittedBoundsSize;
            public float rawWidth;
            public float fittedWidth;
            public float maxVisualFootprint = .72f;
            public string note;
        }

        [Serializable]
        private sealed class KnownOutstanding
        {
            public string task = "DB-ON-259";
            public string guid;
            public string purpose;
            public string resolvedPath;
            public bool stillMissing;
        }

        [Serializable]
        private sealed class Report
        {
            public string utc;
            public string unityVersion;
            public bool batchMode;
            public string graphicsDevice;
            public string scope = "Native source loading plus strict isolated preview-clone construction checks. No Play, Build, physics simulation, shader support assertion, navigation, balance or online acceptance.";
            public bool eventSceneAlreadyOpen;
            public bool eventEnabledInBuild;
            public int eventBuildEntries;
            public int levelCount;
            public int eventLevelIndex = -1;
            public int eventLevelEntries;
            public bool eventIconAssigned;
            public int prefabCount;
            public bool passed;
            public int previewPrefabCount;
            public bool previewExperimentalAssetCachingBlocked;
            public int constructionInstances;
            public int protectedCloneProBuilderComponents;
            public int deletedTransientCloneMeshes;
            public string previewScope = "Source-null meshes are deferred only when that same GameObject has nonempty ProBuilder vertex/face arrays. Preview clones use ordinary Instantiate/Awake only; no forced mesh rebuilding. Cleanup changes preserveMeshAssetOnDestroy only on clones, detaches clone mesh references and deletes only nonpersistent meshes not present on sources.";
            public List<string> proceduralPrefabPaths = new List<string>();
            public List<ProceduralPending> proceduralPending = new List<ProceduralPending>();
            public List<AssetResult> previewAssets = new List<AssetResult>();
            public List<GameplayFinding> knownGameplayFindings = new List<GameplayFinding>();
            public List<AssetResult> assets = new List<AssetResult>();
            public List<KnownOutstanding> knownOutstanding = new List<KnownOutstanding>();
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
        }

        public static void Run()
        {
            var report = new Report
            {
                utc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                batchMode = Application.isBatchMode,
                graphicsDevice = SystemInfo.graphicsDeviceType.ToString()
            };
            Scene opened = default(Scene);
            bool ownsOpenedScene = false;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("IncomingContentAudit requires Edit Mode; the current session was not stopped.");

                foreach (string folder in new[] { "Assets/Prefabs/Locations/Enemy", "Assets/Prefabs/Items/Loot203" })
                {
                    if (!AssetDatabase.IsValidFolder(folder)) { report.errors.Add("Missing audit folder: " + folder); continue; }
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                    if (guids.Length == 0) report.errors.Add("No imported prefabs found in " + folder);
                    foreach (string guid in guids.OrderBy(value => AssetDatabase.GUIDToAssetPath(value), StringComparer.Ordinal))
                        AuditPrefab(AssetDatabase.GUIDToAssetPath(guid), report);
                }
                AuditPrefab("Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab", report);
                AuditPrefab("Assets/Events/NewYear/Boss244/SleighThief_Boss.prefab", report);

                AuditPreviewConstruction(report);

                foreach (var pair in new[]
                {
                    new[] { "070a68964028def489b88b991aedbdbb", "Inherited house/Clocks NavMeshData override" },
                    new[] { "870d564c8687e0940ae7b991de0a9125", "Inherited Lizzi UI sprite" }
                })
                {
                    string resolved = AssetDatabase.GUIDToAssetPath(pair[0]);
                    report.knownOutstanding.Add(new KnownOutstanding
                    {
                        guid = pair[0], purpose = pair[1], resolvedPath = resolved,
                        stillMissing = string.IsNullOrEmpty(resolved)
                    });
                }

                CheckLevelRegistry(report);
                opened = SceneManager.GetSceneByPath(EventScene);
                report.eventSceneAlreadyOpen = opened.IsValid() && opened.isLoaded;
                if (!report.eventSceneAlreadyOpen)
                {
                    opened = EditorSceneManager.OpenScene(EventScene, OpenSceneMode.Additive);
                    ownsOpenedScene = true;
                }
                var sceneResult = new AssetResult { path = EventScene, kind = "scene", loaded = opened.IsValid() && opened.isLoaded };
                report.assets.Add(sceneResult);
                if (!sceneResult.loaded) report.errors.Add("Scene did not load: " + EventScene);
                else
                {
                    foreach (var root in opened.GetRootGameObjects())
                    {
                        sceneResult.roots++;
                        AuditHierarchy(root, sceneResult, report);
                    }
                    if (sceneResult.roots == 0) report.errors.Add("Event scene has no loaded roots.");
                }
            }
            catch (Exception exception)
            {
                report.errors.Add("Unhandled audit exception: " + exception);
            }
            finally
            {
                // Never close a scene that was present before this method; never save any scene.
                if (ownsOpenedScene && opened.IsValid() && opened.isLoaded)
                {
                    try
                    {
                        if (opened.isDirty) report.warnings.Add("Opening LevelEvent caused a dirty scene; audit closed only its own scene without saving.");
                        if (!EditorSceneManager.CloseScene(opened, true)) report.errors.Add("Could not close audit-owned event scene without saving.");
                    }
                    catch (Exception exception) { report.errors.Add("Closing audit-owned scene: " + exception); }
                }
                report.passed = report.errors.Count == 0;
                string project = Path.GetDirectoryName(Application.dataPath);
                string output = Path.Combine(project, "Logs", "IncomingAudit20261113", "native-content-audit.json");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                Debug.Log("[IncomingContentAudit] prefabs=" + report.prefabCount + "; assets=" + report.assets.Count + "; previewClones=" + report.previewPrefabCount +
                    "; errors=" + report.errors.Count + "; warnings=" + report.warnings.Count + "; report=" + output);
            }
            if (!report.passed)
                throw new InvalidOperationException("IncomingContentAudit failed with " + report.errors.Count + " error(s); inspect native-content-audit.json.");
        }

        private static void AuditPrefab(string path, Report report)
        {
            var result = new AssetResult { path = path, kind = "prefab" };
            report.assets.Add(result);
            report.prefabCount++;
            try
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                result.loaded = root != null;
                if (!root) { report.errors.Add("Prefab failed native loading: " + path); return; }
                result.roots = 1;
                AuditHierarchy(root, result, report, true);
            }
            catch (Exception exception) { report.errors.Add(path + ": " + exception); }
        }

        private static void AuditHierarchy(GameObject root, AssetResult result, Report report, bool allowProceduralSource = false)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                result.transforms++;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                result.missingScripts += missing;
                string path = result.path + " :: " + HierarchyPath(child, root.transform);
                if (missing > 0) report.errors.Add(path + ": missing scripts=" + missing);
                bool active = ActiveUnderRoot(child, root.transform);
                var builder = allowProceduralSource ? child.GetComponent<ProBuilderMesh>() : null;
                // These two getters read serialized arrays; do not query pb.mesh or rebuild a source.
                bool canDefer = builder != null && builder.vertexCount > 0 && builder.faceCount > 0;
                bool filterPending = false, colliderPending = false;
                foreach (var filter in child.GetComponents<MeshFilter>())
                {
                    result.meshFilters++;
                    if (!active) continue;
                    result.activeMeshFilters++;
                    if (filter.sharedMesh == null)
                    {
                        if (canDefer) { filterPending = true; result.proceduralPendingReferences++; }
                        else report.errors.Add(path + ": active MeshFilter.sharedMesh is null.");
                    }
                }
                foreach (var collider in child.GetComponents<MeshCollider>())
                {
                    result.meshColliders++;
                    if (!active || !collider.enabled) continue;
                    result.enabledActiveMeshColliders++;
                    if (collider.sharedMesh == null)
                    {
                        if (canDefer) { colliderPending = true; result.proceduralPendingReferences++; }
                        else report.errors.Add(path + ": enabled active MeshCollider.sharedMesh is null.");
                    }
                }
                if (filterPending || colliderPending)
                {
                    report.proceduralPending.Add(new ProceduralPending
                    {
                        prefab = result.path, hierarchy = HierarchyPath(child, root.transform),
                        vertices = builder.vertexCount, faces = builder.faceCount,
                        filterPending = filterPending, colliderPending = colliderPending
                    });
                    if (!report.proceduralPrefabPaths.Contains(result.path)) report.proceduralPrefabPaths.Add(result.path);
                    report.warnings.Add(path + ": proceduralPending source references; nonempty ProBuilder geometry requires a strict ordinary preview-clone check.");
                }
            }
        }

        private static void AuditPreviewConstruction(Report report)
        {
            Scene preview = default(Scene);
            GameObject parent = null;
            var sourceMeshes = new HashSet<Mesh>();
            var transientMeshes = new HashSet<Mesh>();
            try
            {
                // The PB editor initialization listener can cache generated meshes as assets
                // when this package-level experimental define is present. Inspect compilation
                // metadata only: accessing internal Experimental itself can save PB settings.
                var builderEditor = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                    .FirstOrDefault(value => value.name == "Unity.ProBuilder.Editor");
                if (builderEditor == null || builderEditor.defines.Contains("PROBUILDER_EXPERIMENTAL_FEATURES"))
                {
                    report.previewExperimentalAssetCachingBlocked = true;
                    report.errors.Add("Preview skipped: cannot prove ProBuilder experimental asset caching is disabled; no project settings were changed.");
                    return;
                }
                preview = EditorSceneManager.NewPreviewScene();
                parent = new GameObject("IncomingContentAudit_TransientPreview");
                SceneManager.MoveGameObjectToScene(parent, preview);
                parent.hideFlags = HideFlags.HideAndDontSave;
                foreach (string path in report.proceduralPrefabPaths.OrderBy(value => value, StringComparer.Ordinal))
                {
                    var result = new AssetResult { path = path, kind = "preview-prefab-clone" };
                    report.previewAssets.Add(result);
                    report.previewPrefabCount++;
                    try
                    {
                        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (!source) { report.errors.Add("Preview source missing: " + path); continue; }
                        RememberMeshes(source, sourceMeshes);
                        var clone = UnityEngine.Object.Instantiate(source, parent.transform, false);
                        result.loaded = clone != null;
                        if (!clone) { report.errors.Add("Preview clone failed: " + path); continue; }
                        result.roots = 1;
                        // Do not invoke ToMesh/Refresh/Rebuild or a manual Awake. Natural lifecycle is the subject.
                        AuditHierarchy(clone, result, report);
                        var original = report.assets.FirstOrDefault(value => value.kind == "prefab" && value.path == path);
                        if (original != null && (result.transforms < original.transforms ||
                            result.activeMeshFilters < original.activeMeshFilters ||
                            result.enabledActiveMeshColliders < original.enabledActiveMeshColliders))
                            report.errors.Add("Preview lost source hierarchy or active mesh components: " + path);
                    }
                    catch (Exception exception) { report.errors.Add("Preview clone " + path + ": " + exception); }
                }
                CheckCofferConstruction(parent.transform, sourceMeshes, report);
            }
            catch (Exception exception) { report.errors.Add("Preview construction setup: " + exception); }
            finally
            {
                // ProBuilder's Editor OnDestroy can destroy a shared Mesh asset. Protect clones only,
                // including any partially constructed child left behind by a failed Instantiate.
                if (parent)
                {
                    try
                    {
                        RememberMeshes(parent, transientMeshes);
                        foreach (var builder in parent.GetComponentsInChildren<ProBuilderMesh>(true))
                        { builder.preserveMeshAssetOnDestroy = true; report.protectedCloneProBuilderComponents++; }
                        foreach (var filter in parent.GetComponentsInChildren<MeshFilter>(true)) filter.sharedMesh = null;
                        foreach (var collider in parent.GetComponentsInChildren<MeshCollider>(true)) collider.sharedMesh = null;
                        UnityEngine.Object.DestroyImmediate(parent);
                    }
                    catch (Exception exception) { report.errors.Add("Preview clone cleanup: " + exception); }
                }
                if (preview.IsValid())
                {
                    try { EditorSceneManager.ClosePreviewScene(preview); }
                    catch (Exception exception) { report.errors.Add("Preview scene cleanup: " + exception); }
                }
                foreach (var mesh in transientMeshes)
                {
                    if (!mesh || EditorUtility.IsPersistent(mesh) || sourceMeshes.Contains(mesh)) continue;
                    try { UnityEngine.Object.DestroyImmediate(mesh); report.deletedTransientCloneMeshes++; }
                    catch (Exception exception) { report.errors.Add("Transient mesh cleanup: " + exception); }
                }
            }
        }

        private static void RememberMeshes(GameObject root, HashSet<Mesh> meshes)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh) meshes.Add(filter.sharedMesh);
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) if (collider.sharedMesh) meshes.Add(collider.sharedMesh);
        }

        private static void CheckCofferConstruction(Transform parent, HashSet<Mesh> sourceMeshes, Report report)
        {
            const string path = "Assets/Prefabs/Items/Loot203/Loot203_T10_Coffer.prefab";
            var finding = new GameplayFinding { prefab = path };
            report.knownGameplayFindings.Add(finding);
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!source) { finding.note = "Coffer source was not loaded; source integrity is reported separately."; return; }
                RememberMeshes(source, sourceMeshes);
                var slot = new GameObject("Coffer_Fitted_Slot");
                slot.transform.SetParent(parent, false);
                var fitted = UnityEngine.Object.Instantiate(source, parent, false);
                report.constructionInstances++;
                fitted.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
                var raw = UnityEngine.Object.Instantiate(source, parent, false);
                report.constructionInstances++;
                raw.transform.SetPositionAndRotation(parent.position + Vector3.right * 5, parent.rotation);
                LocationContentSpawner.PlaceItemAtSpawnPoint(fitted, new LocationSpawnPoint
                {
                    spawnTransform = slot.transform, alignVisualBase = true, maxVisualFootprint = finding.maxVisualFootprint
                });
                finding.fittedRootScale = fitted.transform.localScale;
                finding.rawRootScale = raw.transform.localScale;
                if (!VisualBounds(fitted, out var fittedBounds) || !VisualBounds(raw, out var rawBounds))
                { finding.note = "Visible renderer bounds could not be measured."; return; }
                finding.measured = true;
                finding.fittedBoundsSize = fittedBounds.size;
                finding.rawBoundsSize = rawBounds.size;
                finding.fittedWidth = Mathf.Max(fittedBounds.size.x, fittedBounds.size.z);
                finding.rawWidth = Mathf.Max(rawBounds.size.x, rawBounds.size.z);
                finding.rawVsFittedMismatch = Mathf.Abs(finding.rawWidth - finding.fittedWidth) > .001f ||
                    (finding.rawRootScale - finding.fittedRootScale).sqrMagnitude > .000001f;
                finding.note = finding.rawVsFittedMismatch ?
                    "Production fitting changes this instance versus a raw catalogue clone. No live receiver/recovery reconstruction was exercised." :
                    "No root-scale/visible-footprint difference measured in this construction check.";
            }
            catch (Exception exception) { finding.note = "Construction measurement failed: " + exception; }
        }

        private static bool VisualBounds(GameObject item, out Bounds bounds)
        {
            bounds = default(Bounds);
            bool found = false;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
                bool visible = true;
                for (var cursor = renderer.transform; cursor != item.transform; cursor = cursor.parent)
                    if (!cursor || !cursor.gameObject.activeSelf) { visible = false; break; }
                if (!visible) continue;
                if (found) bounds.Encapsulate(renderer.bounds);
                else { bounds = renderer.bounds; found = true; }
            }
            return found;
        }

        private static bool ActiveUnderRoot(Transform value, Transform root)
        {
            for (var cursor = value; cursor != null; cursor = cursor.parent)
            {
                if (!cursor.gameObject.activeSelf) return false;
                if (cursor == root) return true;
            }
            return false;
        }

        private static string HierarchyPath(Transform value, Transform root)
        {
            string path = value.name;
            while (value != root && value.parent != null) { value = value.parent; path = value.name + "/" + path; }
            return path;
        }

        private static void CheckLevelRegistry(Report report)
        {
            foreach (var scene in EditorBuildSettings.scenes)
                if (string.Equals(scene.path, EventScene, StringComparison.Ordinal))
                { report.eventBuildEntries++; report.eventEnabledInBuild |= scene.enabled; }
            if (report.eventBuildEntries != 1 || !report.eventEnabledInBuild)
                report.errors.Add("LevelEvent must have exactly one enabled EditorBuildSettings entry.");

            var levels = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LevelsPath);
            if (!levels) { report.errors.Add("Level catalogue did not load: " + LevelsPath); return; }
            using (var serialized = new SerializedObject(levels))
            {
                var entries = serialized.FindProperty("_levels");
                if (entries == null || !entries.isArray) { report.errors.Add("Level catalogue has no _levels array."); return; }
                report.levelCount = entries.arraySize;
                UnityEngine.Object forestIcon = null;
                UnityEngine.Object eventIcon = null;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var sceneName = entry.FindPropertyRelative("_sceneName");
                    var image = entry.FindPropertyRelative("_lvlImg");
                    if (sceneName == null) { report.errors.Add("Level " + i + " has no scene name field."); continue; }
                    if (sceneName.stringValue == "LevelForest") forestIcon = image == null ? null : image.objectReferenceValue;
                    if (sceneName.stringValue != "LevelEvent") continue;
                    report.eventLevelEntries++; report.eventLevelIndex = i;
                    eventIcon = image == null ? null : image.objectReferenceValue;
                    report.eventIconAssigned = eventIcon != null;
                }
                if (report.eventLevelEntries != 1) report.errors.Add("Expected exactly one LevelEvent catalogue entry.");
                if (report.eventLevelIndex != 3) report.errors.Add("LevelEvent must retain stable catalogue index 3.");
                if (!report.eventIconAssigned) report.warnings.Add("Event level has no assigned card icon.");
                else if (eventIcon == forestIcon) report.warnings.Add("Event card still uses the Forest icon; presentation is pending.");
            }
        }
    }
}
