using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class VerifyHouse130{
 public static string Main(){
 AssetDatabase.ImportAsset("Assets/Prefabs/Locations/Enemy/house1_1.prefab",ImportAssetOptions.ForceSynchronousImport);
 var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/house1_1.prefab");
 var preview=new PreviewRenderUtility();
 try{
 var o=(GameObject)PrefabUtility.InstantiatePrefab(p,preview.camera.scene);preview.AddSingleGO(o);
 var t=o.GetComponentsInChildren<Transform>().Single(x=>x.name=="Stone_House_Open_Entrance");
 if(o.GetComponentsInChildren<Transform>().Any(x=>x.name=="GunStore (1)"))throw new Exception("Old house active");
 var filters=t.GetComponentsInChildren<MeshFilter>();var roof=filters.Single(x=>x.name=="Slate_Tiles");
 var verts=roof.sharedMesh.vertices;
 var left=verts.Count(v=>v.x<-.3f);var right=verts.Count(v=>v.x>.3f);if(left<500||right<500||Math.Abs(left-right)>100)throw new Exception("Unequal tile coverage");
 var cols=t.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
 Physics.SyncTransforms();
 for(int i=-1;i<=1;i++){
 var ray=new Ray(t.TransformPoint(new Vector3(i*.18f,1.45f,-6.5f)),t.forward);
 foreach(var c in cols)if(c.Raycast(ray,out var hit,5.5f))throw new Exception("Entrance blocked by "+c.name);
 }
 var items=o.transform.Find("SpawnItem");var enemies=o.transform.Find("SpawnEnemy");
 if(items.childCount!=4||enemies.childCount!=5)throw new Exception("Unexpected point counts");
 var rs=o.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
 var cam=preview.camera;cam.orthographic=true;cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.52f;cam.nearClipPlane=.1f;cam.farClipPlane=1000;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.16f,.19f);preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(48,35,0);preview.lights[1].intensity=.6f;
 foreach(var sign in new[]{1,-1}){
 cam.transform.position=bounds.center+new Vector3(sign*19,16,-14);cam.transform.LookAt(bounds.center);
 preview.BeginStaticPreview(new Rect(0,0,1000,720));cam.Render();var tex=preview.EndStaticPreview();
 File.WriteAllBytes("Docs/locations/previews/house1-open-"+(sign==1?"front":"back")+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
 }
 return "PASS: old geometry disabled; open entrance (three collider rays); roof tiles both slopes "+left+"/"+right+"; 4 loot and 5 enemy points; two renders saved. Bounds "+bounds;
 }finally{preview.Cleanup();}
 }
}
