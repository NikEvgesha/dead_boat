// Unity MCP script-execute: FixClocks1StoneNormals.Apply.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FixClocks1StoneNormals
{
    public static string Apply()
    {
        const string folder="Assets/Prefabs/Workshop/Clocks1/";
        var names=Enumerable.Range(0,4).Select(i=>"Clock_Arch_Mesh_"+i)
            .Concat(new[]{"Smooth_Lower_Inner_Parapet","Smooth_Outer_Parapet"});
        var report=new System.Collections.Generic.List<string>();
        foreach(var name in names)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+name+".asset");
            if(!mesh)throw new InvalidOperationException("Missing mesh "+name);
            var original=mesh.vertices;var triangles=mesh.triangles;var bounds=mesh.bounds;
            var uv=mesh.uv;
            var vertices=triangles.Select(i=>original[i]).ToArray();
            var tex=uv.Length==original.Length?triangles.Select(i=>uv[i]).ToArray():null;
            mesh.Clear();mesh.vertices=vertices;if(tex!=null)mesh.uv=tex;
            mesh.triangles=Enumerable.Range(0,vertices.Length).ToArray();
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var normals=mesh.normals;
            for(int i=0;i<vertices.Length;i+=3)
            {
                var face=Vector3.Cross(vertices[i+1]-vertices[i],vertices[i+2]-vertices[i]).normalized;
                if(face.sqrMagnitude<.5f)continue;
                if(Enumerable.Range(i,3).Any(j=>Vector3.Dot(normals[j],face)<.9999f))
                    throw new InvalidOperationException("Rounded face normal: "+name);
            }
            if(Vector3.Distance(mesh.bounds.center,bounds.center)>.0001f||Vector3.Distance(mesh.bounds.size,bounds.size)>.0001f)
                throw new InvalidOperationException("Changed mesh bounds: "+name);
            EditorUtility.SetDirty(mesh);
            report.Add(name+": flat face normals; geometry, UV coordinates and bounds preserved.");
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("Library/Clocks1ConceptReview/stone-normals-check.txt",report);
        return string.Join("\n",report);
    }
}
