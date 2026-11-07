using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;
public static class DistinctCourts145 {
 const string Dir="Assets/Prefabs/Workshop/Locations/";
 static readonly string[] Names={"House1_1_OpenCourtyard","House1_2_RustCourtyard","House1_3_GreenCourtyard"};
 static readonly string[] Labels={"ЛЕСНИК","МАСТЕР","ПИСАРЬ"};
 static Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/"+n+".mat");
 static void Record(UnityEngine.Object x)=>PrefabUtility.RecordPrefabInstancePropertyModifications(x);
 static void Move(Transform t,Vector3 p){t.localPosition=p;Record(t);}
 static Bounds B(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Add(string path,Transform parent,string name,Vector3 pos,float height){
 var a=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!a)throw new Exception(path);
 var o=(GameObject)PrefabUtility.InstantiatePrefab(a,parent);o.name=name;o.transform.localPosition=pos;
 o.transform.localScale*=height/B(o.transform).size.y;var p=o.transform.position;p.y+=parent.TransformPoint(pos).y-B(o.transform).min.y;o.transform.position=p;
 foreach(var t in o.GetComponentsInChildren<Transform>(true)){t.tag="Untagged";Record(t.gameObject);}Record(o.transform);return o;}
 static GameObject Box(Transform parent,string name,Vector3 p,Vector3 scale,Material mat){
 var o=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(o,parent.gameObject.scene);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;
 UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
 public static string Main(){
 var logs=new System.Text.StringBuilder();
 for(int i=0;i<3;i++){
 var root=PrefabUtility.LoadPrefabContents(Dir+Names[i]+".prefab");
 try{
 if(root.transform.Find("Individual_Details"))throw new Exception("Already decorated "+Names[i]);
 var h=root.transform.Find("Stone_House_Open_Entrance");var props=h.Find("08_Interior_Props");var guard=h.Find("09_Castle_Guardroom");
 foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.IndexOf("snow",StringComparison.OrdinalIgnoreCase)>=0)){t.gameObject.SetActive(false);Record(t.gameObject);}
 foreach(Transform t in root.transform)if(t.name=="Approach_Flagstone"){t.GetComponent<Renderer>().sharedMaterial=M("Roof_WarmBrown");}
 foreach(var r in h.Find("06_Details/chimney").GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>M("CottageStone_Gray")).ToArray();Record(r);}
 var details=new GameObject("Individual_Details");SceneManager.MoveGameObjectToScene(details,root.scene);details.transform.SetParent(root.transform,false);
 Vector3[] sheds={new Vector3(-21,.2f,-10),new Vector3(-36,.2f,-11),new Vector3(-14,.2f,-15)};
 Move(root.transform.Find("Open_Timber_LeanTo"),sheds[i]);
 Move(root.transform.Find("Well"),new[]{new Vector3(-14,.2f,-9),new Vector3(-36,.2f,-25),new Vector3(-12,.2f,-7)}[i]);
 var cart=root.transform.Find("Cart");if(cart.gameObject.activeSelf)Move(cart,i==0?new Vector3(-18,.2f,-13):new Vector3(-36,.2f,-22));
 // Move existing human-scaled furniture to different walls; keep the center clear.
 var table=props.Find("Dining_Table");var bench=guard.Find("Equipment_Rack");
 if(i==1){
 Move(table,new Vector3(-1.45f,table.localPosition.y,-.5f));Move(bench,new Vector3(1.50f,bench.localPosition.y,1.85f));
 Move(guard.Find("Guard_Bed"),new Vector3(1.78f,guard.Find("Guard_Bed").localPosition.y,1.6f));
 Move(guard.Find("Blue_Bedding"),new Vector3(1.78f,guard.Find("Blue_Bedding").localPosition.y,1.6f));
 foreach(var t in h.Cast<Transform>().Where(t=>t.name=="Linen_Pillow")){var p=t.localPosition;p.x+=3.61f;Move(t,p);}
 }else if(i==2){Move(table,new Vector3(-1.45f,table.localPosition.y,-.5f));Move(bench,new Vector3(1.7f,bench.localPosition.y,1.8f));}
 foreach(var t in h.Cast<Transform>().Where(t=>t.name.StartsWith("Interior_Chair"))){
 var p=t.localPosition;p.x=table.localPosition.x;p.z=table.localPosition.z+(t.name.EndsWith("Back")?.88f:-.86f);Move(t,p);
 }
 var sign=new GameObject("Courtyard_Sign");SceneManager.MoveGameObjectToScene(sign,root.scene);sign.transform.SetParent(details.transform,false);sign.transform.localPosition=new Vector3(-14,.2f,-26);sign.transform.localRotation=Quaternion.Euler(0,-90,0);
 Box(sign.transform,"Timber_Post",new Vector3(0,.85f,0),new Vector3(.13f,1.7f,.13f),M("Roof_InteriorBrown"));
 Box(sign.transform,"Sign_Board",new Vector3(0,1.5f,0),new Vector3(1.65f,.55f,.13f),M("CastleStone_Trim"));
 var label=new GameObject("Sign_Label");SceneManager.MoveGameObjectToScene(label,root.scene);label.transform.SetParent(sign.transform,false);label.transform.localPosition=new Vector3(0,1.5f,-.075f);
 var text=label.AddComponent<TextMesh>();text.text=Labels[i];text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=.064f;text.color=new Color(.2f,.12f,.07f);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.GetComponent<Renderer>().sharedMaterial=text.font.material;
 string cat="Assets/Import/CatBorg Studio/3D Dungeon Lowpoly Pack/Prefabs/";
 Transform accent;
 if(i==0){
 accent=Add("Assets/Models/Item/barrel_small.prefab",details.transform,"Woodcutter_Stump",sheds[i]+new Vector3(-1.3f,0,-2),.65f).transform;
 Add("Assets/Import/BizulkaProduction/Hyper_Casual_Weapon/Prefabs/Axe.prefab",details.transform,"Woodcutters_Axe",sheds[i]+new Vector3(-1.3f,.65f,-2),.75f);
 }else if(i==1){
 accent=Add(cat+"03_Blacksmith_01_Anvil.prefab",details.transform,"Anvil",sheds[i]+new Vector3(.4f,0,-1.8f),.72f).transform;
 Add(cat+"03_Blacksmith_01_Hammer#1.prefab",details.transform,"Hammer",sheds[i]+new Vector3(.4f,.72f,-1.8f),.24f);
 }else{
 accent=Add("Assets/Models/Item/table_long.prefab",details.transform,"Book_Stall",sheds[i]+new Vector3(-.4f,0,-1.8f),.80f).transform;
 var top=B(accent);Add(cat+"01_Library_01_BigBook#1.prefab",details.transform,"Ledger",details.transform.InverseTransformPoint(new Vector3(top.center.x,top.max.y,top.center.z)),.16f);
 }
 // Persist four chosen pickup surfaces for updating the existing location points.
 Func<Transform,Vector3> point=t=>{var b=B(t);return root.transform.InverseTransformPoint(new Vector3(b.center.x,b.max.y+.08f,b.center.z));};
 var outerCrate=root.transform.Find("Open_Timber_LeanTo").GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Props_Crate"));
 logs.AppendLine(Names[i]+"|"+string.Join("|",new[]{point(table),point(bench),point(accent),point(outerCrate)}.Select(v=>string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4}",v.x,v.y,v.z))));
 if(!PrefabUtility.SaveAsPrefabAsset(root,Dir+Names[i]+".prefab"))throw new Exception("Save failed");
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 File.WriteAllText("Temp/points145.txt",logs.ToString());return logs.ToString();
 }
}
