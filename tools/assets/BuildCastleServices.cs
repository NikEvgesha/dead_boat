using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildCastleServices
{
 const string Dir="Assets/Prefabs/Workshop/CastleServices/";
 const string Geo="Assets/Prefabs/Workshop/CottageGeometry/";
 const string Kay="Assets/Import/KayKit_DungeonRemastered_1.1_FREE/Assets/fbx(unity)/";
 static Transform root;
 static Material stone, dark, gold, teal;
 static Bounds B(GameObject go){var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static Material Mat(string name,Color c){var m=new Material(Shader.Find("Standard")){name=name,color=c};m.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(m,Dir+name+".mat");return m;}
 static Transform Group(string n,Transform parent){var g=new GameObject(n).transform;g.SetParent(parent,false);return g;}
 static GameObject Box(string n,Transform parent,Vector3 p,Vector3 size,Material m,bool solid=true){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 static void Text(string n,string text,Transform parent,Vector3 p,float size,Color c){var t=Group(n,parent);t.localPosition=p;var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=c;t.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;}
 static GameObject Prop(string path,string n,Transform parent,Vector3 p,float size,Material mat=null)
 {
  var a=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(a==null)throw new Exception(path);
  var go=UnityEngine.Object.Instantiate(a);go.name=n;go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
  foreach(var t in go.GetComponentsInChildren<Transform>(true)){GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);foreach(var mb in t.GetComponents<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(mb);foreach(var col in t.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(col);}
  var b=B(go);go.transform.localScale*=size/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));b=B(go);var target=parent.TransformPoint(p);go.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
  if(mat!=null)foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(x=>mat).ToArray();return go;
 }
 static void Anchor(Transform parent,string n,Vector3 pos){var t=Group(n,parent);t.localPosition=pos;}
 static void SpecialRoom(Transform house,int index)
 {
  var room=Group("12_Service_Interior",house);
  if(index==2)
  {
   foreach(var n in new[]{"Scribe_Bed","Scribe_Bedding"})house.Find("11_Unique_Interior/"+n).gameObject.SetActive(false);
   Prop(Kay+"chest.fbx","Purchased_Loot_Coffer",room,new Vector3(-1.6f,.48f,.2f),1.2f);
   Prop(Kay+"coin_stack_medium.fbx","Counter_Coins",room,new Vector3(1.7f,1.26f,.1f),.22f,gold);
   Prop(Kay+"crates_stacked.fbx","Sorted_Loot",room,new Vector3(-1.8f,.48f,-1.6f),.8f);
  }
  if(index==4||index==5)
  {
   foreach(var n in new[]{"08_Interior_Props","09_Castle_Guardroom"}){var t=house.Find(n);if(t!=null)t.gameObject.SetActive(false);}
   foreach(float x in new[]{-1.8f,1.8f})
   {
    Box(index==4?"Incubation_Plinth":"Merge_Plinth",room,new Vector3(x,.78f,.8f),new Vector3(1.15f,.6f,1.2f),index==4?stone:dark);
    if(index==4)Prop("Assets/_models/ZOO/PrefabEgg/EGG 1.prefab","Display_Egg",room,new Vector3(x,1.09f,.8f),.58f);
    else Prop("Assets/_models/ZOO/PrefabZoo/chicken.prefab","Merge_Animal_Display",room,new Vector3(x,1.09f,.8f),.7f);
   }
   Prop(Kay+"table_small.fbx","Keeper_Desk",room,new Vector3(-1.8f,.48f,-1.6f),1.1f,index==5?dark:null);
   Prop(Kay+"chest.fbx","Egg_Storage",room,new Vector3(1.8f,.48f,-1.6f),.9f,index==5?dark:null);
   Text("Process_Hint",index==4?"ЯЙЦО → ВЫСИЖИВАНИЕ → ПИТОМЕЦ":"ДВА ОДИНАКОВЫХ → УЛУЧШЕНИЕ",room,new Vector3(0,2.65f,2.63f),.07f,Color.white);
   var l=Group("Service_Light",room);l.localPosition=new Vector3(0,2.6f,0);var light=l.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=.9f;light.color=new Color(.8f,.9f,1);
  }
 }
 public static void Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  if(File.Exists("Assets/Scenes/CastleServicesWorkshop.unity")||Directory.Exists(Dir))throw new Exception("Castle services already exist");
  EditorSceneManager.SaveOpenScenes();Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
  root=Group("Castle_Services_Prototype",null);
  stone=Mat("Paving_Gray",new Color(.38f,.42f,.45f));dark=Mat("Sign_Charcoal",new Color(.12f,.17f,.21f));gold=Mat("Service_Gold",new Color(.8f,.64f,.25f));teal=Mat("Heraldry_Teal",new Color(.1f,.35f,.38f));
  var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Castle.prefab");
  var perimeter=UnityEngine.Object.Instantiate(reference.transform.Find("Visual").gameObject);perimeter.name="01_Original_Castle_Perimeter";perimeter.transform.SetParent(root,false);perimeter.transform.localScale*=.45f;
  var b=B(perimeter);perimeter.transform.position+=new Vector3(-b.center.x,-b.min.y,-b.center.z);
  foreach(Transform t in perimeter.transform)if(t.name.StartsWith("pol"))t.gameObject.SetActive(false);
  foreach(var t in perimeter.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
  foreach(var mf in perimeter.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh!=null&&!mf.GetComponent<Collider>()){var mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;}
  var paving=Group("02_Courtyard",root);Box("Solid_Ground",paving,new Vector3(0,-.05f,0),new Vector3(57,.5f,53),stone);
  var avenue=Mat("Avenue_LightStone",new Color(.52f,.54f,.53f));Box("Central_Avenue",paving,new Vector3(0,.205f,0),new Vector3(8,.01f,53),avenue);
  for(int z=-24;z<=24;z+=2)foreach(float x in new[]{-3.7f,3.7f})Box("Avenue_Border",paving,new Vector3(x,.23f,z),new Vector3(.2f,.04f,1.8f),dark,false);
  string[] models={"WoodenWorkshop_Wide_03","StoneArmory_Wide_03","WoodenHall_Tall_02","StoneCaptainHall_Tall_02","WoodenCottage_01","StoneCottage_01"};
  string[] ids={"01_Supplies","02_Armory","03_LootTrader","04_Professions","05_Nursery","06_AnimalMerge"};
  string[] labels={"СНАБЖЕНИЕ","ОРУЖЕЙНАЯ","СКУПЩИК","ПРОФЕССИИ","ПИТОМНИК","ОБЪЕДИНЕНИЕ"};
  string[] adapters={"ItemStore_GeneralStore_BoatShop","ItemStore_GunStore","SellPoint_Items_Enemies","ProfessionNpcPoint","EggNestPoint_AnimalLoadoutPoint","AnimalMergePoint"};
  var houses=Group("03_Service_Houses",root);
  for(int i=0;i<6;i++)
  {
   var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Workshop/"+models[i]+".prefab"),scene);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);go.name=ids[i];go.transform.SetParent(houses,false);go.transform.localPosition=new Vector3(i%2==0?-12:12,.2f,-14+(i/2)*14);go.transform.localRotation=Quaternion.Euler(0,i%2==0?-90:90,0);
   SpecialRoom(go.transform,i);
   var sign=Group("13_Service_Sign",go.transform);float signY=i==2||i==3?4.22f:3.22f;
   Box("Signboard",sign,new Vector3(0,signY,-3.32f),new Vector3(3.35f,.55f,.14f),dark,false);Text("Service_Name",labels[i],sign,new Vector3(0,signY,-3.405f),.115f,new Color(1,.91f,.63f));
   Box("Left_Trim",sign,new Vector3(-1.65f,signY,-3.415f),new Vector3(.06f,.53f,.03f),gold,false);Box("Right_Trim",sign,new Vector3(1.65f,signY,-3.415f),new Vector3(.06f,.53f,.03f),gold,false);
   var anchors=Group("14_Interaction_Anchors",go.transform);Anchor(anchors,"Approach",new Vector3(0,.5f,-4.7f));Anchor(anchors,adapters[i],new Vector3(0,.5f,.25f));Anchor(anchors,"Output_Point",new Vector3(1.5f,.65f,-1.8f));
   Box("Door_Path",paving,new Vector3(i%2==0?-6.5f:6.5f,.211f,-14+(i/2)*14),new Vector3(5,.012f,2.4f),avenue,false);
  }
  var plaza=Group("04_Plaza_Details",root);
  foreach(float z in new[]{-7f,7f})foreach(float x in new[]{-5f,5f})
  {
   Box("Lamp_Pedestal",plaza,new Vector3(x,.4f,z),new Vector3(.5f,.4f,.5f),dark);
   Box("Lamp_Post",plaza,new Vector3(x,1.65f,z),new Vector3(.12f,2.2f,.12f),dark);
   Box("Lamp_Glow",plaza,new Vector3(x,2.85f,z),new Vector3(.33f,.45f,.33f),gold,false);
  }
  foreach(float z in new[]{-7f,7f})foreach(float x in new[]{-18f,18f}){Prop(Kay+"crates_stacked.fbx","Delivery_Crates",plaza,new Vector3(x,.2f,z),1.2f);Prop(Kay+"barrel_large.fbx","Delivery_Barrel",plaza,new Vector3(x+1.5f,.2f,z),1);}
  var camera=Camera.main;camera.transform.position=new Vector3(-46,52,-54);camera.transform.LookAt(new Vector3(0,3,0));camera.fieldOfView=48;camera.farClipPlane=300;camera.backgroundColor=new Color(.18f,.23f,.28f);camera.clearFlags=CameraClearFlags.SolidColor;
  var sun=UnityEngine.Object.FindFirstObjectByType<Light>();sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.intensity=1.15f;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.66f,.72f);
  Polish();AssetDatabase.SaveAssets();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"CastleServices_01.prefab");EditorSceneManager.SaveScene(scene,"Assets/Scenes/CastleServicesWorkshop.unity");
  SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(42,-35,0),40,false,true);
  Capture(camera,"Docs/assets/examples/CastleServicesOverview.png",1800,1350);
 }
 public static void Polish()
 {
  var main=GameObject.Find("Castle_Services_Prototype").transform;var houses=main.Find("03_Service_Houses");
  var darkMat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Sign_Charcoal.mat");var goldMat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Service_Gold.mat");
  var straw=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Nest_Straw.mat")??Mat("Nest_Straw",new Color(.57f,.43f,.24f));
  foreach(string n in new[]{"05_Nursery","06_AnimalMerge"})
  {
   var house=houses.Find(n);var room=house.Find("12_Service_Interior");if(room.Find("Process_Plaque")!=null)continue;
   var chimney=house.Find("06_Details/chimney");if(chimney!=null)chimney.gameObject.SetActive(false);
   var hint=room.Find("Process_Hint");hint.GetComponent<TextMesh>().characterSize=.039f;hint.localPosition=new Vector3(0,2.4f,2.52f);
   Box("Process_Plaque",room,new Vector3(0,2.4f,2.59f),new Vector3(4.9f,.55f,.08f),darkMat,false);
   foreach(var t in room.GetComponentsInChildren<Transform>())if(t.name=="Incubation_Plinth")
   {
    var nest=GameObject.CreatePrimitive(PrimitiveType.Cylinder);nest.name="Straw_Nest";nest.transform.SetParent(room,false);nest.transform.localPosition=new Vector3(t.localPosition.x,1.11f,.8f);nest.transform.localScale=new Vector3(.9f,.07f,.9f);nest.GetComponent<Renderer>().sharedMaterial=straw;UnityEngine.Object.DestroyImmediate(nest.GetComponent<Collider>());
   }
   Prop(Kay+"bottle_A_green.fbx","Keeper_Supplies",room,new Vector3(-1.8f,1.23f,-1.6f),.24f,n=="05_Nursery"?null:goldMat);
   Prop("Assets/Import/CatBorg Studio/3D Dungeon Lowpoly Pack/Models/01_Library_01_BookShelve_1.fbx","Keeper_Shelf",room,new Vector3(-2,.48f,2.1f),1.7f,n=="05_Nursery"?null:darkMat);
  }
  PrefabUtility.SaveAsPrefabAsset(main.gameObject,Dir+"CastleServices_01.prefab");EditorSceneManager.MarkSceneDirty(main.gameObject.scene);if(!string.IsNullOrEmpty(main.gameObject.scene.path))EditorSceneManager.SaveScene(main.gameObject.scene);AssetDatabase.SaveAssets();
 }
 static void Capture(Camera c,string path,int w,int h){var rt=new RenderTexture(w,h,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
}
