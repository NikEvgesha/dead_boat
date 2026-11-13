using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WinterCastle177
{
    const string Castle = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string MeshFolder = "Assets/Events/NewYear/Castle176";
    static Transform root;
    static Material snow, red, gold, blue, cable, warm;
    static Mesh bulb;
    static readonly List<Vector3[]> ground = new List<Vector3[]>();
    static readonly List<string> audit = new List<string>();
    static Bounds interior;
 static Transform Node(string n,Transform p,Vector3 pos){var g=new GameObject(n);g.transform.SetParent(p,false);g.transform.localPosition=pos;return g.transform;}
 static Transform Shape(string n,Transform p,PrimitiveType type,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
 static Bounds LocalBounds(Transform o){bool first=true;var b=new Bounds();foreach(var mf in o.GetComponentsInChildren<MeshFilter>()){var rr=mf.GetComponent<Renderer>();if(!rr||!rr.enabled||!rr.gameObject.activeInHierarchy)continue;foreach(var v in mf.sharedMesh.vertices){var p=root.InverseTransformPoint(mf.transform.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
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


    static int RebuildFir(Transform trees, Vector4 placement)
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

        return Decorate(trees, fir.transform, position, placement.z, placement.w, false);
    }

    static int Decorate(Transform trees, Transform fir, Vector3 position, float height, float yaw, bool extraSnow)
    {
        var bounds = LocalBounds(fir);
        const float decorationMargin = .18f;
        if (bounds.min.x < interior.min.x + decorationMargin || bounds.max.x > interior.max.x - decorationMargin ||
            bounds.min.z < interior.min.z + decorationMargin || bounds.max.z > interior.max.z - decorationMargin)
            throw new InvalidOperationException("Fir beyond the wall clearance at " + position);

        var foliage = Meshes(fir).SelectMany(Triangles).ToArray();
        int ornaments = 0;
        for (int tier = 0; tier < 4; tier++)
        {
            for (int turn = 0; turn < 4; turn++)
            {
                float angle = (turn * 90 + tier * 37 + yaw) * Mathf.Deg2Rad;
                var contact = Contact(position + Vector3.up * height * (.33f + tier * .13f), angle, foliage);
                if (!contact.HasValue) continue;
                var radial = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                if (turn % 2 == 0)
                {
                    var center = contact.Value + radial * .065f - Vector3.up * .07f;
                    Cord(trees, contact.Value, center, .018f, gold);
                    var color = (tier + turn) % 3 == 0 ? blue : (tier + turn) % 2 == 0 ? gold : red;
                    Shape("Round_Bauble", trees, PrimitiveType.Sphere, center, Vector3.one * .17f, color);
                    ornaments++;
                }
                if (turn == tier || (extraSnow && (tier == 1 || tier == 2) && turn == 3))
                {
                    var center = contact.Value - radial * .055f + Vector3.up * .019f;
                    var size = extraSnow ? new Vector3(.21f, .075f, .17f) : new Vector3(.29f, .10f, .24f);
                    Shape("Branch_Snow", trees, PrimitiveType.Sphere, center, size, snow);
                }
            }
        }
        int lights = 0;
        for (int tier = 0; tier < 2; tier++)
        {
            const int samples = 24;
            var contacts = new Vector3?[samples];
            for (int i = 0; i < samples; i++)
            {
                float angle = i * 2f * Mathf.PI / samples + yaw * Mathf.Deg2Rad;
                float wave = Mathf.Sin(angle * 3f) * .025f;
                var center = position + Vector3.up * height * (.43f + .22f * tier + wave);
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

        Gem(trees, "Gilded_Topper", position + Vector3.up * (height + .035f),
            new Vector3(.11f, .18f, .11f), gold);
        if (ornaments != 8 || lights < 20)
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


    // Keep the existing prefab children, local IDs, and mesh asset references.
    // Generate all replacements before touching any persistent mesh.
    static void SaveBatches(Transform scratch, Transform existing)
    {
        var byMaterial = new Dictionary<Material, List<CombineInstance>>();
        foreach (var filter in Meshes(scratch))
        {
            var materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
            {
                var material = materials[Math.Min(i, materials.Length - 1)];
                if (!byMaterial.TryGetValue(material, out var parts))
                    byMaterial.Add(material, parts = new List<CombineInstance>());
                parts.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    subMeshIndex = i,
                    transform = scratch.worldToLocalMatrix * filter.transform.localToWorldMatrix
                });
            }
        }

        var replacements = new Dictionary<Mesh, Mesh>();
        try
        {
            foreach (var batch in byMaterial)
            {
                string name = existing.name + "_" + batch.Key.name;
                var filter = existing.Find(name).GetComponent<MeshFilter>();
                var target = filter.sharedMesh;
                if (!target || AssetDatabase.GetAssetPath(target) != MeshFolder + "/" + name + ".asset")
                    throw new InvalidOperationException("Unexpected mesh target " + name);
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(batch.Value.ToArray(), true, true);
                replacements.Add(target, mesh);
            }
            if (replacements.Count != existing.childCount)
                throw new InvalidOperationException("Missing a material batch");
            foreach (var pair in replacements)
            {
                pair.Key.Clear();
                pair.Key.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                pair.Key.CombineMeshes(new[] { new CombineInstance { mesh = pair.Value } }, true, false);
                if (pair.Key.vertexCount != pair.Value.vertexCount)
                    throw new InvalidOperationException("Mesh vertex count differs after bake");
                EditorUtility.SetDirty(pair.Key);
            }
            foreach (var mesh in replacements.Keys) AssetDatabase.SaveAssetIfDirty(mesh);
        }
        finally
        {
            foreach (var mesh in replacements.Values) UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    public static string Build()
    {
        ground.Clear();
        audit.Clear();
        snow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        red = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Ruby.mat");
        gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Gold.mat");
        blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown170/Gift_Blue.mat");
        cable = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Fir.mat");
        warm = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Warm_Light.mat");
        bulb = FacetedBulb();
        var castle = PrefabUtility.LoadPrefabContents(Castle);
        try
        {
            root = castle.transform;
            interior = CourtyardInterior();
            foreach (var triangle in Meshes(root.Find("02_Courtyard")).SelectMany(Triangles))
                if (triangle.All(p => p.y < .9f && p.y > -.6f) &&
                    Mathf.Abs(Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]).y) > .0001f)
                    ground.Add(triangle);

            var existing = root.Find("Winter_176/Festive_Firs");
            var scratch = Node("Fir_Bake_Preview", root, Vector3.zero);
            var placements = new[]
            {
                new Vector4(-18.8f,-19.0f,3.7f,12), new Vector4(-7.6f,-5.2f,3.2f,69),
                new Vector4(-17.8f,9.0f,4.1f,110), new Vector4(7.3f,-6.9f,3.15f,44),
                new Vector4(17.5f,5.2f,3.55f,133), new Vector4(14.5f,19.2f,3.0f,172)
            };
            int lights = placements.Sum(t => RebuildFir(scratch, t));

            var loneFir = root.Find("08_Distributed_Greenery/Green_Cluster_3 (1)/Shade_Tree (1)");
            if (!loneFir || !loneFir.gameObject.activeInHierarchy)
                throw new InvalidOperationException("The courtyard's plain fir was not found");
            var loneBounds = LocalBounds(loneFir);
            var anchor = new Vector3(loneBounds.center.x, loneBounds.min.y, loneBounds.center.z);
            lights += Decorate(scratch, loneFir, anchor, loneBounds.size.y, 27f, true);
            audit.Add("Existing tree retained at " + anchor + ", added six small branch snow caps");
            SaveBatches(scratch, existing);
            string result = $"Firs={placements.Length + 1}, round ornaments={(placements.Length + 1) * 8}, warm lamps={lights}, large hanging diamonds=0";
            audit.Add(result);
            File.WriteAllLines("Docs/locations/previews/event-castle177-verification.txt", audit);
            return result;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(castle);
            UnityEngine.Object.DestroyImmediate(bulb);
        }
    }
}

