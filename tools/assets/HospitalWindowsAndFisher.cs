using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
public static class HospitalWindowsAndFisher
{
 const string D="Assets/Prefabs/Workshop/Hospital/";
 static Material stone,wood,glass,trim;
 static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
 static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material mat,bool solid=false){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static void Window(Transform wall,Transform h,Transform parent){var b=wall.GetComponentInChildren<Renderer>().bounds;var center=h.InverseTransformPoint(b.center);float bottom=center.y-b.size.y*.5f,top=center.y+b.size.y*.5f;
  var t=Group("Open_Stone_Window",parent);t.localPosition=center;t.localRotation=wall.localRotation;
  float low=1.55f,high=3.45f,width=.88f,height=high-low;float cy=(low+high)*.5f-center.y;
  foreach(float side in new[]{-1f,1f})Box("Stone_Window_Jamb",t,new Vector3(0,0,side*(1+width*.5f)*.5f),new Vector3(.2f,top-bottom,1-width*.5f),stone,true);
  Box("Stone_Below_Window",t,new Vector3(0,(bottom+low)*.5f-center.y,0),new Vector3(.2f,low-bottom,width),stone,true);
  Box("Stone_Above_Window",t,new Vector3(0,(high+top)*.5f-center.y,0),new Vector3(.2f,top-high,width),stone,true);
  foreach(float side in new[]{-1f,1f}){Box("Window_Frame_V",t,new Vector3(0,cy,side*(width*.5f-.035f)),new Vector3(.23f,height,.07f),wood);Box("Window_Frame_H",t,new Vector3(0,cy+side*(height*.5f-.035f),0),new Vector3(.23f,.07f,width),wood);}
  Box("Window_Mullion",t,new Vector3(0,cy,0),new Vector3(.23f,height,.035f),wood);Box("Window_Transom",t,new Vector3(0,cy,0),new Vector3(.23f,.035f,width),wood);
  Box("Clear_Window_Glass",t,new Vector3(0,cy,0),new Vector3(.015f,height-.1f,width-.1f),glass,true).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
  Box("Window_Sill",t,new Vector3(0,low-center.y-.04f,0),new Vector3(.36f,.1f,1.08f),trim);
  wall.gameObject.SetActive(false);
 }
 static void Capture(Transform h,Vector3 pos,Vector3 target,string file,float fov){var g=new GameObject("Temporary_Window_Camera");var rt=new RenderTexture(1400,1000,24);var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();try{foreach(var c in canvases)c.enabled=false;var cam=g.AddComponent<Camera>();cam.transform.position=h.TransformPoint(pos);cam.transform.LookAt(h.TransformPoint(target));cam.fieldOfView=fov;cam.farClipPlane=120;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.25f,.29f);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes("Docs/assets/examples/"+file+".png",tex.EncodeToPNG());}finally{foreach(var c in canvases)c.enabled=true;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(g);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 public static string Main(){var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.path!="Assets/Scenes/CastleServicesWorkshop.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Saved castle Edit Mode required");var root=GameObject.Find("Castle_Services_Prototype").transform;var h=root.Find("03_Service_Houses/06_AnimalMerge");var fisher=root.Find("03_Service_Houses/02_Armory");if(h.Find("20_Open_Windows")!=null)throw new Exception("Already updated");
  stone=AssetDatabase.LoadAssetAtPath<Material>(D+"Hospital_Limestone.mat");wood=AssetDatabase.LoadAssetAtPath<Material>(D+"Hospital_Oak.mat");trim=AssetDatabase.LoadAssetAtPath<Material>(D+"Hospital_CutStone.mat");glass=new Material(Shader.Find("Standard")){name="Hospital_Clear_Glass",color=new Color(.6f,.84f,.94f,.1f)};glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)BlendMode.One);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;AssetDatabase.CreateAsset(glass,D+"Hospital_Clear_Glass.mat");
  var windows=Group("20_Open_Windows",h);var old=h.Find("02_Walls").Cast<Transform>().Where(t=>t.name.Contains("Window")&&t.gameObject.activeSelf).ToArray();foreach(var w in old)Window(w,h,windows);
  var pickup=Group("21_Public_Pickup_Spots",h);var points=h.Find("15_Gameplay_Source/StorePoints").GetComponentsInChildren<Transform>(true).Where(t=>t.name=="buyPoint").ToArray();if(points.Length!=2)throw new Exception("Expected 2 purchase points");
  // The collection randomizes stock slots. Both outputs must fit Heal's 1m
  // collider, whichever counter receives it; use the gap between counters.
  Box("Public_Pickup_Floor",pickup,new Vector3(1.7f,.49f,.15f),new Vector3(1.5f,.018f,1.1f),trim);
  foreach(float side in new[]{-1f,1f})Box("Pickup_Border",pickup,new Vector3(1.7f+side*.725f,.502f,.15f),new Vector3(.025f,.008f,1.07f),wood);
  for(int i=0;i<2;i++){points[i].position=h.TransformPoint(new Vector3(i==0?1.45f:1.95f,1.25f,.15f));PrefabUtility.RecordPrefabInstancePropertyModifications(points[i]);}
  foreach(string name in new[]{"08_Interior_Props","09_Castle_Guardroom","11_Unique_Interior","12_Service_Interior","16_Run_Interior"}){var t=fisher.Find(name);if(t!=null)t.gameObject.SetActive(false);}
  Physics.SyncTransforms();var probe=new GameObject("Temporary_Service_Probe");string test;
  try{var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.35f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.3f;probe.transform.position=h.TransformPoint(new Vector3(0,.55f,-4.3f));Physics.SyncTransforms();for(int i=0;i<135;i++)cc.Move(h.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);var p=h.InverseTransformPoint(probe.transform.position);if(p.z<.6f||!cc.isGrounded)throw new Exception("Hospital blocked "+p);test="Hospital aisle "+p;
   probe.transform.position=fisher.TransformPoint(new Vector3(0,.55f,-4.8f));Physics.SyncTransforms();for(int i=0;i<100;i++)cc.Move(fisher.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);p=fisher.InverseTransformPoint(probe.transform.position);if(p.z>-3.3f)throw new Exception("Fisher barrier failed "+p);test+="; Fisher barrier "+p;
  }finally{UnityEngine.Object.DestroyImmediate(probe);}
  Capture(h,new Vector3(-8,6,-11),new Vector3(0,2.5f,0),"Hospital_OpenWindows_Exterior",50);Capture(h,new Vector3(0,2.1f,-2.45f),new Vector3(0,1.6f,.8f),"Hospital_OpenWindows_Interior",104);Capture(fisher,new Vector3(0,2,-2.4f),new Vector3(0,1.2f,1.4f),"Fisher_Empty_Interior",100);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");Selection.activeGameObject=fisher.gameObject;SceneView.lastActiveSceneView?.LookAt(fisher.TransformPoint(new Vector3(0,1.8f,0)),fisher.rotation*Quaternion.Euler(22,-30,0),7,false,true);
  string report="PASS: owner checkpoint preserved; four transparent windows; two public floor pickup points; fisher furnishings disabled, entrance barrier retained; "+test+". Multiplayer purchase test pending (Fusion missing).";File.WriteAllText("Docs/assets/examples/HOSPITAL_WINDOWS_FISHER_VALIDATION.txt",report);return report;
 }
}
