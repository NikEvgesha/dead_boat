using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online.Editor
{
    public static class SharedStoreSeedProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] Seeds = { 0, 1, -1, int.MinValue, 124578 };
        private static int stores, slots, comparisons, legacyDivergences;

        [MenuItem("Tools/Online/Validate Store Seeds")]
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode");
            var randomState = UnityEngine.Random.state;
            stores = slots = comparisons = legacyDivergences = 0;
            try
            {
                SharedRunValidation.Validate();
                foreach (string level in new[] { "LevelForest", "LevelDesert", "LevelWinter", "LevelMerged" })
                {
                    var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + level + ".unity");
                    try
                    {
                        var sceneStores = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ItemStore>(true)).ToArray();
                        Require(sceneStores.Length > 0, level + " has stores");
                        var keys = sceneStores.Select(s => WorldSpawnIdentity.StablePath(s.transform)).ToArray();
                        Require(keys.Distinct().Count() == keys.Length, "unique authored store keys");
                        var contents = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LocationContentSpawner>(true)).ToArray();
                        var contentKeys = contents.Select(x => x.SharedGenerationKey).ToArray();
                        var oldKeys = sceneStores.Select(s => LegacyScenePath(s.transform)).ToArray();
                        var extra = new GameObject("SeedAudit unrelated root");
                        SceneManager.MoveGameObjectToScene(extra, scene);
                        extra.transform.SetAsFirstSibling();
                        for (int i = 0; i < contents.Length; i++)
                            Require(contents[i].SharedGenerationKey == contentKeys[i], "content key survives root insertion");
                        for (int i = 0; i < sceneStores.Length; i++)
                        {
                            Require(WorldSpawnIdentity.StablePath(sceneStores[i].transform) == keys[i], "store key survives root insertion");
                            string oldAfter = LegacyScenePath(sceneStores[i].transform);
                            Require(oldAfter != oldKeys[i], "fixture changes legacy root index");
                            if (Seeds.Any(seed => !Draw(sceneStores[i], seed, oldKeys[i]).SequenceEqual(Draw(sceneStores[i], seed, oldAfter))))
                                legacyDivergences++;
                            Require(scene.GetRootGameObjects().Count(r => r.name == sceneStores[i].transform.root.name) == 1,
                                "authored world root names must be unique");
                            CheckStore(sceneStores[i], keys[i]);
                        }
                        var city = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CitySpawner>(true)).First();
                        CheckRegeneratedCities(city.cityPrefab, scene);
                    }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                Require(legacyDivergences > 0, "legacy root-index keys reproduce stock divergence");
                return $"[Store seed probe] PASS: {stores} real stores, {slots} slots, {comparisons} seeded comparisons; root reorder, independent RNG, reversed initialization, regenerated city and fixed stock. legacy divergent stores={legacyDivergences}; Generator={RunRandom.Version}";
            }
            finally { UnityEngine.Random.state = randomState; }
        }

        private static string LegacyScenePath(Transform part)
        {
            string path = "";
            while (part != null)
            {
                path = part.name + "[" + part.GetSiblingIndex() + "]/" + path;
                part = part.parent;
            }
            return "scene:" + path;
        }

        private static StorePoint[] Points(ItemStore store) =>
            ((Transform)typeof(ItemStore).GetField("_pointsParent", Private).GetValue(store)).GetComponentsInChildren<StorePoint>();
        private static PickableItem[] Draw(ItemStore store, int seed, string key) =>
            (PickableItem[])typeof(ItemStore).GetMethod("SelectAssortment", Private).Invoke(store,
                new object[] { Points(store), new RunRandom(seed, "store:" + key, 0) });

        private static void CheckStore(ItemStore store, string key)
        {
            var points = Points(store);
            var pool = (StoreItemsCollection)typeof(ItemStore).GetField("_collection", Private).GetValue(store);
            var before = pool == null ? Array.Empty<PickableItem>() : pool.Items.ToArray();
            var variants = new System.Collections.Generic.HashSet<string>();
            foreach (int seed in Seeds)
            {
                var first = Draw(store, seed, key);
                for (int n = 0; n < 137; n++) { UnityEngine.Random.Range(0, 100); new RunRandom(seed, "layout", n).Range(0, 100); }
                var resumed = Draw(store, seed, key);
                Require(first.SequenceEqual(resumed), "seeded stock changed after unrelated random calls");
                Require(before.SequenceEqual(pool == null ? Array.Empty<PickableItem>() : pool.Items.ToArray()), "shared asset pool mutated");
                var remaining = before.ToList();
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].StaticItem)
                    {
                        Require(first[i] == null, "fixed slot consumes random stock");
                        Require(new SerializedObject(points[i]).FindProperty("_itemPrefab").objectReferenceValue != null, "fixed slot has item");
                    }
                    else if (remaining.Count > 0)
                    {
                        Require(first[i] != null && remaining.Remove(first[i]), "stock selection without replacement");
                        Require(first[i].GetComponent<StoreItem>() != null, "stock has price");
                    }
                    else Require(first[i] == null, "exhausted stock must stay empty");
                }
                variants.Add(string.Join("/", first.Select(x => x == null ? "-" : x.name)));
                comparisons++;
            }
            if (before.Distinct().Count() > 1 && points.Any(p => !p.StaticItem))
                Require(variants.Count > 1, "different seeds produce varied stock");
            stores++; slots += points.Length;
        }

        private static void CheckRegeneratedCities(GameObject prefab, Scene scene)
        {
            foreach (string cityKey in new[] { "city:10000", "city:20200", "city:100000" })
            {
                var first = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var resumed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                try
                {
                    Initialize(first, cityKey); Initialize(resumed, cityKey);
                    resumed.name = "Regenerated city";
                    resumed.transform.SetAsFirstSibling();
                    var a = first.GetComponentsInChildren<ItemStore>(true);
                    var b = resumed.GetComponentsInChildren<ItemStore>(true);
                    Require(a.Length == b.Length && a.Length > 0, "regenerated store count");
                    // Clients can initialize cities/shops in a different order.
                    for (int i = a.Length - 1; i >= 0; i--)
                    {
                        string ka = WorldSpawnIdentity.StablePath(a[i].transform);
                        string kb = WorldSpawnIdentity.StablePath(b[i].transform);
                        Require(ka == kb && ka.StartsWith(cityKey + "/", StringComparison.Ordinal), "city key ignores clone/root names");
                        foreach (int seed in Seeds)
                        {
                            Require(Draw(a[i], seed, ka).SequenceEqual(Draw(b[i], seed, kb)), "regenerated city stock differs");
                            comparisons++;
                        }
                        CheckStore(a[i], ka);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(resumed); }
            }
        }

        private static void Initialize(GameObject city, string key)
        {
            city.AddComponent<WorldGenerationAnchor>().Key = key;
            int i = 0;
            foreach (var content in city.GetComponentsInChildren<LocationContentSpawner>(true))
                content.InitializeStableGeneration(key + ":" + i++);
        }
        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException("[Store seed probe] " + message); }
    }
}
