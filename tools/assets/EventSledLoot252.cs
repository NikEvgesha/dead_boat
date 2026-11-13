using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class EventSledLoot252
{
    const string MeshPath="Assets/Events/NewYear/Route185/Painted_Gift_Sleigh.asset";
    const string Output="Docs/locations/previews/event-loot252";
    static Transform Copy(Transform source,Transform parent,Scene scene)
    {
        var g=new GameObject(source.name);SceneManager.MoveGameObjectToScene(g,scene);
        g.transform.SetParent(parent,false);g.transform.localPosition=source.localPosition;
        g.transform.localRotation=source.localRotation;g.transform.localScale=source.localScale;
        var f=source.GetComponent<MeshFilter>();var r=source.GetComponent<MeshRenderer>();
        if(f&&r&&r.enabled&&f.sharedMesh){g.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;}
        foreach(Transform t in source)if(t.gameObject.activeSelf)Copy(t,g.transform,scene);
        return g.transform;
    }
    static void Render(GameObject source,string path,Vector3 direction,bool justSled=false)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        bool fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;
        try
        {
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.66f,.68f,.72f);
            var root=Copy(source.transform,null,scene);root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;
            if(justSled){var selected=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh==AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath)).ToArray();
                foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(!selected.Contains(f))f.GetComponent<MeshRenderer>().enabled=false;}
            var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new Exception("No renderers: "+source.name);
            var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            var cg=new GameObject("Camera");SceneManager.MoveGameObjectToScene(cg,scene);var c=cg.AddComponent<Camera>();c.enabled=false;c.scene=scene;
            c.orthographic=true;c.orthographicSize=bounds.extents.magnitude*1.08f;c.nearClipPlane=.01f;c.farClipPlane=10000;
            c.transform.position=bounds.center+direction.normalized*Mathf.Max(10,bounds.size.magnitude*2);c.transform.LookAt(bounds.center);
            c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.87f,.89f,.91f);c.allowHDR=false;
            foreach(var angle in new[]{new Vector3(40,-30,0),new Vector3(25,160,0)}){
                var lg=new GameObject("Preview Light");SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=angle.y<0?1.05f:.35f;lg.transform.rotation=Quaternion.Euler(angle);}
            int width=justSled?1100:480,height=justSled?800:400;
            var rt=RenderTexture.GetTemporary(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
            finally{RenderTexture.active=prior;c.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
        }
        finally{RenderSettings.fog=fog;RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static string Gallery()
    {
        Directory.CreateDirectory(Output);
        var pool=AssetDatabase.LoadAssetAtPath<LocationItemSpawnCollection>("Assets/Events/NewYear/NY_Loot.asset");
        var entries=new SerializedObject(pool).FindProperty("spawnEntries");var paths=new List<string>();
        for(int i=0;i<entries.arraySize;i++){
            var source=entries.GetArrayElementAtIndex(i).FindPropertyRelative("itemPrefab").objectReferenceValue;
            var path=AssetDatabase.GetAssetPath(source);if(paths.Contains(path))continue;
            paths.Add(path);Render(AssetDatabase.LoadAssetAtPath<GameObject>(path),Output+"/item-"+paths.Count.ToString("00")+".png",Path.GetFileNameWithoutExtension(path)=="skull"?new Vector3(1,.35f,1.5f):new Vector3(1,.75f,-1.5f));
        }
        File.WriteAllLines(Output+"/index.txt",paths);return paths.Count+" actual loot prefabs rendered";
    }
    public static string SledViews()
    {
        Directory.CreateDirectory(Output);var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Route181/Bank_05_Gift_Sled.prefab");
        string suffix=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath).name.Contains("252")?"after":"before";
        // Directions are in the sleigh's authored frame, before its -128 degree bank rotation.
        var yaw=Quaternion.Euler(0,-128,0);
        Render(p,Output+"/sled-front-"+suffix+".png",yaw*new Vector3(1.8f,.25f,3),true);
        Render(p,Output+"/sled-back-"+suffix+".png",yaw*new Vector3(-1.8f,.25f,-3),true);
        return "Sleigh front/back rendered: "+suffix;
    }
    public static string Repair()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);if(mesh.name.Contains("252"))return "Already repaired";
        Directory.CreateDirectory("Library/EventSledLoot252");File.Copy(MeshPath,"Library/EventSledLoot252/Painted_Gift_Sleigh-before.asset",false);
        var v=mesh.vertices.ToList();var tris=Enumerable.Range(0,mesh.subMeshCount).Select(mesh.GetTriangles).Select(t=>t.ToList()).ToArray();
        void Quad(int mat,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal){if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var temp=b;b=d;d=temp;}
            int n=v.Count;v.AddRange(new[]{a,b,c,d});tris[mat].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        var s=new[]{new Vector3(-1.91f,.75f,.75f),new Vector3(-1.64f,.97f,.59f),new Vector3(-1.22f,1.06f,.51f),new Vector3(-.62f,1.09f,.47f),new Vector3(.04f,1.04f,.46f),new Vector3(.72f,.96f,.47f),new Vector3(1.29f,.82f,.54f),new Vector3(1.63f,.65f,.67f),new Vector3(1.78f,.44f,.82f)};
        // Close the lower hull along the existing lower side edge. A continuous timber bed
        // overlaps the old five planks; neither side nor end panels can expose sky below it.
        for(int i=0;i<s.Length-1;i++){
            var a=s[i];var b=s[i+1];float wa=a.y-.12f,wb=b.y-.12f;
            float ya=Mathf.Min(.49f,a.z-.035f),yb=Mathf.Min(.49f,b.z-.035f);
            foreach(float side in new[]{-1f,1f}){
                Quad(0,new Vector3(side*wa,ya,a.x),new Vector3(side*wb,yb,b.x),new Vector3(side*wb,b.z+.008f,b.x),new Vector3(side*wa,a.z+.008f,a.x),Vector3.right*side);
                Quad(3,new Vector3(side*(wa-.045f),ya,a.x),new Vector3(side*(wa-.045f),a.z+.008f,a.x),new Vector3(side*(wb-.045f),b.z+.008f,b.x),new Vector3(side*(wb-.045f),yb,b.x),Vector3.left*side);
            }
            Quad(3,new Vector3(-wa,ya,a.x),new Vector3(wa,ya,a.x),new Vector3(wb,yb,b.x),new Vector3(-wb,yb,b.x),Vector3.down);
            // Follow rising front/rear edges to meet their panels; central top is below the planks.
            float ta=Mathf.Max(.57f,a.z+.01f),tb=Mathf.Max(.57f,b.z+.01f);
            Quad(2,new Vector3(-wa,ta,a.x),new Vector3(-wb,tb,b.x),new Vector3(wb,tb,b.x),new Vector3(wa,ta,a.x),Vector3.up);
        }
        foreach(int end in new[]{0,s.Length-1}){
            var a=s[end];float width=a.y-.12f;float top=Mathf.Max(a.z+.035f,end==0?.68f:.84f);float z=a.x;
            Quad(0,new Vector3(-width,.455f,z),new Vector3(width,.455f,z),new Vector3(width,top,z),new Vector3(-width,top,z),end==0?Vector3.back:Vector3.forward);
            float inside=z+(end==0?.13f:-.13f);
            Quad(3,new Vector3(-width,.455f,inside),new Vector3(-width,top,inside),new Vector3(width,top,inside),new Vector3(width,.455f,inside),end==0?Vector3.forward:Vector3.back);
            Quad(2,new Vector3(-width,top,z),new Vector3(width,top,z),new Vector3(width,top,inside),new Vector3(-width,top,inside),Vector3.up);
        }
        int added=v.Count-mesh.vertexCount;mesh.SetVertices(v);for(int i=0;i<tris.Length;i++)mesh.SetTriangles(tris[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.name="Painted_Gift_Sleigh_ClosedHull252";
        EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
        var report="Closed tapered lower hull and both end seams; added "+added+" vertices. Original mesh reference retained; live instances update without restarting.\n";
        File.WriteAllText(Output+"/repair.txt",report);return report;
    }
}

