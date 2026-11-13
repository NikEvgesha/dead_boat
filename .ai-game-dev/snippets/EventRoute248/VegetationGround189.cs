using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CheckVegetationGround189
{
    static readonly Regex Plant = new Regex(@"prop_(bush|tree)|courtyard_(bush|tree)|shade_tree|tall_pine|small_oak|(^|_)palm|(^|_)tree( |\d|_|$)|(^|_)bush( |\d|_|$)|(^|_)shrub( |\d|$)|(^|_)fir( |\d|$)|(^|_)spruce|yolka", RegexOptions.IgnoreCase);
    static readonly Regex Parts = new Regex(@"^Festive_Firs_|^Bank_Fir_|^Winter_Leafy_Shrubs_|^Presents_Fir|^Festive_Firs$|Snowman|Roof_Snow", RegexOptions.IgnoreCase);
    static readonly Regex Ground = new Regex(@"Courtyard|Terrain|Ground|Grass_Terrace|Soft_Sand_Beach|Grass_|Grassy|Floor|Bank|Land|Rock|Stone|Sand", RegexOptions.IgnoreCase);
    static bool On(Transform t, Transform boundary) { for(;t!=null;t=t.parent){ if(!t.gameObject.activeSelf)return false; if(t==boundary)break; }return true; }
    static string P(Transform t) => t.parent==null?t.name:P(t.parent)+"/"+t.name;
    static bool IsPlant(Transform t) => Plant.IsMatch(t.name)&&!Parts.IsMatch(t.name)&&!t.name.StartsWith("TreeHouse",StringComparison.OrdinalIgnoreCase);
    static string F(float v)=>v.ToString("F3",CultureInfo.InvariantCulture);

    // Support is tested against triangles at the foot, not against a surface's AABB or pivot.
    static float Surface(MeshFilter[] filters, Vector3 foot, out string surface, float penetration = .25f)
    {
        float best=float.NegativeInfinity;surface="-";
        foreach(var f in filters){
            var rr=f.GetComponent<MeshRenderer>(); if(!rr||!rr.enabled)continue;
            var bb=rr.bounds;if(foot.x<bb.min.x-.001f||foot.x>bb.max.x+.001f||foot.z<bb.min.z-.001f||foot.z>bb.max.z+.001f||bb.min.y>foot.y+penetration)continue;
            var mesh=f.sharedMesh;if(!mesh)continue;var v=mesh.vertices;var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3){
                var a=f.transform.TransformPoint(v[indices[i]]);var b=f.transform.TransformPoint(v[indices[i+1]]);var c=f.transform.TransformPoint(v[indices[i+2]]);
                var n=Vector3.Cross(b-a,c-a);if(n.y<=.00001f)continue;
                float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                float u=((b.z-c.z)*(foot.x-c.x)+(c.x-b.x)*(foot.z-c.z))/den;
                float w=((c.z-a.z)*(foot.x-c.x)+(a.x-c.x)*(foot.z-c.z))/den;
                if(u<-.0001f||w<-.0001f||u+w>1.0001f)continue;
                float y=u*a.y+w*b.y+(1-u-w)*c.y;if(y<=foot.y+penetration&&y>best){best=y;surface=P(f.transform);}
            }
        }
        return best;
    }
    static void Audit(GameObject[] roots, string label, bool bankFallback, List<string> output)
    {
        var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true).Where(t=>On(t,g.transform))).ToArray();
        bool broad=label.EndsWith("/Lobby.unity")||Regex.IsMatch(label,@"/pond_[12]\.prefab$");
        var supported=all.Where(t=>(broad||Ground.IsMatch(t.name))&&!IsPlant(t)&&!Parts.IsMatch(t.name)&&!Ancestors(t).Any(IsPlant)).Select(t=>t.GetComponent<MeshFilter>()).Where(f=>f&&f.sharedMesh).ToArray();
        var plants=all.Where(IsPlant).Where(t=>!Ancestors(t).Any(IsPlant)).ToArray();
        int count=0,missing=0,floating=0;
        foreach(var t in plants){
            var filters=t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&On(f.transform,t)&&f.TryGetComponent<MeshRenderer>(out var rr)&&rr.enabled).ToArray();
            if(filters.Length==0)continue;
            var verts=filters.SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();if(verts.Length==0)continue;
            var min=verts.Min(v=>v.y);var lows=verts.Where(v=>v.y<=min+.015f).ToArray();var foot=new Vector3(lows.Average(v=>v.x),min,lows.Average(v=>v.z));
            // Legacy lobby palm roots penetrate stepped island meshes; count that contact too.
            var ground=Surface(supported.Where(f=>!f.transform.IsChildOf(t)).ToArray(),foot,out var surface,label.EndsWith("/Lobby.unity")?.65f:.25f);
            if(bankFallback&&Mathf.Abs(foot.x)>9f&&foot.y>=-.3f)ground=Mathf.Max(ground,0);
            count++;float gap=foot.y-ground;
            if(float.IsInfinity(gap))missing++;else if(gap>.035f)floating++;
            output.Add(label+" | "+P(t)+" | foot="+foot.ToString("F3")+" ground="+F(ground)+" gap="+F(gap)+" surface="+surface);
        }
        output.Add("SUMMARY "+label+" plants="+count+" floating="+floating+" no-matched-surface="+missing);
    }
    static IEnumerable<Transform> Ancestors(Transform t){for(t=t.parent;t!=null;t=t.parent)yield return t;}
    static void AuditWinterCastle(Transform root,string label,List<string> report)
    {
        var fir=root.Find("Winter_176/Festive_Firs/Festive_Firs_Wood_Dark");
        var shrubs=root.Find("Winter_Leafy_Shrubs/Winter_Leafy_Shrubs_Shrub_Green");
        if(!fir||!shrubs)return;
        var court=root.Find("02_Courtyard").GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh).ToArray();
        var beds=root.Find("08_Distributed_Greenery").GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Small_Flowers")
            .GroupBy(t=>t.parent).Select(g=>g.Select(t=>root.InverseTransformPoint(t.position)).Aggregate(Vector3.zero,(a,b)=>a+b)/g.Count()).ToArray();
        var trees=new[]{new Vector3(-18.12f,0,-18.45f),new Vector3(-7.6f,0,-5.2f),new Vector3(-18.24f,0,9),new Vector3(7.3f,0,-6.9f),new Vector3(17.5f,0,5.2f),new Vector3(17.35f,0,19.05f)};
        foreach(var batch in new[]{(name:"fir trunks",filter:fir.GetComponent<MeshFilter>(),points:trees),(name:"shrub beds",filter:shrubs.GetComponent<MeshFilter>(),points:beds)}){
            var vertices=batch.filter.sharedMesh.vertices.Select(batch.filter.transform.TransformPoint).ToArray();int issues=0;
            foreach(var local in batch.points){
                var point=root.TransformPoint(new Vector3(local.x,0,local.z));
                var baseVertices=vertices.Where(v=>Mathf.Abs(v.x-point.x)<.32f&&Mathf.Abs(v.z-point.z)<.32f).OrderBy(v=>v.y).Take(1).ToArray();
                if(baseVertices.Length==0){issues++;report.Add("BAKED missing "+label+" "+batch.name+" "+point);continue;}
                var foot=baseVertices[0];var ground=Surface(court,foot,out _);if(float.IsInfinity(ground)||foot.y-ground>.035f){issues++;report.Add("BAKED gap "+label+" "+batch.name+" "+point+" gap="+F(foot.y-ground));}
            }
            report.Add("BAKED "+label+" "+batch.name+" checked="+batch.points.Length+" issues="+issues);
        }
    }
    public static string Run()
    {
        var report=new List<string>();var decor=new HashSet<string>();var locations=new HashSet<string>();
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).Where(s=>!s.EndsWith("LoadingScene.unity")).ToArray();
        foreach(var path in scenes){
            var sc=EditorSceneManager.OpenPreviewScene(path);
            try {
                var roots=sc.GetRootGameObjects();Audit(roots,path,false,report);
                foreach(var root in roots.Where(g=>g.name=="EventCastle_Winter"))AuditWinterCastle(root.transform,path,report);
                foreach(var l in roots.SelectMany(r=>r.GetComponentsInChildren<LendController>(true))){
                    foreach(var segment in l.locations.SelectMany(p=>p.groundSegments).Where(t=>t&&t.name.StartsWith("Land"))){
                        var bank=segment.GetComponent<MeshFilter>();if(!bank||!bank.sharedMesh)continue;
                        var samples=new[]{-150f,-75f,-35f,-15f,15f,35f,75f,150f}.SelectMany(x=>new[]{-150f,0f,150f}.Select(z=>Surface(new[]{bank},new Vector3(segment.position.x+x,.1f,segment.position.z+z),out _))).ToArray();
                        report.Add("BANK "+path+" "+segment.name+" 24 samples Y="+F(samples.Min())+".."+F(samples.Max()));
                    }
                }
                foreach(var d in roots.SelectMany(r=>r.GetComponentsInChildren<DecorationSpawner>(true)).Where(d=>d.decorationCollection))
                    foreach(var e in d.decorationCollection.spawnItems.Where(e=>e.prefab&&e.chance>0))decor.Add(AssetDatabase.GetAssetPath(e.prefab));
                foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<CitySpawner>(true)).Where(c=>c.cityPrefab))locations.Add(AssetDatabase.GetAssetPath(c.cityPrefab));
            } finally {EditorSceneManager.ClosePreviewScene(sc);}
        }
        foreach(var p in AssetDatabase.GetDependencies(scenes,true).Where(p=>p.StartsWith("Assets/Prefabs/Locations/Enemy/")&&p.EndsWith(".prefab")))locations.Add(p);
        foreach(var p in locations.OrderBy(x=>x)){var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(g){Audit(new[]{g},p,true,report);if(g.name=="EventCastle_Winter")AuditWinterCastle(g.transform,p,report);}}
        foreach(var p in decor.OrderBy(x=>x)){
            var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(!g)continue;
            var plant=IsPlant(g.transform)||g.GetComponentsInChildren<Transform>().Any(IsPlant)||Regex.IsMatch(g.name,"fir|shrub|bush|tree",RegexOptions.IgnoreCase);
            if(!plant)continue;
            // Procedural DecorationSpawner spawns the entire prefab at y=0. Include base parts of baked plants.
            var mesh=g.GetComponentsInChildren<MeshFilter>(true).Where(f=>On(f.transform,g.transform)&&f.sharedMesh&&f.TryGetComponent<MeshRenderer>(out var rr)&&rr.enabled).ToArray();
            if(mesh.Length==0)continue;
            var min=mesh.Min(f=>f.sharedMesh.vertices.Min(v=>f.transform.TransformPoint(v).y-g.transform.position.y));
            report.Add("DECOR "+p+" | base at spawn Y=0: "+F(min));
        }
        File.WriteAllLines("Docs/locations/previews/vegetation-ground189.txt",report);
        return string.Join("\n",report.Where(s=>s.StartsWith("SUMMARY")||s.StartsWith("BANK")||s.StartsWith("BAKED")||s.StartsWith("DECOR")));
    }
}
