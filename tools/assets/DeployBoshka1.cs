// Run Apply in Edit Mode, then Verify for each gameplay scene.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DeployBoshka1
{
    const string Art = "Boshka1_StoneCourt";
    const string Folder = "Assets/Prefabs/Workshop/Boshka1/";
    const string Enemy = "Assets/Prefabs/Locations/Enemy/";
    const string Output = "Library/Boshka1ConceptReview/";
    const string Access = "Boshka_Bank_Access";
    const int Ground = 6;
    const float Raise = .04f;
    const float EdgeY = -.30f;
    const float Court = .04f, Rise = .17f, Rim = Court + 6 * Rise, Top = Court + 26 * Rise;
    static readonly Vector3[] EnemyPositions =
    {
        new(-4, Rim, -19), new(-9, Court, -9), new(8, Court, -10),
        new(11, Court, 2), Polar(-285, 6.65f, Top)
    };

    static string Prefab(int bank) => Enemy + $"boshka_{bank}.prefab";
    static string Navigation(int bank) => Enemy + $"NavMesh-boshka_{bank}-StoneCourt.asset";
    static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    static void Record(Object value) => PrefabUtility.RecordPrefabInstancePropertyModifications(value);

    static Vector3 Polar(float degrees, float radius, float y)
    {
        float angle = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angle) * radius, y, -Mathf.Cos(angle) * radius);
    }

    static float Floor(IEnumerable<Collider> colliders, Vector3 point, float distance = .8f)
    {
        var ray = new Ray(point + Vector3.up * .15f, Vector3.down);
        return colliders.Select(c => c.Raycast(ray, out var hit, distance) && hit.normal.y > .60f
            ? hit.point.y : float.NegativeInfinity).Max();
    }

    static Vector3[] VisibleBase(Transform prop)
    {
        var vertices = prop.GetComponentsInChildren<MeshFilter>()
            .Where(f => f.sharedMesh).SelectMany(f => f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
        float y = vertices.Min(v => v.y);
        return vertices.Where(v => v.y < y + .025f).Distinct().ToArray();
    }

    static Mesh CreateBankBlend(Mesh ring)
    {
        // The workshop sand is level all the way to its edge. Preserve its rim,
        // then grade the outer portion down through the real bank at y=0.
        var source = ring.vertices;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            if (Vector3.Cross(b - a, c - a).y > 0)
                indices.AddRange(new[] { i, i+1, i+2, i, i+2, i+3 });
            else
                indices.AddRange(new[] { i, i+2, i+1, i, i+3, i+2 });
        }
        bool Outer(Vector3 p) => Mathf.Max(Mathf.Abs(p.x)/40f, Mathf.Abs(p.z)/38f) > .99f;
        Vector3 Edge(Vector3 p) => new(p.x * .72f, EdgeY - Raise, p.z * .72f);
        Vector3 Shoulder(Vector3 inner, Vector3 outer)
        {
            var p = Vector3.Lerp(inner, outer, .42f);
            p.y = inner.y;
            return p;
        }
        Require(source.Length == 48, "Unexpected workshop sand topology");
        for (int i = 0; i < source.Length; i += 4)
        {
            var face = source.Skip(i).Take(4).ToArray();
            Require(face.Count(Outer) == 2, "Missing sand edge");
            var inner = face.Where(p => !Outer(p)).ToArray();
            var outer = face.Where(Outer).ToArray();
            if (Vector3.Distance(inner[0], outer[1]) + Vector3.Distance(inner[1], outer[0])
                < Vector3.Distance(inner[0], outer[0]) + Vector3.Distance(inner[1], outer[1]))
                Array.Reverse(outer);
            var a = Edge(outer[0]); var b = Edge(outer[1]);
            var c = Shoulder(inner[0], a); var d = Shoulder(inner[1], b);
            Quad(inner[0], inner[1], d, c);
            Quad(c, d, b, a);
        }
        var mesh = new Mesh { name = "Boshka1_BankBlend" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void PlaceArt(GameObject root, Transform art, Transform center, Mesh blend, int bank)
    {
        art.SetPositionAndRotation(new Vector3(center.position.x, Raise, center.position.z),
            Quaternion.Euler(0, bank == 1 ? 270 : 90, 0));
        center.Find("boshka").gameObject.SetActive(false);
        Record(center.Find("boshka").gameObject);
        foreach (var link in root.GetComponentsInChildren<NavMeshLink>(true).Where(l => l.name != Access))
        {
            link.gameObject.SetActive(false);
            Record(link.gameObject);
        }
        art.Find("Workshop_Walk_Anchors").gameObject.SetActive(false);
        var sand = art.Find("Sandstone_Court/Surrounding_Sand");
        sand.GetComponent<MeshFilter>().sharedMesh = blend;
        sand.GetComponent<MeshCollider>().sharedMesh = blend;
        foreach (var collider in art.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger))
        {
            collider.gameObject.layer = Ground;
            collider.gameObject.tag = "Untagged";
            Record(collider.gameObject);
        }
        Record(sand.GetComponent<MeshFilter>());
        Record(sand.GetComponent<MeshCollider>());
        Record(art.Find("Workshop_Walk_Anchors").gameObject);
        Physics.SyncTransforms();

        var sandCollider = sand.GetComponent<MeshCollider>();
        var props = art.Find("Sandstone_Court/Grounded_Props");
        foreach (Transform prop in props)
        {
            // Only props on the exterior sand are affected by the grade.
            if (prop.name == "Niche_Pot" || art.InverseTransformPoint(prop.position).y < Rim - .1f) continue;
            var feet = VisibleBase(prop);
            var ray = new Ray(feet.OrderBy(v => v.y).First() + Vector3.up, Vector3.down);
            Require(sandCollider.Raycast(ray, out var contact, 3f), "Missing exterior sand beneath " + prop.name);
            prop.position += Vector3.up * (contact.point.y - feet.Min(v => v.y) - .014f);
            Record(prop);
        }
        Physics.SyncTransforms();

        var cleaner = root.GetComponent<ZoneCleaner>();
        var bounds = sandCollider.bounds;
        foreach (var renderer in art.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        var first = root.transform.Find("Point1"); var second = root.transform.Find("Point2");
        first.position = new Vector3(bounds.min.x - .5f, first.position.y, bounds.max.z + .5f);
        second.position = new Vector3(bounds.max.x + .5f, second.position.y, bounds.min.z - .5f);
        cleaner.halfExtents = new Vector3(cleaner.halfExtents.x,
            Mathf.Max(cleaner.halfExtents.y, bounds.max.y), cleaner.halfExtents.z);
        Record(first); Record(second); Record(cleaner);

        var spawner = root.GetComponent<LocationContentSpawner>();
        Require(spawner.itemCollection && spawner.itemSpawnPoints.Count == 5 &&
            spawner.enemySpawnPoints.Count == 5, "Unexpected slot configuration on " + root.name);
        for (int i = 0; i < 5; i++)
        {
            var marker = spawner.itemSpawnPoints[i].spawnTransform;
            // Leave room for the full gameplay loot pool at the second slot.
            var point = i == 1 ? art.TransformPoint(new Vector3(-11.45f, Court, 4))
                : art.Find($"Workshop_Walk_Anchors/Item{i+1:00}").position;
            marker.SetPositionAndRotation(point + Vector3.up * .22f, art.rotation);
            Record(marker);
        }
    }

    static Vector3 Sample(Vector3 point, float radius)
    {
        Require(NavMesh.SamplePosition(point, out var hit, radius, NavMesh.AllAreas),
            "No navigation at " + point.ToString("F2"));
        return hit.position;
    }

    static void Route(Vector3 from, Vector3 to, string label)
    {
        var path = new NavMeshPath();
        Require(NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) &&
            path.status == NavMeshPathStatus.PathComplete, "Incomplete navigation: " + label);
    }

    static NavMeshData Bake(GameObject root, Transform art)
    {
        var surface = root.GetComponent<NavMeshSurface>();
        surface.RemoveData();
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        var solids = art.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger);
        var sources = solids.OfType<MeshCollider>().Where(c => c.sharedMesh).Select(c => new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh, sourceObject = c.sharedMesh, transform = c.transform.localToWorldMatrix
        }).Concat(solids.OfType<BoxCollider>().Select(c => new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box, size = c.size,
            transform = c.transform.localToWorldMatrix * Matrix4x4.Translate(c.center)
        })).ToList();
        var settings = surface.GetBuildSettings();
        settings.agentClimb = .30f;
        var bounds = new Bounds(root.transform.InverseTransformPoint(art.position), new Vector3(66, 24, 66));
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, root.transform.position, root.transform.rotation);
        Require(data, "Navigation bake failed on " + root.name);
        var shift = new Vector3(5000, 0, 5000);
        var handle = NavMesh.AddNavMeshData(data, root.transform.position + shift, root.transform.rotation);
        Require(handle.valid, "Could not load Boshka navigation");
        try
        {
            var inside = Sample(art.TransformPoint(new Vector3(-3.5f, Rim, -19)) + shift, .6f);
            var spawner = root.GetComponent<LocationContentSpawner>();
            for (int i = 0; i < EnemyPositions.Length; i++)
            {
                var point = Sample(art.TransformPoint(EnemyPositions[i]) + shift, .8f);
                Route(inside, point, root.name + " enemy " + (i + 1));
                var marker = spawner.enemySpawnPoints[i];
                marker.position = point - shift + Vector3.up * .06f;
                Record(marker);
            }
            var crossing = root.transform.Find(Access);
            if (!crossing)
            {
                var go = new GameObject(Access);
                SceneManager.MoveGameObjectToScene(go, root.scene);
                crossing = go.transform;
                crossing.SetParent(root.transform, false);
                go.AddComponent<NavMeshLink>();
            }
            var link = crossing.GetComponent<NavMeshLink>();
            var bank = art.TransformPoint(new Vector3(-3.5f, 0, -29));
            bank.y = .03f;
            link.startPoint = crossing.InverseTransformPoint(bank);
            link.endPoint = crossing.InverseTransformPoint(inside - shift);
            link.agentTypeID = surface.agentTypeID;
            link.width = 2;
            link.bidirectional = true;
        }
        finally { handle.Remove(); }
        return data;
    }

    public static string Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Use Unity Edit Mode");
        var workshop = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + Art + ".prefab");
        var sand = workshop.transform.Find("Sandstone_Court/Surrounding_Sand").GetComponent<MeshFilter>().sharedMesh;
        var roots = new GameObject[2];
        var nav = new NavMeshData[2];
        var blend = CreateBankBlend(sand);
        Directory.CreateDirectory(Output + "BeforeDeploy");
        try
        {
            for (int bank = 1; bank <= 2; bank++)
            {
                string backup = Output + "BeforeDeploy/" + Path.GetFileName(Prefab(bank));
                if (!File.Exists(backup)) File.Copy(Prefab(bank), backup);
                var root = roots[bank - 1] = PrefabUtility.LoadPrefabContents(Prefab(bank));
                var center = root.transform.Find("PointCenter (1)");
                var art = root.transform.Find(Art);
                if (!art) art = ((GameObject)PrefabUtility.InstantiatePrefab(workshop, root.transform)).transform;
                PlaceArt(root, art, center, blend, bank);
                nav[bank - 1] = Bake(root, art);
            }

            var blendPath = Folder + blend.name + ".asset";
            var savedBlend = AssetDatabase.LoadAssetAtPath<Mesh>(blendPath);
            if (savedBlend)
            {
                EditorUtility.CopySerialized(blend, savedBlend);
                EditorUtility.SetDirty(savedBlend);
            }
            else
            {
                AssetDatabase.CreateAsset(blend, blendPath);
                savedBlend = blend;
            }
            for (int bank = 1; bank <= 2; bank++)
            {
                var root = roots[bank - 1];
                var gameSand = root.transform.Find(Art + "/Sandstone_Court/Surrounding_Sand");
                var filter = gameSand.GetComponent<MeshFilter>();
                var collider = gameSand.GetComponent<MeshCollider>();
                filter.sharedMesh = savedBlend;
                collider.sharedMesh = savedBlend;
                Record(filter); Record(collider);
                var surface = root.GetComponent<NavMeshSurface>();
                var savedNav = AssetDatabase.LoadAssetAtPath<NavMeshData>(Navigation(bank));
                if (savedNav)
                {
                    EditorUtility.CopySerialized(nav[bank - 1], savedNav);
                    EditorUtility.SetDirty(savedNav);
                }
                else
                {
                    savedNav = nav[bank - 1];
                    AssetDatabase.CreateAsset(savedNav, Navigation(bank));
                }
                surface.navMeshData = savedNav;
                Record(surface);
                Require(PrefabUtility.SaveAsPrefabAsset(root, Prefab(bank)), "Failed to save " + root.name);
            }
            AssetDatabase.SaveAssets();
            return "Saved both Boshka banks with five loot slots and five internally connected enemy spawns each.";
        }
        finally
        {
            foreach (var root in roots) if (root) PrefabUtility.UnloadPrefabContents(root);
            if (blend && !AssetDatabase.Contains(blend)) Object.DestroyImmediate(blend);
            foreach (var data in nav)
                if (data && !AssetDatabase.Contains(data)) Object.DestroyImmediate(data);
        }
    }

    static int Walk(Transform art, Collider land)
    {
        var solids = art.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger)
            .Append(land).ToArray();
        var go = new GameObject("Boshka verification player");
        SceneManager.MoveGameObjectToScene(go, art.gameObject.scene);
        var player = go.AddComponent<CharacterController>();
        var reference = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab")
            .GetComponent<CharacterController>();
        player.center = reference.center;
        player.radius = reference.radius;
        player.height = reference.height;
        player.skinWidth = reference.skinWidth;
        player.stepOffset = reference.stepOffset;
        player.slopeLimit = reference.slopeLimit;
        go.transform.position = art.TransformPoint(new Vector3(-3.5f, -Raise + .04f, -30));
        Physics.SyncTransforms();
        int count = 0;
        void Go(Vector3 local)
        {
            var target = art.TransformPoint(local);
            if (local.z < -19)
                target.y = Floor(solids, new Vector3(target.x, 1.18f, target.z), 2f);
            Require(float.IsFinite(target.y), $"Unsupported approach near {local:F2}");
            int steps = Mathf.CeilToInt(Vector3.Distance(go.transform.position, target) / .051f) + 52;
            float fall = .015f;
            for (int i = 0; i < steps; i++)
            {
                var horizontal = target - go.transform.position;
                horizontal.y = 0;
                fall = player.isGrounded ? .015f : Mathf.Min(.14f, fall + .005f);
                player.Move(Vector3.ClampMagnitude(horizontal, .051f) + Vector3.down * fall);
            }
            var difference = art.InverseTransformPoint(go.transform.position) - art.InverseTransformPoint(target);
            Require(new Vector2(difference.x, difference.z).magnitude < .28f && Mathf.Abs(difference.y) < .29f &&
                player.isGrounded, $"Walk blocked at {local:F2}, reached {art.InverseTransformPoint(go.transform.position):F2}");
            count++;
        }
        var approach = Enumerable.Range(0, 14).Select(i => new Vector3(-3.5f, Rim, -30 + i)).ToArray();
        var entry = Enumerable.Range(0, 6).Select(i => new Vector3(-3.5f, Rim - (i+1)*Rise, -15.6f + (i+.5f)*.6f));
        var court = new[]
        {
            new Vector3(-3.5f, Court, -11.48f), new Vector3(-9.2f, Court, -10.45f),
            new Vector3(-12.4f, Court, -9.25f), new Vector3(-12.6f, Court, -5), new Vector3(-11.5f, Court, 4),
            new Vector3(-12.6f, Court, -5), new Vector3(-12.4f, Court, -9.25f),
            new Vector3(-9.2f, Court, -10.45f), new Vector3(7.9f, Court, -10.15f),
            new Vector3(8.1f, Court, -6.65f), new Vector3(7.9f, Court, -10.15f),
            new Vector3(-.5f, Court, -9.8f), Polar(-10, 7, Court)
        };
        var climb = Enumerable.Range(0, 26)
            .Select(i => Polar(Mathf.Lerp(-18, -231, (i+.5f)/26), 7.04f, Court + Rise*(i+1)));
        var terrace = new[] { -242f, -253f, -265f, -277f, -290f, -304f, -310f }
            .Select(angle => Polar(angle, 6.65f, Top));
        var route = approach.Concat(new[] { new Vector3(-3.5f, Rim, -16) })
            .Concat(entry).Concat(court).Concat(climb).Concat(terrace).ToArray();
        foreach (var point in route.Concat(route.Reverse().Skip(1))) Go(point);
        return count;
    }

    static int VerifyBankRoutes(GameObject root, Transform art, MeshCollider land)
    {
        var nav = root.GetComponent<NavMeshSurface>();
        var bankSource = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh, sourceObject = land.sharedMesh, transform = land.transform.localToWorldMatrix
        };
        var region = new Bounds(art.position, new Vector3(95, 16, 100));
        var data = NavMeshBuilder.BuildNavMeshData(nav.GetBuildSettings(), new List<NavMeshBuildSource> { bankSource },
            region, Vector3.zero, Quaternion.identity);
        var offset = new Vector3(7500, 0, 7500);
        var outside = NavMesh.AddNavMeshData(data, offset, Quaternion.identity);
        var inside = NavMesh.AddNavMeshData(nav.navMeshData, root.transform.position + offset, root.transform.rotation);
        var link = root.transform.Find(Access).GetComponent<NavMeshLink>();
        var crossing = NavMesh.AddLink(new NavMeshLinkData
        {
            startPosition = link.transform.TransformPoint(link.startPoint),
            endPosition = link.transform.TransformPoint(link.endPoint),
            bidirectional = link.bidirectional, width = link.width, agentTypeID = link.agentTypeID,
            costModifier = link.costModifier, area = link.area
        }, offset, Quaternion.identity);
        try
        {
            var approach = art.TransformPoint(new Vector3(-3.5f, -Raise, -34));
            var shore = Sample(approach + offset, .6f);
            int routes = 0;
            foreach (var enemy in root.GetComponent<LocationContentSpawner>().enemySpawnPoints)
            {
                var spawn = Sample(enemy.position + offset, .20f);
                Route(shore, spawn, $"{root.name} shore to {enemy.name}");
                Route(spawn, shore, $"{root.name} {enemy.name} to shore");
                routes += 2;
            }
            return routes;
        }
        finally
        {
            crossing.Remove();
            inside.Remove();
            outside.Remove();
            Object.DestroyImmediate(data);
        }
    }

    public static string Verify(string biome)
    {
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + biome + ".unity");
        var rng = UnityEngine.Random.state;
        var egg = typeof(EggSpawnRuntimeState).GetField("_spawnedInRun", BindingFlags.Static | BindingFlags.NonPublic);
        var eggState = egg.GetValue(null);
        var report = new List<string>();
        try
        {
            var sceneRoots = source.GetRootGameObjects();
            var spawner = sceneRoots.SelectMany(g => g.GetComponentsInChildren<LocationSpawner>(true)).First();
            var pool = new SerializedObject(spawner.locationCollection).FindProperty("spawnItems");
            var prefabs = Enumerable.Range(0, pool.arraySize).Select(i => pool.GetArrayElementAtIndex(i)
                .FindPropertyRelative("prefab").objectReferenceValue).Where(p => p).Select(p => p.name).ToArray();
            var landRoot = sceneRoots.SelectMany(g => g.GetComponentsInChildren<LendController>(true))
                .SelectMany(l => l.locations).SelectMany(l => l.groundSegments).First(t => t && t.name == "Land");
            var landSource = landRoot.GetComponentsInChildren<MeshCollider>(true).First(c => !c.isTrigger && c.sharedMesh);
            var profile = Resources.Load<LocationBalanceProfile>("LocationBalanceProfile");
            var loot = profile.GetBalanceForScene(biome).itemCollectionOverride;
            var spawnItems = typeof(LocationContentSpawner).GetMethod("SpawnItems", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int bank = 1; bank <= 2; bank++)
            {
                var scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(bank)), scene);
                    root.transform.SetPositionAndRotation(new Vector3(spawner.spawnX, spawner.spawnY, 0), Quaternion.identity);
                    var art = root.transform.Find(Art);
                    var quest = root.transform.Find("PointCenter (1)").GetComponent<QuestTrigger>();
                    Require(art && quest && quest.isActiveAndEnabled && !quest.transform.Find("boshka").gameObject.activeSelf,
                        "Missing Boshka art or inherited trigger");
                    var actualBank = new GameObject("Gameplay bank collision");
                    SceneManager.MoveGameObjectToScene(actualBank, scene);
                    actualBank.layer = Ground;
                    actualBank.transform.SetPositionAndRotation(landSource.transform.position, landSource.transform.rotation);
                    actualBank.transform.localScale = landSource.transform.lossyScale;
                    var land = actualBank.AddComponent<MeshCollider>();
                    land.sharedMesh = landSource.sharedMesh;
                    Physics.SyncTransforms();
                    var solids = art.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
                    var sand = art.Find("Sandstone_Court/Surrounding_Sand").GetComponent<MeshCollider>();
                    var edge = bank == 1 ? -sand.bounds.max.x : sand.bounds.min.x;
                    Require(edge > 15, "Boshka apron extends too close to the river");
                    var mesh = sand.sharedMesh.vertices;
                    var joins = new List<float>();
                    for (int i = 4; i < mesh.Length; i += 8)
                    {
                        for (int step = 0; step <= 10; step++)
                        {
                            var p = sand.transform.TransformPoint(Vector3.Lerp(mesh[i+2], mesh[i+3], step / 10f));
                            joins.Add(Floor(new[] { land }, new Vector3(p.x, .8f, p.z), 2f) - p.y);
                        }
                    }
                    Require(joins.Min() > .025f, "Outer sand edge does not sink below the real bank: " + joins.Min());
                    var apron = art.TransformPoint(new Vector3(-3.5f, Court, -11.48f));
                    var rim = art.TransformPoint(new Vector3(-3.5f, Rim, -16.8f));
                    float apronHeight = Floor(solids, apron);
                    float bankHeight = Floor(new[] { land }, apron);
                    float depth = Floor(solids, rim) - apronHeight;
                    Require(Mathf.Abs(depth - 1.02f) < .025f && apronHeight - bankHeight > .035f,
                        "Gameplay ground obscures the courtyard");
                    var sculpture = art.Find("Original_Boshka_Sculpture");
                    var feet = VisibleBase(sculpture.Find("Original_Horned_Skull"));
                    var seat = feet.Aggregate(Vector3.zero, (sum, point) => sum + point) / feet.Length;
                    var plinth = sculpture.Find("Original_Stone_Plinth").GetComponent<MeshCollider>();
                    Require(plinth.Raycast(new Ray(seat + Vector3.up, Vector3.down), out var contact, 3f),
                        "Missing skull support");
                    float skullSeat = contact.point.y - feet.Min(p => p.y);
                    Require(Mathf.Abs(skullSeat - .018f) < .006f, "Skull is no longer seated against the stone");
                    var grass = art.Find("Sandstone_Court/Grounded_Props").Cast<Transform>().Where(p => p.name == "Dry_Grass");
                    var plantGaps = grass.SelectMany(VisibleBase).Select(p => p.y - Floor(solids.Append(land), p)).ToArray();
                    Require(plantGaps.Max() < .025f && plantGaps.Min() > -.16f,
                        $"Grass contact outside tolerance {plantGaps.Min():F3}..{plantGaps.Max():F3}");
                    int walk = Walk(art, land);

                    var content = root.GetComponent<LocationContentSpawner>();
                    Require(content.itemSpawnPoints.Count == 5 && content.enemySpawnPoints.Count == 5 &&
                        content._enemyPrefabs.All(e => e), "Gameplay references missing");
                    foreach (var item in content.itemSpawnPoints)
                    {
                        var p = item.spawnTransform.position;
                        Require(Mathf.Abs(p.y - Floor(solids, p) - .22f) < .045f, "Unsupported loot marker " + item.spawnTransform.name);
                    }
                    UnityEngine.Random.InitState(231 + bank);
                    spawnItems.Invoke(content, new object[] { loot ? loot : content.itemCollection });
                    var items = root.GetComponentsInChildren<PickableItem>();
                    Require(items.Length == 5 && content.itemSpawnPoints.All(p => items.Any(item =>
                        Vector3.Distance(item.transform.position, p.spawnTransform.position) < .02f)), "Native loot did not fill 5/5 slots");
                    int routes = VerifyBankRoutes(root, art, land);
                    report.Add($"{biome}/{root.name} (pool: {prefabs.Contains(root.name)}): depth {depth:F3} m; " +
                        $"court +{apronHeight-bankHeight:F3} m over ground; river edge |X| {edge:F1} m; " +
                        $"outer seam {joins.Min():F3}..{joins.Max():F3} m under bank; " +
                        $"skull seat {skullSeat:F3} m; grass {plantGaps.Min():F3}..{plantGaps.Max():F3} m; " +
                        $"{walk} walking checkpoints; loot 5/5; {routes} complete bank routes.");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }
        finally
        {
            UnityEngine.Random.state = rng;
            egg.SetValue(null, eggState);
            EditorSceneManager.ClosePreviewScene(source);
        }
        File.WriteAllLines(Output + "game-check-" + biome + ".txt", report);
        return string.Join("\n", report);
    }
}
