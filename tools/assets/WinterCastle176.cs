using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WinterCastle176
{
    const string Destination = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string D = "Assets/Events/NewYear/Castle176";
    const string Decor = "Assets/Events/NewYear/NY_Decor.asset";
    static Transform root;
    static Material snow, red, gold, blue, cable, warm;
    static Mesh bulb;
    static readonly List<Vector3[]> ground = new List<Vector3[]>();
    static readonly List<string> audit = new List<string>();
    static Bounds interior;
 static Transform Node(string n,Transform p,Vector3 pos){var g=new GameObject(n);g.transform.SetParent(p,false);g.transform.localPosition=pos;return g.transform;}
 static Transform Shape(string n,Transform p,PrimitiveType type,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
 static Bounds LocalBounds(Transform o){bool first=true;var b=new Bounds();foreach(var mf in o.GetComponentsInChildren<MeshFilter>()){var rr=mf.GetComponent<Renderer>();if(!rr||!rr.enabled||!rr.gameObject.activeInHierarchy)continue;foreach(var v in mf.sharedMesh.vertices){var p=root.InverseTransformPoint(mf.transform.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
 static string Fingerprint(GameObject o){return string.Join("\n",o.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c).Select(c=>c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)))+"\nCOLLIDERS="+string.Join(";",o.GetComponentsInChildren<Collider>(true).Select(c=>EditorJsonUtility.ToJson(c)));}
 static void Batch(Transform category){var filters=category.GetComponentsInChildren<MeshFilter>().Where(m=>m.GetComponent<MeshRenderer>()!=null&&m.gameObject.activeInHierarchy).ToArray();var batches=new Dictionary<Material,List<CombineInstance>>();foreach(var mf in filters){var r=mf.GetComponent<MeshRenderer>();for(int i=0;i<mf.sharedMesh.subMeshCount;i++){var mat=r.sharedMaterials[Math.Min(i,r.sharedMaterials.Length-1)];if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();batches[mat].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=i,transform=category.worldToLocalMatrix*mf.transform.localToWorldMatrix});}}var children=category.Cast<Transform>().ToArray();foreach(var kv in batches){var name=category.name+"_"+kv.Key.name;var path=D+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m){m.Clear();}else{m=new Mesh{name=name};AssetDatabase.CreateAsset(m,path);}m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.CombineMeshes(kv.Value.ToArray(),true,true);EditorUtility.SetDirty(m);var node=Node(m.name,category,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=m;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=kv.Key;}foreach(var t in children)UnityEngine.Object.DestroyImmediate(t.gameObject);}
 static float Ground(float x,float z){float best=float.NegativeInfinity;foreach(var t in ground){var a=t[0];var b=t[1];var c=t[2];float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<.00001f)continue;float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den;float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)best=Mathf.Max(best,u*a.y+v*b.y+(1-u-v)*c.y);}if(float.IsNegativeInfinity(best))throw new Exception("No ground at "+x+","+z);return best;}
    static MeshFilter[] Meshes(Transform group) => group.GetComponentsInChildren<MeshFilter>()
        .Where(m => m.sharedMesh && m.TryGetComponent<MeshRenderer>(out var r) && r.enabled).ToArray();

    static IEnumerable<Vector3[]> Triangles(MeshFilter filter)
    {
        var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        var vertices = filter.sharedMesh.vertices;
        var indices = filter.sharedMesh.triangles;
        for (int i=0;i<indices.Length;i+=3)
            yield return new[]{matrix.MultiplyPoint3x4(vertices[indices[i]]),
                matrix.MultiplyPoint3x4(vertices[indices[i+1]]),matrix.MultiplyPoint3x4(vertices[indices[i+2]])};
    }

    static bool RayTriangle(Vector3 origin, Vector3 direction, Vector3[] triangle, out float distance)
    {
        var edgeA = triangle[1]-triangle[0];
        var edgeB = triangle[2]-triangle[0];
        var p = Vector3.Cross(direction,edgeB);
        float det = Vector3.Dot(edgeA,p);
        distance=0;
        if (Mathf.Abs(det)<.00001f) return false;
        var t = origin-triangle[0];
        float u = Vector3.Dot(t,p)/det;
        if (u<0 || u>1) return false;
        var q = Vector3.Cross(t,edgeA);
        float v = Vector3.Dot(direction,q)/det;
        if (v<0 || u+v>1) return false;
        distance=Vector3.Dot(edgeB,q)/det;
        return distance>=0;
    }

    static void Cord(Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
    {
        var delta=b-a;
        if (delta.sqrMagnitude<.000001f) return;
        var cord=Shape("Cord",parent,PrimitiveType.Cube,(a+b)*.5f,
            new Vector3(thickness,thickness,delta.magnitude),mat);
        cord.localRotation=Quaternion.LookRotation(delta);
    }

    static Vector3? Contact(Vector3 center, float angle, Vector3[][] foliage)
    {
        var radial = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        var origin = center + radial * 3f;
        float nearest = float.PositiveInfinity;
        foreach (var triangle in foliage)
            if (RayTriangle(origin, -radial, triangle, out float distance))
                nearest = Mathf.Min(nearest, distance);
        return float.IsPositiveInfinity(nearest) ? (Vector3?)null : origin - radial * nearest;
    }

    static void Gem(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var gem = Node(name, parent, position);
        gem.localScale = size;
        gem.gameObject.AddComponent<MeshFilter>().sharedMesh = bulb;
        gem.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    static int DecorateFir(Transform trees, Vector4 placement)
    {
        var position = new Vector3(placement.x, Ground(placement.x, placement.y), placement.y);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/NY_FestiveFir.prefab");
        var fir = (GameObject)PrefabUtility.InstantiatePrefab(source, trees);
        fir.transform.Find("NewYear_Decoration").gameObject.SetActive(false);
        fir.transform.localPosition = Vector3.zero;
        fir.transform.localRotation = Quaternion.Euler(0, placement.w, 0) * fir.transform.localRotation;
        fir.transform.localScale *= placement.z / LocalBounds(fir.transform).size.y;
        var bounds = LocalBounds(fir.transform);
        fir.transform.localPosition += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in fir.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        bounds = LocalBounds(fir.transform);
        const float decorationMargin = .18f;
        if (bounds.min.x < interior.min.x + decorationMargin || bounds.max.x > interior.max.x - decorationMargin ||
            bounds.min.z < interior.min.z + decorationMargin || bounds.max.z > interior.max.z - decorationMargin)
            throw new InvalidOperationException("Fir beyond the wall clearance at " + position);

        var foliage = Meshes(fir.transform).SelectMany(Triangles).ToArray();
        int ornaments = 0;
        for (int tier = 0; tier < 4; tier++)
            for (int turn = 0; turn < 4; turn++)
            {
                float angle = (turn * 90 + tier * 37 + placement.w) * Mathf.Deg2Rad;
                var contact = Contact(position + Vector3.up * placement.z * (.33f + tier * .13f), angle, foliage);
                if (!contact.HasValue) continue;
                var radial = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                var center = contact.Value + radial * .065f - Vector3.up * .07f;
                Cord(trees, contact.Value, center, .018f, gold);
                var color = (tier + turn) % 3 == 0 ? blue : (tier + turn) % 2 == 0 ? gold : red;
                if (turn % 2 == 0)
                    Shape("Round_Bauble", trees, PrimitiveType.Sphere, center, Vector3.one * .17f, color);
                else
                    Gem(trees, "Faceted_Bauble", center, new Vector3(.10f, .15f, .10f), color);
                ornaments++;
                if (turn == tier)
                    Shape("Branch_Snow", trees, PrimitiveType.Sphere, contact.Value - radial * .025f + Vector3.up * .025f,
                        new Vector3(.29f, .10f, .24f), snow);
            }

        int lights = 0;
        for (int tier = 0; tier < 2; tier++)
        {
            const int samples = 24;
            var contacts = new Vector3?[samples];
            for (int i = 0; i < samples; i++)
            {
                float angle = i * 2f * Mathf.PI / samples + placement.w * Mathf.Deg2Rad;
                float wave = Mathf.Sin(angle * 3f) * .025f;
                var center = position + Vector3.up * placement.z * (.43f + .22f * tier + wave);
                var hit = Contact(center, angle, foliage);
                if (!hit.HasValue) continue;
                var radial = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                contacts[i] = hit.Value + radial * .035f;
                Cord(trees, hit.Value, contacts[i].Value, .022f, cable);
                if (i % 2 == 0)
                {
                    Gem(trees, "Warm_Garland_Lamp", contacts[i].Value + radial * .022f,
                        new Vector3(.048f, .063f, .048f), warm);
                    lights++;
                }
            }
            for (int i = 0; i < samples; i++)
            {
                var a = contacts[i];
                var b = contacts[(i + 1) % samples];
                if (a.HasValue && b.HasValue) Cord(trees, a.Value, b.Value, .023f, cable);
            }
        }

        Gem(trees, "Gilded_Topper", position + Vector3.up * (placement.z + .035f),
            new Vector3(.11f, .18f, .11f), gold);
        if (ornaments != 16 || lights < 20)
            throw new InvalidOperationException($"Incomplete fir at {position}: {ornaments} ornaments, {lights} lights");
        audit.Add($"Fir {position}, bounds={bounds.min}..{bounds.max}, ornaments={ornaments}, garland lights={lights}");
        return lights;
    }

    static Mesh FacetedBulb()
    {
        var points = new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
        var vertices = new List<Vector3>();
        for (int i = 0; i < 4; i++)
        {
            var a = points[2 + i];
            var b = points[2 + (i + 1) % 4];
            vertices.AddRange(new[] { points[0], b, a, points[1], a, b });
        }
        var mesh = new Mesh { name = "Faceted_Lamp" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(Enumerable.Range(0, vertices.Count).ToArray(), 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    static void Mound(Transform drifts, float x, float z, float width, float length, float height, int phase)
    {
        const int sides = 16;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        float y = Ground(x, z);
        for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < sides; i++)
            {
                float angle = 2f * Mathf.PI * i / sides;
                float radius = (1f - ring * .29f) * (1f + .12f * Mathf.Sin(angle * 3f + phase));
                float dx = Mathf.Cos(angle) * width * .5f * radius + width * .025f * ring;
                float dz = Mathf.Sin(angle) * length * .5f * radius - length * .035f * ring;
                float profile = ring == 0 ? -.035f : height * (ring == 1 ? .51f : .87f) *
                    (1f + .10f * Mathf.Cos(angle * 2f + phase));
                vertices.Add(new Vector3(x + dx, Ground(x + dx, z + dz) + profile, z + dz));
                if (ring < 2)
                {
                    int a = ring * sides + i;
                    int b = ring * sides + (i + 1) % sides;
                    indices.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
                }
            }
        int apex = vertices.Count;
        vertices.Add(new Vector3(x + width * .08f, y + height, z - length * .10f));
        for (int i = 0; i < sides; i++) indices.AddRange(new[] { 2 * sides + i, apex, 2 * sides + (i + 1) % sides });
        var mesh = new Mesh { name = "Settled_Snow" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        var mound = Node(mesh.name, drifts, Vector3.zero);
        mound.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        mound.gameObject.AddComponent<MeshRenderer>().sharedMaterial = snow;
        audit.Add($"Snow x={x}, z={z}, height={height}, rim buried=.035");
    }

    static void DuplicateSnowman(Transform winter)
    {
        var original = root.Find("Winter_174/Snowman");
        var sourceAnchor = new Vector3(-6.6f, Ground(-6.6f, 18.4f), 18.4f);
        const float x = 6.6f, z = -17.7f;
        var pivot = Node("Snowman_East_Bank", winter, new Vector3(x, Ground(x, z), z));
        pivot.localRotation = Quaternion.Euler(0, -155f, 0);
        var model = UnityEngine.Object.Instantiate(original.gameObject, pivot);
        model.name = "Matching_Snowman";
        model.transform.localPosition = -sourceAnchor;
        if (!model.GetComponentInChildren<MeshRenderer>())
            throw new InvalidOperationException("Matching snowman has no renderers");
        audit.Add("Snowmen: opposite banks, west " + sourceAnchor + ", east " + pivot.localPosition);
    }

    static Bounds CourtyardInterior()
    {
        var walls = root.Find("01_Original_Castle_Perimeter/Chateau").Cast<Transform>()
            .Where(t => t.name.StartsWith("Wall2", StringComparison.Ordinal)).Select(LocalBounds)
            .Where(b => b.size.y > 1f).ToArray();
        var left = walls.Where(b => b.center.x < -16 && b.size.z > b.size.x).ToArray();
        var right = walls.Where(b => b.center.x > 16 && b.size.z > b.size.x).ToArray();
        var near = walls.Where(b => b.center.z < -16 && b.size.x > b.size.z).ToArray();
        var far = walls.Where(b => b.center.z > 16 && b.size.x > b.size.z).ToArray();
        if (left.Length == 0 || right.Length == 0 || near.Length == 0 || far.Length == 0)
            throw new InvalidOperationException("Could not measure all courtyard wall faces");
        var box = new Bounds();
        box.SetMinMax(new Vector3(left.Max(b => b.max.x), 0, near.Max(b => b.max.z)),
            new Vector3(right.Min(b => b.min.x), 30, far.Min(b => b.min.z)));
        audit.Add("Inside wall faces: " + box.min + ".." + box.max);
        return box;
    }

    static void RemoveRoadsidePair()
    {
        var collection = AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>(Decor);
        var treeWithGift = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/NY_FestiveFir.prefab");
        int removed = collection.spawnItems.RemoveAll(entry => entry.prefab == treeWithGift);
        if (removed != 0) EditorUtility.SetDirty(collection);
        audit.Add($"Roadside fir-and-gift entries removed={removed}, remaining decor entries={collection.spawnItems.Count}");
    }

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(D);
        AssetDatabase.Refresh();
        ground.Clear();
        audit.Clear();
        snow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        red = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Ruby.mat");
        gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Gold.mat");
        blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown170/Gift_Blue.mat");
        cable = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Fir.mat");
        warm = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Warm_Light.mat");
        bulb = FacetedBulb();
        var castle = PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            root = castle.transform;
            if (root.Find("Winter_176")) throw new InvalidOperationException("Already applied");
            var original = Fingerprint(castle);
            interior = CourtyardInterior();
            var court = root.Find("02_Courtyard");
            foreach (var triangle in Meshes(court).SelectMany(Triangles))
                if (triangle.All(p => p.y < .9f && p.y > -.6f) &&
                    Mathf.Abs(Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]).y) > .0001f)
                    ground.Add(triangle);

            var previous = root.Find("Winter_175");
            var winter = Node("Winter_176", root, Vector3.zero);
            var trees = Node("Festive_Firs", winter, Vector3.zero);
            var drifts = Node("Courtyard_Snow", winter, Vector3.zero);
            var placements = new[]
            {
                new Vector4(-18.8f,-19.0f,3.7f,12), new Vector4(-7.6f,-5.2f,3.2f,69),
                new Vector4(-17.8f,9.0f,4.1f,110), new Vector4(7.3f,-6.9f,3.15f,44),
                new Vector4(17.5f,5.2f,3.55f,133), new Vector4(14.5f,19.2f,3.0f,172)
            };
            int lights = placements.Sum(t => DecorateFir(trees, t));
            var piles = new[]
            {
                new Vector4(-18.8f,-19,2.1f,.52f), new Vector4(-7.6f,-5.2f,1.9f,.41f),
                new Vector4(-17.8f,9,2.3f,.57f), new Vector4(7.3f,-6.9f,2.0f,.46f),
                new Vector4(17.5f,5.2f,2.2f,.53f), new Vector4(14.5f,19.2f,1.7f,.42f),
                new Vector4(-20,-9,2.6f,.69f), new Vector4(-18.5f,1,2.0f,.54f),
                new Vector4(-18,13,2.1f,.48f), new Vector4(-14,20,2.5f,.60f),
                new Vector4(18,-19,2.4f,.63f), new Vector4(19,-7.5f,2.5f,.68f),
                new Vector4(19,12,1.9f,.52f), new Vector4(6.8f,-18.1f,1.6f,.32f)
            };
            for (int i = 0; i < piles.Length; i++)
            {
                var p = piles[i];
                float side = i % 2 == 0 ? 1f : -1f;
                Mound(drifts, p.x, p.y, p.z, p.z * (.70f + .08f * (i % 4)), p.w, i);
                Mound(drifts, p.x + side * p.z * .52f, p.y - p.z * .27f, p.z * .52f, p.z * .42f, p.w * .52f, i + 4);
                if (i % 2 == 0)
                    Mound(drifts, p.x - side * p.z * .38f, p.y + p.z * .48f, .60f, .72f, .16f, i + 9);
            }

            DuplicateSnowman(winter);
            Batch(trees);
            var rawMounds = Meshes(drifts).Select(f => f.sharedMesh).ToArray();
            Batch(drifts);
            foreach (var mesh in rawMounds) UnityEngine.Object.DestroyImmediate(mesh);
            previous.gameObject.SetActive(false);
            if (Fingerprint(castle) != original) throw new InvalidOperationException("Gameplay components changed");
            if (!PrefabUtility.SaveAsPrefabAsset(castle, Destination)) throw new InvalidOperationException("Prefab save failed");
            RemoveRoadsidePair();
            AssetDatabase.SaveAssets();
            audit.Add($"Firs={placements.Length}, ornaments={placements.Length * 16}, warm lamps={lights}, snow piles={piles.Length}, snowmen=2");
            File.WriteAllLines("Docs/locations/previews/event-castle176-verification.txt", audit);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(castle);
            UnityEngine.Object.DestroyImmediate(bulb);
        }
    }

    public static void Main()
    {
        Build();
        CastleView176.Main();
    }
}
