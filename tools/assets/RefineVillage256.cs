using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class RefineVillage256
{
    public const string Canonical = "Assets/Prefabs/Workshop/Village255/Village_Unified.prefab";
    public static readonly string[] Paths = { Canonical, "Assets/Prefabs/Locations/Enemy/Village_1.prefab", "Assets/Prefabs/Locations/Enemy/Village_2.prefab" };
    const string Evidence = "Docs/locations/previews/village-refine256/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    public static Transform[] Homes(GameObject root) => root.GetComponentsInChildren<Transform>(true)
        .Where(t => t.name.StartsWith("Village_House_")).OrderBy(t => t.name).ToArray();
    public static string Houses(GameObject root) => string.Join("\n", Homes(root).SelectMany(h => h.GetComponentsInChildren<Transform>(true)
        .Select(t => AnimationUtility.CalculateTransformPath(t, root.transform)+"|"+t.localPosition.ToString("F5")+"|"+t.localRotation.ToString("F5")+"|"+t.localScale.ToString("F5")+"|"+t.gameObject.activeSelf+
            "|"+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().FullName))+
            "|"+(t.GetComponent<MeshFilter>()?AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh):"")+
            "|"+string.Join(",",t.GetComponent<Renderer>()?t.GetComponent<Renderer>().sharedMaterials.Select(AssetDatabase.GetAssetPath):Enumerable.Empty<string>()))));

    // Vertical intersection with visible triangles, not the enclosing renderer box.
    public static float Surface(IEnumerable<MeshFilter> filters, Vector3 point, float ceiling)
    {
        float best = float.NegativeInfinity;
        foreach (var filter in filters)
        {
            if (!filter.sharedMesh) continue;
            var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=filter.transform.TransformPoint(vertices[indices[i]]);var b=filter.transform.TransformPoint(vertices[indices[i+1]]);var c=filter.transform.TransformPoint(vertices[indices[i+2]]);
                float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if(Mathf.Abs(den)<.0000001f)continue;
                float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/den;
                float w=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/den;
                if(u<-.00001f||w<-.00001f||u+w>1.00001f)continue;
                float y=u*a.y+w*b.y+(1-u-w)*c.y;
                if(y<=ceiling&&y>best)best=y;
            }
        }
        return best;
    }
    public static MeshFilter[] Support(GameObject root,int index)
    {
        var homes=Homes(root);var home=homes[index<20?index/2:(index-20)%homes.Length];
        var target=index<20?home.Find(index%2==0?"08_Interior_Props/Dining_Table":"09_Castle_Guardroom/Equipment_Rack"):home;
        Check(target,"Missing original support " + index);
        return target.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).ToArray();
    }
    public static void Slots(GameObject root,List<string> log)
    {
        var content=root.GetComponent<LocationContentSpawner>();Check(content.itemSpawnPoints.Count==25,"25 slots required");
        for(int i=0;i<25;i++)
        {
            var slot=content.itemSpawnPoints[i];var p=slot.spawnTransform.position;
            float y=Surface(Support(root,i),p,p.y+.25f);Check(!float.IsInfinity(y)&&Mathf.Abs(p.y-y)<.5f,"No visible support "+i);
            slot.spawnTransform.position=new Vector3(p.x,y+.003f,p.z);
            slot.alignVisualBase=true;
            // Fit large collectible models onto narrow furniture without changing loot selection.
            slot.maxVisualFootprint=i>=20?1.1f:(i%2==0?1.05f:.72f);
            log.Add($"{root.name} slot {i:D2}: support={y:F4}, point={slot.spawnTransform.position:F4}, cap={slot.maxVisualFootprint:F2}");
        }
    }
    public static string ApplySaved()
    {
        Directory.CreateDirectory(Evidence);var log=new List<string>();
        foreach(var path in Paths)
        {
            string backup="Library/Village256/Backup/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                string before=Houses(root);var content=root.GetComponent<LocationContentSpawner>();var refs=content.itemSpawnPoints.Select(p=>p.spawnTransform).ToArray();
                Slots(root,log);Check(Houses(root)==before,"House/interior changed");Check(refs.SequenceEqual(content.itemSpawnPoints.Select(p=>p.spawnTransform)),"Slot references changed");
                Check(PrefabUtility.SaveAsPrefabAsset(root,path),"Cannot save "+path);
                File.WriteAllText(Evidence+Path.GetFileNameWithoutExtension(path)+"-houses.txt",before);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllLines(Evidence+"slots.txt",log);return "Saved 75 slot placements in canonical village and both banks; houses/interiors unchanged.";
    }
    static (VillageWalkPreview walk,GameObject root,List<GameObject> items) Live()
    {
        var walk=Object.FindAnyObjectByType<VillageWalkPreview>();Check(walk,"Village walk required");
        var session=(GameObject)typeof(VillageWalkPreview).GetField("session",Private).GetValue(walk);
        Check(session,"Walk session unavailable");var content=session.GetComponentInChildren<LocationContentSpawner>();
        var items=(List<GameObject>)typeof(VillageWalkPreview).GetField("pickups",Private).GetValue(walk);
        Check(items.Count==25,"Live pickup count changed: "+items.Count);return(walk,content.gameObject,items);
    }
    public static string ApplyLive()
    {
        var live=Live();var log=new List<string>();Slots(live.root,log);
        var content=live.root.GetComponent<LocationContentSpawner>();float worst=0;
        for(int i=0;i<25;i++)
        {
            var item=live.items[i];if(!item)continue;
            var p=content.itemSpawnPoints[i];item.transform.SetPositionAndRotation(p.spawnTransform.position,p.spawnTransform.rotation);
            LocationContentSpawner.PlaceItemAtSpawnPoint(item,p);
            float bottom=Bottom(item);float gap=bottom-(p.spawnTransform.position.y-.003f);worst=Mathf.Max(worst,Mathf.Abs(gap));
            Check(Mathf.Abs(gap-.003f)<.001f,"Live base mismatch "+i);
            log.Add($"PASS live {i:D2} {item.name}: visible bottom={bottom:F4}, support={p.spawnTransform.position.y-.003f:F4}, gap={gap:F4} m");
        }
        File.WriteAllLines(Evidence+"live.txt",log);return $"25 live pickups aligned; maximum base/support gap={worst:F4} m. Player, collected state and village were not restarted.";
    }
    public static float Bottom(GameObject item)=>item.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled)
        .SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).Min(p=>p.y);

    public static string AuditLive()
    {
        var live=Live();var content=live.root.GetComponent<LocationContentSpawner>();var log=new List<string>();
        for(int i=0;i<25;i++)
        {
            var item=live.items[i];var p=content.itemSpawnPoints[i];float support=Surface(Support(live.root,i),p.spawnTransform.position,p.spawnTransform.position.y+.02f);
            var vertices=item.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
            float gap=vertices.Min(v=>v.y)-support;float width=Mathf.Max(vertices.Max(v=>v.x)-vertices.Min(v=>v.x),vertices.Max(v=>v.z)-vertices.Min(v=>v.z));
            Check(Mathf.Abs(gap-.003f)<.001f,"Runtime base mismatch "+i);Check(width<=p.maxVisualFootprint+.01f,"Runtime footprint exceeds furniture "+i+"/"+item.name+" width="+width);
            log.Add($"PASS {live.root.name} slot {i:D2} {item.name}: base gap={gap:F4} m; visual width={width:F4} m <= {p.maxVisualFootprint:F2}");
        }
        File.WriteAllLines(Evidence+live.root.name+"-runtime.txt",log);return "PASS "+live.root.name+": 25 actual walk items contact their supports and fit the slot footprints.";
    }

    public static string Verify()
    {
        var report=new List<string>();int count=0;
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var path in Paths)
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var content=root.GetComponent<LocationContentSpawner>();
                Check(Houses(root)==File.ReadAllText(Evidence+Path.GetFileNameWithoutExtension(path)+"-houses.txt"),"Saved house/interior mismatch "+path);
                for(int seed=0;seed<5;seed++)
                {
                    var rng=new DeadBoat.Online.RunRandom(256+seed,"village-base",seed);
                    for(int i=0;i<25;i++)
                    {
                        var p=content.itemSpawnPoints[i];var prefab=content.itemCollection.GetRandomItem(p.allowedType,p.allowedSize,rng);Check(prefab,"No item");
                        // Keep runtime behaviour disabled while auditing real item geometry.
                        var staging=new GameObject("Inactive placement test");SceneManager.MoveGameObjectToScene(staging,scene);staging.SetActive(false);
                        var item=Object.Instantiate(prefab.gameObject,staging.transform);
                        item.transform.SetPositionAndRotation(p.spawnTransform.position,p.spawnTransform.rotation);
                        LocationContentSpawner.PlaceItemAtSpawnPoint(item,p);
                        float y=Surface(Support(root,i),p.spawnTransform.position,p.spawnTransform.position.y+.02f);
                        Check(Mathf.Abs(Bottom(item)-y-.003f)<.001f,"Base/support mismatch "+path+"/"+i+"/"+prefab.name);
                        Object.DestroyImmediate(staging);count++;
                    }
                }
                report.Add("PASS "+path+": houses/interiors preserved; 125 real seeded loot placements have visible bases 3 mm above the furniture/floor triangles.");
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        Check(!EditorUtility.scriptCompilationFailed,"Script compilation errors");report.Add("PASS total "+count+" placements; Unity compilation succeeds.");
        File.WriteAllLines(Evidence+"verification.txt",report);return string.Join("\n",report);
    }
    public static string CaptureBefore()=>Capture("before");
    public static string CaptureAfter()=>Capture("after");
    static string Capture(string suffix)
    {
        var live=Live();Directory.CreateDirectory(Evidence);var home=Homes(live.root)[1];
        var cameraObject=new GameObject("Loot verification camera 256");SceneManager.MoveGameObjectToScene(cameraObject,live.root.scene);
        var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.03f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.48f,.63f,.72f);
        try
        {
            foreach(var name in new[]{"Dining_Table","Equipment_Rack"})
            {
                var support=home.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
                camera.transform.position=support.position-home.forward*2+home.right*1.7f+Vector3.up*1.3f;
                camera.transform.LookAt(support.position+Vector3.up*.75f);
                var rt=RenderTexture.GetTemporary(1280,850,24);var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);var old=RenderTexture.active;
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(Evidence+name+"-"+suffix+".png",tex.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
            }
        }
        finally{Object.DestroyImmediate(cameraObject);}
        return "Saved table and rack loot close-ups: "+suffix;
    }
}
