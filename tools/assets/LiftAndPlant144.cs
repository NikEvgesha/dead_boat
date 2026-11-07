using System;using System.Linq;using UnityEditor;using UnityEngine;
public static class LiftAndPlant144{
 const string Dir="Assets/Prefabs/Workshop/Locations/";
 static readonly string[] Names={"House1_1_OpenCourtyard","House1_2_RustCourtyard","House1_3_GreenCourtyard"};
 static readonly string[] TreeNames={"Hilly_Prop_Tree_Oak_1","Hilly_Prop_Tree_Cedar_1","Hilly_Prop_Tree_Pine_3","Hilly_Prop_Tree_Pine_1","Hilly_Prop_Tree_Oak_3","Hilly_Prop_Tree_Cedar_2"};
 static Bounds B(GameObject root){var rs=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Add(GameObject root,string source,string name,Vector3 pos,float height,float yaw){
 var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Decoration/"+source+".prefab"),root.transform);
 o.name=name;o.transform.localPosition=pos;o.transform.localRotation=Quaternion.Euler(0,yaw,0);o.transform.localScale*=height/B(o).size.y;
 var p=o.transform.position;p.y+=root.transform.position.y+.2f-B(o).min.y;o.transform.position=p;
 foreach(var t in o.GetComponentsInChildren<Transform>(true)){t.tag="Untagged";PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
 PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);
 }
 public static string Main(){
 var log=new System.Text.StringBuilder();
 for(int i=0;i<Names.Length;i++){
 var path=Dir+Names[i]+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
 try{
 if(root.transform.Find("Extra_Tree_Back"))throw new Exception("Already planted "+path);
 var h=root.transform.Find("Stone_House_Open_Entrance");var p=h.localPosition;p.y+=.06f;h.localPosition=p;PrefabUtility.RecordPrefabInstancePropertyModifications(h);
 Add(root,TreeNames[i*2],"Extra_Tree_Back",new Vector3(-28.5f,.2f,-30),5.2f+i*.3f,25+i*35);
 Add(root,TreeNames[i*2+1],"Extra_Tree_Side",new Vector3(-12,.2f,-16.4f),3.8f+i*.25f,115+i*20);
 Add(root,"Shared_Prop_Boulder_"+(i+1),"Small_Courtyard_Rock",new Vector3(-14.5f,.2f,-14.5f),.4f,30+i*65);
 var floor=h.TransformPoint(new Vector3(0,.514f,0)).y;
 if(Mathf.Abs(floor-.26f)>.002f)throw new Exception("Unexpected floor "+floor);
 if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new Exception("Failed to save "+path);
 log.AppendLine(Names[i]+": floor="+floor+"; added two trees and one small rock");
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 return log.ToString();
 }
}
