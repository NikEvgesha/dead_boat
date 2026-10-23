using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class DecorateCastleServices
{
 const string Dir="Assets/Prefabs/Workshop/CastleServices/";
 const string Village="Assets/Import/3D Cartoon Village/Prefabs/";
 static Bounds B(GameObject go){var rr=go.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static Material Mat(string name,Color c){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+name+".mat");if(m!=null)return m;m=new Material(Shader.Find("Standard")){name=name,color=c};m.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(m,Dir+name+".mat");return m;}
 static void Strip(GameObject go)
 {
  foreach(var canvas in go.GetComponentsInChildren<Canvas>(true))if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas.gameObject);
  foreach(var t in go.GetComponentsInChildren<Transform>(true))
  {
   GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
   foreach(var c in t.GetComponents<Component>())if(c!=null&&!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)&&!(c is SkinnedMeshRenderer)&&!(c is LODGroup))UnityEngine.Object.DestroyImmediate(c);
   t.gameObject.tag="Untagged";t.gameObject.layer=0;
  }
 }
 static GameObject Prop(string path,string name,Transform parent,Vector3 pos,float height,float yaw=0,bool solid=true)
 {
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new Exception(path);var go=UnityEngine.Object.Instantiate(source);go.name=name;Strip(go);go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,yaw,0);var b=B(go);go.transform.localScale*=height/b.size.y;b=B(go);var target=parent.TransformPoint(pos);go.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
  if(solid){b=B(go);var col=go.AddComponent<BoxCollider>();col.center=go.transform.InverseTransformPoint(b.center);var size=go.transform.InverseTransformVector(b.size);col.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));}return go;
 }
 static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool solid=true){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");var root=GameObject.Find("Castle_Services_Prototype").transform;if(root.Find("06_Territory_Decor")!=null)throw new Exception("Decor already installed");
  var group=new GameObject("06_Territory_Decor").transform;group.SetParent(root,false);
  var stone=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Paving_Gray.mat");var dark=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Sign_Charcoal.mat");var soil=Mat("Garden_Soil",new Color(.21f,.25f,.2f));var trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/CastleStone_Trim.mat");
  foreach(Transform h in root.Find("03_Service_Houses"))h.position+=Vector3.up*.04f;
  foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-20f,-7f,7f,20f})
  {
   float x=side*22;
   Box("Stone_Garden_Base",group,new Vector3(x,.32f,z),new Vector3(3.2f,.24f,3.2f),stone);
   Box("Garden_Soil",group,new Vector3(x,.45f,z),new Vector3(2.95f,.02f,2.95f),soil,false);
   Prop(Village+"Tree2.prefab","Courtyard_Tree",group,new Vector3(x,.47f,z),3.8f,0,false);
   foreach(float offset in new[]{-.85f,.85f})Prop(Village+"Flower.prefab","Garden_Flowers",group,new Vector3(x+offset,.47f,z-.95f),.5f,0,false);
  }
  foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-7f,7f})
  {
   Prop(Village+"Bench.prefab","Riverside_Bench",group,new Vector3(side*6.3f,.2f,z),.75f,side<0?90:-90);
   Box("Mooring_Bollard_Base",group,new Vector3(side*3.8f,.29f,z+2.1f),new Vector3(.45f,.18f,.45f),trim);
   Box("Mooring_Bollard",group,new Vector3(side*3.8f,.54f,z+2.1f),new Vector3(.16f,.35f,.16f),dark);
  }
  Prop(Village+"Well.prefab","West_Courtyard_Well",group,new Vector3(-21,.2f,0),2.3f);
  Prop(Village+"Cart.prefab","Merchant_Cart",group,new Vector3(20,.2f,-13),1.25f,30);
  Prop(Village+"Bench.prefab","East_Garden_Bench",group,new Vector3(20,.2f,0),.8f,-90);
  foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-20f,20f})
  {
   var p=new Vector3(side*7,.2f,z);Box("Banner_Base",group,p+new Vector3(0,.13f,0),new Vector3(.65f,.26f,.65f),stone);Box("Banner_Pole",group,p+new Vector3(0,1.65f,0),new Vector3(.12f,3,.12f),dark);
   var cloth=new GameObject("Blue_Castle_Banner");cloth.transform.SetParent(group,false);cloth.transform.localPosition=p+new Vector3(0,1.15f,0);cloth.transform.localScale=new Vector3(1.1f,1.6f,1);cloth.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Workshop/CottageGeometry/GuardBannerFabric.asset");cloth.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/CastleCloth_Blue.mat");
  }
  var boat=BuildBoat(root);var bounds=B(boat);if(bounds.min.x<=-3.5f||bounds.max.x>=3.5f)throw new Exception("Boat does not fit water gap");
  Physics.SyncTransforms();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(42,-35,0),40,false,true);SceneView.lastActiveSceneView?.Focus();
  File.WriteAllText("Temp/castle-decor-result.txt","Decor children="+group.childCount+"; boat bounds="+bounds+"; floorTop=0.250");
 }
 static GameObject BuildBoat(Transform root){return Prop("Assets/Import/Kenney Assets/Pirate Kit/Models/FBX format/boat_large.fbx","07_Boat_Static_Example",root,new Vector3(0,-.95f,0),1.25f,0,false);}
 public static void ReplaceBoat()
 {
  var root=GameObject.Find("Castle_Services_Prototype").transform;var old=root.Find("07_Boat_Static_Example");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);var boat=BuildBoat(root);
  PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(root.gameObject.scene);File.WriteAllText("Temp/castle-decor-result.txt","Decor children="+root.Find("06_Territory_Decor").childCount+"; boat bounds="+B(boat)+"; floorTop=0.250");
 }
}
