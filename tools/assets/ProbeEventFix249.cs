using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class ProbeEventFix249
{
 static string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name;
 public static string Run(){
 var lines=new List<string>{"playing="+EditorApplication.isPlaying+" compile="+EditorApplication.isCompiling};
 foreach(var path in new[]{"Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab","Assets/Prefabs/Workshop/Hospital/Hospital_Stone_01.prefab","Assets/Prefabs/Locations/Bilding/Hospital.prefab","Assets/Events/NewYear/ShorePOI/POI_05_Gift_Sled.prefab","Assets/Prefabs/Locations/Enemy/ship_2.prefab","Assets/Prefabs/Locations/Enemy/castle_1.prefab","Assets/Events/NewYear/NY_Ruins_1.prefab"}){
 var g=AssetDatabase.LoadAssetAtPath<GameObject>(path);lines.Add("ASSET "+path);
 if(path.Contains("Hospital")||path.Contains("EventCastle")){
 foreach(var c in g.GetComponentsInChildren<Canvas>(true).Where(c=>path.Contains("Hospital")||P(c.transform).Contains("06_AnimalMerge")||P(c.transform).Contains("Hospital")))lines.Add("CANVAS "+P(c.transform)+" mode="+c.renderMode+" local="+c.transform.localScale+" lossy="+c.transform.lossyScale+" size="+((RectTransform)c.transform).sizeDelta+" pos="+c.transform.position);
 foreach(var r in g.GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterials.Any(m=>m&&m.name.ToLower().Contains("outline"))))lines.Add("OUTLINE "+P(r.transform));
 }
 else foreach(var t in g.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponent<MeshFilter>()||t.name.Contains("Stair")||t.name.Contains("Ladder")||t.GetComponent<Collider>()).Take(120)){
 var f=t.GetComponent<MeshFilter>();var r=t.GetComponent<Renderer>();var c=t.GetComponent<Collider>();
 lines.Add(P(t)+" pos="+t.position+" scale="+t.lossyScale+" layer="+t.gameObject.layer+" tag="+t.tag+(f?" mesh="+AssetDatabase.GetAssetPath(f.sharedMesh):"")+(r?" mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+":"+m.shader.name:"NULL")):"")+(c?" collider="+c.GetType().Name+" trigger="+c.isTrigger:""));
 }
 }
 Directory.CreateDirectory("Library/EventFix249");File.WriteAllLines("Library/EventFix249/probe.txt",lines);return "Probe saved, "+lines.Count+" lines";
 }
}
