using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterRoute181
{
    const string Folder = "Assets/Events/NewYear/Route181";
    const string Report = "Docs/locations/previews/event-winter181-assets.txt";
    const string Prefix = "Docs/locations/previews/event-winter181-";
    static readonly List<string> audit = new List<string>();
    static Material snow, shadedSnow, ice, iceDeep, stone, stoneDark, wood, bark, barkLight, red, pine, blue, gold, lamp;
    static Mesh rock, shard, runner;

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

    static Mesh SaveMesh(Mesh mesh, string path)
    {
        var target = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!target)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        target.Clear();
        target.indexFormat = mesh.indexFormat;
        target.vertices = mesh.vertices;
        target.normals = mesh.normals;
        target.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++)
            target.SetTriangles(mesh.GetTriangles(i), i);
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssetIfDirty(target);
        Object.DestroyImmediate(mesh);
        return target;
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
            mesh = SaveMesh(mesh, Folder + "/" + name + ".asset");
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


    sealed class Faces
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int>[] faces;

        public Faces(int materialCount) =>
            faces = Enumerable.Range(0, materialCount).Select(_ => new List<int>()).ToArray();

        public void Triangle(int material, Vector3 a, Vector3 b, Vector3 c)
        {
            int first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            faces[material].AddRange(new[] { first, first + 1, first + 2 });
        }

        public void Quad(int material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(material, a, b, c);
            Triangle(material, a, c, d);
        }

        public Mesh Save(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = faces.Length;
            for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i], i);
            mesh.RecalculateNormals();
            return SaveMesh(mesh, Folder + "/" + name + ".asset");
        }
    }

    static Mesh RockMesh()
    {
        const int count = 7;
        var faces = new Faces(1);
        Vector3 Point(int i, int ring)
        {
            float angle = 2 * Mathf.PI * (i % count) / count;
            float radius = ring == 0 ? .47f : ring == 1 ? .51f : .28f;
            radius *= 1f + .11f * Mathf.Sin(angle * 3 + ring * .52f);
            float y = ring == 0 ? -.46f : ring == 1 ? -.01f : .39f;
            return new Vector3(Mathf.Cos(angle) * radius + ring * .018f,
                y + .075f * Mathf.Cos(angle * 2 + ring), Mathf.Sin(angle) * radius);
        }
        for (int i = 0; i < count; i++)
        {
            for (int r = 0; r < 2; r++)
                faces.Quad(0, Point(i, r), Point(i, r + 1), Point(i + 1, r + 1), Point(i + 1, r));
            faces.Triangle(0, Point(i, 2), new Vector3(.03f, .46f, -.025f), Point(i + 1, 2));
            faces.Triangle(0, Point(i + 1, 0), new Vector3(0, -.51f, 0), Point(i, 0));
        }
        return faces.Save("Faceted_Boulder");
    }

    static Mesh CrystalMesh()
    {
        const int count = 6;
        var faces = new Faces(1);
        Vector3 Point(int i, bool top)
        {
            float a = i * Mathf.PI * 2 / count;
            return new Vector3(Mathf.Cos(a) * (top ? .40f : .51f) + (top ? .09f : 0),
                top ? .68f + .19f * Mathf.Cos(a + .4f) : 0,
                Mathf.Sin(a) * (top ? .4f : .51f));
        }
        for (int i = 0; i < count; i++)
        {
            faces.Quad(0, Point(i, false), Point(i, true), Point(i + 1, true), Point(i + 1, false));
            faces.Triangle(0, Point(i, true), new Vector3(.17f, .99f, -.10f), Point(i + 1, true));
            faces.Triangle(0, Point(i + 1, false), Vector3.zero, Point(i, false));
        }
        return faces.Save("Solid_Ice_Crystal");
    }

    static Mesh RunnerMesh()
    {
        var section = new[]
        {
            new Vector2(-1.48f,.03f), new Vector2(1.25f,.03f), new Vector2(1.64f,.16f),
            new Vector2(1.91f,.49f), new Vector2(1.91f,.77f), new Vector2(1.73f,.89f),
            new Vector2(1.65f,.71f), new Vector2(1.62f,.53f), new Vector2(1.42f,.30f),
            new Vector2(1.12f,.21f), new Vector2(-1.48f,.21f)
        };
        var faces = new Faces(1);
        Vector3 Point(int index, float x) => new Vector3(x, section[index].y, section[index].x);
        const float half = .095f;
        for (int i = 0; i < section.Length; i++)
        {
            int j = (i + 1) % section.Length;
            faces.Quad(0, Point(i, -half), Point(i, half), Point(j, half), Point(j, -half));
        }
        // Fill the concave runner profile as a strip so the curved tip stays open.
        var pairs = new[] { 0, 1, 2, 3, 4, 5 };
        for (int i = 0; i < pairs.Length - 1; i++)
        {
            int a = pairs[i], b = pairs[i + 1], c = section.Length - 1 - b, d = section.Length - 1 - a;
            if (b == c)
            {
                faces.Triangle(0, Point(a, -half), Point(b, -half), Point(d, -half));
                faces.Triangle(0, Point(a, half), Point(d, half), Point(b, half));
            }
            else
            {
                faces.Quad(0, Point(a, -half), Point(b, -half), Point(c, -half), Point(d, -half));
                faces.Quad(0, Point(a, half), Point(d, half), Point(c, half), Point(b, half));
            }
        }
        return faces.Save("Sled_Curved_Runner");
    }

    static Transform Piece(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Vector3 angles)
    {
        var node = AddMesh(parent, name, mesh, material);
        node.localPosition = position;
        node.localScale = scale;
        node.localEulerAngles = angles;
        return node;
    }

    static Transform Boulder(Transform parent, Vector3 pos, Vector3 size, float yaw, Material color) =>
        Piece(parent, "Faceted_Rock", rock, color, pos, size, new Vector3(0, yaw, 0));

    static Transform Crystal(Transform parent, Vector3 pos, Vector3 size, Vector3 angles, Material color) =>
        Piece(parent, "Ice_Facet", shard, color, pos, size, angles);

    static Transform Beam(Transform parent, Vector3 a, Vector3 b, Vector2 size, Material material)
    {
        var delta = b - a;
        var node = Shape("Timber", parent, PrimitiveType.Cube, (a + b) * .5f,
            new Vector3(size.x, size.y, delta.magnitude), material);
        node.localRotation = Quaternion.LookRotation(delta);
        return node;
    }

    static Mesh ArchMesh(bool grotto)
    {
        const int rings = 5, sides = 11;
        var faces = new Faces(3);
        Vector3 Point(int ring, int side, bool interior)
        {
            float angle = side * Mathf.PI / sides;
            float along = (float)ring / (rings - 1);
            float z = Mathf.Lerp(-3.83f, 3.78f, along);
            float curve = Mathf.Sin(Mathf.PI * along);
            float radius = interior ? 1.51f + curve * .07f : (grotto ? 3.23f : 3.38f) - curve * .17f;
            float height = interior ? 2.68f + curve * .16f : (grotto ? 4.03f : 4.15f) + curve * .39f;
            float twist = interior ? 0f : Mathf.Sin(angle * 3f + ring * .92f) * .24f;
            return new Vector3(Mathf.Cos(angle) * (radius + twist) + (interior ? 0 : .16f * curve),
                -.17f + Mathf.Sin(angle) * (height + twist) + (interior ? 0f : Mathf.Sin(angle * 2f) * .26f),
                z + (interior ? 0f : .19f * Mathf.Cos(angle * 3f + ring)));
        }
        for (int side = 0; side < sides; side++)
        {
            int next = side + 1;
            for (int i = 0; i < rings - 1; i++)
            {
                faces.Quad(grotto && (i + side) % 4 == 0 ? 2 : 0,
                    Point(i, side, false), Point(i, next, false), Point(i + 1, next, false), Point(i + 1, side, false));
                faces.Quad(1, Point(i, side, true), Point(i + 1, side, true),
                    Point(i + 1, next, true), Point(i, next, true));
            }
            faces.Quad(0, Point(0, side, false), Point(0, side, true), Point(0, next, true), Point(0, next, false));
            int far = rings - 1;
            faces.Quad(0, Point(far, side, false), Point(far, next, false), Point(far, next, true), Point(far, side, true));
        }
        return faces.Save(grotto ? "Grotto_Open_Arch" : "Drift_Open_Arch");
    }

    static void Arch(Transform root, bool grotto)
    {
        var mesh = ArchMesh(grotto);
        var shell = AddMesh(root, "Walkthrough_Shell", mesh, grotto ? stone : snow);
        shell.GetComponent<MeshRenderer>().sharedMaterials = grotto ?
            new[] { stone, stoneDark, stoneDark } : new[] { snow, shadedSnow, snow };
        shell.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    static GameObject Prop(string name, Action<Transform> build)
    {
        var root = Node(name, null, Vector3.zero);
        try
        {
            root.tag = "Decoration";
            root.gameObject.layer = 9;
            build(root);
            var bounds = BoundsOf(root);
            float radius = 0;
            foreach (var filter in Meshes(root))
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    var p = root.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    radius = Mathf.Max(radius, new Vector2(p.x,p.z).magnitude);
                }
            if (bounds.min.y > .025f || bounds.min.y < -.6f ||
                Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)) > 5.1f ||
                Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z)) > 5.0f || radius > 6.0f)
                throw new InvalidOperationException("Unsafe road model bounds " + name + " " + bounds + " radius=" + radius);
            var trigger = root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = bounds.center;
            trigger.size = bounds.size;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/" + name + ".prefab");
            if (!prefab) throw new InvalidOperationException("Could not save " + name);
            audit.Add($"{name}: bounds {bounds.min}..{bounds.max}, radial extent {radius:F2} m");
            return prefab;
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    static GameObject SnowTunnel() => Prop("Bank_01_Snow_Tunnel", root =>
    {
        Arch(root, false);
        var accents = Node("Tunnel_Snow_Accents", root, Vector3.zero);
        Boulder(accents, new Vector3(1.76f, 4.03f, .61f), new Vector3(4.48f, 2.34f, 4.75f), -11, snow);
        Boulder(accents, new Vector3(2.70f, 3.54f, .12f), new Vector3(3.80f, 1.24f, 3.94f), 22, shadedSnow);
        Boulder(accents, new Vector3(3.14f, 4.51f, .25f), new Vector3(2.46f, 1.47f, 2.77f), -22, snow);
        Boulder(accents, new Vector3(-3.15f, .19f, -2.89f), new Vector3(1.34f, .63f, 1.67f), 17, snow);
        Boulder(accents, new Vector3(3.56f, .12f, -1.68f), new Vector3(2.01f, .62f, 2.1f), -28, snow);
        Bake(accents);
    });

    static void Gift(Transform parent, Vector3 position, float size, float yaw, Material wrap)
    {
        var group = Node("Wrapped_Gift", parent, position);
        group.localRotation = Quaternion.Euler(0, yaw, 0);
        var cube = PrimitiveType.Cube;
        Shape("Box", group, cube, Vector3.up * size * .43f, new Vector3(size, size * .86f, size), wrap);
        Shape("Ribbon", group, cube, Vector3.up * size * .435f, new Vector3(size * .19f, size * .89f, size * 1.018f), gold);
        Shape("Ribbon", group, cube, Vector3.up * size * .445f, new Vector3(size * 1.018f, size * .89f, size * .18f), gold);
        Beam(group, new Vector3(0,size*.9f,0),new Vector3(-size*.19f,size*1.04f,0),Vector2.one*size*.11f,gold);
        Beam(group, new Vector3(0,size*.9f,0),new Vector3(size*.19f,size*1.04f,0),Vector2.one*size*.11f,gold);
    }

    static GameObject RootedLog() => Prop("Bank_02_Rooted_Log", root =>
    {
        var old = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Route179/Bank_Walkthrough_Log.prefab");
        var log = (GameObject)PrefabUtility.InstantiatePrefab(old, root);
        log.name = "Hollow_Log_Core";
        log.tag = "Untagged";
        log.GetComponent<BoxCollider>().enabled = false;
        var accents = Node("Roots_And_Gift", root, Vector3.zero);
        var roots = new[]
        {
            new[] {new Vector3(-1.59f,.91f,-2.91f), new Vector3(-2.43f,.46f,-3.49f), new Vector3(-3.65f,.05f,-3.96f)},
            new[] {new Vector3(1.65f,.69f,-2.65f), new Vector3(2.55f,.36f,-3.18f), new Vector3(3.37f,.06f,-3.74f)},
            new[] {new Vector3(-1.56f,1.41f,-2.43f), new Vector3(-2.37f,1.48f,-2.93f), new Vector3(-3.00f,1.89f,-3.25f)},
            new[] {new Vector3(1.62f,.73f,2.69f), new Vector3(2.30f,.37f,3.33f), new Vector3(3.17f,.04f,3.80f)}
        };
        foreach (var path in roots)
        {
            Beam(accents,path[0],path[1],new Vector2(.49f,.41f),bark);
            Beam(accents,path[1],path[2],new Vector2(.27f,.24f),barkLight);
        }
        Boulder(accents,new Vector3(-2.4f,1.65f,-2.97f),new Vector3(.74f,.22f,.49f),35,snow);
        Boulder(accents,new Vector3(-.62f,2.78f,.30f),new Vector3(1.26f,.69f,4.17f),-5,snow);
        Gift(accents,new Vector3(2.81f,0,-1.35f),.50f,21,red);
        Bake(accents);
    });

    static GameObject FrozenGrotto() => Prop("Bank_03_Frozen_Grotto", root =>
    {
        Arch(root, true);
        var accents = Node("Rock_Snow_And_Frozen_Waterfall", root, Vector3.zero);
        var rocks = new[]
        {
            new Vector4(-2.78f,.64f,-3.05f,1.46f), new Vector4(-2.31f,1.83f,-2.97f,1.45f),
            new Vector4(-1.84f,2.94f,-2.85f,1.35f), new Vector4(-.45f,3.65f,-2.8f,1.66f),
            new Vector4(1.04f,3.51f,-2.83f,1.58f), new Vector4(2.43f,2.75f,-2.9f,1.36f),
            new Vector4(3.04f,.72f,-2.85f,1.47f), new Vector4(3.37f,2.01f,-2.12f,1.58f)
        };
        for (int i = 0; i < rocks.Length; i++)
        {
            var p = rocks[i];
            Boulder(accents, new Vector3(p.x,p.y,p.z), new Vector3(p.w,p.w,p.w*.93f), 19 + i*31, i % 3 == 0 ? stoneDark : stone);
            Boulder(accents, new Vector3(p.x-.03f,p.y+p.w*.405f,p.z-.045f),
                new Vector3(p.w*.75f,.22f,p.w*.69f), 19+i*31, snow);
        }
        for (int i = 0; i < 4; i++)
        {
            float x = 2.88f + i * .29f;
            Crystal(accents, new Vector3(x,-.06f,-3.93f+.15f*i), new Vector3(.86f,3.68f-.27f*i,.90f),
                new Vector3(-6+i, 16*i, -3+i), i % 2 == 0 ? ice : iceDeep);
        }
        Crystal(accents,new Vector3(3.49f,-.05f,-4.29f),new Vector3(.44f,.83f,.42f),new Vector3(0,24,-14),ice);
        Crystal(accents,new Vector3(2.88f,-.03f,-4.42f),new Vector3(.33f,.59f,.38f),new Vector3(12,0,15),iceDeep);
        Boulder(accents,new Vector3(-3.37f,.08f,-2.7f),new Vector3(1.9f,.49f,1.69f),20,snow);
        Bake(accents);
    });

    static GameObject GiftSled() => Prop("Bank_05_Gift_Sled", root =>
    {
        var assembly = Node("Sled_And_Presents", root, new Vector3(0,-.03f,0));
        assembly.localRotation = Quaternion.Euler(0,-28,0);
        foreach (float side in new[] {-1f,1f})
        {
            Piece(assembly,"Red_Runner",runner,red,new Vector3(side*.68f,0,0),Vector3.one,Vector3.zero);
            foreach (float z in new[] { -1.04f,.63f })
                Beam(assembly,new Vector3(side*.66f,.15f,z),new Vector3(side*.61f,.59f,z+.17f),new Vector2(.20f,.18f),wood);
            for (int i=0;i<2;i++)
                Shape("Side_Plank",assembly,PrimitiveType.Cube,new Vector3(side*.70f,.80f+i*.23f,-.24f),new Vector3(.14f,.19f,2.39f),wood);
            foreach (float z in new[] {-1.28f,.72f})
                Shape("Stake",assembly,PrimitiveType.Cube,new Vector3(side*.77f,.91f,z),new Vector3(.15f,.83f,.19f),barkLight);
        }
        for (int i=0;i<6;i++)
            Shape("Cargo_Bed",assembly,PrimitiveType.Cube,new Vector3(-.59f+i*.236f,.55f,-.03f),new Vector3(.216f,.16f,2.81f),wood);
        Shape("Back_Plank",assembly,PrimitiveType.Cube,new Vector3(0,.88f,-1.35f),new Vector3(1.42f,.58f,.17f),barkLight);
        Gift(assembly,new Vector3(-.26f,.62f,-.65f),.59f,-9,pine);
        Gift(assembly,new Vector3(.34f,.62f,-.67f),.48f,19,blue);
        Gift(assembly,new Vector3(.01f,.62f,.02f),.43f,11,red);
        Boulder(assembly,new Vector3(.43f,.65f,.50f),new Vector3(.58f,.14f,.97f),0,snow);
        Gift(root,new Vector3(1.74f,-.02f,-.74f),.51f,22,red);
        Gift(root,new Vector3(2.19f,-.02f,-.25f),.31f,-17,pine);
        Boulder(root,new Vector3(0,.04f,-1.37f),new Vector3(3.03f,.45f,2.20f),-18,snow);
        Bake(root);
    });

    static GameObject FirArch() => Prop("Bank_06_Fir_Arch", root =>
    {
        var fir = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Route178/Bank_Decorated_Fir.prefab");
        foreach (float side in new[] {-1f,1f})
        {
            var lean = Node("Leaning_Fir",root,new Vector3(side*2.48f,-.035f,0));
            lean.localRotation = Quaternion.Euler(0,side*8f,side*23f);
            lean.localScale = Vector3.one * (side < 0 ? 1.16f : 1.10f);
            foreach (var filter in Meshes(fir.transform))
            {
                var material = filter.GetComponent<MeshRenderer>().sharedMaterial;
                if (material.name == "Pine_Leaves" || material.name == "Wood_Dark")
                    AddMesh(lean,material.name,filter.sharedMesh,material);
            }
            Boulder(lean,new Vector3(.0f,2.98f,.05f),new Vector3(.60f,.18f,.61f),-17,snow);
            Boulder(lean,new Vector3(side*.42f,2.16f,-.31f),new Vector3(.51f,.16f,.44f),15,snow);
            Boulder(lean,new Vector3(-side*.59f,1.45f,-.43f),new Vector3(.62f,.15f,.45f),-25,snow);
            Shape("Ribbon_Knot",lean,PrimitiveType.Cube,new Vector3(0,1.17f,-.27f),new Vector3(.27f,.14f,.14f),red);
            for (int i=0;i<2;i++)
                Shape("Ribbon_Tail",lean,PrimitiveType.Cube,new Vector3((i-.5f)*.12f,.97f,-.26f),
                    new Vector3(.105f,.40f,.035f),red).localEulerAngles = new Vector3(9,0,(i-.5f)*25);
            Boulder(root,new Vector3(side*2.96f,.05f,.32f),new Vector3(1.64f,.44f,1.47f),side*22,snow);
        }
        const int segments = 8;
        Vector3 Cable(int i)
        {
            float t=(float)i/segments;
            return new Vector3(Mathf.Lerp(-1.06f,1.06f,t),3.13f-.46f*Mathf.Sin(Mathf.PI*t),-.23f);
        }
        for (int i=0;i<segments;i++)
        {
            Beam(root,Cable(i),Cable(i+1),Vector2.one*.039f,bark);
            if (i==0) continue;
            var point = Cable(i) - Vector3.up*.11f;
            Shape("Warm_Lamp",root,PrimitiveType.Cube,point,Vector3.one*.13f,lamp).localEulerAngles = new Vector3(0,0,45);
        }
        Bake(root);
    });

    static GameObject CrystalField() => Prop("Bank_07_Ice_Crystal_Field", root =>
    {
        foreach (float side in new[] {-1f,1f})
        {
            for (int i=0;i<5;i++)
            {
                float x = side * (2.16f + .35f * (i % 3));
                float z = -2.8f + i * 1.31f + side * .31f;
                float height = (i == 2 ? 3.62f : i == 0 ? 1.15f : 2.18f) * (side > 0 ? .85f : 1f);
                Crystal(root,new Vector3(x,-.055f,z),new Vector3(.82f+i*.08f,height,.74f+i*.07f),
                    new Vector3(-9+i*4,27+i*34,-side*(7+i*2)),i%2==0?ice:iceDeep);
                Boulder(root,new Vector3(x+side*.24f,.12f,z+.16f),new Vector3(1.51f,.54f,1.26f),21+i*30,snow);
            }
            Boulder(root,new Vector3(side*3.87f,.26f,1.39f),new Vector3(1.32f,.71f,1.06f),41,stone);
        }
        Bake(root);
    });

    public static string Build()
    {
        if (AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("Route assets already exist");
        AssetDatabase.CreateFolder("Assets/Events/NewYear", "Route181");
        audit.Clear();
        snow = Mat("Snow");
        shadedSnow = Mat("Route179/Snow_Drift_Blue");
        bark = Mat("Route179/Log_Bark");
        barkLight = Mat("Route179/Log_Bark_Ridges");
        wood = Mat("Route179/Log_Exposed_Wood");
        red = Mat("Ruby");
        pine = Mat("Fir");
        blue = Mat("WinterTown170/Gift_Blue");
        gold = Mat("Gold");
        lamp = Mat("WinterTown169/Warm_Light");
        stone = NewColor("Grotto_Stone", new Color(.41f,.44f,.47f));
        stoneDark = NewColor("Grotto_Stone_Dark", new Color(.29f,.32f,.36f));
        ice = NewColor("Satin_Ice_Cyan", new Color(.21f,.72f,.82f));
        iceDeep = NewColor("Satin_Ice_Blue", new Color(.13f,.50f,.69f));
        rock = RockMesh();
        shard = CrystalMesh();
        runner = RunnerMesh();
        var props = new[] { SnowTunnel(), RootedLog(), FrozenGrotto(), GiftSled(), FirArch(), CrystalField() };
        var collection = AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>("Assets/Events/NewYear/NY_Decor.asset");
        var previousLog = collection.spawnItems.SingleOrDefault(entry => entry.prefab && entry.prefab.name == "Bank_Walkthrough_Log");
        if (previousLog == null || collection.spawnItems.Count != 6)
            throw new InvalidOperationException("Unexpected previous route collection");
        collection.spawnItems.Remove(previousLog);
        var weights = new[] { 6f, 7f, 6f, 9f, 8f, 8f };
        for (int i=0; i<props.Length; i++)
            collection.spawnItems.Add(new DecorationSpawnItem { prefab=props[i], chance=weights[i] });
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssetIfDirty(collection);
        audit.Add("NY_Decor: six selected designs plus five existing winter variants.");
        File.WriteAllLines(Report,audit);
        return string.Join(" | ",audit);
    }

    public static string RefineArchSilhouettes()
    {
        audit.Clear();
        snow = Mat("Snow");
        shadedSnow = Mat("Route179/Snow_Drift_Blue");
        stone = Mat("Route181/Grotto_Stone");
        stoneDark = Mat("Route181/Grotto_Stone_Dark");
        ice = Mat("Route181/Satin_Ice_Cyan");
        iceDeep = Mat("Route181/Satin_Ice_Blue");
        rock = AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Faceted_Boulder.asset");
        shard = AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Solid_Ice_Crystal.asset");
        SnowTunnel();
        FrozenGrotto();
        var lines = File.ReadAllLines(Report).Where(line => !line.StartsWith("Bank_01") && !line.StartsWith("Bank_03"));
        File.WriteAllLines(Report,audit.Concat(lines));
        return string.Join(" | ",audit);
    }
}


