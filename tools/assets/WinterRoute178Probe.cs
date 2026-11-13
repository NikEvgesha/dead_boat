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
public static class WinterRoute178Probe
{
    const string Flag = "WinterRoute178Probe";
    const string Report = "Docs/locations/previews/event-winter178-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static WinterRoute178Probe() => EditorApplication.playModeStateChanged += StateChanged;

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
        if (!SessionState.GetBool(Flag, false) || state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.CallbackFunction clock = () => Time.timeScale = 1f;
        EditorApplication.update += clock;
        try
        {
            await Until(() => LoadingManager.Instance && LoadingManager.Instance.CurrentLocation == Location.Lobby, "Lobby");
            Check(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave, "Isolated editor save");
            LoadingManager.Instance.LoadLocation(Location.Game, "LevelEvent", false);
            await Until(() => SceneManager.GetActiveScene().name == "LevelEvent" &&
                LoadingManager.Instance.CurrentLocation == Location.Game && BoardController.Instance.StartGame, "LevelEvent started");
            await Task.Yield();
            Verify();
            Note("PASS");
        }
        catch (Exception ex)
        {
            Note("FAIL " + ex);
        }
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
        var land = Object.FindAnyObjectByType<LendController>();
        var cities = Object.FindAnyObjectByType<CitySpawner>();
        var decor = Object.FindAnyObjectByType<DecorationSpawner>();
        var bankRenderers = land.locations[0].groundSegments.SelectMany(s => s.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r => r.sharedMaterial && r.sharedMaterial.name == "Snowy_Riverbanks").ToArray();
        Check(bankRenderers.Length == 3, "All three recycled ground segments use snow material");
        Check(decor.decorationCollection.spawnItems.Count == 4, "Four winter decor variants loaded");
        var shrubs = GameObject.Find("Winter_Snow_Capped_Shrubs");
        Check(shrubs && shrubs.transform.parent.GetComponentsInChildren<Transform>().All(t => t.name != "Small_Flowers"),
            "Start castle uses snowy shrubs in place of flowers");
        Check(cities.cityPrefab.transform.Find("Winter_Snow_Capped_Shrubs"), "Future castles use the same shrubs");

        float clearance = (float)typeof(DecorationSpawner).GetField("_cityExclusionRadius", Private).GetValue(decor);
        Check(clearance == 60, "Configured castle exclusion");
        for (int i = 0; i < cities.Cities; i++)
        {
            float center = i * cities.baseSpawnDistance;
            Check(cities.IsWithinCityClearance(center - 60f, clearance) && cities.IsWithinCityClearance(center + 60f, clearance)
                && !cities.IsWithinCityClearance(center + 60.5f, clearance), "Exclusion edges at " + center);
        }
        Check(!cities.IsWithinCityClearance(cities.Cities * cities.baseSpawnDistance, clearance), "No exclusion for an unscheduled castle");

        var spawn = typeof(DecorationSpawner).GetMethod("SpawnDecoration", Private);
        var placed = (List<GameObject>)typeof(DecorationSpawner).GetField("spawnedDecorations", Private).GetValue(decor);
        var routeDistances = (List<float>)typeof(DecorationSpawner).GetField("_zPositions", Private).GetValue(decor);
        var oldRandom = UnityEngine.Random.state;
        var originField = typeof(FixCoordinate).GetField("_boardAddPos", Private);
        var coordinates = FixCoordinate.Instance;
        float oldOrigin = coordinates.BoardAddPos;
        try
        {
            UnityEngine.Random.InitState(178);
            int baseline = placed.Count;
            SpawnAtRoute(0f, oldOrigin, 18);
            SpawnAtRoute(cities.baseSpawnDistance, oldOrigin, 18);
            Check(placed.Count == baseline, "No decorations spawned at start or next castle before it exists");
            const float shiftedOrigin = 4632f;
            originField.SetValue(coordinates, shiftedOrigin);
            SpawnAtRoute(cities.baseSpawnDistance, shiftedOrigin, 18);
            Check(placed.Count == baseline, "Castle exclusion survives floating-origin shift");
            SpawnAtRoute(cities.baseSpawnDistance - 115f, shiftedOrigin, 32);
            SpawnAtRoute(cities.baseSpawnDistance + 115f, shiftedOrigin, 32);
            Check(placed.Count > baseline + 20, "Winter decor spawns on both sides of the exclusion zone");
            var generated = placed.Skip(baseline).ToArray();
            Check(generated.Select(o => o.name).Distinct().Count() == 4 &&
                generated.Any(o => o.transform.position.x < 0) && generated.Any(o => o.transform.position.x > 0),
                "All four variants sampled across both banks");
            Check(routeDistances.Skip(baseline).All(d => !cities.IsWithinCityClearance(d, clearance)), "All generated centers are outside castle buffers");
            Check(generated.All(o => o.GetComponentsInChildren<MeshRenderer>().All(r => r.bounds.min.x >= 3.5f || r.bounds.max.x <= -3.5f)),
                "Full prefab geometry clears the river channel");
        }
        finally
        {
            UnityEngine.Random.state = oldRandom;
            originField.SetValue(coordinates, oldOrigin);
        }
        void SpawnAtRoute(float center, float origin, int repeats)
        {
            float baseZ = center - origin - decor.spawnAreaOffset.y - decor.spawnAreaSize.y * .5f;
            for (int i = 0; i < repeats; i++) spawn.Invoke(decor, new object[] { baseZ });
        }
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Note(label);
    }

    static void Note(string line)
    {
        File.AppendAllText(Report, line + Environment.NewLine);
        Debug.Log("[Winter178] " + line);
    }
}
