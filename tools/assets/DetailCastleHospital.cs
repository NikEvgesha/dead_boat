using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DetailCastleHospital
{
 const string D="Assets/Prefabs/Workshop/Hospital/";
 const string Kay="Assets/Import/KayKit_DungeonRemastered_1.1_FREE/Assets/fbx(unity)/";
 static Material stone,trim,teal,wood,linen,red,iron;
 static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
 static Material Mat(string n,Color c){var m=new Material(Shader.Find("Standard")){name=n,color=c};m.SetFloat("_Glossiness",.18f);AssetDatabase.CreateAsset(m,D+n+".mat");return m;}
 static GameObject Box(string n,Transform p,Vector3 v,Vector3 sz,Material m,bool solid=false){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=v;g.transform.localScale=sz;g.GetComponent<Renderer>().sharedMaterial=m;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static void Cross(Transform p,Vector3 v,float sz){Box("Cross_V",p,v,new Vector3(sz*.25f,sz,.055f),red);Box("Cross_H",p,v,new Vector3(sz,sz*.25f,.06f),red);}
 static Bounds B(GameObject g){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Prop(string f,string n,Transform p,Vector3 v,float height,float yaw=0){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kay+f+".fbx"));g.name=n;g.transform.SetParent(p,false);g.transform.localRotation=Quaternion.Euler(0,yaw,0);var b=B(g);g.transform.localScale*=height/b.size.y;b=B(g);var dest=p.TransformPoint(v);g.transform.position+=new Vector3(dest.x-b.center.x,dest.y-b.min.y,dest.z-b.center.z);}
 static void Bed(Transform p,float z,int i){var t=Group("Patient_Bed_"+i,p);t.localPosition=new Vector3(-1.63f,0,z);
  Box("Bed_Frame",t,new Vector3(0,.82f,0),new Vector3(1.88f,.18f,.92f),wood,true);
  Box("Mattress",t,new Vector3(0,.99f,0),new Vector3(1.79f,.2f,.84f),linen);
  Box("Teal_Blanket",t,new Vector3(.28f,1.105f,0),new Vector3(1.15f,.035f,.86f),teal);
  Box("Pillow",t,new Vector3(-.6f,1.13f,0),new Vector3(.36f,.16f,.64f),linen);
  foreach(float x in new[]{-.94f,.94f}){Box("Bed_End",t,new Vector3(x,1.04f,0),new Vector3(.08f,.62f,.95f),wood);foreach(float zz in new[]{-.45f,.45f}){Box("Bed_Post",t,new Vector3(x,.93f,zz),new Vector3(.105f,.9f,.105f),wood);Box("Brass_Post_Cap",t,new Vector3(x,1.4f,zz),new Vector3(.13f,.08f,.13f),trim);}}
 }
 static void Capture(Transform h,Vector3 pos,Vector3 target,string name,float fov){var g=new GameObject("Temporary_Hospital_Camera");var rt=new RenderTexture(1500,1100,24);var tex=new Texture2D(1500,1100,TextureFormat.RGB24,false);var old=RenderTexture.active;var cs=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();try{foreach(var c in cs)c.enabled=false;var cam=g.AddComponent<Camera>();cam.transform.position=h.TransformPoint(pos);cam.transform.LookAt(h.TransformPoint(target));cam.fieldOfView=fov;cam.farClipPlane=100;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.25f,.29f);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1500,1100),0,0);tex.Apply();File.WriteAllBytes("Docs/assets/examples/"+name+".png",tex.EncodeToPNG());}finally{foreach(var c in cs)c.enabled=true;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(g);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 public static string Main(){var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");
  if(s.isDirty&&!EditorSceneManager.SaveScene(s))throw new Exception("Owner save failed");
  var h=GameObject.Find("Castle_Services_Prototype").transform.Find("03_Service_Houses/06_AnimalMerge");if(h.Find("19_Hospital_Detail")!=null)throw new Exception("Already detailed");
  Directory.CreateDirectory("Library/WorkshopBackups/DB-ON-077");File.Copy(s.path,"Library/WorkshopBackups/DB-ON-077/OwnerSaved.unity",true);
  Directory.CreateDirectory(D);AssetDatabase.Refresh();stone=Mat("Hospital_Limestone",new Color(.64f,.68f,.65f));trim=Mat("Hospital_CutStone",new Color(.83f,.8f,.67f));teal=Mat("Hospital_Teal",new Color(.13f,.34f,.34f));wood=Mat("Hospital_Oak",new Color(.32f,.24f,.16f));linen=Mat("Hospital_Linen",new Color(.91f,.88f,.77f));red=Mat("Hospital_Cross",new Color(.65f,.12f,.09f));iron=Mat("Hospital_Iron",new Color(.18f,.23f,.24f));
  float rise=1.45f,ratio=(2.8f+rise)/2.8f;
  foreach(Transform w in h.Find("02_Walls")){if(!w.gameObject.activeSelf)continue;if(w.name=="Door_Lintel")w.localScale=new Vector3(w.localScale.x,.55f+rise,w.localScale.z);else w.localScale=new Vector3(w.localScale.x,w.localScale.y*ratio,w.localScale.z);}
  foreach(Transform p in h.Find("03_Frame"))p.localScale+=new Vector3(0,rise,0);
  h.Find("04_Roof").localPosition+=Vector3.up*rise;
  var masonry=h.Find("10_Castle_Masonry");masonry.localScale=new Vector3(1,ratio,1);masonry.localPosition+=Vector3.up*(.48f*(1-ratio));
  foreach(string n in new[]{"02_Walls","03_Frame","10_Castle_Masonry"})foreach(var r in h.Find(n).GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>n=="03_Frame"?trim:stone).ToArray();
  foreach(var r in h.Find("04_Roof").GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>teal).ToArray();
  var sign=h.Find("13_Service_Sign");sign.localPosition+=Vector3.up*.8f;sign.Find("Signboard").GetComponent<Renderer>().sharedMaterial=teal;
  var room=h.Find("16_Run_Interior");foreach(Transform t in room)if(t.name=="Patient_Bed"||t.name=="Clean_Bedding"||t.name.StartsWith("Medical_Cross"))t.gameObject.SetActive(false);
  var d=Group("19_Hospital_Detail",h);var outside=Group("Exterior",d);var inside=Group("Interior",d);
  Box("Entrance_Cross_Plaque",outside,new Vector3(0,4.87f,-3.22f),new Vector3(1.18f,1.28f,.12f),linen);Cross(outside,new Vector3(0,4.87f,-3.32f),.88f);
  for(int row=0;row<10;row++)foreach(float x in new[]{-2.92f,2.92f})foreach(float z in new[]{-2.92f,2.92f})Box("Corner_Quoin",outside,new Vector3(x,.7f+row*.42f,z),new Vector3(.42f,.32f,.42f),row%2==0?trim:stone);
  foreach(float side in new[]{-1f,1f}){Box("Cornice",outside,new Vector3(0,4.68f,side*3.02f),new Vector3(6.18f,.18f,.22f),trim);Box("Side_Cornice",outside,new Vector3(side*3.02f,4.68f,0),new Vector3(.22f,.18f,6.18f),trim);}
  for(int i=0;i<3;i++)Bed(inside,-1.3f+i*1.35f,i+1);
  foreach(float x in new[]{-1.8f,1.85f})Prop("chair","Visitor_Chair",inside,new Vector3(x,.48f,-2.25f),.94f,180);
  Box("Rear_Wainscot",inside,new Vector3(0,.9f,2.64f),new Vector3(5.15f,.8f,.08f),teal);Box("Rear_Trim",inside,new Vector3(0,1.34f,2.58f),new Vector3(5.15f,.055f,.06f),trim);
  Box("Wall_Cross_Plaque",inside,new Vector3(-1.5f,2.55f,2.62f),new Vector3(.95f,1.12f,.07f),linen);Cross(inside,new Vector3(-1.5f,2.55f,2.55f),.73f);
  var shelves=Group("Medicine_Shelves",inside);shelves.localPosition=new Vector3(1.35f,0,2.35f);
  foreach(float x in new[]{-.98f,.98f})Box("Shelf_Upright",shelves,new Vector3(x,1.7f,0),new Vector3(.09f,2.35f,.48f),wood,true);
  Box("Shelf_Back",shelves,new Vector3(0,1.72f,.2f),new Vector3(2,2.4f,.055f),teal);
  for(int row=0;row<4;row++){float y=.65f+row*.65f;Box("Apothecary_Shelf",shelves,new Vector3(0,y,0),new Vector3(2.1f,.09f,.5f),wood,true);for(int col=0;col<6;col++)Prop(col%2==0?"bottle_A_labeled_green":"bottle_C_brown","Medicine_Bottle",shelves,new Vector3(-.8f+col*.32f,y+.05f,-.04f),.26f+(col%2)*.08f);}
  foreach(float z in new[]{-1f,1.3f})Box("Counter_Teal_Front",inside,new Vector3(1.8f,.85f,z-.34f),new Vector3(1,.58f,.03f),teal);
  foreach(float side in new[]{-1f,1f}){Box("Side_Wainscot",inside,new Vector3(side*2.64f,.9f,0),new Vector3(.05f,.8f,5.15f),teal);Box("Side_Trim",inside,new Vector3(side*2.64f,1.34f,0),new Vector3(.05f,.055f,5.15f),trim);}
  for(int i=0;i<4;i++){var roll=GameObject.CreatePrimitive(PrimitiveType.Cylinder);roll.name="Spare_Bandage_Roll";roll.transform.SetParent(inside,false);roll.transform.localPosition=new Vector3(1.49f+i*.2f,1.25f,-1);roll.transform.localRotation=Quaternion.Euler(0,0,90);roll.transform.localScale=new Vector3(.16f,.075f,.16f);roll.GetComponent<Renderer>().sharedMaterial=linen;UnityEngine.Object.DestroyImmediate(roll.GetComponent<Collider>());}
  var light=Group("Hospital_Warm_Light",inside).gameObject.AddComponent<Light>();light.transform.localPosition=new Vector3(0,3.4f,0);light.type=LightType.Point;light.range=8;light.intensity=1.3f;light.color=new Color(1,.91f,.75f);
  Physics.SyncTransforms();var probe=new GameObject("Temporary_Hospital_Probe");Vector3 end;
  try{var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.35f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.3f;probe.transform.position=h.TransformPoint(new Vector3(0,.55f,-4.3f));Physics.SyncTransforms();for(int i=0;i<135;i++)cc.Move(h.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);end=h.InverseTransformPoint(probe.transform.position);if(end.z<.6f||!cc.isGrounded)throw new Exception("Hospital aisle blocked "+end);}finally{UnityEngine.Object.DestroyImmediate(probe);}
  Capture(h,new Vector3(-8,7,-12),new Vector3(0,2.8f,0),"Hospital_Detailed_Exterior",48);
  Capture(h,new Vector3(0,2.1f,-2.45f),new Vector3(0,1.5f,.8f),"Hospital_Detailed_Interior",104);
  Capture(h,new Vector3(.4f,2.2f,1.9f),new Vector3(0,1.1f,-2.2f),"Hospital_Detailed_Visitors",103);
  Selection.activeGameObject=h.gameObject;SceneView.lastActiveSceneView?.LookAt(h.TransformPoint(new Vector3(0,2.7f,0)),h.rotation*Quaternion.Euler(23,-30,0),8,false,true);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");
  string report="PASS: hospital only; walls +1.45m; 3 beds; 2 visitor chairs; 24 medicine bottles; exterior/interior crosses; original bandage/Heal source and points retained without unpack. Player-sized central aisle: "+end+". Scene saved; gameplay purchases untested (Fusion unavailable).";File.WriteAllText("Docs/assets/examples/HOSPITAL_VALIDATION.txt",report);return report;
 }
}
