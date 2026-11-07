using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class ExpandHouse141{
 const string Path="Assets/Prefabs/Workshop/Locations/House1_1_OpenCourtyard.prefab";
 static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Save(UnityEngine.Object o){PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
 static void Fit(Transform t,Vector3 scale,float floor){t.localScale=Vector3.Scale(t.localScale,scale);var p=t.position;p.y+=floor-BoundsOf(t).min.y;t.position=p;Save(t);}
 public static string Main(){
 var root=PrefabUtility.LoadPrefabContents(Path);
 try{
 var h=root.transform.Find("Stone_House_Open_Entrance");
 if(h.localScale.x>1.5f)throw new Exception("Already enlarged");
 h.localScale=Vector3.Scale(h.localScale,new Vector3(2,1.55f,2));Save(h);
 // Keep furnishings at human scale while spreading the groups along the walls.
 var guard=h.Find("09_Castle_Guardroom");var props=h.Find("08_Interior_Props");
 var inv=new Vector3(.5f,1f/1.55f,.5f);
 var floor=h.TransformPoint(new Vector3(0,.514f,0)).y;
 var bed=guard.Find("Guard_Bed");var bedding=guard.Find("Blue_Bedding");
 float beddingOffset=BoundsOf(bedding).min.y-BoundsOf(bed).min.y;
 Fit(bed,inv,floor);Fit(bedding,inv,floor+beddingOffset/1.55f);
 var bench=guard.Find("Equipment_Rack");Fit(bench,inv,floor);
 var table=props.Find("Dining_Table");Fit(table,inv,floor);
 var chair=h.Find("Interior_Chair");Fit(chair,inv,floor);
 // Keep the chair near the table instead of doubling the gap between them.
 chair.localPosition=new Vector3(table.localPosition.x,chair.localPosition.y,table.localPosition.z-.86f);Save(chair);
 // A small barrel near the back wall, outside the central aisle.
 var barrel=props.Find("Storage_Barrel");barrel.gameObject.SetActive(true);Save(barrel.gameObject);
 barrel.localPosition=new Vector3(-.75f,.52f,2.22f);Fit(barrel,inv,floor);
 var chimney=h.Find("06_Details/chimney");var cp=chimney.localPosition;cp.y+=.37f;chimney.localPosition=cp;Save(chimney);
 // The expanded wall overlaps the old tree and canopy unless they shift outward.
 var tree=root.transform.Find("Courtyard_Tree");tree.localPosition+=new Vector3(-3,0,-3);Save(tree);
 var canopy=root.transform.Find("Open_Timber_LeanTo");canopy.localPosition+=new Vector3(4,0,1.4f);Save(canopy);
 int i=0;foreach(Transform t in root.transform)if(t.name=="Approach_Flagstone"){t.localPosition=new Vector3(-15.1f+i*1.05f,t.localPosition.y,-21);i++;}
 var bush=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Decoration/Hilly_Prop_Bush_2.prefab"),root.transform);
 bush.name="Courtyard_Bush";bush.transform.localPosition=new Vector3(-36,.2f,-13.4f);
 foreach(var t in bush.GetComponentsInChildren<Transform>(true)){t.tag="Untagged";Save(t.gameObject);}
 float bh=BoundsOf(bush.transform).size.y;bush.transform.localScale*=.9f/bh;var bp=bush.transform.position;bp.y+=root.transform.position.y+.2f-BoundsOf(bush.transform).min.y;bush.transform.position=bp;Save(bush.transform);
 if(!PrefabUtility.SaveAsPrefabAsset(root,Path))throw new Exception("Save failed");
 return "Saved scale "+h.localScale+"; room redistributed and bush placed";
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}

