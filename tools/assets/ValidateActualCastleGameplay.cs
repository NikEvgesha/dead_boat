using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class ValidateActualCastleGameplay
{
 static void Assert(bool ok,string text){if(!ok)throw new Exception(text);}
 static Bounds B(GameObject g){var rr=g.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();Assert(scene.path=="Assets/Scenes/CastleServicesWorkshop.unity","Wrong scene");var root=GameObject.Find("Castle_Services_Prototype").transform;var report=new System.Text.StringBuilder();
  var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardController.prefab"));source.transform.position=new Vector3(200,0,0);
  try
  {
   foreach(string n in new[]{"Board_Game_Reference","Board_CastleCargo_Variant"})
   {
    var boat=root.Find(n);Assert(boat!=null,"Missing "+n);
    foreach(string anchorName in new[]{"Point","Fire","steering (1)"})
    {
     var a=source.GetComponentsInChildren<Transform>(true).First(t=>t.name==anchorName);string path=a.name;for(var parent=a.parent;parent!=source.transform;parent=parent.parent)path=parent.name+"/"+path;var b=boat.Find(path);Assert(b!=null,"Anchor missing "+path);
     var p=source.transform.InverseTransformPoint(a.position);Assert(Vector3.Distance(p,boat.InverseTransformPoint(b.position))<.0001f,"Moved anchor "+n+"/"+path);
     Assert(Quaternion.Angle(Quaternion.Inverse(source.transform.rotation)*a.rotation,Quaternion.Inverse(boat.rotation)*b.rotation)<.001f,"Rotated anchor");Assert(Vector3.Distance(a.lossyScale,b.lossyScale)<.0001f,"Scaled anchor");
     var ac=a.GetComponent<BoxCollider>();var bc=b.GetComponent<BoxCollider>();if(ac!=null){Assert(bc!=null&&ac.isTrigger==bc.isTrigger&&Vector3.Distance(ac.center,bc.center)<.0001f&&Vector3.Distance(ac.size,bc.size)<.0001f,"Changed trigger geometry");}
     report.AppendLine(n+" "+path+" exact root position="+p+"; rotation/scale/collider PASS");
    }
    foreach(var mf in boat.GetComponentsInChildren<MeshFilter>())Assert(mf.sharedMesh!=null,"Missing boat mesh "+mf.name);
    var main=boat.Find("BoardDefault").GetComponent<Renderer>().bounds;Assert(main.min.x>=-3.5f&&main.max.x<=3.5f,"Boat too wide");
   }
  }finally{UnityEngine.Object.DestroyImmediate(source);}
  Assert(!root.Cast<Transform>().Any(t=>t.name.StartsWith("Boat_")),"Old demo boats remain");
  Physics.SyncTransforms();
  string[] originalSources={"General Store","Fisherman Shark","Trading hut","Guns Store","Sheriffs","Hospital"};int i=0;
  foreach(Transform h in root.Find("03_Service_Houses"))
  {
   var runtime=h.Find("15_Gameplay_Source");Assert(runtime!=null,"Source missing "+h.name);string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(runtime.gameObject);Assert(prefab=="Assets/Prefabs/Locations/Bilding/"+originalSources[i]+".prefab","Source mismatch "+prefab);
   if(runtime.Find("StorePoints")!=null){var store=runtime.Find("StorePoints");int points=store.Cast<Transform>().Count(t=>t.name.StartsWith("storePoint"));Assert(points==(i==0?3:i==3?4:2),"Shop points count");foreach(var c in store.GetComponentsInChildren<BoxCollider>())Assert(c.isTrigger,"Shop collider not trigger");report.AppendLine(h.name+" source="+prefab+"; shop slots="+points);}
   else if(runtime.Find("SellPoint")!=null){var sell=runtime.Find("SellPoint");Assert(sell.GetComponent<BoxCollider>().isTrigger,"Sell trigger missing");Assert(sell.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="MoneyBagPoint"),"Payment point missing");report.AppendLine(h.name+" source="+prefab+"; sale/payment references retained");}
   else report.AppendLine(h.name+" source="+prefab+"; decorative Fisherman in original game");
   var probe=new GameObject("Temporary_Door_Probe");var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.3f;cc.center=new Vector3(0,.9f,0);cc.skinWidth=.02f;cc.stepOffset=.25f;
   try{probe.transform.position=h.TransformPoint(new Vector3(0,.55f,-6.3f));Physics.SyncTransforms();for(int j=0;j<190;j++)cc.Move(h.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);var p=h.InverseTransformPoint(probe.transform.position);Assert(p.z>1.15f&&cc.isGrounded,"Door blocked "+h.name+" "+p);report.AppendLine(h.name+" controller PASS "+p);}finally{UnityEngine.Object.DestroyImmediate(probe);}
   i++;
  }
  report.AppendLine("Game scripts preserved as source prefab GUIDs; Play Mode transactions unverified: local Fusion SDK missing.");File.WriteAllText("Temp/actual-castle-validation.txt",report.ToString());
  // Temporarily hide world UI while rendering; retain its original state in the actual prefabs/scene.
  var canvases=root.GetComponentsInChildren<Canvas>(true);var enabled=canvases.Select(c=>c.enabled).ToArray();var camera=Camera.main;
  var boatLights=new[]{root.Find("Board_Game_Reference"),root.Find("Board_CastleCargo_Variant")}.SelectMany(t=>t.GetComponentsInChildren<Light>(true)).ToArray();var lightEnabled=boatLights.Select(l=>l.enabled).ToArray();
  try
  {
   foreach(var c in canvases)c.enabled=false;foreach(var l in boatLights)l.enabled=false;camera.transform.position=new Vector3(-50,65,-61);camera.transform.LookAt(new Vector3(0,3,0));camera.fieldOfView=49;Capture(camera,"Docs/assets/examples/CastleServicesOverview.png",1800,1350);
   foreach(string name in new[]{"Board_Game_Reference","Board_CastleCargo_Variant"})
   {
    var boat=root.Find(name);var all=boat.GetComponentsInChildren<Transform>(true);var layers=all.Select(t=>t.gameObject.layer).ToArray();
    try{foreach(var t in all)t.gameObject.layer=30;camera.cullingMask=1<<30;var b=B(boat.gameObject);camera.transform.position=b.center+new Vector3(16,18,-23);camera.transform.LookAt(b.center);camera.fieldOfView=48;Capture(camera,"Docs/assets/examples/"+name+".png",1400,1050);}finally{for(int j=0;j<all.Length;j++)all[j].gameObject.layer=layers[j];camera.cullingMask=~0;}
   }
   foreach(string house in new[]{"01_Supplies","04_Professions","05_Nursery","06_AnimalMerge"}){var h=root.Find("03_Service_Houses/"+house);camera.transform.position=h.TransformPoint(new Vector3(0,2.05f,-2.6f));camera.transform.LookAt(h.TransformPoint(new Vector3(0,1.4f,1.3f)));camera.fieldOfView=82;Capture(camera,"Docs/assets/examples/RunHouse_"+house+".png",1200,850);}
  }
  finally{for(int j=0;j<canvases.Length;j++)canvases[j].enabled=enabled[j];for(int j=0;j<boatLights.Length;j++)boatLights[j].enabled=lightEnabled[j];camera.transform.position=new Vector3(-50,65,-61);camera.transform.LookAt(new Vector3(0,3,0));camera.fieldOfView=49;camera.cullingMask=~0;}
  EditorSceneManager.SaveScene(scene);SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(48,-30,0),43,false,true);SceneView.lastActiveSceneView?.Focus();
 }
 static void Capture(Camera c,string path,int w,int h){var rt=new RenderTexture(w,h,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
}
