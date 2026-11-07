using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;
public static class Courts146{
 const string Dir="Assets/Prefabs/Workshop/Locations/";
 static void Record(UnityEngine.Object o)=>PrefabUtility.RecordPrefabInstancePropertyModifications(o);
 static void Move(Transform t,Vector3 p){t.localPosition=p;Record(t);}
 static Bounds Bounds(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static string Point(Transform r,Transform t){var b=Bounds(t);var p=r.InverseTransformPoint(new Vector3(b.center.x,b.max.y+.08f,b.center.z));return string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4}",p.x,p.y,p.z);}
 static void Rock(Transform root,int index,Vector3 pos,float height){var p="Assets/Prefabs/Locations/Decoration/Shared_Prop_Boulder_"+(index%3+1)+".prefab";var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p),root);o.name="Entry_Rock_"+index;Move(o.transform,pos);o.transform.localRotation=Quaternion.Euler(0,35+index*61,0);o.transform.localScale*=height/Bounds(o.transform).size.y;var v=o.transform.position;v.y+=root.TransformPoint(pos).y-Bounds(o.transform).min.y;o.transform.position=v;foreach(var t in o.GetComponentsInChildren<Transform>(true)){t.tag="Untagged";Record(t.gameObject);}Record(o.transform);}
 public static string Main(){var log=new System.Text.StringBuilder();var names=new[]{"House1_1_OpenCourtyard","House1_2_RustCourtyard","House1_3_GreenCourtyard"};
 for(int i=0;i<3;i++){var path=Dir+names[i]+".prefab";var root=PrefabUtility.LoadPrefabContents(path);try{
 var details=root.transform.Find("Individual_Details");var sign=details.Find("Courtyard_Sign");if(sign)UnityEngine.Object.DestroyImmediate(sign.gameObject);
 var shed=root.transform.Find("Open_Timber_LeanTo");
 if(i==1){
 var crates=shed.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Props_Crate")).ToArray();
 for(int j=0;j<crates.Length;j++){crates[j].SetParent(details,true);Move(crates[j],new Vector3(-20.3f+j*.95f,.2f,-16.7f+j*.3f));}
 Move(details.Find("Anvil"),new Vector3(-19.9f,.2f,-25.1f));Move(details.Find("Hammer"),new Vector3(-19.9f,.92f,-25.1f));
 UnityEngine.Object.DestroyImmediate(shed.gameObject);
 if(!details.Find("Entry_Rock_0")){Rock(details,0,new Vector3(-17.2f,.2f,-28.9f),1.35f);Rock(details,1,new Vector3(-15.5f,.2f,-28.2f),.65f);Rock(details,2,new Vector3(-20.4f,.2f,-14.5f),.85f);Rock(details,3,new Vector3(-19.1f,.2f,-14.1f),.35f);}
 log.AppendLine(names[i]+"|"+Point(root.transform,details.Find("Anvil"))+"|"+Point(root.transform,crates[0]));
 }
 if(i==2){
 var well=root.transform.Find("Well");if(well)UnityEngine.Object.DestroyImmediate(well.gameObject);
 Move(shed,new Vector3(-19,.2f,-30));
 foreach(Transform t in shed){if(t.name.StartsWith("Props_Crate"))continue;var p=t.localPosition;p.x*=-2;p.z*=2;t.localPosition=p;var s=t.localScale;s.x*=2;s.z*=2;t.localScale=s;var e=t.localEulerAngles;e.x=-e.x;t.localEulerAngles=e;Record(t);}
 var crates=shed.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Props_Crate")).ToArray();for(int j=0;j<crates.Length;j++)Move(crates[j],new Vector3(-2.1f+j*1.2f,0,1.8f));
 Move(details.Find("Book_Stall"),new Vector3(-18.2f,.2f,-30.8f));Move(details.Find("Ledger"),new Vector3(-18.2f,1.01f,-30.8f));
 log.AppendLine(names[i]+"|"+Point(root.transform,details.Find("Book_Stall"))+"|"+Point(root.transform,crates[0]));
 }
 if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new Exception("Save failed");
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
 File.WriteAllText("Temp/points146.txt",log.ToString());return log.ToString();}
}
