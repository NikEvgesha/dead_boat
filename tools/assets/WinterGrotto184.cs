using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterGrotto184
{
    const string Event = "Assets/Events/NewYear/";
    const string Folder = Event + "Route184";
    const string Prefab = Event + "Route181/Bank_03_Frozen_Grotto.prefab";

    static readonly Vector2[] Outer =
    {
        new(-3.90f,-.17f), new(-3.58f,1.00f), new(-3.03f,2.82f),
        new(-1.86f,3.57f), new(-.43f,3.82f), new(.91f,4.38f),
        new(2.05f,5.10f), new(3.25f,4.32f), new(4.42f,-.17f)
    };

    static readonly Vector2[] Inner =
    {
        new(-1.90f,-.17f), new(-1.96f,.67f), new(-1.78f,1.62f),
        new(-1.27f,2.34f), new(-.86f,2.94f), new(.03f,2.77f),
        new(.81f,2.28f), new(1.15f,1.35f), new(1.27f,-.17f)
    };

    static readonly float[] Depth = { -3.38f, -1.57f, .81f, 2.61f, 3.92f };
    static readonly float[] Shoulder = { .91f, 1.02f, 1.07f, .88f, .79f };
    static readonly float[] Ridge = { .97f, 1.02f, .91f, .88f, .80f };
    static readonly float[] Skew = { -.11f, .15f, .34f, .11f, -.02f };
    static readonly float[] FaceEdge = { -.14f, -.28f, -.10f, -.27f, -.11f, -.42f, .04f, -.21f, .24f };

    sealed class Facets
    {
        readonly List<Vector3> points = new();
        readonly List<int>[] indices;

        public Facets(int materials) => indices = Enumerable.Range(0, materials).Select(_ => new List<int>()).ToArray();

        public void Face(int material, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-9f) return;
            if (Vector3.Dot(normal, outward) < 0f) (b, c) = (c, b);
            indices[material].Add(points.Count);
            indices[material].Add(points.Count + 1);
            indices[material].Add(points.Count + 2);
            points.AddRange(new[] { a, b, c });
        }

        public Mesh Create(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(points);
            mesh.subMeshCount = indices.Length;
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i].Count == 0) throw new InvalidOperationException("Empty grotto material " + i);
                mesh.SetTriangles(indices[i], i);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    static Vector3 RockPoint(int row, int i)
    {
        var p = Outer[i];
        float strata = Mathf.Sin(i * 1.7f + row * 1.38f);
        float y = p.y < 0f ? p.y : p.y * Ridge[row] + strata * .22f;
        float face = row == 0 ? .50f * (p.x / 4f) + FaceEdge[i] : strata * .16f;
        return new Vector3(p.x * Shoulder[row] + Skew[row], y, Depth[row] + face);
    }

    static Vector3 CavePoint(int row, int i)
    {
        var p = Inner[i];
        float x = p.x + Skew[row] * .62f + .09f * Mathf.Sin(i * 1.8f + row);
        float y = p.y < 0f ? p.y : p.y + .08f * Mathf.Cos(row * 1.32f + i);
        float face = row == 0 ? .53f * (p.x / 3f) + .24f + FaceEdge[i] * .46f : .16f * Mathf.Sin(i + row);
        return new Vector3(x, y, Depth[row] + face);
    }

    static float SnowLevel(Vector3 p) => p.y - (3.37f + .21f * Mathf.Sin(p.x * 1.49f + p.z * .83f));

    static void SnowFace(Facets mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        var corners = new[] { a, b, c };
        for (int material = 0; material < 2; material++)
        {
            var clipped = new List<Vector3>(4);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 p = corners[i], q = corners[(i + 1) % corners.Length];
                float d = SnowLevel(p), e = SnowLevel(q);
                bool keep = material == 1 ? d >= 0f : d <= 0f;
                if (keep) clipped.Add(p);
                if (d * e < 0f) clipped.Add(Vector3.LerpUnclamped(p, q, d / (d - e)));
            }
            for (int i = 1; i + 1 < clipped.Count; i++)
                mesh.Face(material, clipped[0], clipped[i], clipped[i + 1], outward);
        }
    }

    static Mesh Rock()
    {
        var mesh = new Facets(3);
        int top = Outer.Length - 1;
        int far = Depth.Length - 1;
        for (int row = 0; row < far; row++)
        {
            for (int i = 0; i < top; i++)
            {
                var a = RockPoint(row, i);
                var b = RockPoint(row, i + 1);
                var c = RockPoint(row + 1, i + 1);
                var d = RockPoint(row + 1, i);
                var outward = new Vector3((a.x + b.x) * .5f, Mathf.Min(a.y,b.y) + .35f, 0f);
                if ((row + i) % 2 == 0)
                {
                    SnowFace(mesh, a, b, c, outward);
                    SnowFace(mesh, a, c, d, outward);
                }
                else
                {
                    SnowFace(mesh, a, b, d, outward);
                    SnowFace(mesh, b, c, d, outward);
                }

                a = CavePoint(row, i);
                b = CavePoint(row, i + 1);
                c = CavePoint(row + 1, i + 1);
                d = CavePoint(row + 1, i);
                var inward = new Vector3(-.34f - (a.x + b.x) * .5f, -.52f * (a.y + b.y) - .15f, 0f);
                mesh.Face(2, a, b, c, inward);
                mesh.Face(2, a, c, d, inward);
            }
            foreach (int edge in new[] { 0, top })
            {
                mesh.Face(0, RockPoint(row, edge), CavePoint(row, edge), CavePoint(row + 1, edge), Vector3.down);
                mesh.Face(0, RockPoint(row, edge), CavePoint(row + 1, edge), RockPoint(row + 1, edge), Vector3.down);
            }
        }
        foreach (int row in new[] { 0, far })
            for (int i = 0; i < top; i++)
            {
                var outward = row == 0 ? Vector3.back : Vector3.forward;
                var a = RockPoint(row, i);
                var b = RockPoint(row, i + 1);
                var c = CavePoint(row, i + 1);
                var d = CavePoint(row, i);
                var shoulder = Vector3.Lerp((a + b) * .5f, (c + d) * .5f, .42f) + outward * .26f;
                SnowFace(mesh, a, b, shoulder, outward);
                SnowFace(mesh, b, c, shoulder, outward);
                SnowFace(mesh, c, d, shoulder, outward);
                SnowFace(mesh, d, a, shoulder, outward);
            }
        return mesh.Create("Cloven_Winter_Rock");
    }

    static Mesh Waterfall()
    {
        var ice = new Facets(2);
        var tiers = new[]
        {
            new Vector4(2.13f,4.40f,-3.17f,.91f),
            new Vector4(2.30f,3.91f,-3.19f,.92f),
            new Vector4(2.52f,2.75f,-3.21f,.97f),
            new Vector4(2.83f,1.17f,-3.37f,1.15f),
            new Vector4(2.98f,-.06f,-3.66f,1.47f)
        };
        Vector3 Lip(int row, int ridge)
        {
            var tier = tiers[row];
            float t = (ridge - 2) / 4f;
            float relief = ridge % 2 == 0 ? .03f : .22f;
            float seam = Mathf.Sin(row * 1.18f + ridge * 1.66f);
            return new Vector3(tier.x + t * tier.w + .055f * seam,
                tier.y + (row == tiers.Length - 1 ? 0f : .10f * seam),
                tier.z - relief - (1f - Mathf.Abs(t) * 2f) * .16f);
        }
        for (int row = 0; row < tiers.Length - 1; row++)
            for (int i = 0; i < 4; i++)
            {
                var a = Lip(row, i);
                var b = Lip(row + 1, i);
                var c = Lip(row + 1, i + 1);
                var d = Lip(row, i + 1);
                ice.Face(i % 2, a, b, c, Vector3.back);
                ice.Face(i % 2, a, c, d, Vector3.back);
            }
        foreach (int edge in new[] { 0, 4 })
            for (int row = 0; row < tiers.Length - 1; row++)
            {
                var a = Lip(row, edge);
                var b = Lip(row + 1, edge);
                var back = Vector3.forward * .78f;
                var outward = edge == 0 ? Vector3.left : Vector3.right;
                ice.Face(1, a, b, b + back, outward);
                ice.Face(1, a, b + back, a + back, outward);
            }
        for (int i = 0; i < 4; i++)
        {
            var a = Lip(0, i);
            var b = Lip(0, i + 1);
            var back = Vector3.forward * .78f;
            ice.Face(0, a, b, b + back, Vector3.up);
            ice.Face(0, a, b + back, a + back, Vector3.up);
        }

        var pond = new[]
        {
            new Vector3(2.18f,.01f,-3.14f), new Vector3(1.99f,.01f,-3.93f),
            new Vector3(2.43f,.016f,-4.60f), new Vector3(3.19f,.01f,-4.61f),
            new Vector3(3.92f,.01f,-4.26f), new Vector3(4.04f,.01f,-3.74f),
            new Vector3(3.65f,.01f,-3.02f)
        };
        for (int i = 0; i < pond.Length; i++)
            ice.Face(i % 2, pond[i], pond[(i + 1) % pond.Length], new Vector3(2.99f,.075f,-3.79f), Vector3.up);
        return ice.Create("Frozen_Fall_And_Basin");
    }

    static Mesh Save(Mesh source)
    {
        string path = Folder + "/" + source.name + ".asset";
        var target = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!target)
        {
            AssetDatabase.CreateAsset(source, path);
            return source;
        }
        target.Clear();
        target.vertices = source.vertices;
        target.normals = source.normals;
        target.subMeshCount = source.subMeshCount;
        for (int i = 0; i < source.subMeshCount; i++) target.SetTriangles(source.GetTriangles(i), i);
        target.RecalculateBounds();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssetIfDirty(target);
        Object.DestroyImmediate(source);
        return target;
    }

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var rock = Rock();
        var waterfall = Waterfall();
        var bounds = rock.bounds;
        bounds.Encapsulate(waterfall.bounds);
        float radius = rock.vertices.Concat(waterfall.vertices).Max(v => new Vector2(v.x, v.z).magnitude);
        if (radius > 6f || bounds.max.x > 5.1f || bounds.min.x < -5.1f ||
            bounds.max.z > 5f || bounds.min.z < -5f || bounds.min.y < -.60f)
            throw new InvalidOperationException("Grotto exceeds bank footprint " + bounds + ", radius " + radius);
        var materials = new[]
        {
            "Assets/Materials/Stone/Stone.mat", Event + "Snow.mat", Event + "Route181/Grotto_Stone_Dark.mat",
            Event + "Route181/Satin_Ice_Cyan.mat", Event + "Route181/Satin_Ice_Blue.mat"
        }.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
        if (materials.Any(m => !m)) throw new InvalidOperationException("Missing winter grotto material");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Event.TrimEnd('/'), "Route184");
        var prefab = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var shell = prefab.transform.Find("Walkthrough_Shell").gameObject;
            var ice = prefab.transform.Find("Frozen_Waterfall")?.gameObject;
            if (!ice)
            {
                ice = new GameObject("Frozen_Waterfall");
                ice.transform.SetParent(prefab.transform, false);
                ice.AddComponent<MeshFilter>();
                ice.AddComponent<MeshRenderer>();
            }
            shell.GetComponent<MeshFilter>().sharedMesh = Save(rock);
            var collision = shell.GetComponent<MeshCollider>();
            collision.sharedMesh = null;
            collision.sharedMesh = shell.GetComponent<MeshFilter>().sharedMesh;
            shell.GetComponent<MeshRenderer>().sharedMaterials = materials.Take(3).ToArray();
            shell.GetComponent<MeshRenderer>().receiveShadows = false;
            ice.GetComponent<MeshFilter>().sharedMesh = Save(waterfall);
            ice.GetComponent<MeshRenderer>().sharedMaterials = materials.Skip(3).ToArray();
            var accents = prefab.transform.Find("Rock_Snow_And_Frozen_Waterfall");
            if (accents) accents.gameObject.SetActive(false);
            var trigger = prefab.GetComponent<BoxCollider>();
            trigger.center = bounds.center;
            trigger.size = bounds.size;
            if (!PrefabUtility.SaveAsPrefabAsset(prefab, Prefab)) throw new InvalidOperationException("Grotto save failed");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        string report = $"Grotto 03: continuous faceted rock, snowline and open cleft; frozen cascade attached to the front flank. Bounds {bounds.min}..{bounds.max}; radius {radius:F2} m.";
        File.WriteAllText("Docs/locations/previews/event-winter184-assets.txt", report + Environment.NewLine);
        return report;
    }
}
