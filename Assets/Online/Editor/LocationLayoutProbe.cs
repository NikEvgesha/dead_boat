using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DeadBoat.Online.Editor
{
    public static class LocationLayoutProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Run()
        {
            if (!EditorApplication.isPlaying || LoadingManager.Instance != null || SharedRunContext.Active || SoloRunBranch.Active)
                throw new InvalidOperationException("empty isolated Play required");
            int pools = 0, comparisons = 0;
            var savedRandom = Random.state;
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:LocationSpawnCollection"))
                {
                    var source = AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(AssetDatabase.GUIDToAssetPath(guid));
                    foreach (int seed in new[] { 0, 1, -1, int.MinValue, 27182818 })
                    {
                        var legacy = Object.Instantiate(source); var planned = Object.Instantiate(source);
                        var replay = Object.Instantiate(source);
                        try
                        {
                            float a = 0, b = 0, c = 0;
                            for (int index = 0; index < 512; index++)
                            {
                                float min = 50 + index % 3 * 5, max = 350;
                                var oldRandom = new RunRandom(seed, "layout", index);
                                a += oldRandom.Range(min, max);
                                var originalPrefab = legacy.GetRandomSpawnPrefab(a, oldRandom);
                                // Unrelated gameplay randomness must not affect seeded planning.
                                Random.Range(0, 100); new RunRandom(seed, "items:other", index).Value.ToString();
                                var step = LocationLayoutPlan.Advance(index, b, min, max, planned, new RunRandom(seed, "layout", index));
                                b = step.LogicalZ;
                                var reconstructed = LocationLayoutPlan.Advance(index, c, min, max, replay, new RunRandom(seed, "layout", index));
                                c = reconstructed.LogicalZ;
                                Require(step.Index == index && a == b && b == c && originalPrefab == step.Prefab && step.Prefab == reconstructed.Prefab,
                                    "seeded layout/replay mismatch: " + source.name + " index=" + index);
                                comparisons++;
                            }
                        }
                        finally { Object.Destroy(legacy); Object.Destroy(planned); Object.Destroy(replay); }
                    }
                    // Preserve the exact legacy Unity.Random consumption in unseeded solo.
                    var oldPool = Object.Instantiate(source); var newPool = Object.Instantiate(source);
                    try
                    {
                        var expected = new List<(float z, LocationContentSpawner prefab)>();
                        Random.InitState(314159); float z = 0;
                        for (int i = 0; i < 256; i++)
                        {
                            z += Random.Range(100f, 200f);
                            expected.Add((z, oldPool.GetRandomSpawnPrefab(z)));
                        }
                        float expectedNext = Random.value;
                        Random.InitState(314159); z = 0;
                        for (int i = 0; i < 256; i++)
                        {
                            var step = LocationLayoutPlan.Advance(i, z, 100, 200, newPool, null); z = step.LogicalZ;
                            Require(z == expected[i].z && step.Prefab == expected[i].prefab, "solo layout mismatch"); comparisons++;
                        }
                        Require(Random.value == expectedNext, "solo RNG consumption changed");
                    }
                    finally { Object.Destroy(oldPool); Object.Destroy(newPool); }
                    CheckRetiredSkip(source);
                    pools++;
                }
                Require(pools > 0, "actual location pools missing");
                string result = $"[Layout probe] PASS pools={pools} comparisons={comparisons}; legacy/plan/replay, unrelated RNG, solo RNG, retired skip/no false warning";
                Debug.Log(result); System.IO.File.WriteAllText("Logs/LocationLayout-20261102.log", result); return result;
            }
            finally { Random.state = savedRandom; }
        }

        private static void CheckRetiredSkip(LocationSpawnCollection source)
        {
            var root = new GameObject("Temporary layout skip fixture"); root.SetActive(false);
            var pool = Object.Instantiate(source);
            var originalFix = FixCoordinate.Instance;
            int warnings = 0, changes = 0;
            Application.LogCallback log = (_, _, type) => { if (type == LogType.Warning) warnings++; };
            Action changed = () => changes++;
            try
            {
                var prefab = root.AddComponent<LocationContentSpawner>();
                typeof(LocationSpawnCollection).GetField("spawnItems", Private).SetValue(pool,
                    new List<LocationSpawnItem> { new() { prefab = prefab, chance = 1 } });
                typeof(LocationSpawnCollection).GetMethod("OnEnable", Private).Invoke(pool, null);
                FixCoordinate.Instance = root.AddComponent<FixCoordinate>(); // inactive: no Awake/save access
                var spawner = root.AddComponent<LocationSpawner>(); spawner.locationCollection = pool;
                spawner.minDistance = spawner.maxDistance = 100;
                var spawn = typeof(LocationSpawner).GetMethod("SpawnLocation", Private);
                Application.logMessageReceived += log; LocationSpawner.Change += changed;
                for (int i = 0; i < 10; i++) spawn.Invoke(spawner, new object[] { 1000000f });
                Require(warnings == 0 && changes == 10, "retired skip warning/callback mismatch");
                Require((int)typeof(LocationSpawner).GetField("generationIndex", Private).GetValue(spawner) == 10 &&
                    (float)typeof(LocationSpawner).GetField("lastSpawnZ", Private).GetValue(spawner) == 1000 && root.transform.childCount == 0,
                    "retired skip did not advance without creating geometry");
            }
            finally
            {
                Application.logMessageReceived -= log; LocationSpawner.Change -= changed;
                FixCoordinate.Instance = originalFix;
                Object.Destroy(root); Object.Destroy(pool);
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
