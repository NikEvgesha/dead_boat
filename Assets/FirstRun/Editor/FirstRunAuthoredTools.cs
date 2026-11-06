using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.FirstRun.Editor
{
    [InitializeOnLoad]
    public static class FirstRunAuthoredTools
    {
        public const string ScenePath = "Assets/Scenes/FirstRunAuthored.unity";
        private const string Backup = "DeadBoat.FirstRunAuthored.";
        [Serializable] private sealed class BuildBackup { public string[] paths; public bool[] enabled; }
        public static string Result { get; private set; } = "not run";

        static FirstRunAuthoredTools()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode) Restore();
            };
            EditorApplication.update += Tick;
        }

        // Creation is intentionally one-shot: manual scene edits survive subsequent runs.
        [MenuItem("Tools/Dead Boat/First Run/Create Authored Scene")]
        public static string Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) return ScenePath;
            if (!AssetDatabase.CopyAsset("Assets/Scenes/LevelForest.unity", ScenePath))
                throw new InvalidOperationException("Could not copy Forest scene");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c).ToArray();
                foreach (var c in all)
                    if (c is LocationSpawner || c is CitySpawner || c is DecorationSpawner || c is ZombieSpawner ||
                        c is BossSpawner || c is LocationContentSpawner || c is LendController || c is ZoneCleaner)
                        c.enabled = false;
                var tutorial = all.OfType<TutorialManager>().Single();
                Set(tutorial, "_authoredFirstRun", true);
                var manager = new SerializedObject(all.OfType<GameManager>().Single());
                manager.FindProperty("PlayDistance").floatValue = 900;
                manager.ApplyModifiedPropertiesWithoutUndo();

                // Make every shop slot static, retaining existing tutorial targets and prices.
                // A fixed catalog order is authored once into this scene, not rolled at runtime.
                foreach (var store in all.OfType<ItemStore>())
                {
                    var so = new SerializedObject(store);
                    var collection = so.FindProperty("_collection").objectReferenceValue as StoreItemsCollection;
                    var parent = so.FindProperty("_pointsParent").objectReferenceValue as Transform;
                    if (!parent) continue;
                    int i = 0;
                    foreach (var point in parent.GetComponentsInChildren<StorePoint>(true))
                    {
                        var p = new SerializedObject(point);
                        if (!p.FindProperty("_staticItem").boolValue && collection && collection.Items.Count > 0)
                            p.FindProperty("_itemPrefab").objectReferenceValue = collection.Items[i++ % collection.Items.Count];
                        if (p.FindProperty("_itemPrefab").objectReferenceValue == null)
                            throw new InvalidOperationException("Empty authored shop slot: " + point.name);
                        p.FindProperty("_staticItem").boolValue = true;
                        p.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                var land = all.OfType<LendController>().Single();
                foreach (var t in land.locations[0].groundSegments) t.gameObject.SetActive(true);
                foreach (var loc in land.locations.Skip(1))
                    foreach (var t in loc.groundSegments) t.gameObject.SetActive(false);
                var extension = UnityEngine.Object.Instantiate(land.locations[0].groundSegments[1].gameObject);
                extension.name = "Authored bank 1000";
                extension.transform.SetParent(land.locations[0].groundSegments[1].parent, true);
                extension.transform.position = new Vector3(0, -1.01f, 1000);

                var root = new GameObject("First Run Authored Route");
                var route = root.AddComponent<FirstRunAuthoredRoute>();
                route.tutorial = tutorial;
                var first = Stop("house1_1", 300, root.transform);
                var second = Stop("house2_1", 600, root.transform);
                route.stops = new[] { first.transform, second.transform };
                route.arena = new GameObject("Mini dragon arena - reserved").transform;
                route.arena.SetParent(root.transform);
                route.arena.position = new Vector3(0, 0, 900);
                route.supplies = new[] {
                    Supply(first, 0, "goldBar"), Supply(first, 1, "coal"), Supply(first, 2, "EggChicken"),
                    Supply(second, 0, "Pistol"), Supply(second, 1, "AmmoMusket"), Supply(second, 2, "barrel")
                };
                route.guards = new[] { Guard(first), Guard(second) };
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            return ScenePath;
        }

        private static GameObject Stop(string name, float z, Transform parent)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Locations/Enemy/" + name + ".prefab"), parent);
            g.name = "Stop " + z + " - " + name;
            g.transform.position = new Vector3(0, 0, z);
            foreach (var c in g.GetComponentsInChildren<LocationContentSpawner>(true)) c.enabled = false;
            foreach (var c in g.GetComponentsInChildren<ZoneCleaner>(true)) c.enabled = false;
            foreach (var c in g.GetComponentsInChildren<QuestTrigger>(true)) c.enabled = false;
            return g;
        }
        private static PickableItem Supply(GameObject stop, int index, string name)
        {
            var point = stop.GetComponent<LocationContentSpawner>().itemSpawnPoints[index].spawnTransform;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Items/" + name + ".prefab"), stop.transform);
            g.transform.position = point.position;
            g.transform.rotation = point.rotation;
            return g.GetComponent<PickableItem>();
        }
        private static ZombieController Guard(GameObject stop)
        {
            var point = stop.GetComponent<LocationContentSpawner>().enemySpawnPoints[1];
            var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemy/Zombie.prefab"), stop.transform);
            g.transform.position = point.position;
            var guard = g.GetComponent<ZombieController>();
            guard.mobLevel = 1; guard.minHP = guard.maxHP = 12;
            guard.minSpeed = guard.maxSpeed = 1.2f;
            var so = new SerializedObject(guard);
            so.FindProperty("attackDamage").intValue = 3;
            so.FindProperty("detectionDistance").floatValue = 15;
            so.ApplyModifiedPropertiesWithoutUndo();
            return guard;
        }
        private static void Set(UnityEngine.Object target, string key, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(key).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Tools/Dead Boat/First Run/Play Authored Scene (isolated save)")]
        public static void Play()
        {
            Prepare(); EditorApplication.isPlaying = true;
        }
        public static string Prepare()
        {
            Create();
            if (SessionState.GetBool(Backup + "active", false)) throw new InvalidOperationException("Already prepared");
            var scenes = EditorBuildSettings.scenes;
            SessionState.SetString(Backup + "build", JsonUtility.ToJson(new BuildBackup {
                paths = scenes.Select(s => s.path).ToArray(), enabled = scenes.Select(s => s.enabled).ToArray()
            }));
            SessionState.SetFloat(Backup + "scale", Time.timeScale);
            DeadBoat.Online.Editor.SharedRecoveryGameplayProbe.PrepareColdRestart();
            SessionState.SetBool(Backup + "active", true);
            SessionState.SetBool(Backup + "loaded", false);
            EditorBuildSettings.scenes = scenes.Where(s => s.path != ScenePath)
                .Concat(new[] {new EditorBuildSettingsScene(ScenePath, true)}).ToArray();
            Result = "waiting for isolated Lobby";
            return Result;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Backup + "active", false) || !EditorApplication.isPlaying ||
                SessionState.GetBool(Backup + "loaded", false)) return;
            if (LoadingManager.Instance == null || LoadingManager.Instance.CurrentLocation != Location.Lobby ||
                SceneManager.GetActiveScene().name != "Lobby") return;
            if (!(SaveManager.Instance?.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave))
                throw new InvalidOperationException("Isolated save required");
            SessionState.SetBool(Backup + "loaded", true);
            LoadingManager.Instance.LoadLocation(Location.Game, "FirstRunAuthored", false);
            Result = "loading authored route";
        }
        private static void Restore()
        {
            if (!SessionState.GetBool(Backup + "active", false)) return;
            var b = JsonUtility.FromJson<BuildBackup>(SessionState.GetString(Backup + "build", ""));
            EditorBuildSettings.scenes = b.paths.Select((p, i) => new EditorBuildSettingsScene(p, b.enabled[i])).ToArray();
            Time.timeScale = SessionState.GetFloat(Backup + "scale", 1);
            SessionState.SetBool(Backup + "active", false);
        }
        public static string Validate()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c).ToArray();
                var route = all.OfType<FirstRunAuthoredRoute>().Single();
                Require(route.tutorial.AuthoredFirstRun, "authored tutorial override");
                Require(route.stops.Length == 2 && route.stops.All(t => t != null), "two route buildings");
                Require(route.supplies.Length == 6 && route.supplies.All(p => p != null), "real fixed pickups");
                Require(route.guards.Length == 2 && route.guards.All(g => g.minHP == 12 && g.maxHP == 12), "fixed guards");
                Require(all.OfType<GameManager>().Single().PlayDistance == 900, "short route");
                Require(all.OfType<StorePoint>().All(p => p.StaticItem), "fixed shops");
                Require(!all.Any(c => c.enabled && (c is LocationSpawner || c is CitySpawner || c is DecorationSpawner ||
                    c is ZombieSpawner || c is BossSpawner || c is LocationContentSpawner || c is LendController || c is ZoneCleaner)), "no random world spawn or recycling");
                var steps = new SerializedObject(route.tutorial).FindProperty("_tutorialSteps");
                Require(steps.arraySize == 7, "legacy seven-step chain");
                for (int i = 0; i < steps.arraySize; i++)
                {
                    var step = steps.GetArrayElementAtIndex(i).objectReferenceValue;
                    Require(step != null, "step reference");
                    var so = new SerializedObject(step);
                    foreach (var field in new[] { "_goal", "_line", "_item", "_storePoint", "_sellPoint" })
                    {
                        var p = so.FindProperty(field);
                        // Step8 resolves the just-purchased item from its goal at runtime.
                        if (p != null && !(step is TutorialStep8 && field == "_item"))
                            Require(p.objectReferenceValue != null, step.name + " " + field);
                    }
                }
                var gun = route.supplies[3].GetComponentInChildren<RangedWeaponController>(true);
                var ammo = route.supplies[4].GetComponent<AmmoItem>();
                Require(new SerializedObject(gun).FindProperty("weaponType").enumValueIndex ==
                    new SerializedObject(ammo).FindProperty("AmmoType").enumValueIndex, "matching ammunition");
                return "PASS: 7 original steps and scene links, 2 fixed buildings, 6 real supplies, matching ammo, 2 guards, static shops, 900m route, generators disabled";
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        // Calls the actual gameplay entry points; does not directly fire tutorial events.
        // It is an automated wiring/economy check, not a substitute for a player walkthrough.
        public static async void SmokeTutorial()
        {
            Result = "testing tutorial";
            try
            {
                Require(EditorApplication.isPlaying && SaveManager.Instance?.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave, "isolated Play required");
                var route = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>();
                Require(route && BoardController.Instance.StartGame, "authored level ready");
                var manager = route.tutorial;
                var steps = (System.Collections.Generic.List<TutorialStep>)Get(manager, "_tutorialSteps");
                Require((int)Get(manager, "_currentIndex") == 0, "original tutorial started despite old flag");
                var item = (PickableItem)Get(steps[0], "_item");
                item.PickUp(PlayerMovement.Instance.transform);
                Require((int)Get(manager, "_currentIndex") == 1, "pickup advances tutorial");
                var sell = (SellPoint)Get(steps[1], "_sellPoint");
                Call(sell, "OnTriggerEnter", item.GetComponent<Collider>());
                Require((int)Get(manager, "_currentIndex") == 2, "real sale advances tutorial");
                var bag = (MoneyBag)Get(steps[2], "_moneyBag");
                Require(bag && bag.Money > 0, "sale creates money bag at original target");
                int before = CurrencyManager.Instance.GetBalance(CurrencyType.Coins);
                int payout = bag.Money;
                Put(bag, "_progress", 1f);
                ((IEnumerator)Call(bag, "BuyProcess")).MoveNext();
                Require((int)Get(manager, "_currentIndex") == 3, "bag collection advances tutorial");
                Require(CurrencyManager.Instance.GetBalance(CurrencyType.Coins) == before + payout, "sale payout");
                var shop = (StorePoint)Get(steps[3], "_storePoint");
                int price = (int)Get(shop, "price");
                Require(CurrencyManager.Instance.GetBalance(CurrencyType.Coins) >= price, "tutorial sale funds purchase without ad");
                Put(shop, "_progress", 1f);
                ((IEnumerator)Call(shop, "BuyProcess", true)).MoveNext();
                Require((int)Get(manager, "_currentIndex") == 4, "real purchase advances tutorial");
                var bought = (PickableItem)Get(steps[4], "_item");
                Require(bought && bought.GetComponent<FuelItem>(), "purchased tutorial fuel exists at pickup target");
                bought.PickUp(PlayerMovement.Instance.transform);
                Require((int)Get(manager, "_currentIndex") == 5, "bought item pickup");
                var deposit = (FuelDeposit)Get(steps[5], "_item");
                Call(deposit, "OnTriggerEnter", bought.GetComponent<Collider>());
                Require((int)Get(manager, "_currentIndex") == 6 && BoardController.Instance.currentFuel > 0, "real refueling advances to steering");
                manager.QuickStopTutorial();
                Require((int)Get(manager, "_currentIndex") == steps.Count, "steering completion");
                await Task.Delay(500);
                Require(route.supplies.All(p => p != null) && route.guards.All(g => g != null), "hand-placed content survives startup cleaners");
                Require(UnityEngine.Object.FindObjectsByType<Fusion.NetworkRunner>(FindObjectsSortMode.None).Length == 0, "offline, no Photon allocation");
                Result = "PASS: real pickup, sale, money bag/payout, affordable purchase, fuel pickup/deposit and steering completion; route content survives; no Photon";
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError(Result); }
        }
        private static object Get(object o, string field) => o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
        private static void Put(object o, string field, object value) => o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, value);
        private static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, args);
        private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        public static string VerifyRestored()
        {
            if (EditorApplication.isPlaying || SessionState.GetBool(Backup + "active", false)) throw new Exception("Still active");
            var b = JsonUtility.FromJson<BuildBackup>(SessionState.GetString(Backup + "build", ""));
            if (!EditorBuildSettings.scenes.Select(s => s.path).SequenceEqual(b.paths) ||
                !EditorBuildSettings.scenes.Select(s => s.enabled).SequenceEqual(b.enabled) ||
                Time.timeScale != SessionState.GetFloat(Backup + "scale", 1)) throw new Exception("Settings not restored");
            return DeadBoat.Online.Editor.SharedRewardGameplayProbe.VerifyRestored() + "; build scenes and clock restored";
        }
    }
}
