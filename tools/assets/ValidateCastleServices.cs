using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class ValidateCastleServices
{
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity")throw new Exception("Wrong scene");
  var root=GameObject.Find("Castle_Services_Prototype");var houses=root.transform.Find("03_Service_Houses");var log=new System.Text.StringBuilder();
  Physics.SyncTransforms();
  foreach(Transform h in houses)
  {
   var test=new GameObject("Temporary_Walk_Probe");var cc=test.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.3f;cc.center=new Vector3(0,.9f,0);cc.skinWidth=.02f;cc.stepOffset=.25f;
   try
   {
    test.transform.position=h.TransformPoint(new Vector3(0,.55f,-6.3f));Physics.SyncTransforms();for(int i=0;i<190;i++)cc.Move(h.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);
    var p=h.InverseTransformPoint(test.transform.position);if(p.z<1.15f)throw new Exception("Door blocked "+h.name+" at "+p);log.AppendLine(h.name+" controller PASS "+p+" grounded="+cc.isGrounded);
   }finally{UnityEngine.Object.DestroyImmediate(test);}
   if(h.Find("14_Interaction_Anchors").childCount!=3)throw new Exception("Anchors missing");
   foreach(var r in h.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||!m.shader.isSupported)throw new Exception("Material "+r.name);
   for(float x=-2.8f;x<=2.8f;x+=.7f)for(float z=-2.8f;z<=2.8f;z+=.7f){RaycastHit hit;if(!Physics.Raycast(h.TransformPoint(new Vector3(x,3,z)),Vector3.up,out hit,7)||!hit.transform.IsChildOf(h))throw new Exception("Roof gap "+h.name);}
   log.AppendLine(h.name+" roof=81 rays PASS; anchors=3; materials PASS");
  }
  if(root.transform.Find("05_RuntimeWaterGap_7m")!=null)
  {
   float expectedFloor=root.transform.Find("06_Territory_Decor")!=null?.25f:.21f;
   foreach(Transform h in houses){float top=h.Find("01_Floor").GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y);if(Mathf.Abs(top-expectedFloor)>.002f)throw new Exception("Wrong raised floor "+h.name);if(h.Find("05_Porch").Cast<Transform>().Any(t=>t.name.Contains("Steps")||t.name.Contains("stairs")))throw new Exception("Stairs remain");}
   if(root.transform.Find("05_River")!=null)throw new Exception("Preview water remains");
   var courtyard=root.transform.Find("02_Courtyard");var west=courtyard.Find("West_Bank").GetComponent<Renderer>().bounds;var east=courtyard.Find("East_Bank").GetComponent<Renderer>().bounds;if(Mathf.Abs(east.min.x-west.max.x-7)>.002f)throw new Exception("Water gap width mismatch");
   for(float z=-24;z<=24;z+=2){RaycastHit hit;if(Physics.Raycast(new Vector3(0,.5f,z),Vector3.down,out hit,2))throw new Exception("Geometry obstructs runtime water "+z);foreach(float x in new[]{-5f,5f})if(!Physics.Raycast(new Vector3(x,2,z),Vector3.down,out hit,4)||Mathf.Abs(hit.point.y-.2f)>.02f)throw new Exception("Bank gap");}
   log.AppendLine("Runtime water gap=7.000; preview river removed; 25 clear gap rays and 50 bank rays PASS; six floors="+expectedFloor);
  }
  else if(root.transform.Find("05_River")!=null)
  {
   foreach(Transform h in houses){var floorTop=h.Find("01_Floor").GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y);if(Mathf.Abs(floorTop-.2f)>.002f)throw new Exception("Floating floor "+h.name);if(h.Find("05_Porch").Cast<Transform>().Any(t=>t.name.Contains("Steps")||t.name.Contains("stairs")))throw new Exception("Stairs remain");}
   for(float z=-24;z<=24;z+=2){RaycastHit hit;if(!Physics.Raycast(new Vector3(0,2,z),Vector3.down,out hit,5)||hit.transform.name!="Riverbed")throw new Exception("Solid road under water "+z);foreach(float x in new[]{-5f,5f})if(!Physics.Raycast(new Vector3(x,2,z),Vector3.down,out hit,4)||Mathf.Abs(hit.point.y-.2f)>.02f)throw new Exception("Bank gap");}
   if(root.transform.Find("05_River/Water_Surface").GetComponent<Collider>()!=null)throw new Exception("Solid water");
   log.AppendLine("River: no solid road, 25 bed rays PASS; both banks 50 rays PASS; six floors=0.200; no stairs");
  }
  else
  {
   var probe=new GameObject("Temporary_Avenue_Probe");var character=probe.AddComponent<CharacterController>();character.height=1.8f;character.radius=.3f;character.center=new Vector3(0,.9f,0);character.skinWidth=.02f;
   try{probe.transform.position=new Vector3(0,.25f,-27);Physics.SyncTransforms();for(int i=0;i<1350;i++)character.Move(new Vector3(0,-.025f,.04f));log.AppendLine("Central avenue: "+probe.transform.position);if(probe.transform.position.z<25)throw new Exception("Avenue/gate blocked");}finally{UnityEngine.Object.DestroyImmediate(probe);}
  }
  foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
  var decor=root.transform.Find("06_Territory_Decor");
  if(decor!=null)
  {
   foreach(var r in decor.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||!m.shader.isSupported)throw new Exception("Decor material "+r.name);
   string[] boats=root.transform.Find("Boat_MedievalTrader_03")!=null?new[]{"Boat_Rustic_01","Boat_MedievalTrader_03"}:root.transform.Find("Boat_Rustic_01")!=null?new[]{"Boat_Rustic_01","Boat_Modern_02"}:new[]{"07_Boat_Static_Example"};
   foreach(string name in boats){var boat=root.transform.Find(name);if(boat==null||boat.GetComponentsInChildren<MonoBehaviour>(true).Length!=0||boat.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new Exception("Boat must be static scenery");
    var rr=boat.GetComponentsInChildren<Renderer>();var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);if(bounds.min.x<=-3.5f||bounds.max.x>=3.5f)throw new Exception("Boat bank collision");foreach(var r in rr)foreach(var m in r.sharedMaterials)if(m==null||!m.shader.isSupported)throw new Exception("Boat material");log.AppendLine(name+" static; fits 7m gap; size="+bounds.size);}
   log.AppendLine("Decor: "+decor.childCount+" objects; materials PASS");
   var greenery=root.transform.Find("08_Distributed_Greenery");if(greenery!=null){foreach(var r in greenery.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m==null||!m.shader.isSupported)throw new Exception("Greenery material");if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="lantern"||t.name.StartsWith("Lamp_")))throw new Exception("Outdoor lantern remains");if(root.transform.Find("Boat_MedievalTrader_03")!=null&&greenery.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Low_Shrub"||t.name=="Grass_Island"))throw new Exception("Round greenery remains");log.AppendLine("Greenery: "+greenery.childCount+" distributed groups; supported materials; outdoor lanterns removed");}
  }
  var camera=Camera.main;camera.transform.position=new Vector3(-49,62,-58);camera.transform.LookAt(new Vector3(0,4,0));Capture(camera,"Docs/assets/examples/CastleServicesOverview.png",1800,1350);
  foreach(var name in new[]{"03_LootTrader","05_Nursery","06_AnimalMerge"}){var h=houses.Find(name);camera.transform.position=h.TransformPoint(new Vector3(0,2.1f,-2.4f));camera.transform.LookAt(h.TransformPoint(new Vector3(0,1.4f,1.2f)));camera.fieldOfView=78;Capture(camera,"Docs/assets/examples/Castle_"+name+"_Interior.png",1200,850);}
  camera.fieldOfView=48;camera.transform.position=new Vector3(-49,62,-58);camera.transform.LookAt(new Vector3(0,4,0));
  PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Temp/castle-services-validation.txt",log.ToString());
 }
 static void Capture(Camera c,string path,int w,int h){var rt=new RenderTexture(w,h,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
}
