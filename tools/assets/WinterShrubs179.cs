using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterShrubs179
{
    const string Castle = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string Folder = "Assets/Events/NewYear/Route179/";
    sealed class Surface
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();

        public void Face(Vector3 a, Vector3 b, Vector3 c, bool twoSided = false)
        {
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            if (twoSided)
            {
                vertices.Add(a); vertices.Add(c); vertices.Add(b);
                triangles.Add(index + 3); triangles.Add(index + 4); triangles.Add(index + 5);
            }
        }

        public Mesh CreateMesh()
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    static void Leaf(Surface surface, Vector3 start, Vector3 tip, float halfWidth)
    {
        var direction = tip - start;
        var side = Vector3.Cross(direction, Vector3.up).normalized * halfWidth;
        var middle = Vector3.Lerp(start, tip, .51f);
        var left = middle + side;
        var right = middle - side;
        var fold = middle + Vector3.up * halfWidth * .41f;
        surface.Face(start, left, fold, true);
        surface.Face(left, tip, fold, true);
        surface.Face(tip, right, fold, true);
        surface.Face(right, start, fold, true);
    }

    static void Twig(Surface surface, Vector3 from, Vector3 to)
    {
        var axis = (to - from).normalized;
        var side = Vector3.Cross(axis, Vector3.right).normalized;
        var front = Vector3.Cross(side, axis);
        const int count = 5;
        Vector3 Radial(int i)
        {
            float angle = i * Mathf.PI * 2f / count;
            return side * Mathf.Cos(angle) + front * Mathf.Sin(angle);
        }
        for (int i = 0; i < count; i++)
        {
            var a = Radial(i);
            var b = Radial(i + 1);
            surface.Face(from + a * .013f, to + a * .005f, to + b * .005f, true);
            surface.Face(from + a * .013f, to + b * .005f, from + b * .013f, true);
        }
    }

    static void SnowPatch(Surface surface, Vector3 center, Vector3 along, float length)
    {
        var width = Vector3.Cross(along, Vector3.up);
        var peak = center + Vector3.up * .041f;
        const int sides = 8;
        Vector3 Rim(int i)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float uneven = 1f + .13f * Mathf.Sin(angle * 3f + center.z);
            return center + uneven * (along * Mathf.Cos(angle) * length + width * Mathf.Sin(angle) * length * .58f);
        }
        for (int i = 0; i < sides; i++) surface.Face(Rim(i), peak, Rim(i + 1), true);
    }

    static void Bed(Surface deep, Surface tips, Surface snow, Vector3 center, int index)
    {
        var random = new System.Random(1790 + index);
        float Range(float lo, float hi) => lo + (hi - lo) * (float)random.NextDouble();
        float rotation = index * 2.39996f;
        for (int shoot = 0; shoot < 11; shoot++)
        {
            float yaw = shoot * 2.39996f + rotation + Range(-.20f, .20f);
            var outward = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw));
            var sideways = new Vector3(-outward.z, 0, outward.x);
            var basePoint = center + outward * Range(.018f, .10f);
            float radius = Range(.25f, .40f);
            float height = Range(.30f, .49f);
            var end = basePoint + outward * radius + Vector3.up * height;
            Twig(deep, basePoint, end);
            for (int ring = 0; ring < 4; ring++)
            {
                float t = .28f + .19f * ring;
                var point = Vector3.Lerp(basePoint, end, t);
                float leafLength = Range(.105f, .176f) * (1.1f - .055f * ring);
                foreach (float sign in new[] { -1f, 1f })
                {
                    var direction = (outward * Range(.40f, .83f) + sideways * sign).normalized;
                    var tip = point + direction * leafLength + Vector3.up * Range(-.018f, .065f);
                    Leaf((shoot + ring) % 3 == 0 ? tips : deep, point, tip, Range(.034f, .051f));
                    if (ring == 3 && shoot % 3 == 0 && sign > 0)
                    {
                        var patch = Vector3.Lerp(point, tip, .54f) + Vector3.up * .022f;
                        SnowPatch(snow, patch, direction, leafLength * .46f);
                    }
                }
            }
            var terminal = end + outward * .115f + Vector3.up * .026f;
            Leaf(tips, end, terminal, .040f);
        }
    }

    static void SaveSurface(Transform shrubs, string name, Surface surface)
    {
        var filter = shrubs.Find("Winter_Leafy_Shrubs_" + name).GetComponent<MeshFilter>();
        var target = filter.sharedMesh;
        if (AssetDatabase.GetAssetPath(target) != Folder + "Winter_Leafy_Shrubs_" + name + ".asset")
            throw new InvalidOperationException("Unexpected shrub mesh " + name);
        var mesh = surface.CreateMesh();
        try
        {
            target.Clear();
            target.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            target.CombineMeshes(new[] { new CombineInstance { mesh = mesh } }, true, false);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
        finally { Object.DestroyImmediate(mesh); }
    }

    public static string Refine()
    {
        var castle = PrefabUtility.LoadPrefabContents(Castle);
        try
        {
            var root = castle.transform;
            var oldFlowers = root.Find("08_Distributed_Greenery").GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Small_Flowers").GroupBy(t => t.parent).ToArray();
            if (oldFlowers.Length != 15) throw new InvalidOperationException("Expected 15 planting beds");
            var deep = new Surface();
            var tips = new Surface();
            var snow = new Surface();
            for (int i = 0; i < oldFlowers.Length; i++)
            {
                var points = oldFlowers[i].Select(t => root.InverseTransformPoint(t.position)).ToArray();
                var center = points.Aggregate(Vector3.zero, (sum, p) => sum + p) / points.Length;
                center.y = .19f;
                Bed(deep, tips, snow, center, i);
            }
            var shrubs = root.Find("Winter_Leafy_Shrubs");
            SaveSurface(shrubs, "Shrub_Green", deep);
            SaveSurface(shrubs, "Shrub_Young_Leaves", tips);
            SaveSurface(shrubs, "Snow", snow);
            return "15 asymmetrical evergreen bushes built from tapered branches, separate folded leaves and small snow patches.";
        }
        finally { PrefabUtility.UnloadPrefabContents(castle); }
    }
}
