using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class ImproveCastleBoatsAndGreenery
{
 const string Dir="Assets/Prefabs/Workshop/Boats/";
 const string Village="Assets/Import/3D Cartoon Village/Prefabs/";
 static Material Mat(string n,Color c){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+n+".mat");if(m!=null)return m;m=new Material(Shader.Find("Standard")){name=n,color=c};m.SetFloat("_Glossiness",.3f);AssetDatabase.CreateAsset(m,Dir+n+".mat");return m;}
 static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
 static Bounds B(GameObject go){var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static GameObject Primitive(PrimitiveType type,string n,Transform p,Vector3 pos,Vector3 size,Material m){var go=GameObject.CreatePrimitive(type);go.name=n;go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 static void Beam(string n,Transform p,Vector3 a,Vector3 b,float width,Material m){var t=Primitive(PrimitiveType.Cylinder,n,p,(a+b)*.5f,new Vector3(width,(a-b).magnitude*.5f,width),m).transform;t.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
 static GameObject Plant(string src,string n,Transform p,Vector3 target,float h,float yaw)
 {
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Village+src+".prefab"));go.name=n;go.transform.SetParent(p,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
  foreach(var t in go.GetComponentsInChildren<Transform>(true)){GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);foreach(var c in t.GetComponents<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(c);foreach(var c in t.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);}
  var bounds=B(go);go.transform.localScale*=h/bounds.size.y;bounds=B(go);go.transform.position+=new Vector3(target.x-bounds.center.x,target.y-bounds.min.y,target.z-bounds.center.z);return go;
 }
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");var root=GameObject.Find("Castle_Services_Prototype").transform;
  if(root.Find("08_Distributed_Greenery")!=null||Directory.Exists(Dir))throw new Exception("Already installed");Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
  var original=root.Find("07_Boat_Static_Example");if(original==null)throw new Exception("Original boat missing");
  var rustic=Group("Boat_Rustic_01",root);rustic.position=new Vector3(0,-.95f,0);original.SetParent(rustic,true);original.name="Original_Wooden_Hull";
  rustic.localPosition=Vector3.zero;PrefabUtility.SaveAsPrefabAssetAndConnect(rustic.gameObject,Dir+"Boat_Rustic_01.prefab",InteractionMode.AutomatedAction);rustic.localPosition=new Vector3(0,-.95f,-10);
  var modern=Group("Boat_Modern_02",root);modern.position=new Vector3(0,-.95f,0);
  var hull=UnityEngine.Object.Instantiate(original.gameObject);hull.transform.SetParent(modern,false);hull.transform.localPosition=original.localPosition;hull.transform.RotateAround(B(hull).center,Vector3.up,180);hull.name="Navy_Hull_Aligned";
  var navy=Mat("Launch_Navy",new Color(.08f,.17f,.25f));var white=Mat("Launch_Ivory",new Color(.83f,.87f,.86f));var steel=Mat("Launch_Steel",new Color(.45f,.53f,.58f));var cushion=Mat("Launch_Cushions",new Color(.15f,.3f,.34f));var black=Mat("Launch_Motor",new Color(.06f,.08f,.09f));
  foreach(var r in hull.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>navy).ToArray();
  Primitive(PrimitiveType.Cube,"NonSlip_Deck",modern,new Vector3(0,.62f,-.25f),new Vector3(2.22f,.12f,4.2f),white);
  foreach(float side in new[]{-1f,1f})
  {
   Beam("Ivory_Gunwale",modern,new Vector3(side*1.36f,1.13f,-2.5f),new Vector3(side*1.36f,1.13f,1.9f),.085f,white);
   Beam("Bow_Gunwale",modern,new Vector3(side*1.36f,1.13f,1.9f),new Vector3(0,1.13f,3.05f),.085f,white);
   Primitive(PrimitiveType.Cube,"Pilot_Seat_Base",modern,new Vector3(side*.58f,.86f,-.4f),new Vector3(.62f,.36f,.58f),white);
   Primitive(PrimitiveType.Cube,"Pilot_Seat_Cushion",modern,new Vector3(side*.58f,1.08f,-.4f),new Vector3(.64f,.12f,.6f),cushion);
   Primitive(PrimitiveType.Cube,"Pilot_Seat_Back",modern,new Vector3(side*.58f,1.37f,-.67f),new Vector3(.64f,.52f,.12f),cushion);
  }
  Primitive(PrimitiveType.Cube,"Stern_Bench",modern,new Vector3(0,.92f,-1.8f),new Vector3(1.95f,.45f,.55f),white);Primitive(PrimitiveType.Cube,"Bench_Cushion",modern,new Vector3(0,1.19f,-1.8f),new Vector3(1.95f,.12f,.57f),cushion);
  Primitive(PrimitiveType.Cube,"Helm_Console",modern,new Vector3(0,1.03f,.82f),new Vector3(1.75f,.7f,.48f),white);
  var glass=Mat("Launch_Windscreen",new Color(.28f,.52f,.59f,.28f));glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHABLEND_ON");glass.renderQueue=3000;
  var wind=Primitive(PrimitiveType.Cube,"Windscreen",modern,new Vector3(0,1.65f,.95f),new Vector3(1.9f,.6f,.035f),glass);wind.transform.localRotation=Quaternion.Euler(-14,0,0);wind.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  foreach(float side in new[]{-1f,1f})Beam("Windscreen_Frame",modern,new Vector3(side*.97f,1.35f,.88f),new Vector3(side*.97f,1.94f,1.03f),.045f,steel);
  Beam("Windscreen_Top",modern,new Vector3(-.97f,1.94f,1.03f),new Vector3(.97f,1.94f,1.03f),.045f,steel);
  Steering(modern,black,steel);
  Primitive(PrimitiveType.Sphere,"Outboard_Cover",modern,new Vector3(0,.86f,-3.27f),new Vector3(.65f,.8f,.6f),black);
  Primitive(PrimitiveType.Cube,"Outboard_Shaft",modern,new Vector3(0,.27f,-3.3f),new Vector3(.13f,.8f,.16f),steel);
  Primitive(PrimitiveType.Cube,"Propeller_A",modern,new Vector3(0,-.12f,-3.4f),new Vector3(.52f,.07f,.08f),black);Primitive(PrimitiveType.Cube,"Propeller_B",modern,new Vector3(0,-.12f,-3.4f),new Vector3(.07f,.52f,.08f),black);
  modern.localPosition=Vector3.zero;PrefabUtility.SaveAsPrefabAssetAndConnect(modern.gameObject,Dir+"Boat_Modern_02.prefab",InteractionMode.AutomatedAction);modern.localPosition=new Vector3(0,-.95f,9);
  foreach(Transform h in root.Find("03_Service_Houses")){var lantern=h.Find("06_Details/lantern");if(lantern!=null)UnityEngine.Object.DestroyImmediate(lantern.gameObject);}
  var plaza=root.Find("04_Plaza_Details");foreach(var t in plaza.Cast<Transform>().Where(t=>t.name.StartsWith("Lamp_")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  var old=root.Find("06_Territory_Decor");foreach(var t in old.Cast<Transform>().Where(t=>new[]{"Stone_Garden_Base","Garden_Soil","Courtyard_Tree","Garden_Flowers"}.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  var greenery=Group("08_Distributed_Greenery",root);var green=Mat("Garden_Grass",new Color(.19f,.34f,.2f));var leaves=new[]{Mat("Shrub_Sage",new Color(.28f,.43f,.26f)),Mat("Shrub_Forest",new Color(.12f,.29f,.2f)),Mat("Shrub_Olive",new Color(.34f,.43f,.23f))};
  for(int i=0;i<16;i++)
  {
   float side=i%2==0?-1:1;bool outer=i>=8;float x=side*(outer?20f:8.1f);float z=new[]{-21f,-7f,7f,21f}[(i/2)%4];var patch=Group("Green_Cluster_"+i,greenery);
   Primitive(PrimitiveType.Cylinder,"Grass_Island",patch,new Vector3(x,.215f,z),new Vector3(outer?4.4f:3.3f,.015f,outer?3.2f:2.8f),green);
   if(i%3!=1)Plant(i%2==0?"Tree":"Tree3","Shade_Tree",patch,new Vector3(x+side*.45f,.24f,z+.45f),outer?4.2f:2.8f,i*37);
   for(int j=0;j<3;j++)Primitive(PrimitiveType.Sphere,"Low_Shrub",patch,new Vector3(x+(j-1)*.8f,.53f,z-.35f+(j%2)*.6f),new Vector3(.95f,.65f,.85f),leaves[(i+j)%3]);
   for(int j=0;j<4;j++)Plant("Grass2","Grass_Tuft",patch,new Vector3(x+(j-1.5f)*.55f,.24f,z-1.0f),.38f,j*79+i*17);
   for(int j=0;j<2;j++)Plant("Flower","Small_Flowers",patch,new Vector3(x+(j==0?-.6f:.6f),.24f,z-.9f),.42f,i*31);
  }
  Physics.SyncTransforms();PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 }
 static void Steering(Transform modern,Material black,Material steel)
 {
  var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"ModernSteeringRing.asset");if(mesh==null){mesh=new Mesh{name="ModernSteeringRing"};var v=new Vector3[32*8];var triangles=new int[32*8*6];int k=0;for(int i=0;i<32;i++)for(int j=0;j<8;j++){float a=i*Mathf.PI*2/32,b=j*Mathf.PI*2/8,r=.16f+.018f*Mathf.Cos(b);v[i*8+j]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,.018f*Mathf.Sin(b));int p=i*8+j,q=((i+1)%32)*8+j,s=i*8+(j+1)%8,t=((i+1)%32)*8+(j+1)%8;triangles[k++]=p;triangles[k++]=q;triangles[k++]=s;triangles[k++]=s;triangles[k++]=q;triangles[k++]=t;}mesh.vertices=v;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+"ModernSteeringRing.asset");}
  var wheel=Group("Modern_Steering",modern);wheel.localPosition=new Vector3(-.55f,1.43f,.51f);wheel.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;wheel.gameObject.AddComponent<MeshRenderer>().sharedMaterial=black;
  for(int i=0;i<3;i++){float a=(i*120+90)*Mathf.Deg2Rad;Beam("Wheel_Spoke",wheel,Vector3.zero,new Vector3(Mathf.Cos(a)*.15f,Mathf.Sin(a)*.15f,0),.025f,steel);}
 }
 public static void PolishBoat()
 {
  var root=GameObject.Find("Castle_Services_Prototype").transform;var modern=root.Find("Boat_Modern_02");if(PrefabUtility.IsPartOfPrefabInstance(modern.gameObject))PrefabUtility.UnpackPrefabInstance(modern.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);var hull=modern.Find("Navy_Hull");if(hull!=null){hull.RotateAround(B(hull.gameObject).center,Vector3.up,180);hull.name="Navy_Hull_Aligned";}
  var old=modern.Find("Steering_Wheel");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);if(modern.Find("Modern_Steering")==null)Steering(modern,AssetDatabase.LoadAssetAtPath<Material>(Dir+"Launch_Motor.mat"),AssetDatabase.LoadAssetAtPath<Material>(Dir+"Launch_Steel.mat"));
  var pos=modern.localPosition;modern.localPosition=Vector3.zero;PrefabUtility.SaveAsPrefabAsset(modern.gameObject,Dir+"Boat_Modern_02.prefab");modern.localPosition=pos;PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(root.gameObject.scene);
 }
}
