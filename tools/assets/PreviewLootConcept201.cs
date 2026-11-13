using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PreviewLootConcept201
{
    const string Root="Assets/Prefabs/Workshop/LootConcept201", Output="Docs/locations/previews/loot203";
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static GameObject Node(Scene scene,string name){var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,scene);return g;}
    static Camera MakeCamera(Scene scene){var c=Node(scene,"Loot Preview Camera").AddComponent<Camera>();c.scene=scene;c.enabled=false;c.nearClipPlane=.045f;c.farClipPlane=250;c.allowHDR=false;c.allowMSAA=true;return c;}
    static Camera Orbit(Scene s){var c=MakeCamera(s);c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.83f,.85f,.87f);c.orthographic=true;c.orthographicSize=.56f;c.transform.position=V(.94f,.91f,-1.56f);c.transform.LookAt(V(0,.21f,0));
        var key=Node(s,"Daylight").AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(42,30,0);key.intensity=.83f;key.shadows=LightShadows.Soft;
        var fill=Node(s,"Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(30,220,0);fill.intensity=.18f;return c;}
    static void Read(Camera c,RenderTexture rt,Texture2D tex,int x,int y){var old=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),x,y,false);}finally{RenderTexture.active=old;c.targetTexture=null;}}
    static GameObject Place(Scene s,GameObject source,Vector3 p,float yaw){var o=(GameObject)PrefabUtility.InstantiatePrefab(source,s);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return o;}
    static void Board(GameObject[] list,string file){var s=EditorSceneManager.NewPreviewScene();var rt=RenderTexture.GetTemporary(340,340,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var tex=new Texture2D(1700,680,TextureFormat.RGB24,false);var label=default(TextMesh);var floor=new Material(Shader.Find("Standard")){color=new Color(.51f,.52f,.54f)};try{
        var c=Orbit(s);var stage=GameObject.CreatePrimitive(PrimitiveType.Plane);SceneManager.MoveGameObjectToScene(stage,s);stage.transform.position=Vector3.down*.004f;stage.transform.localScale=Vector3.one*20;
        var stageRenderer=stage.GetComponent<MeshRenderer>();stageRenderer.sharedMaterial=floor;stageRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var g=Node(s,"Index label");label=g.AddComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");g.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;label.fontSize=72;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.16f,.21f,.28f);
        for(int i=0;i<list.Length;i++){
            float yaw=list[i].name.StartsWith("T05")?-105:list[i].name.StartsWith("F09")?-27:0;
            var p=Place(s,list[i],Vector3.zero,yaw);var mesh=p.GetComponent<MeshFilter>().sharedMesh;
            label.text=list[i].name.Substring(0,3);
            c.orthographicSize=Mathf.Clamp(mesh.bounds.size.magnitude*.61f,.30f,.60f);
            var aim=mesh.bounds.center;aim.y=mesh.bounds.center.y-c.orthographicSize*.10f;
            c.transform.position=aim+V(.94f,.70f,-1.56f);c.transform.LookAt(aim);
            label.characterSize=.022f*c.orthographicSize;
            label.transform.rotation=c.transform.rotation;label.transform.position=c.transform.position+c.transform.forward*.61f-c.transform.up*(c.orthographicSize*.84f);
            Read(c,rt,tex,(i%5)*340,(1-i/5)*340);
            UnityEngine.Object.DestroyImmediate(p);
        }
        tex.Apply();File.WriteAllBytes(Output+"/"+file,tex.EncodeToPNG());
    }finally{UnityEngine.Object.DestroyImmediate(tex);RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(s);UnityEngine.Object.DestroyImmediate(floor);}}
    static float Ground(MeshFilter f,float x,float z){var mesh=f.sharedMesh;var vs=mesh.vertices;var ids=mesh.triangles;float best=float.NegativeInfinity;
        for(int i=0;i<ids.Length;i+=3){var a=f.transform.TransformPoint(vs[ids[i]]);var b=f.transform.TransformPoint(vs[ids[i+1]]);var c=f.transform.TransformPoint(vs[ids[i+2]]);float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Vector3.Cross(b-a,c-a).y<=.00001f)continue;
            float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d;float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d;
            if(u>=-.0001f&&v>=-.0001f&&u+v<=1.0001f)best=Mathf.Max(best,u*a.y+v*b.y+(1-u-v)*c.y);}
        if(float.IsInfinity(best))throw new Exception("No ground at "+x+","+z+" on "+f.name);return best;
    }
    static string Context(Dictionary<string,GameObject> props,bool winter,string[] ids,string filename){
        string map=winter?"LevelEvent":"LevelForest";var s=EditorSceneManager.OpenPreviewScene("Assets/Scenes/"+map+".unity");var rt=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try{
            var roots=s.GetRootGameObjects();var bank=roots.SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).Single(f=>f.name=="Land");
            var sun=roots.SelectMany(g=>g.GetComponentsInChildren<Light>()).FirstOrDefault(l=>l.type==LightType.Directional);if(sun){sun.transform.rotation=Quaternion.Euler(53,72,0);sun.shadows=LightShadows.Soft;}
            var c=MakeCamera(s);c.cameraType=CameraType.Preview;c.fieldOfView=65;c.clearFlags=CameraClearFlags.Skybox;c.backgroundColor=winter?new Color(.62f,.79f,.89f):new Color(.67f,.79f,.89f);c.farClipPlane=450;
            var center=winter?V(-32.8f,0,-9.8f):V(-39.0f,0,-6.0f);
            var slots=ids.SequenceEqual(new[]{"T02","T05","F01","F09"})?new[]{("T02",-.90f,.39f,-17f),("T05",-.27f,.49f,20f),("F01",.49f,-.07f,-25f),("F09",1.11f,.04f,10f)}:
                winter?ids.Select((id,i)=>(id,(i-(ids.Length-1)/2f)*.69f,i%2==0?.08f:.26f,i%2==0?-13f:16f)).ToArray():new[]{("T06",-.40f,-.10f,-14f),("T07",.39f,.10f,7f)};
            var landed=new List<string>();
            foreach(var t in slots){var p=center+V(t.Item3,0,-t.Item2);p.y=Ground(bank,p.x,p.z)+.002f;var obj=Place(s,props[t.Item1],p,90+t.Item4);var b=obj.GetComponent<MeshRenderer>().bounds;if(Mathf.Abs(b.min.y-p.y)>.002f)throw new Exception("Ground check failed: "+obj.name);landed.Add(t.Item1+" at "+p.ToString("F3"));}
            center.y=Ground(bank,center.x,center.z);c.transform.position=center+V(winter?-1.85f:-1.45f,1.65f,winter?.18f:.05f);c.transform.LookAt(center+V(.24f,.37f,0));
            Read(c,rt,tex,0,0);tex.Apply();File.WriteAllBytes(Output+"/"+filename,tex.EncodeToPNG());
            return map+" preview copy, camera eye height 1.65 m, preview lens 65 degrees; props supported by "+bank.name+": "+string.Join(", ",landed);
        }finally{UnityEngine.Object.DestroyImmediate(tex);RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(s);}
    }
    public static string Run(){
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();var all=paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        if(all.Length!=20||all.Any(g=>!g||g.GetComponentsInChildren<Component>().Length!=3||g.GetComponent<MeshFilter>().sharedMesh==null||Mathf.Abs(g.GetComponent<MeshFilter>().sharedMesh.bounds.min.y)>.001f))throw new Exception("Expected 20 render-only grounded prefabs");
        var byId=all.ToDictionary(p=>p.name.Substring(0,3));var t=all.Where(p=>p.name.StartsWith("T")).OrderBy(p=>p.name).ToArray();var f=all.Where(p=>p.name.StartsWith("F")).OrderBy(p=>p.name).ToArray();
        if(t.Length!=10||f.Length!=10||paths.Count(p=>p.Contains("/MainMaps/"))!=3||!paths.Where(p=>p.Contains("/MainMaps/")).All(p=>p.Contains("/T06_")||p.Contains("/T07_")||p.Contains("/T10_")))throw new Exception("Category mismatch");
        Directory.CreateDirectory(Output);Board(t,"01-treasures-3d.png");Board(f,"02-fuel-3d.png");
        var details=string.Join("\n",new[]{
            Context(byId,true,new[]{"T02","T05","F01","F09"},"03-winter-in-level.png"),
            Context(byId,false,new[]{"T06","T07"},"04-main-in-level.png"),
            Context(byId,true,new[]{"T01","T03","T04","T08","T09"},"05-winter-treasures.png"),
            Context(byId,true,new[]{"T10","F02","F03","F04","F05"},"06-winter-mixed.png"),
            Context(byId,true,new[]{"F06","F07","F08","F10"},"07-winter-fuel.png")});
        File.WriteAllText(Output+"/preview-validation.txt",details+"\n20 prefabs verified; each contains only Transform/MeshFilter/MeshRenderer. Preview scenes closed without saving.\n");return details;
    }
}
