using System;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedRunValidation
    {
        [MenuItem("Tools/Online/Validate Seeded Generation")]
        public static void Validate()
        {
            foreach (int seed in new[] { 0, 1, -1, int.MinValue, 124578 })
            {
                var first = new RunRandom(seed, "items:procedural:17", 0);
                var second = new RunRandom(seed, "items:procedural:17", 0);
                for (int i = 0; i < 1000; i++)
                {
                    // Other streams and Unity randomness must not affect the sequence.
                    new RunRandom(seed, "enemies:procedural:99", i).Range(0, 100);
                    if (first.Range(0, 101) != second.Range(0, 101))
                        throw new Exception("Independent streams diverged");
                }
                var resumed = new RunRandom(seed, "items:procedural:17", 0);
                var original = new RunRandom(seed, "items:procedural:17", 0);
                for (int i = 0; i < 50; i++)
                    if (resumed.Value != original.Value) throw new Exception("Key reconstruction failed");
            }
            int checkedPools = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LocationSpawnCollection"))
            {
                var source = AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(AssetDatabase.GUIDToAssetPath(guid));
                var a = UnityEngine.Object.Instantiate(source);
                var b = UnityEngine.Object.Instantiate(source);
                try
                {
                    float za = 0, zb = 0;
                    for (int index = 0; index < 200; index++)
                    {
                        var ra = new RunRandom(481516, "layout", index);
                        var rb = new RunRandom(481516, "layout", index);
                        za += ra.Range(100f, 200f);
                        zb += rb.Range(100f, 200f);
                        if (za != zb || a.GetRandomSpawnPrefab(za, ra) != b.GetRandomSpawnPrefab(zb, rb))
                            throw new Exception("Map differs: " + source.name);
                    }
                    checkedPools++;
                }
                finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
            }
            Debug.Log($"[Shared run validation] PASS: independent streams, reconstruction, {checkedPools} actual map pools x 200 locations.");
        }
    }
}
