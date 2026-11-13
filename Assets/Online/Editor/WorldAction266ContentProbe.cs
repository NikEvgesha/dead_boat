using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online.Editor
{
    // Source-prefab references only: no clones, Start, scene changes or gameplay execution.
    public static class WorldAction266ContentProbe
    {
        [Serializable] private sealed class Callback
        {
            public string owner, eventName, method, targetType, targetName, targetAsset, listenerState;
            public bool targetMissing;
        }
        [Serializable] private sealed class TrapRow
        {
            public string path, enemyPrefab, spawnPath;
            public int count, torchCount;
            public float cooldown, firstDelay;
            public bool boundToTrapEvent, spawnInsidePrefab;
        }
        [Serializable] private sealed class LeverRow
        {
            public string path, subject, subjectController, ownController, usePoint, hideTarget;
            public bool hideMatchesOwnUsePoint;
            public List<string> subjectClips = new List<string>();
            public List<string> subjectBindings = new List<string>();
        }
        [Serializable] private sealed class PortalRow
        {
            public string path, next;
            public bool initiallyActive, mutual, targetInsidePrefab;
        }
        [Serializable] private sealed class PodiumRow
        {
            public string path;
            public List<string> requirements = new List<string>();
            public bool configured, visualTargetInspected, visualTargetMissing;
            public int persistentCallbacks;
            public string visualTarget;
            public float activationDelay;
        }
        [Serializable] private sealed class PrefabRow
        {
            public string path, error;
            public int zoneCount, trapComponentCount, trapCallbacks, uniqueBoundTraps, authoredWaveCount, configuredPodiums;
            public List<TrapRow> traps = new List<TrapRow>();
            public List<LeverRow> levers = new List<LeverRow>();
            public List<PortalRow> portals = new List<PortalRow>();
            public List<PodiumRow> podiums = new List<PodiumRow>();
            public List<Callback> callbacks = new List<Callback>();
        }
        [Serializable] private sealed class BalanceRow
        {
            public string scene;
            public float multiplier;
            public int offset, sourceLevel, resolvedLevel;
        }
        [Serializable] private sealed class ObjectBudget
        {
            public string prefab;
            public int words, bytes, behaviourWords, additionalObjectBytes, headroomBytes;
        }
        [Serializable] private sealed class Report
        {
            public string utc, unityVersion, result;
            public string scope = "Native Editor source-prefab loading and resolved persistent UnityEvent targets/authoring references. Empty-requirement decorative podiums are reported, not treated as active Skull offerings. Configured podium callback2 is inspected through native GetPersistentTarget; a resolved visual is reported as valid and a null is recorded as an outstanding content finding, never required to be null. No prefab instances, scene loading, Start/Update, Play, physics, animations, callbacks, networking, Player Build, SaveAssets or settings mutation. This is not network or complete gameplay acceptance.";
            public string balanceScope = "Pure LocationSceneBalance.ResolveEnemyLevel(source) for real profile rows. The live LocationContentSpawner helper is not invoked on source assets: EnsureBalanceProfile can populate a serialized cache. No source SetLevel/Start is called; guard chance/count caps are not applied to scripted waves.";
            public string budgetScope = "Full baked NetworkObject.GetWordCount includes SDK object words and all baked behaviours. Capacity bytes only, not traffic, history, external allocator overhead or measured Web heap. Hard acceptance is strictly below32768 bytes.";
            public string activeEditorScene;
            public bool scriptedLevelHelperPresent;
            public int checks;
            public List<PrefabRow> prefabs = new List<PrefabRow>();
            public List<BalanceRow> balance = new List<BalanceRow>();
            public List<ObjectBudget> objectBudgets = new List<ObjectBudget>();
            public List<string> findings = new List<string>();
            public List<string> errors = new List<string>();
        }

        public static string Run()
        {
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                activeEditorScene = SceneManager.GetActiveScene().name };
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Requires EditMode; existing play state was not changed.");
                foreach (string path in new[]
                {
                    "Assets/Prefabs/Locations/Enemy/Sklep_1.prefab",
                    "Assets/Prefabs/Locations/Enemy/ruins_1.prefab",
                    "Assets/Events/NewYear/NY_Ruins_1.prefab",
                    "Assets/Prefabs/Workshop/Sklep1/Sklep1_OvergrownRuins.prefab"
                })
                {
                    var row = new PrefabRow { path = path }; report.prefabs.Add(row);
                    try { InspectPrefab(path, row, report); }
                    catch (Exception error) { row.error = error.ToString(); report.errors.Add(path + ": " + error); }
                }
                try { InspectBalance(report); } catch (Exception error) { report.errors.Add("Balance: " + error); }
                foreach (string path in new[] { "Assets/Resources/Online/SharedWorldPage.prefab",
                    "Assets/Resources/Online/SharedDepartureState.prefab", "Assets/Online/Avatars/LobbyNetworkAvatar.prefab" })
                    try { InspectBudget(path, report); } catch (Exception error) { report.errors.Add(path + " budget: " + error); }
            }
            catch (Exception error) { report.errors.Add(error.ToString()); }
            finally
            {
                report.result = report.errors.Count == 0 ? "PASS: source content references and full Fusion object capacities" : "FAIL: source content audit";
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "WorldActions266-20261114");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "content.json"), JsonUtility.ToJson(report, true));
                Debug.Log("[World action266 content] " + report.result + "; checks=" + report.checks +
                    "; findings=" + report.findings.Count + "; errors=" + report.errors.Count);
            }
            if (report.errors.Count != 0) throw new InvalidOperationException(report.result + ": " + string.Join("; ", report.errors));
            return report.result;
        }

        private static void InspectPrefab(string path, PrefabRow row, Report report)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!Check(report, root != null, path + " missing prefab")) return;
            var zones = root.GetComponentsInChildren<ActivateZone>(true); row.zoneCount = zones.Length;
            var traps = root.GetComponentsInChildren<DungeonTrap>(true); row.trapComponentCount = traps.Length;
            var activated = new HashSet<DungeonTrap>();
            foreach (var zone in zones)
            {
                RecordCallbacks(zone, "PlayerOnTriggerEnter", zone.PlayerOnTriggerEnter, row);
                RecordCallbacks(zone, "PlayerOnTriggerExit", zone.PlayerOnTriggerExit, row);
                foreach (var events in new[] { zone.PlayerOnTriggerEnter, zone.PlayerOnTriggerExit })
                    for (int i = 0; i < events.GetPersistentEventCount(); i++)
                    {
                        string method = events.GetPersistentMethodName(i);
                        if (method != "_InTrap" && method != "_InDungeons") continue;
                        var trap = events.GetPersistentTarget(i) as DungeonTrap;
                        Check(report, trap != null && traps.Contains(trap), path + " zone trap target is missing/external");
                        if (method == "_InTrap" && trap != null) { row.trapCallbacks++; activated.Add(trap); }
                    }
            }
            row.uniqueBoundTraps = activated.Count;
            foreach (var trap in traps)
            {
                var serialized = new SerializedObject(trap);
                var torchs = serialized.FindProperty("_torchs");
                var spawn = trap.ScriptedSpawnPosition;
                bool inside = spawn != null && (spawn == root.transform || spawn.IsChildOf(root.transform));
                row.traps.Add(new TrapRow { path = Relative(trap.transform, root.transform),
                    enemyPrefab = AssetDatabase.GetAssetPath(trap.ScriptedEnemyPrefab), spawnPath = spawn != null ? Relative(spawn, root.transform) : null,
                    count = trap.ScriptedSpawnCount, torchCount = torchs.arraySize, cooldown = trap.ScriptedCooldown,
                    firstDelay = trap.ScriptedFirstDelay, boundToTrapEvent = activated.Contains(trap), spawnInsidePrefab = inside });
                Check(report, trap.ScriptedEnemyPrefab != null && inside, path + " invalid enemy/spawn binding: " + trap.name);
                if (activated.Contains(trap)) row.authoredWaveCount += trap.ScriptedSpawnCount;
                for (int i = 0; i < torchs.arraySize; i++)
                    Check(report, torchs.GetArrayElementAtIndex(i).objectReferenceValue != null, path + " missing torch " + i);
            }
            bool shop = path.Contains("Sklep");
            Check(report, row.trapCallbacks == (shop ? 4 : 1) && row.uniqueBoundTraps == (shop ? 4 : 1) &&
                row.authoredWaveCount == (shop ? 12 : 20), path + " authored wave must be four*three or one*twenty");
            Check(report, row.trapComponentCount == (shop ? 5 : 1), path + " source trap count changed; do not activate unbound extra trap");
            foreach (var lever in root.GetComponentsInChildren<Lever>(true)) InspectLever(root, lever, row, report);
            Check(report, row.levers.Count > 0, path + " no actual lever binding");
            foreach (var portal in root.GetComponentsInChildren<Portal>(true))
            {
                var so = new SerializedObject(portal);
                var next = so.FindProperty("_nextPortal").objectReferenceValue as Portal;
                bool mutual = next != null && new SerializedObject(next).FindProperty("_nextPortal").objectReferenceValue == portal;
                bool inside = next != null && next.transform.IsChildOf(root.transform);
                bool active = so.FindProperty("_isActivate").boolValue;
                row.portals.Add(new PortalRow { path = Relative(portal.transform, root.transform),
                    next = next != null ? Relative(next.transform, root.transform) : null,
                    initiallyActive = active, mutual = mutual, targetInsidePrefab = inside });
                Check(report, mutual && inside && !active, path + " portal pair must be mutual, local and initially inactive");
            }
            foreach (var podium in root.GetComponentsInChildren<CheckItemOnLocation>(true)) InspectPodium(root, podium, row, report);
            if (!shop) Check(report, row.portals.Count == 2 && row.podiums.Count == 4 && row.configuredPodiums == 1,
                path + " expected actual portal pair and four podiums with one configured Skull offering");
        }

        private static void InspectLever(GameObject root, Lever lever, PrefabRow row, Report report)
        {
            var entry = new LeverRow { path = Relative(lever.transform, root.transform),
                subject = lever._activeSubject != null ? Relative(lever._activeSubject.transform, root.transform) : null };
            row.levers.Add(entry);
            Check(report, lever._activeSubject != null, row.path + " missing lever subject");
            var configured = new SerializedObject(lever).FindProperty("_useButtonShow").objectReferenceValue as UseButtonShow;
            var resolved = configured != null ? configured : lever.GetComponentInChildren<UseButtonShow>(true);
            var authored = root.GetComponentsInChildren<UseButtonShow>(true).Where(use => use.Activate != null && Enumerable.Range(0, use.Activate.GetPersistentEventCount())
                .Any(i => use.Activate.GetPersistentTarget(i) == lever && use.Activate.GetPersistentMethodName(i) == "Use")).ToArray();
            if (Check(report, authored.Length == 1, row.path + " lever must have one actual authored UseButton event"))
            {
                var use = authored[0]; entry.usePoint = Relative(use.transform, root.transform);
                RecordCallbacks(use, "Activate", use.Activate, row);
                int count = use.Activate.GetPersistentEventCount();
                Check(report, count == 2, row.path + " lever event has extra/missing callbacks requiring replay");
                if (count >= 2)
                {
                    var hide = use.Activate.GetPersistentTarget(1) as GameObject;
                    entry.hideTarget = hide != null ? Relative(hide.transform, root.transform) : null;
                    entry.hideMatchesOwnUsePoint = hide == use.gameObject && resolved == use;
                    Check(report, use.Activate.GetPersistentTarget(0) == lever && use.Activate.GetPersistentMethodName(0) == "Use" &&
                        use.Activate.GetPersistentMethodName(1) == "SetActive" && entry.hideMatchesOwnUsePoint,
                        row.path + " adapter must hide exactly the authored UseButton GO");
                    Check(report, !BooleanArgument(use, "Activate", 1), row.path + " authored UseButton hide argument must be false");
                }
            }
            Animator subjectAnimator = lever._activeSubject != null
                ? new SerializedObject(lever._activeSubject).FindProperty("animator").objectReferenceValue as Animator : null;
            entry.subjectController = subjectAnimator != null ? AssetDatabase.GetAssetPath(subjectAnimator.runtimeAnimatorController) : null;
            InspectAnimator(subjectAnimator, row.path + " subject", report, entry.subjectClips, entry.subjectBindings);
            var ownAnimator = lever._animator != null ? lever._animator : lever.GetComponent<Animator>();
            entry.ownController = ownAnimator != null ? AssetDatabase.GetAssetPath(ownAnimator.runtimeAnimatorController) : null;
            InspectAnimator(ownAnimator, row.path + " lever", report, null, null);
        }

        private static void InspectAnimator(Animator animator, string label, Report report, List<string> clips, List<string> bindings)
        {
            if (!Check(report, animator != null && animator.runtimeAnimatorController != null, label + " missing controller")) return;
            var runtime = animator.runtimeAnimatorController;
            while (runtime is AnimatorOverrideController overridden) runtime = overridden.runtimeAnimatorController;
            var controller = runtime as AnimatorController;
            Check(report, controller != null && controller.parameters.Any(p => p.name == "Start" && p.type == AnimatorControllerParameterType.Trigger),
                label + " missing Start trigger");
            var animations = animator.runtimeAnimatorController.animationClips.Where(c => c != null).Distinct().ToArray();
            Check(report, animations.Length > 0, label + " no author animation clips");
            foreach (var clip in animations)
            {
                clips?.Add(AssetDatabase.GetAssetPath(clip));
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    bindings?.Add(clip.name + ":" + binding.path + ":" + binding.propertyName);
            }
        }

        private static void InspectPodium(GameObject root, CheckItemOnLocation podium, PrefabRow row, Report report)
        {
            var entry = new PodiumRow { path = Relative(podium.transform, root.transform) }; row.podiums.Add(entry);
            var so = new SerializedObject(podium); var required = so.FindProperty("ItemCheck");
            for (int i = 0; i < required.arraySize; i++)
            {
                var item = required.GetArrayElementAtIndex(i).objectReferenceValue as ItemData;
                entry.requirements.Add(item != null ? item.Name : "MISSING");
            }
            RecordCallbacks(podium, "PutItem", podium.PutItem, row);
            int count = podium.PutItem.GetPersistentEventCount();
            entry.persistentCallbacks = count;
            entry.configured = entry.requirements.Count > 0;
            if (!entry.configured) return; // The production handler also returns for empty decorative requirements.
            row.configuredPodiums++;
            Check(report, entry.requirements.Count == 1 && entry.requirements[0] == "Skull", row.path + " configured podium requires Skull");
            Check(report, count == 3, row.path + " podium expects unlock/disable/visual callbacks");
            if (count < 3) return;
            Check(report, podium.PutItem.GetPersistentTarget(0) is Portal && podium.PutItem.GetPersistentMethodName(0) == "_ActivateTeleport",
                row.path + " missing actual Portal unlock callback");
            Check(report, podium.PutItem.GetPersistentTarget(1) is Collider && podium.PutItem.GetPersistentMethodName(1) == "set_enabled" &&
                !BooleanArgument(podium, "PutItem", 1), row.path + " podium trigger must disable after accepted offering");
            var calls = so.FindProperty("PutItem.m_PersistentCalls.m_Calls");
            entry.activationDelay = calls.GetArrayElementAtIndex(0).FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue;
            var visual = podium.PutItem.GetPersistentTarget(2);
            entry.visualTargetInspected = true;
            entry.visualTargetMissing = visual == null; entry.visualTarget = visual != null ? visual.name : null;
            Check(report, podium.PutItem.GetPersistentMethodName(2) == "SetActive" && BooleanArgument(podium, "PutItem", 2),
                row.path + " podium callback2 must be authored visual enable");
            if (entry.visualTargetMissing)
                report.findings.Add(row.path + ": podium callback2 GetPersistentTarget(2) is null; legacy visual reference requires separate content repair, no asset was changed.");
            else Check(report, visual is GameObject, row.path + " podium visual target type changed");
        }

        private static void InspectBalance(Report report)
        {
            report.scriptedLevelHelperPresent = typeof(LocationContentSpawner).GetMethod("ResolveScriptedEnemyLevel") != null;
            Check(report, report.scriptedLevelHelperPresent, "scripted level resolver missing");
            var profile = AssetDatabase.LoadAssetAtPath<LocationBalanceProfile>("Assets/Resources/LocationBalanceProfile.asset");
            if (!Check(report, profile != null, "production location balance profile missing")) return;
            foreach (string scene in new[] { "LevelForest", "LevelDesert", "LevelWinter", "LevelEvent" })
                foreach (int source in new[] { 1, 5, 10 })
                {
                    var balance = profile.GetBalanceForScene(scene);
                    int resolved = balance.ResolveEnemyLevel(source);
                    report.balance.Add(new BalanceRow { scene = scene, multiplier = balance.enemyLevelMultiplier,
                        offset = balance.enemyLevelOffset, sourceLevel = source, resolvedLevel = resolved });
                    Check(report, resolved >= 1 && resolved <= 10, scene + " scripted level out of range");
                }
        }

        private static void InspectBudget(string path, Report report)
        {
            var obj = AssetDatabase.LoadAssetAtPath<NetworkObject>(path);
            if (!Check(report, obj != null && obj.NetworkedBehaviours != null, path + " missing baked NetworkObject")) return;
            int behaviours = 0;
            foreach (var behaviour in obj.NetworkedBehaviours)
                if (Check(report, behaviour != null, path + " missing baked behaviour")) behaviours += NetworkBehaviourUtils.GetWordCount(behaviour);
            int words = NetworkObject.GetWordCount(obj);
            report.objectBudgets.Add(new ObjectBudget { prefab = path, words = words, bytes = words * 4,
                behaviourWords = behaviours, additionalObjectBytes = (words - behaviours) * 4, headroomBytes = 32768 - words * 4 });
            Check(report, words > 0 && words >= behaviours && words * 4 < 32768, path + " full object capacity reaches32768 bytes or is invalid");
        }

        private static void RecordCallbacks(Component owner, string name, UnityEventBase events, PrefabRow row)
        {
            if (events == null) return;
            for (int i = 0; i < events.GetPersistentEventCount(); i++)
            {
                var target = events.GetPersistentTarget(i);
                row.callbacks.Add(new Callback { owner = Relative(owner.transform, owner.transform.root), eventName = name + "[" + i + "]",
                    method = events.GetPersistentMethodName(i), targetMissing = target == null, targetName = target != null ? target.name : null,
                    targetType = target != null ? target.GetType().FullName : null, targetAsset = target != null ? AssetDatabase.GetAssetPath(target) : null,
                    listenerState = events.GetPersistentListenerState(i).ToString() });
            }
        }
        private static bool BooleanArgument(Component owner, string eventName, int index) => new SerializedObject(owner)
            .FindProperty(eventName + ".m_PersistentCalls.m_Calls").GetArrayElementAtIndex(index)
            .FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue;
        private static string Relative(Transform target, Transform root)
        {
            if (target == null) return null;
            var names = new List<string>();
            for (var current = target; current != null; current = current.parent)
            { names.Add(current.name + "[" + current.GetSiblingIndex() + "]"); if (current == root) break; }
            names.Reverse(); return string.Join("/", names);
        }
        private static bool Check(Report report, bool value, string error)
        { report.checks++; if (!value) report.errors.Add(error); return value; }
    }
}
