using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterRoute179
{
    const string Folder = "Assets/Events/NewYear/Route179";
    const string Castle = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string ScenePath = "Assets/Scenes/LevelEvent.unity";
    const string Report = "Docs/locations/previews/event-winter179-art.txt";
    static readonly List<string> audit = new List<string>();
    static Material snow, blueSnow, foliage, foliageLight, bark, barkLight, wood;
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


    static Material NewColor(string name, Color color)
    {
        var material = new Material(snow) { name = name, color = color };
        AssetDatabase.CreateAsset(material, Folder + "/" + name + ".mat");
        return material;
    }

    static Mesh SnowOnLeaves(Mesh leaves)
    {
        var vertices = leaves.vertices;
        var triangles = leaves.triangles;
        var bounds = leaves.bounds;
        var snowVertices = new List<Vector3>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]];
            var b = vertices[triangles[i + 1]];
            var c = vertices[triangles[i + 2]];
            var center = (a + b + c) / 3f;
            var normal = Vector3.Cross(b - a, c - a).normalized;
            float height = Mathf.InverseLerp(bounds.min.y, bounds.max.y, center.y);
            if (normal.y < .42f || height < .53f + .09f * Mathf.Sin(i * .67f)) continue;
            var lift = (normal + Vector3.up) * bounds.size.y * .013f;
            snowVertices.Add(center + (a - center) * .98f + lift);
            snowVertices.Add(center + (b - center) * .98f + lift);
            snowVertices.Add(center + (c - center) * .98f + lift);
        }
        if (snowVertices.Count == 0) throw new InvalidOperationException("No upward foliage for snow");
        var mesh = new Mesh { name = "Snow_Tracing_Leaves" };
        mesh.SetVertices(snowVertices);
        mesh.SetTriangles(Enumerable.Range(0, snowVertices.Count).ToArray(), 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    static void AddShrub(Transform parent, Vector3 position, Mesh leaves, Mesh cap, float height, float yaw, Material color)
    {
        var node = Node("Evergreen_Leaves", parent, position);
        node.localRotation = Quaternion.Euler(0, yaw, 0);
        float scale = height / leaves.bounds.size.y;
        node.localScale = new Vector3(scale * 1.05f, scale, scale * .92f);
        var b = leaves.bounds;
        var pivot = -new Vector3(b.center.x, b.min.y, b.center.z);
        AddMesh(node, "Leaf_Clusters", leaves, color).localPosition = pivot;
        AddMesh(node, "Snow_On_Leaves", cap, snow).localPosition = pivot;
    }

    static void RebuildShrubs()
    {
        var castle = PrefabUtility.LoadPrefabContents(Castle);
        var caps = new List<Mesh>();
        try
        {
            var original = castle.transform.Find("Winter_Snow_Capped_Shrubs");
            if (!original) throw new InvalidOperationException("Old shrubs missing");
            var beds = castle.transform.Find("08_Distributed_Greenery");
            var flowerGroups = beds.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Small_Flowers").GroupBy(t => t.parent).ToArray();
            if (flowerGroups.Length != 15) throw new InvalidOperationException("Expected 15 courtyard beds");
            var leaves = new[] { 1, 3, 4 }.Select(i =>
                AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Locations/Decoration/Hilly_Prop_Bush_{i}.prefab")
                    .GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).First(m => m)).ToArray();
            caps.AddRange(leaves.Select(SnowOnLeaves));

            var shrubs = Node("Winter_Leafy_Shrubs", castle.transform, Vector3.zero);
            for (int i = 0; i < flowerGroups.Length; i++)
            {
                var points = flowerGroups[i].Select(t => castle.transform.InverseTransformPoint(t.position)).ToArray();
                var center = points.Aggregate(Vector3.zero, (sum, p) => sum + p) / points.Length;
                center.y = .185f;
                int main = i % leaves.Length;
                int side = (i + 1) % leaves.Length;
                AddShrub(shrubs, center, leaves[main], caps[main], .43f + .06f * (i % 3), i * 79f, foliage);
                var offset = Quaternion.Euler(0, i * 73f, 0) * new Vector3(.31f, 0, .08f);
                AddShrub(shrubs, center + offset, leaves[side], caps[side], .29f + .035f * (i % 4), i * 137f, foliageLight);
            }
            Bake(shrubs);
            foreach (var child in shrubs.Cast<Transform>().Where(t => t.childCount == 0 && !t.GetComponent<MeshFilter>()).ToArray())
                Object.DestroyImmediate(child.gameObject);
            Object.DestroyImmediate(original.gameObject);
            if (!PrefabUtility.SaveAsPrefabAsset(castle, Castle)) throw new InvalidOperationException("Castle save failed");
            audit.Add("Castle: 15 beds with 30 native irregular foliage meshes and snow fitted to upper leaves.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(castle);
            foreach (var cap in caps) Object.DestroyImmediate(cap);
        }
    }

    static Mesh DriftMesh(string name, float width, float length, float height)
    {
        const int count = 20;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (int ring = 0; ring < 4; ring++)
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                float radius = (1f - ring * .23f) * (1f + .13f * Mathf.Sin(angle * 3 + .7f));
                float x = Mathf.Cos(angle) * width * .5f * radius + ring * .10f;
                float z = Mathf.Sin(angle) * length * .5f * radius - ring * .11f;
                float y = ring == 0 ? -.12f : height * (.29f * ring) *
                    (1f + .11f * Mathf.Cos(angle * 2f + .5f));
                vertices.Add(new Vector3(x, y, z));
                if (ring < 3)
                {
                    int a = ring * count + i;
                    int b = ring * count + (i + 1) % count;
                    triangles.AddRange(new[] { a, a + count, b, b, a + count, b + count });
                }
            }
        int peak = vertices.Count;
        vertices.Add(new Vector3(.39f, height, -.52f));
        for (int i = 0; i < count; i++) triangles.AddRange(new[] { 3 * count + i, peak, 3 * count + (i + 1) % count });
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, Folder + "/" + name + ".asset");
        return mesh;
    }

    static Mesh TrunkMesh(bool topping)
    {
        const int sides = 16;
        var vertices = new List<Vector3>();
        var darkFaces = new List<int>();
        var lightFaces = new List<int>();
        var cutFaces = new List<int>();
        void Quad(List<int> faces, int a, int b, int c, int d) => faces.AddRange(new[] { a, b, c, a, c, d });
        for (int ring = 0; ring < 2; ring++)
            for (int surface = 0; surface < 2; surface++)
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * 2f * Mathf.PI / sides;
                    float roughness = 1f + .07f * Mathf.Sin(angle * 5f + ring * .8f);
                    float x = Mathf.Cos(angle) * (surface == 0 ? 1.92f * roughness : 1.51f);
                    float y = 1.25f + Mathf.Sin(angle) * (surface == 0 ? 1.84f * roughness : 1.52f);
                    float z = (ring == 0 ? -3.5f : 3.5f) + .20f * Mathf.Cos(angle * 3f + ring);
                    if (topping) { y += .14f + .06f * Mathf.Sin(angle * 3f); z *= .88f; }
                    vertices.Add(new Vector3(x, Mathf.Max(-.22f, y), z));
                }
        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            if (topping)
            {
                if (i >= 2 && i < 6) Quad(darkFaces, i, j, j + 2 * sides, i + 2 * sides);
                continue;
            }
            var outer = i % 3 == 0 ? lightFaces : darkFaces;
            Quad(outer, i, j, j + 2 * sides, i + 2 * sides);
            Quad(cutFaces, i + sides, i + 3 * sides, j + 3 * sides, j + sides);
            Quad(cutFaces, i, i + sides, j + sides, j);
            Quad(cutFaces, i + 2 * sides, j + 2 * sides, j + 3 * sides, i + 3 * sides);
        }
        var mesh = new Mesh { name = topping ? "Snow_On_Bark" : "Open_Log_Shell" };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = topping ? 1 : 3;
        mesh.SetTriangles(darkFaces, 0);
        if (!topping) { mesh.SetTriangles(lightFaces, 1); mesh.SetTriangles(cutFaces, 2); }
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, Folder + "/" + mesh.name + ".asset");
        return mesh;
    }

    static GameObject BuildProp(string name, Action<Transform> setup)
    {
        var root = Node(name, null, Vector3.zero);
        try
        {
            root.tag = "Decoration";
            root.gameObject.layer = 9;
            setup(root);
            var bounds = BoundsOf(root);
            if (Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)) > 5.2f ||
                Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z)) > 5f)
                throw new InvalidOperationException("Prop exceeds the cleared bank footprint " + bounds);
            var trigger = root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = bounds.center;
            trigger.size = bounds.size;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/" + name + ".prefab");
            if (!prefab) throw new InvalidOperationException("Could not save " + name);
            audit.Add(name + " bounds " + bounds.min + ".." + bounds.max);
            return prefab;
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    static GameObject TallDrift() => BuildProp("Bank_Tall_Snowdrifts", root =>
    {
        var main = AddMesh(root, "Wind_Shaped_Drift", DriftMesh("Wind_Shaped_Drift", 7.6f, 6.7f, 4.15f), snow);
        main.gameObject.AddComponent<MeshCollider>().sharedMesh = main.GetComponent<MeshFilter>().sharedMesh;
        var edge = AddMesh(root, "Secondary_Drift", DriftMesh("Secondary_Drift", 3.4f, 2.9f, 1.34f), blueSnow);
        edge.localPosition = new Vector3(-2.85f, -.06f, -1.8f);
        Shape("Scattered_Snow", root, PrimitiveType.Sphere, new Vector3(3.45f, .16f, 1.65f),
            new Vector3(2.2f, .55f, 2.6f), snow);
    });

    static GameObject HollowLog() => BuildProp("Bank_Walkthrough_Log", root =>
    {
        var shell = TrunkMesh(false);
        var log = AddMesh(root, "Hollow_Bark_And_Interior", shell, bark);
        log.GetComponent<MeshRenderer>().sharedMaterials = new[] { bark, barkLight, wood };
        log.gameObject.AddComponent<MeshCollider>().sharedMesh = shell;
        AddMesh(root, "Snow_On_Log", TrunkMesh(true), snow);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? 1.85f : -1.83f;
            float z = -2.3f + i * 1.5f;
            var knot = Shape("Bark_Ridge", root, PrimitiveType.Cube, new Vector3(x, 1.18f + .12f * i, z),
                new Vector3(.20f, .18f, 1.85f), i % 2 == 0 ? barkLight : bark);
            knot.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 10 : -12);
        }
        Shape("Snow_At_Log_Foot", root, PrimitiveType.Sphere, new Vector3(-2.05f,.16f, .30f), new Vector3(1.45f,.60f, 4.9f), blueSnow);
        Shape("Snow_At_Log_Foot", root, PrimitiveType.Sphere, new Vector3(2.05f,.12f, -.50f), new Vector3(1.30f,.49f, 4.4f), snow);
    });

    static Material MakeFlakes()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f;
                float dy = (y + .5f) / size * 2f - 1f;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .88f, Mathf.Sqrt(dx * dx + dy * dy)));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        string path = Folder + "/Snowflake.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var material = new Material(Shader.Find("Sprites/Default")) { name = "Soft_Snowflakes", color = new Color(.94f, .98f, 1f, .87f) };
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        AssetDatabase.CreateAsset(material, Folder + "/Soft_Snowflakes.mat");
        return material;
    }

    public static string Build()
    {
        if (AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("Route art already exists");
        AssetDatabase.CreateFolder("Assets/Events/NewYear", "Route179");
        audit.Clear();
        snow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        blueSnow = NewColor("Snow_Drift_Blue", new Color(.75f, .86f, .95f));
        foliage = NewColor("Shrub_Green", new Color(.16f, .37f, .23f));
        foliageLight = NewColor("Shrub_Young_Leaves", new Color(.25f, .45f, .28f));
        bark = NewColor("Log_Bark", new Color(.27f, .16f, .105f));
        barkLight = NewColor("Log_Bark_Ridges", new Color(.39f, .245f, .15f));
        wood = NewColor("Log_Exposed_Wood", new Color(.57f, .36f, .21f));
        MakeFlakes();
        var drift = TallDrift();
        var log = HollowLog();
        RebuildShrubs();

        var collection = AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>("Assets/Events/NewYear/NY_Decor.asset");
        if (collection.spawnItems.Count != 4) throw new InvalidOperationException("Unexpected route decoration list");
        collection.spawnItems[0].chance = 39f;
        collection.spawnItems.Add(new DecorationSpawnItem { prefab = drift, chance = 10f });
        collection.spawnItems.Add(new DecorationSpawnItem { prefab = log, chance = 8f });
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssetIfDirty(collection);
        audit.Add("Route pool: six weighted alternatives including a tall drift and an open log.");
        File.WriteAllLines(Report, audit);
        return string.Join(" | ", audit);
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
            var spawner = roots.SelectMany(g => g.GetComponentsInChildren<DecorationSpawner>(true)).Single();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_decorationSpacing").floatValue = 12f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
            if (roots.Any(r => r.name == "Event Snowfall")) throw new InvalidOperationException("Snowfall already configured");

            var weather = new GameObject("Event Snowfall");
            SceneManager.MoveGameObjectToScene(weather, scene);
            var flakes = weather.AddComponent<ParticleSystem>();
            flakes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flakes.main;
            main.loop = true;
            main.duration = 6f;
            main.prewarm = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.8f, 7.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(.045f, .12f);
            main.startColor = new Color(.98f, .99f, 1f, .82f);
            main.gravityModifier = 0f;
            main.maxParticles = 820;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var shape = flakes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 3f, 38f);
            var emission = flakes.emission;
            emission.rateOverTime = 115f;
            var velocity = flakes.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(.18f, .46f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2.65f, -1.85f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.21f, .16f);
            var renderer = flakes.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Soft_Snowflakes.mat");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            weather.AddComponent<EventSnowfall>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("LevelEvent save failed");
            return "World-space camera snowfall and 12 m prop separation saved in LevelEvent.";
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }
}
