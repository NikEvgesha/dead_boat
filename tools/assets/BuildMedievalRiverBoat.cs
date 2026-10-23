using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class BuildMedievalRiverBoat
{
 const string Dir="Assets/Prefabs/Workshop/Boats/";
 static Bounds B(GameObject go){var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static Material Mat(string n,Color c){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+n+".mat");if(m!=null)return m;m=new Material(Shader.Find("Standard")){name=n,color=c};m.SetFloat("_Glossiness",.1f);AssetDatabase.CreateAsset(m,Dir+n+".mat");return m;}
 static GameObject Box(string n,Transform parent,Vector3 p,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static void Beam(string n,Transform p,Vector3 a,Vector3 b,float width,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=(a+b)*.5f;g.transform.localScale=new Vector3(width,(a-b).magnitude*.5f,width);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
 static void Prop(string path,string n,Transform parent,Vector3 target,float size)
 {
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=n;go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
  foreach(var t in go.GetComponentsInChildren<Transform>(true)){GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);foreach(var c in t.GetComponents<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(c);foreach(var c in t.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);}
  var b=B(go);go.transform.localScale*=size/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));b=B(go);var p=parent.TransformPoint(target);go.transform.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);
 }
 static Mesh Sail()
 {
  var mesh=new Mesh{name="MedievalRiverSail"};var v=new Vector3[50];var uv=new Vector2[50];var tri=new int[4*4*12];int k=0;
  for(int y=0;y<5;y++)for(int x=0;x<5;x++){float u=x/4f,t=y/4f;int i=y*5+x;v[i]=v[i+25]=new Vector3((u-.5f)*2.7f,2.0f+t*1.85f,.52f+.35f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(t*Mathf.PI));uv[i]=uv[i+25]=new Vector2(u,t);}
  for(int y=0;y<4;y++)for(int x=0;x<4;x++){int a=y*5+x,b=a+1,c=a+5,d=c+1;foreach(int i in new[]{a,c,b,b,c,d,a+25,b+25,c+25,b+25,d+25,c+25})tri[k++]=i;}
  mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+"MedievalRiverSail.asset");return mesh;
 }
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");
  var root=GameObject.Find("Castle_Services_Prototype").transform;string path=Dir+"Boat_MedievalTrader_03.prefab";if(File.Exists(path)||root.Find("Boat_MedievalTrader_03")!=null)throw new Exception("Medieval boat already exists");
  var boat=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"Boat_Rustic_01.prefab"),scene);PrefabUtility.UnpackPrefabInstance(boat,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);boat.name="Boat_MedievalTrader_03";boat.transform.SetParent(root,false);boat.transform.localPosition=Vector3.zero;
  var hull=boat.transform.Find("Original_Wooden_Hull");hull.RotateAround(B(hull.gameObject).center,Vector3.up,180);
  var oak=Mat("Medieval_Oak",new Color(.48f,.3f,.17f));var timber=Mat("Medieval_DarkTimber",new Color(.24f,.15f,.09f));var rope=Mat("Medieval_Rope",new Color(.62f,.51f,.34f));var canvas=Mat("Medieval_Linen",new Color(.82f,.77f,.61f));var red=Mat("Medieval_Burgundy",new Color(.38f,.14f,.13f));
  foreach(var r in hull.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>oak).ToArray();
  foreach(float side in new[]{-1f,1f}){Beam("Wooden_Gunwale",boat.transform,new Vector3(side*1.36f,1.1f,-2.55f),new Vector3(side*1.36f,1.1f,1.8f),.085f,timber);Beam("Bow_Timber",boat.transform,new Vector3(side*1.36f,1.1f,1.8f),new Vector3(0,1.1f,3.03f),.085f,timber);}
  Beam("Wooden_Mast",boat.transform,new Vector3(0,.65f,.5f),new Vector3(0,4.2f,.5f),.11f,timber);Beam("Sail_Yard",boat.transform,new Vector3(-1.5f,3.9f,.5f),new Vector3(1.5f,3.9f,.5f),.075f,timber);
  var sail=new GameObject("Linen_Square_Sail");sail.transform.SetParent(boat.transform,false);sail.AddComponent<MeshFilter>().sharedMesh=Sail();sail.AddComponent<MeshRenderer>().sharedMaterial=canvas;
  foreach(float side in new[]{-1f,1f}){Beam("Mast_Stay",boat.transform,new Vector3(0,4.15f,.5f),new Vector3(side*1.25f,1.03f,-2.6f),.018f,rope);Beam("Sail_Sheet",boat.transform,new Vector3(side*1.34f,2.03f,.52f),new Vector3(side*1.3f,1.05f,1.8f),.018f,rope);}
  Beam("Bow_Stay",boat.transform,new Vector3(0,4.15f,.5f),new Vector3(0,1.03f,2.8f),.018f,rope);
  Box("Sail_Cloth_Mark",boat.transform,new Vector3(0,2.9f,.89f),new Vector3(.43f,.6f,.012f),red).transform.localRotation=Quaternion.Euler(0,0,45);
  Box("Stern_Rudder",boat.transform,new Vector3(0,.25f,-3.24f),new Vector3(.1f,1.1f,.52f),timber);Beam("Tiller",boat.transform,new Vector3(0,.99f,-3.18f),new Vector3(0,1.06f,-2.13f),.065f,timber);
  const string Kay="Assets/Import/KayKit_DungeonRemastered_1.1_FREE/Assets/fbx(unity)/";
  Prop(Kay+"barrel_small.fbx","Trade_Barrel",boat.transform,new Vector3(-.78f,.77f,-1.5f),.75f);Prop(Kay+"crates_stacked.fbx","Trade_Crates",boat.transform,new Vector3(.65f,.77f,-1.65f),.8f);
  foreach(float side in new[]{-1f,1f}){var a=new Vector3(side*.8f,1.27f,-1.1f);var b=new Vector3(side*.48f,1.27f,1.2f);Beam("Spare_Oar_Shaft",boat.transform,a,b,.045f,timber);var blade=Box("Spare_Oar_Blade",boat.transform,b,new Vector3(.22f,.035f,.65f),oak);blade.transform.localRotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);}
  var modern=root.Find("Boat_Modern_02");if(modern!=null)UnityEngine.Object.DestroyImmediate(modern.gameObject);
  var greenery=root.Find("08_Distributed_Greenery");int removed=0;foreach(var t in greenery.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Low_Shrub"||t.name=="Grass_Island").ToArray()){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;}
  PrefabUtility.SaveAsPrefabAsset(boat,path);boat.transform.localPosition=new Vector3(0,-.95f,9);Physics.SyncTransforms();PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  File.WriteAllText("Temp/medieval-boat-result.txt","Removed round vegetation="+removed+"; boat bounds="+B(boat));
 }
}
