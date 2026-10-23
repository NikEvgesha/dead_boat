using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class MapBoatSource {
 static List<string> lines=new List<string>();
 static void Info(UnityEngine.Object o,out string guid,out long id){guid="";id=0;if(o!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out guid,out id);}
 static void Walk(Transform src,Transform dst,string path){
  Info(PrefabUtility.GetCorrespondingObjectFromOriginalSource(src.gameObject),out string guid,out long id);
  lines.Add("G\t"+path+"\t"+guid+"\t"+id+"\t"+GlobalObjectId.GetGlobalObjectIdSlow(dst.gameObject).targetObjectId);
  var cs=src.GetComponents<Component>();var ds=dst.GetComponents<Component>();
  if(cs.Length!=ds.Length)throw new Exception("Count mismatch "+path);
  for(int i=0;i<cs.Length;i++){Info(cs[i]==null?null:PrefabUtility.GetCorrespondingObjectFromOriginalSource(cs[i]),out guid,out id);lines.Add("C\t"+path+"\t"+i+"\t"+guid+"\t"+id);}
  for(int i=0;i<src.childCount;i++)Walk(src.GetChild(i),dst.GetChild(i),path+"/"+i);
 }
 public static void Main(){var src=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardController.prefab"));try{Walk(src.transform,GameObject.Find("Castle_Services_Prototype/Board_CastleCargo_Variant").transform,"root");System.IO.File.WriteAllLines("Temp/boat_source_map.tsv",lines);}finally{UnityEngine.Object.DestroyImmediate(src);}}
}
