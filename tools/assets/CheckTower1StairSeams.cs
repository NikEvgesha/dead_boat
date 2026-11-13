// Unity MCP script-execute: CheckTower1StairSeams.Run.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CheckTower1StairSeams
{
    static Vector3 Polar(float a,float r,float y){a*=Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*r,y,-Mathf.Cos(a)*r);}
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float Overlap(Vector2[] subject,Vector2[] clip)
    {
        if(Cross(clip[1]-clip[0],clip[2]-clip[0])<0)Array.Reverse(clip);
        var polygon=subject.ToList();
        for(int edge=0;edge<3&&polygon.Count>0;edge++)
        {
            var a=clip[edge];var b=clip[(edge+1)%3];var input=polygon;polygon=new List<Vector2>();
            for(int i=0;i<input.Count;i++)
            {
                var p=input[i];var q=input[(i+1)%input.Count];
                float dp=Cross(b-a,p-a),dq=Cross(b-a,q-a);
                bool pin=dp>=-1e-6f,qin=dq>=-1e-6f;
                if(pin)polygon.Add(p);
                if(pin!=qin&&Mathf.Abs(dp-dq)>1e-8f)polygon.Add(Vector2.LerpUnclamped(p,q,dp/(dp-dq)));
            }
        }
        float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);
        return Mathf.Abs(area)*.5f;
    }
    public static string Run()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var report=new List<string>();
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Workshop/Tower1/Tower1_OvergrownCourt.prefab"),scene);
            Physics.SyncTransforms();
            var stairs=root.transform.Find("Low_Wraparound_Stair").GetComponentsInChildren<Collider>();
            var floor=root.transform.Find("Tower/Rooftop_Floor").GetComponent<Collider>();
            var surfaces=stairs.Append(floor).ToArray();int count=0;
            // Dense rays across the former open strip, just outside the actual octagonal wall.
            for(int step=0;step<40;step++)for(int sample=0;sample<9;sample++)
            {
                float angle=-35-(step+(sample+.5f)/9)*6.75f;
                float d=Mathf.DeltaAngle(Mathf.Round(angle/45)*45,angle);
                float wall=2.8f*Mathf.Cos(22.5f*Mathf.Deg2Rad)/Mathf.Cos(d*Mathf.Deg2Rad);
                for(int radial=0;radial<8;radial++)
                {
                    float radius=Mathf.Lerp(wall+.006f,3.16f,radial/7f),height=(step+1)*.16f;
                    var p=Polar(angle,radius,height);
                    bool supported=surfaces.Any(c=>c.Raycast(new Ray(p+Vector3.up*.045f,Vector3.down),out var h,.09f)
                        &&h.normal.y>.95f&&Mathf.Abs(h.point.y-height)<.003f);
                    if(!supported)throw new InvalidOperationException($"Open stair seam at {angle:F2} deg, radius {radius:F3}, height {height:F2}");
                    count++;
                }
            }
            report.Add($"PASS {count} support samples along the former gap: wall edge +6 mm through radius 3.16 m, all 40 steps.");
            for(int a=43;a<=61;a++)for(int radial=0;radial<8;radial++)
            {
                var p=Polar(a,2.9f+radial*.34f,6.4f);
                if(!surfaces.Any(c=>c.Raycast(new Ray(p+Vector3.up*.04f,Vector3.down),out var h,.08f)&&Mathf.Abs(h.point.y-6.4f)<.003f))
                    throw new InvalidOperationException("Unsupported added landing "+p);
            }
            report.Add("PASS 152 support samples across the expanded top step and its roof junction.");
            var top=new List<(string name,Vector2[] triangle)>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();if(!renderer||!renderer.enabled)continue;
                var mesh=filter.sharedMesh;if(!mesh||!mesh.isReadable)continue;
                var v=mesh.vertices.Select(filter.transform.TransformPoint).ToArray();var tris=mesh.triangles;
                for(int i=0;i<tris.Length;i+=3)
                {
                    var t=new[]{v[tris[i]],v[tris[i+1]],v[tris[i+2]]};
                    var n=Vector3.Cross(t[1]-t[0],t[2]-t[0]).normalized;
                    if(n.y>.9999f&&t.All(p=>Mathf.Abs(p.y-6.4f)<.0002f))
                        top.Add((filter.name,t.Select(p=>new Vector2(p.x,p.z)).ToArray()));
                }
            }
            float max=0;
            for(int i=0;i<top.Count;i++)for(int j=i+1;j<top.Count;j++)
            {
                float area=Overlap(top[i].triangle,top[j].triangle);max=Mathf.Max(max,area);
                if(area>.00002f)throw new InvalidOperationException($"Coplanar roof overlap: {top[i].name}/{top[j].name}, {area:F5} m2");
            }
            report.Add($"PASS roof and top landing: {top.Count} upward triangles; maximum coplanar overlap {max:F7} m2.");
            report.Add("Old Top_Landing object absent: "+!root.transform.Find("Low_Wraparound_Stair/Top_Landing"));
        }
        finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        File.WriteAllLines("Library/Tower1ConceptReview/stair-seams-check.txt",report);
        return string.Join("\n",report);
    }
}
