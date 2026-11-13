using System;
using UnityEngine;
using UnityEditor;

public static class ShapeFirSnow172
{
    public static void Main()
    {
        const string path = "Assets/Events/NewYear/WinterTown172/Sparse_Snowy_Firs_Snow.asset";
        var snow = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Vector3[] sphere;
        try { sphere = primitive.GetComponent<MeshFilter>().sharedMesh.vertices; }
        finally { UnityEngine.Object.DestroyImmediate(primitive); }
        var vertices = snow.vertices;
        if (vertices.Length != 24 * sphere.Length) throw new Exception("Unexpected branch snow mesh layout");
        int bottom = 0, top = 0;
        for (int i=1;i<sphere.Length;i++)
        {
            if (sphere[i].y < sphere[bottom].y) bottom=i;
            if (sphere[i].y > sphere[top].y) top=i;
        }
        for (int start=0;start<vertices.Length;start+=sphere.Length)
        {
            int cap = start/sphere.Length;
            var center = (vertices[start+bottom]+vertices[start+top])*.5f;
            var normal = (vertices[start+top]-vertices[start+bottom]).normalized;
            var tangent = Vector3.Cross(Vector3.up,normal).normalized;
            if (tangent.sqrMagnitude<.001f) tangent=Vector3.right;
            var downslope = Vector3.Cross(tangent,normal).normalized;
            for (int i=0;i<sphere.Length;i++)
            {
                var p = sphere[i];
                float a = Mathf.Atan2(p.z,p.x);
                float edge = 1+.13f*Mathf.Sin(a*3+cap*.91f)+.07f*Mathf.Cos(a*5-cap);
                vertices[start+i] = center + tangent*(p.x*(.61f+.04f*(cap%4))*edge)
                    + downslope*(p.z*(.26f+.03f*(cap%3))*edge)
                    + normal*(p.y*(.105f+.018f*Mathf.Cos(a*3+cap)));
            }
        }
        snow.vertices=vertices;
        snow.RecalculateNormals();
        snow.RecalculateBounds();
        EditorUtility.SetDirty(snow);
        AssetDatabase.SaveAssets();
    }
}
