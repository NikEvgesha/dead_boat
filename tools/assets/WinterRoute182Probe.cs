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
public static class WinterRoute182Probe
{
    const string Flag = "WinterRoute182Probe";
    const string Report = "Docs/locations/previews/event-winter182-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Names =
    {
        "Bank_01_Snow_Tunnel", "Bank_02_Rooted_Log", "Bank_03_Frozen_Grotto",
        "Bank_05_Gift_Sled", "Bank_06_Fir_Arch", "Bank_07_Ice_Crystal_Field"
    };

    static WinterRoute182Probe() => EditorApplication.playModeStateChanged += StateChanged;

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
        var land = Object.FindAnyObjectByType<LendController>();
        var rocks = land.locations[0].groundSegments.SelectMany(t => t.GetComponentsInChildren<MeshFilter>(true))
            .Where(f => f.name.StartsWith("rock", StringComparison.Ordinal)).ToArray();
        Check(rocks.Length == 30 && rocks.All(f => f.sharedMesh.name.EndsWith("_WinterCap") &&
            f.sharedMesh.subMeshCount == 2 && f.sharedMesh.GetIndexCount(1) > 0 &&
            f.GetComponent<MeshRenderer>().sharedMaterials[0].name == "Stone" &&
            f.GetComponent<MeshRenderer>().sharedMaterials[1].name == "Snow"),
            "Snow caps on all 30 rock renderers in three streaming bank segments");
        Check(rocks.Any(f=>f.transform.position.x<0) && rocks.Any(f=>f.transform.position.x>0),
            "Snow caps on both banks");
        var city = Object.FindAnyObjectByType<CitySpawner>();
        var spawner = Object.FindAnyObjectByType<DecorationSpawner>();
        var assets = Names.Select(n => spawner.decorationCollection.spawnItems.Single(i => i.prefab.name == n).prefab).ToArray();
        Check(assets[0].GetComponentInChildren<MeshCollider>().sharedMesh.name == "Wind_Carved_Snow_Hill",
            "Generator uses the new solid hill with the carved passage");
        Check(spawner.decorationCollection.spawnItems.Count == 11, "Six new designs and five prior variants in LevelEvent");
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

        var source = PlayerMovement.Instance.GetComponent<CharacterController>();
        for (int i=0; i<3; i++)
        {
            var prop = Object.Instantiate(assets[i],new Vector3(60f,0f,130f+i*19f), Quaternion.identity);
            Physics.SyncTransforms();
            VerifyOpening(prop, source);
        }

        void SpawnAt(float distance, float origin)
        {
            float baseZ = distance - origin - spawner.spawnAreaOffset.y - spawner.spawnAreaSize.y*.5f;
            method.Invoke(spawner, new object[] { baseZ });
        }
    }

    static void VerifyOpening(GameObject prop, CharacterController source)
    {
        var marker = prop.transform.position;
        var shape = prop.GetComponent<BoxCollider>().bounds;
        float near = shape.min.z - 1f, far = shape.max.z + 1f;
        var test = new GameObject("Player capsule for " + prop.name) { layer = source.gameObject.layer };
        try
        {
            var cc = test.AddComponent<CharacterController>();
            cc.radius = source.radius;
            cc.height = source.height;
            cc.center = new Vector3(0, source.height*.5f, 0);
            cc.stepOffset = source.stepOffset;
            cc.skinWidth = source.skinWidth;
            foreach (float direction in new[]{1f,-1f})
                foreach (float offset in new[]{-.32f,0f,.32f})
                {
                    cc.enabled = false;
                    test.transform.position = new Vector3(marker.x+offset,.055f,direction > 0 ? near : far);
                    cc.enabled = true;
                    Physics.SyncTransforms();
                    int count = Mathf.CeilToInt((far-near+.2f)/.23f);
                    for (int i=0;i<count;i++) cc.Move(Vector3.forward * direction * .23f);
                    CheckGeometry(direction > 0 ? test.transform.position.z >= far-.05f : test.transform.position.z <= near+.05f,
                        prop.name + " blocked direction " + direction + ", x offset " + offset + " at " + test.transform.position);
                }
            cc.enabled = false;
            test.transform.position = new Vector3(shape.max.x + .7f,.055f,marker.z);
            cc.enabled = true;
            Physics.SyncTransforms();
            for(int i=0;i<32;i++) cc.Move(Vector3.left*.21f);
            CheckGeometry(test.transform.position.x > marker.x + 1.3f, prop.name + " does not block entry through the shell");
            Note(prop.name + ": 6 capsule passes (both directions, center and offsets); shell blocks lateral movement");
        }
        finally { Object.Destroy(test); }
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
        Debug.Log("[Winter182] "+line);
    }
}


