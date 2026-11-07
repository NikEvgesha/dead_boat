using System;using System.Linq;using UnityEngine;using UnityEditor;
public static class PolishHouse130 {
 static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Size(Transform t,float goal,bool height,float floor){var b=BoundsOf(t);float d=height?b.size.y:Mathf.Max(b.size.x,b.size.z);t.localScale*=goal/d;t.position+=Vector3.up*(floor-BoundsOf(t).min.y);}
 public static string Main(){
 const string path="Assets/Prefabs/Workshop/Locations/House1_1_OpenCourtyard.prefab";
 var root=PrefabUtility.LoadPrefabContents(path);
 try{
 Size(root.transform.Find("Well"),2.15f,true,.2f);
 var cart=root.transform.Find("Cart");Size(cart,3.1f,false,.2f);
 var shed=root.transform.Find("Open_Timber_LeanTo");
 foreach(Transform t in shed)if(t.name.StartsWith("Props_Crate"))Size(t,.62f,true,.2f);
 if(!root.transform.Find("Courtyard_Tree")){
 var tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Decoration/Hilly_Prop_Tree_Oak_4.prefab"),root.transform);
 tree.name="Courtyard_Tree";tree.transform.localPosition=new Vector3(-34,.2f,-19.3f);Size(tree.transform,4.3f,true,.2f);
 foreach(var t in tree.GetComponentsInChildren<Transform>(true))t.tag="Untagged";
 }
 if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new Exception("Save failed");
 return "Resized props and added one existing tree";
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}
