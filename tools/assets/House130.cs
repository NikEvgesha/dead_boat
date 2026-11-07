using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class House130 {
 const string Out="Assets/Prefabs/Workshop/Locations/";
 const string Court=Out+"House1_1_OpenCourtyard.prefab";
 const string Mats="Assets/Prefabs/Workshop/CottageGeometry/";
 static Material M(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+name+".mat");
 static GameObject Add(string path,Transform parent,Vector3 pos,float size){
 var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
 if(!obj)throw new Exception(path);
 obj.transform.localPosition=pos;obj.transform.localScale*=size;return obj;}
 static GameObject Box(string name,Transform root,Vector3 pos,Vector3 size,Material mat,bool solid=true){
 var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(obj,root.gameObject.scene);obj.name=name;obj.transform.SetParent(root,false);obj.transform.localPosition=pos;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;
 if(!solid)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;}
 public static string Main(){
 if(File.Exists(Court))throw new Exception("Court exists");
 var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("House1_1_OpenCourtyard");SceneManager.MoveGameObjectToScene(root,scene);
 try{
 // Root is attached to the existing PointCenter anchor (-25.56, 0, 7).
 var home=Add("Assets/Prefabs/Workshop/StoneCottage_01.prefab",root.transform,new Vector3(-27.44f,.20f,-21),1);
 home.name="Stone_House_Open_Entrance";home.transform.localRotation=Quaternion.Euler(0,-90,0);
 var roof=home.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="Slate_Tiles");
 var verts=roof.sharedMesh.vertices;
 if(!verts.Any(v=>v.x<-.3f)||!verts.Any(v=>v.x>.3f))throw new Exception("Tiles not on both slopes");
 if(home.GetComponentsInChildren<Transform>().Any(t=>t.name.Equals("Door",StringComparison.OrdinalIgnoreCase)))throw new Exception("Unexpected door");
 var wood=M("Roof_InteriorBrown");var slate=M("SlateTiles_Gray");var dark=M("SlateTiles_Dark");
 var shed=new GameObject("Open_Timber_LeanTo");SceneManager.MoveGameObjectToScene(shed,scene);shed.transform.SetParent(root.transform,false);shed.transform.localPosition=new Vector3(-25,.2f,-12.4f);
 foreach(float x in new[]{-1.65f,1.65f})foreach(float z in new[]{-1.25f,1.25f})Box("Timber_Post",shed.transform,new Vector3(x,1.25f,z),new Vector3(.18f,2.5f,.18f),wood);
 foreach(float x in new[]{-1.65f,1.65f})Box("Timber_Beam",shed.transform,new Vector3(x,2.4f,0),new Vector3(.2f,.22f,2.9f),wood);
 var cover=Box("Slate_Canopy",shed.transform,new Vector3(0,2.68f,0),new Vector3(3.95f,.18f,3.15f),slate);cover.transform.localRotation=Quaternion.Euler(7,0,0);
 for(int i=0;i<8;i++){var strip=Box("Slate_Course",shed.transform,new Vector3(-1.74f+i*.5f,2.79f,0),new Vector3(.47f,.045f,3.13f),i%3==0?dark:slate,false);strip.transform.localRotation=cover.transform.localRotation;}
 const string crate="Assets/Import/LowpolyDungeonAssets/Assets/Prefabs/Props/Props_Crate.prefab";
 Add(crate,shed.transform,new Vector3(-.8f,0,.55f),1);Add(crate,shed.transform,new Vector3(.2f,0,.65f),.85f);
 Add("Assets/Import/3D Cartoon Village/Prefabs/Well.prefab",root.transform,new Vector3(-16.2f,.20f,-11.7f),.24f);
 Add("Assets/Import/3D Cartoon Village/Prefabs/Cart.prefab",root.transform,new Vector3(-19.1f,.20f,-13.8f),.20f);
 var stone=M("CastleStone_Trim");
 for(int i=0;i<4;i++)Box("Approach_Flagstone",root.transform,new Vector3(-20.5f+i*1.05f,.212f,-21+i*.13f),new Vector3(.83f,.025f,1.05f),stone,false);
 var asset=PrefabUtility.SaveAsPrefabAsset(root,Court);if(!asset)throw new Exception("Save failed");
 if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset.transform,out string guid,out long id))throw new Exception("Root ID missing");
 File.WriteAllText("Temp/court130.txt",guid+" "+id);
 return "Court saved; tile mesh has "+verts.Length+" vertices covering both sides; no door leaf object. "+guid+" "+id;
 }finally{EditorSceneManager.ClosePreviewScene(scene);}
 }
}
