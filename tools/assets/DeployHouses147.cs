using System;using System.Linq;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEditor;
public static class DeployHouses147{
 const string Enemy="Assets/Prefabs/Locations/Enemy/",Workshop="Assets/Prefabs/Workshop/Locations/";
 static readonly string[] Courts={"House1_1_OpenCourtyard","House1_2_RustCourtyard","House1_3_GreenCourtyard"};
 static readonly string[] Sources={Enemy+"house1_1.prefab",Workshop+"House1_2_LocationVariant.prefab",Workshop+"House1_3_LocationVariant.prefab"};
 static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
 static Transform Home(GameObject r)=>r.GetComponentsInChildren<Transform>().Single(t=>t.name=="Stone_House_Open_Entrance");
 static Vector3 Surface(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled && r.bounds.size.sqrMagnitude>.0001f).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return new Vector3(b.center.x,b.max.y+.08f,b.center.z);}
 static Vector3 Floor(Transform h)=>h.TransformPoint(new Vector3(0,.514f,0));
 static GameObject Court(int i,Transform parent,Vector3 target,float houseYaw,bool buildingOnly){var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Workshop+Courts[i]+".prefab"),parent);o.transform.rotation=Quaternion.Euler(0,houseYaw+90,0);var h=Home(o);o.transform.position+=target-Floor(h);if(buildingOnly)foreach(Transform t in o.transform)if(t!=h){t.gameObject.SetActive(false);Record(t.gameObject);}Record(o.transform);return o;}
 static void Save(GameObject root,string path){if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new Exception("Save failed: "+path);}
 public static string Main(){
 var names=new[]{"house1_1","house1_2","house2_1","house2_2","house3_1","house3_2","Village_1","Village_2"};
 Directory.CreateDirectory("Temp/DeployHouses147");foreach(var n in names){var backup="Temp/DeployHouses147/"+n+".prefab";if(File.Exists(backup))throw new Exception("Backup exists; do not rerun deployment");File.Copy(Enemy+n+".prefab",backup);}
 // Read authored item surfaces before modifying any source gameplay prefab.
 var sourcePoints=new Vector3[3][];for(int i=0;i<3;i++){var s=AssetDatabase.LoadAssetAtPath<GameObject>(Sources[i]);var court=Home(s).parent;sourcePoints[i]=s.GetComponent<LocationContentSpawner>().itemSpawnPoints.Select(p=>court.InverseTransformPoint(p.spawnTransform.position)).ToArray();}
 var report=new System.Text.StringBuilder();
 for(int index=0;index<names.Length;index++){string n=names[index],path=Enemy+n+".prefab";var root=PrefabUtility.LoadPrefabContents(path);try{
 var center=root.transform.Cast<Transform>().Single(t=>t.name.StartsWith("PointCenter"));var sp=root.GetComponent<LocationContentSpawner>();var oldItems=sp.itemSpawnPoints.Select(p=>p.spawnTransform).ToArray();var oldEnemies=sp.enemySpawnPoints.ToArray();
 if(index<6){int style=index/2;bool right=index%2==1;GameObject court;
 if(index==0)court=Home(root).parent.gameObject;
 else{var old=center.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("GunStore"));var roof=old.GetComponentsInChildren<Renderer>().First(r=>r.name=="Mesh55");var target=roof.bounds.center;target.y=.26f;
 foreach(Transform child in center){child.gameObject.SetActive(false);Record(child.gameObject);}court=Court(style,center,target,right?90:270,false);}
 for(int i=0;i<oldItems.Length;i++){oldItems[i].position=court.transform.TransformPoint(sourcePoints[style][i]);Record(oldItems[i]);}
 var h=Home(court);for(int i=0;i<oldEnemies.Length;i++){oldEnemies[i].position=h.TransformPoint(new Vector3((i%3-1)*1.4f,.54f,-4.8f-(i/3)*1.8f));Record(oldEnemies[i]);}
 report.AppendLine(n+": style="+Courts[style]+" side="+(right?"right":"left")+" points="+oldItems.Length+"/"+oldEnemies.Length);
 }else{
 var oldBuildings=center.GetComponentsInChildren<Transform>().Where(t=>t.name=="dom"||t.name.StartsWith("dom (")||t.name.StartsWith("GunStore")).ToArray();
 var newHomes=new List<Transform>();var surfaces=new List<Vector3>();
 for(int j=0;j<oldBuildings.Length;j++){var old=oldBuildings[j];var roof=old.GetComponentsInChildren<Renderer>().First(r=>r.name=="Mesh55");var target=roof.bounds.center;target.y=.26f;var local=center.InverseTransformPoint(target);float yaw=center.eulerAngles.y+(local.z<0?180:0);
 old.gameObject.SetActive(false);Record(old.gameObject);var court=Court(j%3,center,target,yaw,true);court.name="Village_NewHouse_"+(j+1);Record(court);var h=Home(court);newHomes.Add(h);
 var props=h.Find("08_Interior_Props");var guard=h.Find("09_Castle_Guardroom");surfaces.Add(Surface(props.Find("Dining_Table")));surfaces.Add(Surface(guard.Find("Equipment_Rack")));}
 // Keep every existing point and its item restrictions. Extra points use open floor near entrance.
 for(int i=0;i<oldItems.Length;i++){oldItems[i].position=i<surfaces.Count?surfaces[i]:newHomes[(i-surfaces.Count)%newHomes.Count].TransformPoint(new Vector3(0,.56f,-1.2f));Record(oldItems[i]);}
 for(int i=0;i<oldEnemies.Length;i++){var h=newHomes[i%newHomes.Count];oldEnemies[i].position=h.TransformPoint(new Vector3(i<newHomes.Count?-1.3f:1.3f,.54f,-4.5f));Record(oldEnemies[i]);}
 report.AppendLine(n+": replaced buildings="+oldBuildings.Length+" points="+oldItems.Length+"/"+oldEnemies.Length);
 }
 // Extend the existing cleaner rectangle only as far as the new authored geometry requires.
 var renderers=center.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.bounds.size.sqrMagnitude>.0001f).ToArray();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);var p1=root.transform.Find("Point1");var p2=root.transform.Find("Point2");
 Vector3 min=Vector3.Min(p1.position,p2.position),max=Vector3.Max(p1.position,p2.position);min=Vector3.Min(min,b.min-Vector3.one);max=Vector3.Max(max,b.max+Vector3.one);p1.position=new Vector3(min.x,0,min.z);p2.position=new Vector3(max.x,0,max.z);Record(p1);Record(p2);
 if(oldItems.Any(t=>!t)||oldEnemies.Any(t=>!t))throw new Exception("Lost references");Save(root,path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
 File.WriteAllText("Temp/deploy147-result.txt",report.ToString());return report.ToString();}
}
