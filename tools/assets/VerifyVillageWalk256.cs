using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class VerifyVillageWalk256
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void Field(VillageWalkPreview walk,string name,object value)=>typeof(VillageWalkPreview).GetField(name,Private).SetValue(walk,value);
    static Transform Copy(Transform src,Transform parent,Scene scene)
    {
        var g=new GameObject(src.name);SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(parent,false);g.transform.localPosition=src.localPosition;g.transform.localRotation=src.localRotation;g.transform.localScale=src.localScale;
        var f=src.GetComponent<MeshFilter>();var r=src.GetComponent<MeshRenderer>();if(f&&r&&r.enabled&&f.sharedMesh){g.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;}
        foreach(Transform child in src)if(child.gameObject.activeSelf)Copy(child,g.transform,scene);return g.transform;
    }
    public static string Verify()
    {
        string folder="Docs/locations/previews/village-refine256/";var lines=new List<string>();var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var staging=new GameObject("Inactive isolated walk test");SceneManager.MoveGameObjectToScene(staging,scene);staging.SetActive(false);
            var walk=staging.AddComponent<VillageWalkPreview>();walk.leftBankPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/Village_1.prefab");walk.rightBankPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/Village_2.prefab");
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(ground,scene);ground.transform.SetParent(staging.transform,false);walk.ground=ground.transform;
            var player=new GameObject("Isolated player controller");SceneManager.MoveGameObjectToScene(player,scene);player.transform.SetParent(staging.transform,false);var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.center=Vector3.up*.9f;cc.radius=.35f;
            var view=new GameObject("Inactive controller camera").AddComponent<Camera>();view.transform.SetParent(player.transform,false);view.enabled=false;Field(walk,"player",cc);Field(walk,"view",view);
            // Run the actual preview spawn path, but keep its parent inactive so no
            // game services, input, pickup lifecycle or preview Start can execute.
            for(int bank=0;bank<2;bank++)
            {
                walk.SetBank(bank);
                var session=(GameObject)typeof(VillageWalkPreview).GetField("session",Private).GetValue(walk);var root=session.GetComponentInChildren<LocationContentSpawner>(true);
                var items=(List<GameObject>)typeof(VillageWalkPreview).GetField("pickups",Private).GetValue(walk);Check(items.Count==25,"Actual SetBank loot count");
                for(int i=0;i<25;i++)
                {
                    var point=root.itemSpawnPoints[i];var item=items[i];var vertices=item.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
                    float support=RefineVillage256.Surface(RefineVillage256.Support(root.gameObject,i),point.spawnTransform.position,point.spawnTransform.position.y+.02f);float gap=vertices.Min(p=>p.y)-support;
                    float width=Mathf.Max(vertices.Max(p=>p.x)-vertices.Min(p=>p.x),vertices.Max(p=>p.z)-vertices.Min(p=>p.z));
                    Check(Mathf.Abs(gap-.003f)<.001f,"Actual SetBank base mismatch "+bank+"/"+i);Check(width<=point.maxVisualFootprint+.01f,"Actual SetBank footprint mismatch "+bank+"/"+i+" "+width);
                    lines.Add($"PASS bank {bank+1} slot {i:D2} {item.name}: visible base gap={gap:F4} m, width={width:F4} <= {point.maxVisualFootprint:F2} m");
                }
                if(bank==0)
                {
                    var visible=Copy(session.transform,null,scene);var home=visible.GetComponentsInChildren<Transform>().Single(t=>t.name=="Village_House_02");
                    var cg=new GameObject("Isolated loot close-up");SceneManager.MoveGameObjectToScene(cg,scene);var camera=cg.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=65;camera.nearClipPlane=.04f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.48f,.63f,.72f);
                    var sun=new GameObject("Isolated inspection sun");SceneManager.MoveGameObjectToScene(sun,scene);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;sun.transform.rotation=Quaternion.Euler(48,-35,0);
                    foreach(var name in new[]{"Dining_Table","Equipment_Rack"})
                    {
                        var support=home.GetComponentsInChildren<Transform>().Single(t=>t.name==name);camera.transform.position=support.position-home.forward*2+home.right*1.7f+Vector3.up*1.3f;camera.transform.LookAt(support.position+Vector3.up*.75f);
                        var rt=RenderTexture.GetTemporary(1280,850,24);var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);var old=RenderTexture.active;
                        try{camera.targetTexture=rt;camera.aspect=(float)rt.width/rt.height;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(folder+name+"-after.png",tex.EncodeToPNG());}
                        finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
                    }
                    visible.gameObject.SetActive(false);Object.DestroyImmediate(visible.gameObject);Object.DestroyImmediate(cg);Object.DestroyImmediate(sun);
                }
            }
            lines.Add("PASS actual VillageWalkPreview.SetBank for both banks: 50 items aligned and fitted. Inactive staging prevents lifecycle/input/save side effects; the unrelated LevelEvent session was not switched.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        Check(!EditorUtility.scriptCompilationFailed,"Compilation errors");File.WriteAllLines(folder+"walk-code-verification.txt",lines);return lines.Last();
    }
}
