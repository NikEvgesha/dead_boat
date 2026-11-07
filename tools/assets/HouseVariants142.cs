using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;
public static class HouseVariants142 {
 const string Dir="Assets/Prefabs/Workshop/Locations/";
 const string Court=Dir+"House1_1_OpenCourtyard.prefab";
 static Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/"+n+".mat");
 static void Record(UnityEngine.Object o)=>PrefabUtility.RecordPrefabInstancePropertyModifications(o);
 static void Active(Transform t,bool on){t.gameObject.SetActive(on);Record(t.gameObject);}
 static void Paint(Transform t,string mat){foreach(var r in t.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>M(mat)).ToArray();Record(r);}}
 static void Move(Transform t,Vector3 v){t.localPosition=v;Record(t);}
 static void Edit(string path,Action<GameObject,Transform> fn){
 var root=PrefabUtility.LoadPrefabContents(path);try{fn(root,root.transform.Find("Stone_House_Open_Entrance"));if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new Exception(path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
 public static string Main(){
 string a=Dir+"House1_2_RustCourtyard.prefab",b=Dir+"House1_3_GreenCourtyard.prefab";
 if(AssetDatabase.LoadMainAssetAtPath(a)||AssetDatabase.LoadMainAssetAtPath(b))throw new Exception("Variant path occupied");
 Edit(Court,(r,h)=>{
  var p=h.localPosition;p.y=.20f-.514f*h.localScale.y;Move(h,p);
  Active(h.Find("05_Porch"),false);
  UnityEngine.Object.DestroyImmediate(h.Find("Grounded_Stone_Plinth").gameObject);
  int i=0;foreach(Transform t in r.transform)if(t.name=="Approach_Flagstone")Move(t,new Vector3(-20.4f+i++*1.06f,.212f,-21));
 });
 if(!AssetDatabase.CopyAsset(Court,a)||!AssetDatabase.CopyAsset(Court,b))throw new Exception("Copy failed");
 for(int variant=2;variant<=3;variant++){
  int n=variant;var path=n==2?a:b;
  Edit(path,(r,h)=>{
   r.name=n==2?"House1_2_RustCourtyard":"House1_3_GreenCourtyard";
   var guard=h.Find("09_Castle_Guardroom");var props=h.Find("08_Interior_Props");
   var roof=h.GetComponentsInChildren<Renderer>().Single(x=>x.name=="Slate_Tiles");
   var palette=n==2?new[]{"RustTiles_Base","RustTiles_Dark","RustTiles_Light"}:new[]{"CaptainTiles_Navy","CaptainTiles_Dark","CaptainTiles_Light"};
   roof.sharedMaterials=roof.sharedMaterials.Select((_,i)=>M(palette[i%3])).ToArray();Record(roof);
   var cloth=n==2?"VariantCloth_RoyalPurple":"VariantCloth_Emerald";
   foreach(var flag in new[]{"Interior_FlagCloth","Entrance_FlagCloth","Blue_Bedding"})Paint(guard.Find(flag),cloth);
   var oldTree=r.transform.Find("Courtyard_Tree");oldTree.gameObject.SetActive(false);
   var tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Decoration/"+(n==2?"Hilly_Prop_Tree_Pine_2":"Hilly_Prop_Tree_Oak_2")+".prefab"),r.transform);
   tree.name=n==2?"Tall_Pine":"Small_Oak";tree.transform.localPosition=n==2?new Vector3(-37,.2f,-14.5f):new Vector3(-33,.2f,-10);
   var meshes=tree.GetComponentsInChildren<Renderer>();var bounds=meshes[0].bounds;foreach(var m in meshes)bounds.Encapsulate(m.bounds);
   tree.transform.localScale*=(n==2?5.6f:3.25f)/bounds.size.y;
   bounds=meshes[0].bounds;foreach(var m in meshes)bounds.Encapsulate(m.bounds);tree.transform.position+=Vector3.up*(r.transform.position.y+.20f-bounds.min.y);
   foreach(var t in tree.GetComponentsInChildren<Transform>(true)){t.tag="Untagged";Record(t.gameObject);}
   if(n==2){
    Active(r.transform.Find("Cart"),false);
    Active(h.Find("Interior_Chair_Back"),false);
    var shelf=props.Find("Bookshelf");Active(shelf,true);shelf.localScale=Vector3.Scale(shelf.localScale,new Vector3(.5f,1/1.55f,.5f));Move(shelf,new Vector3(.48f,.514f,2.47f));
   }else{
    Active(r.transform.Find("Courtyard_Bush"),false);
    var barrel=props.Find("Storage_Barrel");Move(barrel,new Vector3(-2.30f,barrel.localPosition.y,-1.55f));
    var crate=props.Find("Storage_Crate");Active(crate,true);crate.localScale=Vector3.Scale(crate.localScale,new Vector3(.5f,1/1.55f,.5f));Move(crate,new Vector3(-1.25f,.54f,2.34f));
   }
  });
 }
 return "Grounded base and saved two courtyard variants";
 }
}
