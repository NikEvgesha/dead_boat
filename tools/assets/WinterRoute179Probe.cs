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
public static class WinterRoute179Probe
{
    const string Flag = "WinterRoute179Probe";
    const string Prefix = "Docs/locations/previews/event-winter179-";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static WinterRoute179Probe() => EditorApplication.playModeStateChanged += StateChanged;

    public static void Main()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        File.WriteAllText(Prefix + "play.txt", "");
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
            await Verify();
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

    static async Task Verify()
    {
        var snowfall = Object.FindAnyObjectByType<EventSnowfall>();
        Check(snowfall, "Snowfall component is present");
        var flakes = snowfall.GetComponent<ParticleSystem>();
        Check(flakes.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.isSupported, "Snow material shader supported");
        await Until(() => flakes.particleCount > 100 && Camera.main, "Snow particles visible near start castle");
        await CheckSnowFalls(flakes);
        var shrubs = GameObject.Find("Winter_Leafy_Shrubs");
        Check(shrubs && shrubs.GetComponentsInChildren<MeshRenderer>().Length == 3 &&
            !GameObject.Find("Winter_Snow_Capped_Shrubs"), "Leafy shrubs replaced the old spheres");
        var city = Object.FindAnyObjectByType<CitySpawner>();
        Check(city.cityPrefab.transform.Find("Winter_Leafy_Shrubs"), "Streamed castles use updated shrub prefab");
        Capture("castle", new Vector3(-6.0f, 1.49f, -10.2f), new Vector3(-8.05f, .60f, -7.75f));

        var spawner = Object.FindAnyObjectByType<DecorationSpawner>();
        var pool = spawner.decorationCollection.spawnItems;
        Check(pool.Count == 6, "Six route variants in live scene");
        var driftPrefab = pool.Single(i => i.prefab.name == "Bank_Tall_Snowdrifts").prefab;
        var logPrefab = pool.Single(i => i.prefab.name == "Bank_Walkthrough_Log").prefab;
        Check((float)typeof(DecorationSpawner).GetField("_decorationSpacing", Private).GetValue(spawner) == 12f &&
            (float)typeof(DecorationSpawner).GetField("_cityExclusionRadius", Private).GetValue(spawner) == 60f,
            "Scene has prop spacing and castle clearance");
        var spawn = typeof(DecorationSpawner).GetMethod("SpawnDecoration", Private);
        var placed = (List<GameObject>)typeof(DecorationSpawner).GetField("spawnedDecorations", Private).GetValue(spawner);
        int baseline = placed.Count;
        for (int i = 0; i < 30; i++)
        {
            spawn.Invoke(spawner, new object[] { city.baseSpawnDistance - 505f });
            spawn.Invoke(spawner, new object[] { -505f });
        }
        Check(placed.Count == baseline, "No prop spawns beside starting or future castle");
        var randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(179);
        try
        {
            for (int i = 0; i < 160; i++) spawn.Invoke(spawner, new object[] { 120f + i * 14f - 505f });
        }
        finally { UnityEngine.Random.state = randomState; }
        var generated = placed.Skip(baseline).ToArray();
        Check(generated.Any(o => o.name.StartsWith(logPrefab.name)) && generated.Any(o => o.name.StartsWith(driftPrefab.name)),
            "Both new assets spawned through the regular generator");
        foreach (var prop in generated)
        {
            var p = prop.transform.position;
            if (city.IsWithinCityClearance(p.z + FixCoordinate.Instance.BoardAddPos, 60f) ||
                prop.GetComponentsInChildren<MeshRenderer>().Any(r => r.bounds.min.x < 3.5f && r.bounds.max.x > -3.5f))
                throw new InvalidOperationException("Route clearance failed for " + prop.name);
            if (placed.Any(o => o && o != prop &&
                Vector2.Distance(new Vector2(o.transform.position.x, o.transform.position.z), new Vector2(p.x, p.z)) < 11.99f))
                throw new InvalidOperationException("Route spacing failed for " + prop.name);
        }
        Note("Generated props clear the channel, castle buffers and neighbouring props");

        var log = Object.Instantiate(logPrefab, new Vector3(50, 0, 142), Quaternion.identity);
        var drift = Object.Instantiate(driftPrefab, new Vector3(56.5f, 0, 149), Quaternion.identity);
        Physics.SyncTransforms();
        float height = drift.GetComponentsInChildren<MeshRenderer>().Max(r => r.bounds.max.y);
        Check(height > 4.1f && height < 4.3f, "Tall drift height " + height.ToString("F2") + " m");
        VerifyWalkThrough(log.transform.position);

        var player = PlayerMovement.Instance;
        player.Teleport(new Vector3(49f, 1.2f, 130f));
        await Until(() => Camera.main && Camera.main.transform.position.z > 110f && flakes.particleCount > 100 &&
            Vector3.Distance(snowfall.transform.position, Camera.main.transform.position + Vector3.up * 9f) < .08f,
            "Snowfall follows observer onto the route");
        await CheckSnowFalls(flakes);
        Capture("route", new Vector3(47.95f, 2.32f, 131.3f), new Vector3(53.5f, 1.52f, 146.0f));
    }

    static void VerifyWalkThrough(Vector3 position)
    {
        var test = new GameObject("Log traversal test capsule");
        try
        {
            var controller = test.AddComponent<CharacterController>();
            controller.radius = .4f;
            controller.height = 1.8f;
            controller.center = new Vector3(0, .9f, 0);
            controller.stepOffset = .25f;
            test.transform.position = position + new Vector3(0, .045f, -4.6f);
            Physics.SyncTransforms();
            for (int i = 0; i < 24; i++) controller.Move(Vector3.forward * .4f);
            Check(test.transform.position.z > position.z + 4.7f, "Player-sized capsule traverses both log entrances");
            controller.enabled = false;
            test.transform.position = position + new Vector3(3.5f, .045f, 0);
            controller.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 12; i++) controller.Move(Vector3.left * .3f);
            Check(test.transform.position.x > position.x + 1.65f, "Solid bark blocks lateral movement");
        }
        finally { Object.Destroy(test); }
    }

    static async Task CheckSnowFalls(ParticleSystem flakes)
    {
        var buffer = new ParticleSystem.Particle[flakes.main.maxParticles];
        var before = new Dictionary<uint, float>();
        int count = flakes.GetParticles(buffer);
        for (int i = 0; i < count; i++) before[buffer[i].randomSeed] = buffer[i].position.y;
        await Task.Delay(280);
        count = flakes.GetParticles(buffer);
        int falling = 0;
        for (int i = 0; i < count; i++)
            if (before.TryGetValue(buffer[i].randomSeed, out var y) && buffer[i].position.y < y - .1f) falling++;
        Check(falling > 40, "Snow particles move down in world space (" + falling + ")");
    }

    static void Capture(string file, Vector3 position, Vector3 target)
    {
        var go = new GameObject("Preview camera");
        var image = new Texture2D(1400, 900, TextureFormat.RGB24, false);
        var output = new RenderTexture(1400, 900, 24);
        var previous = RenderTexture.active;
        try
        {
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 55f;
            camera.targetTexture = output;
            camera.Render();
            RenderTexture.active = output;
            image.ReadPixels(new Rect(0,0,1400,900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Prefix + file + ".png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            Object.Destroy(go);
            Object.Destroy(output);
            Object.Destroy(image);
        }
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Note(label);
    }

    static void Note(string line)
    {
        File.AppendAllText(Prefix + "play.txt", line + Environment.NewLine);
        Debug.Log("[Winter179] " + line);
    }
}

