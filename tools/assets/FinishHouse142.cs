using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;
public static class FinishHouse142 {
 const string Path="Assets/Prefabs/Workshop/Locations/House1_1_OpenCourtyard.prefab";
 static Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/"+n+".mat");
 static void Record(UnityEngine.Object o)=>PrefabUtility.RecordPrefabInstancePropertyModifications(o);
 static Bounds B(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Move(Transform t,float x,float z){var p=t.localPosition;p.x=x;p.z=z;t.localPosition=p;Record(t);}
 static void Box(Transform h,string name,Vector3 pos,Vector3 size,Material mat){
 var o=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(o,h.gameObject.scene);o.name=name;o.transform.SetParent(h,false);o.transform.localPosition=pos;o.transform.localScale=size;
 UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=mat;
 }
 public static string Main(){
 var root=PrefabUtility.LoadPrefabContents(Path);
 try{
 var h=root.transform.Find("Stone_House_Open_Entrance");
 if(h.Find("Grounded_Stone_Plinth"))throw new Exception("Already updated");
 // Close the gap below the raised floor, retaining the checked stairs and floor collision.
 Box(h,"Grounded_Stone_Plinth",new Vector3(0,.208f,0),new Vector3(5.83f,.46f,5.83f),M("CastleStone_BlockDark"));
 var g=h.Find("09_Castle_Guardroom");var props=h.Find("08_Interior_Props");
 var bed=g.Find("Guard_Bed");var bedding=g.Find("Blue_Bedding");
 foreach(var t in new[]{bed,bedding}){
 var sc=t.localScale;sc.x*=1.7f;t.localScale=sc;Move(t,-1.83f,1.69f);
 }
 var b=B(bedding);float y=h.InverseTransformPoint(new Vector3(b.center.x,b.max.y,b.center.z)).y;
 // Bed length follows the house's local Z. Place two cushions at the head.
 foreach(float x in new[]{-2.02f,-1.64f})Box(h,"Linen_Pillow",new Vector3(x,y+.045f,2.02f),new Vector3(.30f,.09f,.24f),M("CastleStone_Trim"));
 foreach(var name in new[]{"Interior_Banner","Interior_FlagCloth"}){var t=g.Find(name);t.gameObject.SetActive(true);Record(t.gameObject);}
 var chair=h.Find("Interior_Chair");var second=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyRonin/Medieval Blacksmith Props/Prefabs/Chair.prefab"),h);
 second.name="Interior_Chair_Back";second.transform.localScale=chair.localScale;second.transform.localRotation=Quaternion.identity;
 var table=props.Find("Dining_Table");
 second.transform.localPosition=new Vector3(table.localPosition.x,chair.localPosition.y,table.localPosition.z+.88f);
 foreach(var r in second.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>M("Roof_WarmBrown")).ToArray();Record(r);}
 var cp=second.transform.position;cp.y+=B(chair).min.y-B(second.transform).min.y;second.transform.position=cp;
 if(!second.GetComponentsInChildren<Collider>().Any()){var col=second.AddComponent<BoxCollider>();var cb=B(second.transform);var sc=second.transform.lossyScale;col.center=second.transform.InverseTransformPoint(cb.center);col.size=new Vector3(cb.size.x/Mathf.Abs(sc.x),cb.size.y/Mathf.Abs(sc.y),cb.size.z/Mathf.Abs(sc.z));}
 var barrel=props.Find("Storage_Barrel");Move(barrel,-.84f,2.24f);
 foreach(var r in barrel.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select((_,i)=>M(i==0?"Roof_WarmBrown":"CastleIron_Charcoal")).ToArray();Record(r);}
 if(!PrefabUtility.SaveAsPrefabAsset(root,Path))throw new Exception("Save failed");
 return "Saved stone plinth to ground, double bed, pillows, second chair, wooden barrel and rear banner";
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}
