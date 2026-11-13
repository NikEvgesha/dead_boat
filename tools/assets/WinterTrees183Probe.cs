using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DeadBoat.Online;
using DeadBoat.Online.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WinterTrees183Probe
{
    const string Flag = "WinterTrees183Probe";
    const string Report = "Docs/locations/previews/event-winter183-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Names =
    {
        "Bank_Tall_And_Small_Firs", "Bank_Young_Fir_Grove"
    };

    static WinterTrees183Probe() => EditorApplication.playModeStateChanged += StateChanged;

    public static void Main()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        File.WriteAllText(Report, "");
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    static async void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
        EditorApplication.CallbackFunction clock = () => Time.timeScale = 1f;
        EditorApplication.update += clock;
        try
        {
            await Until(() => LoadingManager.Instance && LoadingManager.Instance.CurrentLocation == Location.Lobby, "Lobby");
            Check(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave, "Isolated save");
            LoadingManager.Instance.LoadLocation(Location.Game, "LevelEvent", false);
            await Until(() => SceneManager.GetActiveScene().name == "LevelEvent" && LoadingManager.Instance.CurrentLocation == Location.Game &&
                BoardController.Instance && BoardController.Instance.StartGame, "LevelEvent started through the normal loader");
            await Task.Yield();
            Verify();
            Note("PASS");
        }
        catch (Exception ex) { Note("FAIL " + ex); }
        finally
        {
            EditorApplication.update -= clock;
            SessionState.EraseBool(Flag);
            EditorApplication.ExitPlaymode();
        }
    }

    static async Task Until(Func<bool> condition, string label)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException(label);
            await Task.Delay(35);
        }
        Note(label);
    }

    static void Verify()
    {
        var city = Object.FindAnyObjectByType<CitySpawner>();
        var spawner = Object.FindAnyObjectByType<DecorationSpawner>();
        Check(spawner.decorationCollection.spawnItems.Count == 13, "Thirteen winter variants in LevelEvent");
        var castle = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(o=>o.GetComponentsInChildren<Transform>())
            .Single(t=>t.Find("Winter_176/Festive_Firs"));
        Check(AssetDatabase.GetAssetPath(city.cityPrefab) == "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab",
            "The route spawner uses the updated castle prefab");
        VerifyCastle(castle);
        VerifyCastle(city.cityPrefab.transform);
        var method = typeof(DecorationSpawner).GetMethod("SpawnDecoration", Private);
        var placed = (List<GameObject>)typeof(DecorationSpawner).GetField("spawnedDecorations", Private).GetValue(spawner);
        var zValues = (List<float>)typeof(DecorationSpawner).GetField("_zPositions", Private).GetValue(spawner);
        var coordinate = FixCoordinate.Instance;
        var originField = typeof(FixCoordinate).GetField("_boardAddPos", Private);
        float priorOrigin = coordinate.BoardAddPos;
        var random = UnityEngine.Random.state;
        float castleHalfDepth = city.cityPrefab.GetComponentsInChildren<MeshRenderer>()
            .Select(r=>Mathf.Max(Mathf.Abs(r.bounds.min.z),Mathf.Abs(r.bounds.max.z))).Max();
        Check(castleHalfDepth >= 24f && castleHalfDepth < 30f, "Measured castle half depth " + castleHalfDepth.ToString("F2") + " m");
        Check(city.transform.childCount == 0, "Next castle is not instantiated yet");

        try
        {
            UnityEngine.Random.InitState(181);
            int before = placed.Count;
            for (int i=0;i<28;i++)
            {
                SpawnAt(0, priorOrigin);
                SpawnAt(city.baseSpawnDistance, priorOrigin);
            }
            Check(placed.Count == before, "Start and future castle exclusion before city spawn");

            const float shifted = 4673f;
            originField.SetValue(coordinate, shifted);
            for (int i=0;i<24;i++) SpawnAt(city.baseSpawnDistance, shifted);
            Check(placed.Count == before, "Same castle exclusion after coordinate rebase");
            for (int i=0;i<160;i++)
                SpawnAt(city.baseSpawnDistance - 1750f + i * 22f, shifted);
            Physics.SyncTransforms();
            var generated = placed.Skip(before).ToArray();
            float minGap = float.PositiveInfinity;
            foreach (var design in Names)
            {
                var found = generated.Where(o => o.name == design+"(Clone)").ToArray();
                Check(found.Length > 0, design + " spawned " + found.Length + " times through DecorationSpawner");
            }
            Check(generated.Any(o=>o.transform.position.x<0) && generated.Any(o=>o.transform.position.x>0), "Samples on both banks");
            for (int i=before; i<placed.Count; i++)
            {
                var prop = placed[i];
                var footprint = prop.GetComponent<BoxCollider>().bounds;
                CheckGeometry(footprint.min.x >= 3.5f || footprint.max.x <= -3.5f, prop.name + " hits river");
                float logicalZ = zValues[i];
                float nearbyCastle = Mathf.Clamp(Mathf.Round(logicalZ / city.baseSpawnDistance), 0, city.Cities - 1) * city.baseSpawnDistance;
                float distanceToFootprint = Mathf.Max(nearbyCastle - (footprint.max.z + shifted),
                    footprint.min.z + shifted - nearbyCastle);
                minGap = Mathf.Min(minGap, distanceToFootprint - castleHalfDepth);
                CheckGeometry(!city.IsWithinCityClearance(logicalZ,60f), prop.name + " enters castle buffer");
                foreach (var other in placed.Take(i))
                {
                    if (!other) continue;
                    var a = prop.transform.position;
                    var b = other.transform.position;
                    CheckGeometry(new Vector2(a.x-b.x,a.z-b.z).sqrMagnitude >= 11.99f*11.99f, "Decoration spacing failed");
                }
            }
            Check(minGap >= 25f, "Smallest sampled geometry-to-castle gap " + minGap.ToString("F2") + " m");
            Note($"Checked {generated.Length} spawned footprints, water and adjacent decoration spacing.");
        }
        finally
        {
            originField.SetValue(coordinate, priorOrigin);
            UnityEngine.Random.state = random;
        }

        void SpawnAt(float distance, float origin)
        {
            float baseZ = distance - origin - spawner.spawnAreaOffset.y - spawner.spawnAreaSize.y*.5f;
            method.Invoke(spawner, new object[] { baseZ });
        }
    }

    static void VerifyCastle(Transform castle)
    {
        Vector2[] centers =
        {
            new(-18.12f,-18.45f), new(-7.6f,-5.2f), new(-18.24f,9f),
            new(7.3f,-6.9f), new(17.5f,5.2f), new(17.35f,19.05f), new(-7.54f,8.33f)
        };
        var firs = castle.Find("Winter_176/Festive_Firs");
        var lone = castle.Find("08_Distributed_Greenery/Green_Cluster_3 (1)/Shade_Tree (1)");
        var points = firs.GetComponentsInChildren<MeshFilter>().Concat(lone.GetComponentsInChildren<MeshFilter>())
            .SelectMany(f=>f.sharedMesh.vertices.Select(v=>castle.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
        var houses = castle.Find("03_Service_Houses").Cast<Transform>().Select(BoundsFor).ToArray();
        var perimeter = castle.Find("01_Original_Castle_Perimeter/Chateau").Cast<Transform>()
            .Where(t=>(t.name.StartsWith("Tower") || t.name.StartsWith("Wall2")) && t.GetComponentInChildren<MeshRenderer>())
            .Select(BoundsFor).ToArray();
        var bank = BoundsFor(castle.Find("02_Courtyard/West_Bank"));
        var east = BoundsFor(castle.Find("02_Courtyard/East_Bank"));
        for (int i=0;i<centers.Length;i++)
        {
            var center=centers[i];
            var tree=points.Where(p=>(new Vector2(p.x,p.z)-center).sqrMagnitude<4f).ToArray();
            CheckGeometry(tree.Length>50,"Missing tree " + (i+1));
            var bounds = new Bounds(tree[0],Vector3.zero);
            foreach(var vertex in tree) bounds.Encapsulate(vertex);
            var ground = center.x<0 ? bank : east;
            CheckGeometry(Mathf.Abs(bounds.min.y-ground.max.y)<.022f,"Tree floats or sinks " + (i+1));
            CheckGeometry(bounds.min.x>=ground.min.x && bounds.max.x<=ground.max.x &&
                bounds.min.z>=ground.min.z && bounds.max.z<=ground.max.z,"Tree outside bank " + (i+1));
            float house = Nearest(bounds,houses), wall = Nearest(bounds,perimeter);
            Check(house>=.25f && wall>=.25f,$"Castle tree {i+1}: grounded, house {house:F2} m, wall/tower {wall:F2} m");
        }

        Bounds BoundsFor(Transform t)
        {
            var vertices=t.GetComponentsInChildren<MeshFilter>().Where(f=>f.TryGetComponent<MeshRenderer>(out var renderer) && renderer.enabled).SelectMany(f =>
                f.sharedMesh.vertices.Select(v=>castle.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
            var result=new Bounds(vertices[0],Vector3.zero);
            foreach(var vertex in vertices) result.Encapsulate(vertex);
            return result;
        }
    }

    static float Nearest(Bounds tree, IEnumerable<Bounds> candidates) => candidates
        .Where(b=>b.min.y<tree.max.y && b.max.y>tree.min.y)
        .Select(b=>Gap(tree,b)).DefaultIfEmpty(float.PositiveInfinity).Min();

    static float Gap(Bounds a,Bounds b)
    {
        float x=Mathf.Max(0,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x));
        float z=Mathf.Max(0,Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z));
        return new Vector2(x,z).magnitude;
    }

    static void CheckGeometry(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
    static void Check(bool condition, string label)
    {
        CheckGeometry(condition, label);
        Note(label);
    }
    static void Note(string line)
    {
        File.AppendAllText(Report,line+Environment.NewLine);
        Debug.Log("[Winter183] "+line);
    }
}


