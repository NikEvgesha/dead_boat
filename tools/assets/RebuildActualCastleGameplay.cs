using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RebuildActualCastleGameplay
{
 // Initial migration only. With the main assembly unavailable, Unity unpack
 // drops missing-script fields: the subsequent source-field restoration and
 // linked prefab replacement are required before this is a gameplay candidate.
 const string Dir="Assets/Prefabs/Workshop/GameAligned/";
 const string Building="Assets/Prefabs/Locations/Bilding/";
 const string Cat="Assets/Import/CatBorg Studio/3D Dungeon Lowpoly Pack/Models/";
 const string Kay="Assets/Import/KayKit_DungeonRemastered_1.1_FREE/Assets/fbx(unity)/";
 static int meshIndex;
 static Material wood,iron,cloth;
 static readonly System.Text.StringBuilder report=new System.Text.StringBuilder();
 static Transform Group(string n,Transform parent){var t=new GameObject(n).transform;t.SetParent(parent,false);return t;}
 static Material Mat(string n,Color color){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+n+".mat");if(m!=null)return m;m=new Material(Shader.Find("Standard")){name=n,color=color};m.SetFloat("_Glossiness",.15f);AssetDatabase.CreateAsset(m,Dir+n+".mat");return m;}
 static Bounds BoundsOf(GameObject go){var rr=go.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();if(rr.Length==0)throw new Exception("No visual "+go.name);var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static GameObject Box(string n,Transform parent,Vector3 pos,Vector3 size,Material mat,bool solid=true){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 static void Beam(string n,Transform parent,Vector3 a,Vector3 b,float width,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=n;go.transform.SetParent(parent,false);go.transform.localPosition=(a+b)*.5f;go.transform.localScale=new Vector3(width,(a-b).magnitude*.5f,width);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}
 static void Text(string n,string text,Transform parent,Vector3 pos,float size){var go=Group(n,parent);go.localPosition=pos;var tm=go.gameObject.AddComponent<TextMesh>();tm.text=text;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=48;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.92f,.87f,.69f);go.GetComponent<Renderer>().sharedMaterial=tm.font.material;}
 static void BakeProBuilder(GameObject go,string prefix)
 {
  foreach(var pb in go.GetComponentsInChildren<ProBuilderMesh>(true))
  {
   pb.ToMesh();pb.Refresh();var mf=pb.GetComponent<MeshFilter>();if(mf==null||mf.sharedMesh==null)continue;
   var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=prefix+"_"+meshIndex;AssetDatabase.CreateAsset(mesh,Dir+mesh.name+".asset");meshIndex++;mf.sharedMesh=mesh;var col=pb.GetComponent<MeshCollider>();if(col!=null)col.sharedMesh=mesh;
  }
 }
 static void StripPreview(GameObject go,string name)
 {
  var snapshots=new Dictionary<GameObject,Mesh>();
  foreach(var pb in go.GetComponentsInChildren<ProBuilderMesh>(true)){pb.ToMesh();pb.Refresh();var mf=pb.GetComponent<MeshFilter>();if(mf!=null&&mf.sharedMesh!=null){var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=name+"_preview_"+meshIndex++;AssetDatabase.CreateAsset(mesh,Dir+mesh.name+".asset");snapshots[pb.gameObject]=mesh;}}
  foreach(var c in go.GetComponentsInChildren<Canvas>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c.gameObject);
  foreach(var t in go.GetComponentsInChildren<Transform>(true))
  {
   GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
   foreach(var mb in t.GetComponents<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(mb);
   foreach(var c in t.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
   foreach(var c in t.GetComponents<Rigidbody>())UnityEngine.Object.DestroyImmediate(c);
   foreach(var c in t.GetComponents<Animator>())UnityEngine.Object.DestroyImmediate(c);
   t.gameObject.tag="Untagged";t.gameObject.layer=0;
  }
  foreach(var pair in snapshots)if(pair.Key!=null){var mf=pair.Key.GetComponent<MeshFilter>()??pair.Key.AddComponent<MeshFilter>();mf.sharedMesh=pair.Value;}
 }
 static GameObject Prop(string path,string name,Transform parent,Vector3 pos,float size,Material mat=null,float yaw=0,bool solid=false)
 {
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new Exception("Missing "+path);var go=UnityEngine.Object.Instantiate(source);go.name=name;StripPreview(go,name);go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
  var b=BoundsOf(go);go.transform.localScale*=size/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));b=BoundsOf(go);var target=parent.TransformPoint(pos);go.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
  if(mat!=null)foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>mat).ToArray();
  if(solid){var col=go.AddComponent<BoxCollider>();b=BoundsOf(go);col.center=go.transform.InverseTransformPoint(b.center);var v=go.transform.InverseTransformVector(b.size);col.size=new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));}return go;
 }
 static Transform SourceBuilding(Transform house,string sourceName)
 {
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Building+sourceName+".prefab"),house.gameObject.scene);go.transform.SetParent(house,false);go.name="15_Gameplay_Source";go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
  // Keep serialized script GUIDs and references in these source prefab instances, including currently missing game scripts.
  foreach(Transform child in go.transform)
   if(child.name!="StorePoints"&&child.name!="SellPoint")child.gameObject.SetActive(false);
  return go.transform;
 }
 static void ConfigureStore(Transform house,Transform source,Transform room,string[] items,string[] names,bool guns)
 {
  var store=source.Find("StorePoints");store.localPosition=Vector3.zero;store.localRotation=Quaternion.identity;store.localScale=Vector3.one;
  var roomTrigger=store.GetComponent<BoxCollider>();roomTrigger.center=new Vector3(0,1.65f,0);roomTrigger.size=new Vector3(5.7f,2.3f,5.7f);roomTrigger.isTrigger=true;
  var points=store.Cast<Transform>().Where(t=>t.name.StartsWith("storePoint")).ToArray();
  Vector3[] spots=guns?new[]{new Vector3(-1.8f,.48f,-1.2f),new Vector3(-1.8f,.48f,1.35f),new Vector3(1.8f,.48f,-1.2f),new Vector3(1.8f,.48f,1.35f)}:items.Length==2?new[]{new Vector3(1.8f,.48f,-1),new Vector3(1.8f,.48f,1.3f)}:new[]{new Vector3(-2.8f,.48f,1.4f),new Vector3(0,.48f,2.15f),new Vector3(2.8f,.48f,1.4f)};
  var previews=Group("EditorOnly_StockPreview",room);previews.gameObject.tag="EditorOnly";
  for(int i=0;i<points.Length;i++)
  {
   var point=points[i];point.localPosition=spots[i];point.localRotation=Quaternion.identity;point.localScale=Vector3.one;var col=point.GetComponent<BoxCollider>();col.center=new Vector3(0,.95f,0);col.size=new Vector3(1.25f,1.8f,1.35f);col.isTrigger=true;
   foreach(var canvas in point.GetComponentsInChildren<Canvas>(true)){canvas.transform.localPosition=new Vector3(0,1.1f,-.5f);canvas.transform.localRotation=Quaternion.identity;canvas.transform.localScale=Vector3.one*.35f;canvas.gameObject.SetActive(false);}
   var buy=point.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="buyPoint");if(buy!=null){buy.localPosition=new Vector3(0,.5f,-.6f);buy.localRotation=Quaternion.identity;buy.localScale=Vector3.one;}
   var display=point.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="SellPoint");if(display!=null){display.localPosition=new Vector3(0,1,0);display.localRotation=Quaternion.identity;display.localScale=Vector3.one;}
   Box("Merchandise_Stand",room,spots[i]+new Vector3(0,.36f,0),new Vector3(1.1f,.72f,.65f),wood);
   Prop("Assets/Prefabs/Items/"+items[i]+".prefab",names[i],previews,spots[i]+new Vector3(0,.88f,0),guns?1.1f:items[i]=="Fence"?1.4f:.6f);
   Text("Product_Label",names[i],room,spots[i]+new Vector3(0,.45f,-.38f),.065f);
   report.AppendLine(house.name+" stock slot "+i+": "+items[i]+"; source StorePoint retained");
  }
 }
 static void ConfigureSell(Transform source,Vector3 local,Vector3 output,Vector3 size)
 {
  var sell=source.Find("SellPoint");sell.localPosition=local;sell.localRotation=Quaternion.identity;sell.localScale=Vector3.one;var col=sell.GetComponent<BoxCollider>();col.center=new Vector3(0,.85f,0);col.size=size;col.isTrigger=true;
  var money=sell.GetComponentsInChildren<Transform>(true).First(t=>t.name=="MoneyBagPoint");money.position=source.TransformPoint(output);money.localRotation=Quaternion.identity;money.localScale=Vector3.one;
  foreach(var canvas in sell.GetComponentsInChildren<Canvas>(true)){canvas.transform.localPosition=new Vector3(0,1.6f,-.4f);canvas.transform.localRotation=Quaternion.identity;canvas.transform.localScale=Vector3.one*.4f;}
 }
 static void Houses(Transform root)
 {
  string[] ids={"01_Supplies","02_Armory","03_LootTrader","04_Professions","05_Nursery","06_AnimalMerge"};
  string[] sourceNames={"General Store","Fisherman Shark","Trading hut","Guns Store","Sheriffs","Hospital"};
  string[] labels={"ПЯТЁРОЧКА","ДОМ РЫБАКА","ТОРГОВЫЙ ДОМ","ОРУЖЕЙНЫЙ","ШЕРИФ","БОЛЬНИЦА"};
  var houses=root.Find("03_Service_Houses");
  for(int i=0;i<6;i++)
  {
   var h=houses.Find(ids[i]);foreach(string n in new[]{"08_Interior_Props","09_Castle_Guardroom","11_Unique_Interior","12_Service_Interior","14_Interaction_Anchors"}){var old=h.Find(n);if(old!=null)old.gameObject.SetActive(false);}
   h.Find("13_Service_Sign/Service_Name").GetComponent<TextMesh>().text=labels[i];h.Find("13_Service_Sign/Service_Name").GetComponent<TextMesh>().characterSize=.09f;
   var room=Group("16_Run_Interior",h);var source=SourceBuilding(h,sourceNames[i]);
   if(i==0)ConfigureStore(h,source,room,new[]{"coal","Lantern","Fence"},new[]{"Уголь","Фонарь","Ограда"},false);
   if(i==3)ConfigureStore(h,source,room,new[]{"Pistol","Rifle","Shotgun","AmmoMusket"},new[]{"Пистолет","Винтовка","Дробовик","Патроны"},true);
   if(i==5)
   {
    ConfigureStore(h,source,room,new[]{"bandage","Heal"},new[]{"Бинты","Лекарство"},false);
    Prop(Cat+"04_Bedroom_01_Beed_1.fbx","Patient_Bed",room,new Vector3(-1.5f,.48f,.3f),2.15f,wood,0,true);
    Prop(Cat+"04_Bedroom_01_Matrace_ForBeed_1.fbx","Clean_Bedding",room,new Vector3(-1.5f,.87f,.3f),2.1f,cloth);
    Box("Medical_Cross_V",room,new Vector3(0,2.1f,2.66f),new Vector3(.2f,.9f,.045f),cloth,false);Box("Medical_Cross_H",room,new Vector3(0,2.1f,2.66f),new Vector3(.65f,.2f,.045f),cloth,false);
   }
   if(i==2)
   {
    ConfigureSell(source,new Vector3(1.65f,.48f,.45f),new Vector3(-1.5f,1.24f,.5f),new Vector3(1.4f,1.5f,1.8f));
    Box("Loot_Counter",room,new Vector3(1.65f,.88f,.45f),new Vector3(1.2f,.8f,1.8f),wood);Box("Payment_Counter",room,new Vector3(-1.5f,.86f,.5f),new Vector3(1.15f,.76f,1.2f),wood);
    Prop(Kay+"chest.fbx","Treasury_Chest",room,new Vector3(-1.65f,.48f,2),1,iron,0,true);Prop(Cat+"01_Library_01_BigBook#1.fbx","Accounts_Ledger",room,new Vector3(-1.5f,1.25f,.5f),.5f);
    Text("Loot_Hint","СДАЧА НАХОДОК",room,new Vector3(1.65f,1.38f,-.5f),.09f);Text("Payment_Hint","НАГРАДА",room,new Vector3(-1.5f,1.55f,-.14f),.075f);
   }
   if(i==4)
   {
    ConfigureSell(source,new Vector3(-1.65f,.48f,.85f),new Vector3(1.6f,1.24f,.6f),new Vector3(1.7f,1.6f,1.8f));
    for(int j=0;j<6;j++)Box("Cell_Iron_Bar",room,new Vector3(-.65f,1.53f,-.35f+j*.43f),new Vector3(.06f,2.1f,.06f),iron);
    foreach(float y in new[]{.53f,2.5f})Box("Cell_Crossbar",room,new Vector3(-.65f,y,.72f),new Vector3(.08f,.07f,2.3f),iron);
    Prop(Cat+"04_Bedroom_01_Beed_1.fbx","Cell_Bunk",room,new Vector3(-1.7f,.48f,1),1.6f,iron,0,true);
    Box("Bounty_Counter",room,new Vector3(1.6f,.87f,.6f),new Vector3(1.2f,.78f,1.3f),wood);Text("Prison_Hint","ДОСТАВЬ ПИРАТА",room,new Vector3(-1.65f,2.7f,2.61f),.095f);Text("Bounty_Hint","НАГРАДА",room,new Vector3(1.6f,1.55f,-.08f),.075f);
   }
   if(i==1)
   {
    Prop(Kay+"barrel_large.fbx","Fishing_Barrel",room,new Vector3(-2.9f,.48f,1.8f),1,wood,0,true);Prop(Kay+"crates_stacked.fbx","Fishing_Crates",room,new Vector3(2.7f,.48f,1.8f),1.1f,wood,0,true);
    Prop(Kay+"table_small.fbx","Fisher_Table",room,new Vector3(-2.7f,.48f,-1.6f),1.2f,wood,0,true);
    Beam("Fishing_Rod_A",room,new Vector3(3.6f,.48f,-1.4f),new Vector3(3.2f,2.7f,-.7f),.055f,wood);Beam("Fishing_Rod_B",room,new Vector3(3.3f,.48f,-1.4f),new Vector3(2.9f,2.7f,-.7f),.045f,wood);
    Text("Fisher_Source_Note","ДОМ РЫБАКА",room,new Vector3(0,2.2f,2.65f),.095f);
   }
   var light=Group("Run_Interior_Light",room);light.localPosition=new Vector3(0,2.6f,0);var l=light.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=8;l.intensity=.65f;l.color=new Color(.9f,.92f,1);
   report.AppendLine(ids[i]+" => "+sourceNames[i]+" / "+labels[i]+" source="+Building+sourceNames[i]+".prefab");
  }
 }
 static void ChangeBoat(GameObject boat)
 {
  var main=boat.transform.Find("BoardDefault");var pb=main.GetComponent<ProBuilderMesh>();var points=pb.positions.ToArray();
  for(int i=0;i<points.Length;i++)if(points[i].z>3.5f){float t=(points[i].z-3.5f)/3.5f;points[i].x*=Mathf.Lerp(1,.25f,t);points[i].z+=.8f*t*t;points[i].y+=.18f*t;}
  pb.positions=points;pb.ToMesh();pb.Refresh();
  foreach(string name in new[]{"BoardDefault","TrailerDefault"})foreach(var r in boat.transform.Find(name).GetComponents<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>wood).ToArray();
  var trim=Group("Variant_Art_Only",boat.transform);
  foreach(float side in new[]{-1f,1f})
  {
   for(int j=0;j<3;j++)Box("Hull_Timber_Strake",trim,new Vector3(side*3.045f,-.7f+j*.65f,-1.7f),new Vector3(.07f,.13f,10.6f),wood,false);
   for(int j=0;j<3;j++)Box("Trailer_Strake",trim,new Vector3(side*3.15f,-.72f+j*.55f,-13.5f),new Vector3(.06f,.12f,10.5f),wood,false);
   foreach(float z in new[]{-5.5f,-3f})Box("Awning_Post",trim,new Vector3(side*2.1f,2.1f,z),new Vector3(.1f,1.9f,.1f),wood,false);
   Beam("Awning_Eaves",trim,new Vector3(side*2.1f,3.02f,-5.8f),new Vector3(side*2.1f,3.02f,-2.7f),.08f,wood);
   var roof=Box("Canvas_Awning",trim,new Vector3(side*1.05f,3.22f,-4.25f),new Vector3(2.2f,.06f,3.3f),cloth,false);roof.transform.localRotation=Quaternion.Euler(0,0,side<0?12:-12);
  }
  Prop(Kay+"crates_stacked.fbx","Cargo_Crates",trim,new Vector3(-1.9f,.98f,-16.1f),1.4f,wood);Prop(Kay+"barrel_large.fbx","Cargo_Barrel",trim,new Vector3(1.9f,.98f,-14.8f),1.2f,wood);
  Prop(Kay+"chest.fbx","Cargo_Chest",trim,new Vector3(-1.8f,.98f,-11.2f),1.1f,iron);
 }
 static void Boats(Transform root)
 {
  foreach(var t in root.Cast<Transform>().Where(t=>t.name.StartsWith("Boat_")||t.name=="07_Boat_Static_Example").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardController.prefab");
  for(int i=0;i<2;i++)
  {
   var go=(GameObject)PrefabUtility.InstantiatePrefab(source,root.gameObject.scene);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name=i==0?"Board_Game_Reference":"Board_CastleCargo_Variant";go.transform.SetParent(root,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
   if(i==1)ChangeBoat(go);BakeProBuilder(go,go.name);var rb=go.GetComponent<Rigidbody>();if(rb!=null)rb.isKinematic=true;
   PrefabUtility.SaveAsPrefabAsset(go,Dir+go.name+".prefab");go.transform.localPosition=new Vector3(0,0,i==0?-7.6f:20.4f);
   report.AppendLine(go.name+": source BoardController, root position="+go.transform.localPosition+"; control and fuel branches unchanged");
  }
 }
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open Castle Edit Mode");var root=GameObject.Find("Castle_Services_Prototype").transform;if(root.Find("10_GameAlignedSources")!=null||Directory.Exists(Dir))throw new Exception("Already rebuilt");
  Directory.CreateDirectory(Dir);AssetDatabase.Refresh();wood=Mat("Run_Oak",new Color(.38f,.24f,.14f));iron=Mat("Run_Iron",new Color(.22f,.26f,.28f));cloth=Mat("Run_Linen",new Color(.78f,.76f,.64f));
  Houses(root);Boats(root);Group("10_GameAlignedSources",root);Physics.SyncTransforms();PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/game-aligned-migration.txt",report.ToString());
  SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(48,-30,0),43,false,true);SceneView.lastActiveSceneView?.Focus();
 }
}
