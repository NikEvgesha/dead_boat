using System;using System.Linq;using UnityEngine;using UnityEditor;
public static class Tone145{public static string Main(){
 string path="Assets/Prefabs/Workshop/Locations/House1_2_RustCourtyard.prefab";
 var root=PrefabUtility.LoadPrefabContents(path);try{
 var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/CastleIron_Charcoal.mat");
 foreach(var name in new[]{"Anvil","Hammer"}){var t=root.transform.Find("Individual_Details/"+name);foreach(var r in t.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>metal).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
 if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new Exception("Save failed");return "Anvil and hammer now project charcoal iron";
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }}
