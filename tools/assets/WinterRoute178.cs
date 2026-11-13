using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterRoute178
{
    const string Folder = "Assets/Events/NewYear/Route178";
    const string Castle = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string ScenePath = "Assets/Scenes/LevelEvent.unity";
    const string Preview = "Docs/locations/previews/event-winter178-";
    static Material snow, pine, berry, gold, blue, ground;
    static readonly List<string> audit = new List<string>();

    static Material Mat(string path) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/" + path + ".mat");

    static Transform Node(string name, Transform parent, Vector3 position)
    {
        var node = new GameObject(name).transform;
        if (parent) node.SetParent(parent, false);
        node.localPosition = position;
        return node;
    }

    static Transform Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }

    static MeshFilter[] Meshes(Transform parent) =>
        parent.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh && f.GetComponent<MeshRenderer>()).ToArray();

    static Bounds BoundsOf(Transform parent)
    {
        var bounds = parent.GetComponentInChildren<Renderer>().bounds;
        foreach (var renderer in parent.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static Transform AddMesh(Transform parent, string name, Mesh mesh, Material material)
    {
        var node = Node(name, parent, Vector3.zero);
        node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        node.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        return node;
    }

    static void Bake(Transform root)
    {
        var source = Meshes(root);
        var batches = source.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
        foreach (var batch in batches)
        {
            var name = root.name + "_" + batch.Key.name;
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.Select(f => new CombineInstance
            {
                mesh = f.sharedMesh, transform = root.worldToLocalMatrix * f.transform.localToWorldMatrix
            }).ToArray(), true, true);
            AssetDatabase.CreateAsset(mesh, Folder + "/" + name + ".asset");
            AddMesh(root, name, mesh, batch.Key);
        }
        foreach (var filter in source) Object.DestroyImmediate(filter.gameObject);
    }

    static void BuildGround()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color[size * size];
        var random = new System.Random(178);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = 2 * Mathf.PI * x / size;
                float v = 2 * Mathf.PI * y / size;
                float ripple = .024f * Mathf.Sin(2f * u + v + .8f * Mathf.Sin(3f * v)) +
                    .016f * Mathf.Cos(3f * u - 2f * v) + .009f * (float)random.NextDouble();
                pixels[y * size + x] = new Color(.88f + ripple, .92f + ripple, .97f + ripple);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        string path = Folder + "/Snow_Grain.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ground = new Material(snow) { name = "Snowy_Riverbanks", color = Color.white };
        ground.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        ground.mainTextureScale = new Vector2(.11f, .11f);
        AssetDatabase.CreateAsset(ground, Folder + "/Snowy_Riverbanks.mat");
    }

    static Mesh ExtractFirPart(Mesh source, Vector3 center)
    {
        var sourceVertices = source.vertices;
        var sourceNormals = source.normals;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();
        var remap = new Dictionary<int, int>();
        bool Near(int i) => Mathf.Abs(sourceVertices[i].x - center.x) < 1.45f &&
            Mathf.Abs(sourceVertices[i].z - center.z) < 1.45f;
        int Add(int i)
        {
            if (remap.TryGetValue(i, out int index)) return index;
            index = vertices.Count;
            remap.Add(i, index);
            vertices.Add(sourceVertices[i] - center);
            normals.Add(sourceNormals[i]);
            return index;
        }
        var triangles = source.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            if (!Near(a) || !Near(b) || !Near(c)) continue;
            indices.Add(Add(a));
            indices.Add(Add(b));
            indices.Add(Add(c));
        }
        if (indices.Count == 0) throw new InvalidOperationException("No fir geometry in " + source.name);
        var mesh = new Mesh { name = "Bank_Fir_" + source.name.Replace("Festive_Firs_", "") };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(indices, 0);
        return mesh;
    }

    static void Drift(Transform group, float x, float z, float width, float length, float height)
    {
        Shape("Snowdrift", group, PrimitiveType.Sphere, new Vector3(x, height * .35f, z),
            new Vector3(width, height * 1.30f, length), snow);
    }

    static void Gift(Transform parent, float x, float z, float size, float yaw, Material wrap)
    {
        var group = Node("Wrapped_Present", parent, new Vector3(x, 0, z));
        group.localRotation = Quaternion.Euler(0, yaw, 0);
        var cube = PrimitiveType.Cube;
        Shape("Box", group, cube, new Vector3(0, size * .45f, 0), new Vector3(size, size * .9f, size), wrap);
        Shape("Ribbon_A", group, cube, new Vector3(0, size * .46f, 0), new Vector3(size * .20f, size * .94f, size * 1.02f), gold);
        Shape("Ribbon_B", group, cube, new Vector3(0, size * .47f, 0), new Vector3(size * 1.02f, size * .94f, size * .20f), gold);
        Shape("Bow_Left", group, PrimitiveType.Sphere, new Vector3(-size * .14f, size * .95f, 0),
            new Vector3(size * .27f, size * .13f, size * .18f), gold).localRotation = Quaternion.Euler(0, 0, -24);
        Shape("Bow_Right", group, PrimitiveType.Sphere, new Vector3(size * .14f, size * .95f, 0),
            new Vector3(size * .27f, size * .13f, size * .18f), gold).localRotation = Quaternion.Euler(0, 0, 24);
    }

    static void Bush(Transform parent, Vector3 center, float scale, bool berries)
    {
        var bush = Node("Snow_Capped_Evergreen", parent, center);
        bush.localScale = Vector3.one * scale;
        var sphere = PrimitiveType.Sphere;
        Shape("Foliage", bush, sphere, new Vector3(-.17f, .23f, -.03f), new Vector3(.56f, .50f, .53f), pine);
        Shape("Foliage", bush, sphere, new Vector3(.17f, .29f, .035f), new Vector3(.51f, .59f, .48f), pine);
        Shape("Snow_Cap", bush, sphere, new Vector3(-.19f, .43f, -.05f), new Vector3(.41f, .15f, .39f), snow);
        Shape("Snow_Cap", bush, sphere, new Vector3(.16f, .53f, .03f), new Vector3(.37f, .16f, .34f), snow);
        if (!berries) return;
        foreach (float side in new[] { -1f, 1f })
        {
            Shape("Winter_Berry", bush, sphere, new Vector3(-.19f, .28f, side * .26f), Vector3.one * .072f, berry);
            Shape("Winter_Berry", bush, sphere, new Vector3(.15f, .34f, side * .24f), Vector3.one * .066f, berry);
        }
    }

    static GameObject RoadPrefab(string name, Action<Transform> build, bool rootCollider)
    {
        var root = Node(name, null, Vector3.zero);
        try
        {
            build(root);
            var bounds = BoundsOf(root);
            if (rootCollider)
            {
                if (bounds.min.y > .02f || bounds.min.y < -.5f || Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z)) > 6f ||
                    Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)) > 5f)
                    throw new InvalidOperationException("Invalid road decoration bounds " + name + " " + bounds);
                root.tag = "Decoration";
                root.gameObject.layer = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Locations/Decoration/Hilly_Prop_Tree_Pine_1.prefab").layer;
                var collider = root.gameObject.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
                collider.isTrigger = true;
            }
            audit.Add(name + " bounds " + bounds.min + " to " + bounds.max);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/" + name + ".prefab");
            if (!prefab) throw new InvalidOperationException("Could not save " + name);
            return prefab;
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    static void Fir(GameObject prefab, Transform parent, Vector3 position, float scale, float yaw)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        instance.transform.localScale = Vector3.one * scale;
    }

    static GameObject[] BuildRoute(Transform castle)
    {
        var first = castle.Find("Winter_176/Festive_Firs");
        var fir = RoadPrefab("Bank_Decorated_Fir", root =>
        {
            foreach (var filter in Meshes(first))
            {
                var mesh = ExtractFirPart(filter.sharedMesh, new Vector3(-7.6f, .2f, -5.2f));
                AssetDatabase.CreateAsset(mesh, Folder + "/" + mesh.name + ".asset");
                AddMesh(root, mesh.name, mesh, filter.GetComponent<MeshRenderer>().sharedMaterial);
            }
        }, false);

        return new[]
        {
            RoadPrefab("Bank_Snowdrifts", root =>
            {
                Drift(root, -.7f, .3f, 5.7f, 3.6f, .52f);
                Drift(root, 2.1f, -1.4f, 2.4f, 1.8f, .30f);
                Drift(root, -.4f, -2.5f, 2.9f, 1.2f, .22f);
                Bush(root, new Vector3(1.45f, 0, 1.15f), 1.25f, false);
                Bake(root);
            }, true),
            RoadPrefab("Bank_Single_Fir", root =>
            {
                var snowAndGift = Node("Single_Fir_Base", root, Vector3.zero);
                Drift(snowAndGift, -.3f, .2f, 3.8f, 3.2f, .45f);
                Drift(snowAndGift, 1.8f, -.9f, 1.9f, 1.5f, .23f);
                Gift(snowAndGift, 1.05f, -1.1f, .48f, 24, berry);
                Bake(snowAndGift);
                Fir(fir, root, Vector3.zero, 1.28f, 37);
            }, true),
            RoadPrefab("Bank_Firs_And_Gifts", root =>
            {
                var snowAndGifts = Node("Fir_Grove_Base", root, Vector3.zero);
                Drift(snowAndGifts, -1.6f, .5f, 4.7f, 3.3f, .48f);
                Drift(snowAndGifts, 1.7f, -1.0f, 3.5f, 2.4f, .37f);
                Gift(snowAndGifts, .35f, -.55f, .58f, 17, berry);
                Gift(snowAndGifts, .98f, .04f, .41f, 58, blue);
                Gift(snowAndGifts, -.5f, -1.31f, .34f, -12, pine);
                Bake(snowAndGifts);
                Fir(fir, root, new Vector3(-1.65f, 0, .55f), 1.48f, 18);
                Fir(fir, root, new Vector3(1.9f, 0, -1.25f), 1.05f, 125);
            }, true),
            RoadPrefab("Bank_Snow_And_Gifts", root =>
            {
                Drift(root, -.7f, .8f, 3.5f, 2.4f, .58f);
                Drift(root, 1.6f, -1.15f, 2.2f, 1.6f, .33f);
                Gift(root, -.65f, -.96f, .56f, 21, blue);
                Gift(root, .25f, -.54f, .43f, -12, berry);
                Gift(root, 1.1f, .45f, .31f, 56, pine);
                Bake(root);
            }, true)
        };
    }

    static void BuildCastle(GameObject castle)
    {
        var root = castle.transform;
        var beds = root.Find("08_Distributed_Greenery");
        var flowers = beds.GetComponentsInChildren<Transform>()
            .Where(t => t.name == "Small_Flowers").GroupBy(t => t.parent).ToArray();
        if (flowers.Length < 10) throw new InvalidOperationException("Expected flower groups in winter castle");
        var shrubs = Node("Winter_Snow_Capped_Shrubs", root, Vector3.zero);
        int count = 0;
        foreach (var group in flowers)
        {
            var positions = group.Select(t => root.InverseTransformPoint(t.position)).ToArray();
            var center = positions.Aggregate(Vector3.zero, (sum, p) => sum + p) / positions.Length;
            center.y = .2f;
            Bush(shrubs, center, .88f + .12f * (count % 4), count % 3 != 0);
            foreach (var plant in group) plant.gameObject.SetActive(false);
            count++;
        }
        Bake(shrubs);
        if (!PrefabUtility.SaveAsPrefabAsset(castle, Castle)) throw new InvalidOperationException("Castle save failed");
        audit.Add($"Castle: {flowers.Sum(g => g.Count())} flower props hidden, {count} snowy bush groups");
    }

    static void SetupCollection(GameObject[] prefabs)
    {
        var collection = AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>("Assets/Events/NewYear/NY_Decor.asset");
        var serialized = new SerializedObject(collection);
        serialized.FindProperty("temporarySpawnItems").ClearArray();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var weights = new[] { 52f, 24f, 13f, 11f };
        collection.spawnItems = prefabs.Select((p, i) => new DecorationSpawnItem { prefab = p, chance = weights[i] }).ToList();
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssetIfDirty(collection);
    }

    public static string Build()
    {
        if (AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("Route assets already built");
        AssetDatabase.CreateFolder("Assets/Events/NewYear", "Route178");
        audit.Clear();
        snow = Mat("Snow");
        pine = Mat("Fir");
        berry = Mat("Ruby");
        gold = Mat("Gold");
        blue = Mat("WinterTown170/Gift_Blue");
        BuildGround();
        var castle = PrefabUtility.LoadPrefabContents(Castle);
        try
        {
            var prefabs = BuildRoute(castle.transform);
            BuildCastle(castle);
            SetupCollection(prefabs);
        }
        finally { PrefabUtility.UnloadPrefabContents(castle); }
        File.WriteAllLines(Preview + "art.txt", audit);
        return string.Join("\n", audit);
    }

    public static string ConfigureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("LevelEvent has unsaved changes");
            var roots = scene.GetRootGameObjects();
            var land = roots.SelectMany(g => g.GetComponentsInChildren<LendController>(true)).Single();
            var banks = land.locations.SelectMany(l => l.groundSegments).SelectMany(s => s.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.sharedMaterial && (r.sharedMaterial.name == "Grass_1" || r.sharedMaterial.name == "Snowy_Riverbanks"))
                .Distinct().ToArray();
            if (banks.Length != 3) throw new InvalidOperationException("Expected three playable bank segments");
            var spawner = roots.SelectMany(g => g.GetComponentsInChildren<DecorationSpawner>(true)).Single();
            var serialized = new SerializedObject(spawner);
            var clearance = serialized.FindProperty("_cityExclusionRadius");
            if (clearance == null) throw new InvalidOperationException("Winter placement code needs to compile first");
            clearance.floatValue = 60f;
            serialized.FindProperty("spawnInterval").floatValue = 15f;
            serialized.FindProperty("spawnAreaOffset").vector2Value = new Vector2(-37, 480);
            serialized.FindProperty("spawnAreaSize").vector2Value = new Vector2(74, 50);
            serialized.FindProperty("avoidAreaOffset").vector2Value = new Vector2(-11, 479);
            serialized.FindProperty("avoidAreaSize").vector2Value = new Vector2(22, 52);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
            var snowMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Snowy_Riverbanks.mat");
            foreach (var bank in banks)
            {
                bank.sharedMaterial = snowMaterial;
                PrefabUtility.RecordPrefabInstancePropertyModifications(bank);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save LevelEvent");
            return "Three snow bank segments and 60 m decoration exclusion saved for LevelEvent.";
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }
}
