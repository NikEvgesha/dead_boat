using System;using System.Linq;using UnityEngine;using UnityEditor;
public static class FinishRoom140{
 public static string Main(){
 const string path="Assets/Prefabs/Workshop/Locations/House1_1_OpenCourtyard.prefab";var a=PrefabUtility.LoadPrefabContents(path);
 try{
 var h=a.transform.Find("Stone_House_Open_Entrance");foreach(Transform t in h.Find("Oak_Plank_Floor")){var s=t.localScale;s.x=.36f;t.localScale=s;}
 var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/Roof_WarmBrown.mat");
 foreach(var r in h.Find("Interior_Chair").GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>wood).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 if(!PrefabUtility.SaveAsPrefabAsset(a,path))throw new Exception("Save failed");
 }finally{PrefabUtility.UnloadPrefabContents(a);}
 AssetDatabase.ImportAsset("Assets/Prefabs/Locations/Enemy/house1_1.prefab",ImportAssetOptions.ForceSynchronousImport);
 foreach(var w in Resources.FindObjectsOfTypeAll<EditorWindow>())if(w.titleContent.text=="Прогулка — house1_1")w.Close();
 return "Room ready, refreshed prefab and closed stale previews";
 }}
