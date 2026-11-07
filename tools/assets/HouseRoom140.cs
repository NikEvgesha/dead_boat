using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class HouseRoom140 {
 const string Court="Assets/Prefabs/Workshop/Locations/House1_1_OpenCourtyard.prefab";
 const string House="Assets/Prefabs/Locations/Enemy/house1_1.prefab";
 const string Mats="Assets/Prefabs/Workshop/CottageGeometry/";
 static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+n+".mat");
 static void Active(Transform t,bool active){if(!t)throw new Exception("Missing room object");t.gameObject.SetActive(active);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
 static void Move(Transform t,Vector3 pos){t.localPosition=pos;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
 static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 public static string Main(){
 var root=PrefabUtility.LoadPrefabContents(Court);
 try{
 var h=root.transform.Find("Stone_House_Open_Entrance");
 if(h.Find("Oak_Plank_Floor"))throw new Exception("Room already furnished");
 var guard=h.Find("09_Castle_Guardroom");var props=h.Find("08_Interior_Props");
 foreach(var n in new[]{"Guard_Chest","Supply_Chest","Wall_Shield","Rack_Weapons","Interior_Banner","Interior_FlagCloth"})Active(guard.Find(n),false);
 foreach(var t in guard.Cast<Transform>().Where(t=>t.name=="Entrance_Sconce"))Move(t,new Vector3(t.localPosition.x,1.40f,-3.58f));
 Active(props,true);
 foreach(Transform t in props)Active(t,t.name=="Dining_Table");
 var table=props.Find("Dining_Table");Move(table,new Vector3(1.57f,.49f,-.36f));
 var chair=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyRonin/Medieval Blacksmith Props/Prefabs/Chair.prefab"),h);
 chair.name="Interior_Chair";chair.transform.localPosition=new Vector3(1.52f,.51f,-1.98f);chair.transform.localRotation=Quaternion.Euler(0,180,0);
 var cb=BoundsOf(chair.transform);chair.transform.localScale*=.88f/cb.size.y;
 chair.transform.position+=Vector3.up*(h.TransformPoint(new Vector3(0,.515f,0)).y-BoundsOf(chair.transform).min.y);
 if(!chair.GetComponentsInChildren<Collider>().Any()){var col=chair.AddComponent<BoxCollider>();var b=BoundsOf(chair.transform);col.center=chair.transform.InverseTransformPoint(b.center);var scale=chair.transform.lossyScale;col.size=new Vector3(b.size.x/Mathf.Abs(scale.x),b.size.y/Mathf.Abs(scale.y),b.size.z/Mathf.Abs(scale.z));}
 var floor=new GameObject("Oak_Plank_Floor");SceneManager.MoveGameObjectToScene(floor,root.scene);floor.transform.SetParent(h,false);
 for(int i=0;i<15;i++){
 var plank=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(plank,root.scene);plank.name="Oak_Plank";plank.transform.SetParent(floor.transform,false);
 plank.transform.localPosition=new Vector3(-2.52f+i*.36f,.507f,0);plank.transform.localScale=new Vector3(.355f,.014f,5.38f);
 UnityEngine.Object.DestroyImmediate(plank.GetComponent<Collider>());plank.GetComponent<Renderer>().sharedMaterial=Mat(i%5==1?"Roof_InteriorBrown":"Roof_WarmBrown");
 }
 var roof=h.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Continuous_Pitched_Roof");
 var renderer=roof.GetComponent<Renderer>();var mats=renderer.sharedMaterials;var mesh=roof.sharedMesh;
 var normals=mesh.normals;
 int underside=Enumerable.Range(0,mesh.subMeshCount).OrderBy(i=>mesh.GetTriangles(i).Average(j=>normals[j].y)).First();
 mats[underside]=Mat("Roof_InteriorBrown");renderer.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
 if(!PrefabUtility.SaveAsPrefabAsset(root,Court))throw new Exception("Save failed");
 return "Saved: furniture, floor, sconces, underside slot "+underside;
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 public static string Check(){
 var p=AssetDatabase.LoadAssetAtPath<GameObject>(House);var preview=new PreviewRenderUtility();
 try{
 var o=(GameObject)PrefabUtility.InstantiatePrefab(p,preview.camera.scene);preview.AddSingleGO(o);
 var h=o.GetComponentsInChildren<Transform>().Single(t=>t.name=="Stone_House_Open_Entrance");
 var items=o.transform.Find("SpawnItem");var table=h.Find("08_Interior_Props/Dining_Table");var bench=h.Find("09_Castle_Guardroom/Equipment_Rack");
 var log=new System.Text.StringBuilder();
 foreach(var pair in new[]{table,bench}){var b=BoundsOf(pair);log.AppendLine(pair.name+" spawn local "+items.InverseTransformPoint(new Vector3(b.center.x,b.max.y+.08f,b.center.z)).ToString("F4"));}
 var cam=preview.camera;cam.transform.position=h.TransformPoint(new Vector3(0,1.95f,-2.48f));cam.transform.LookAt(h.TransformPoint(new Vector3(0,1.55f,1.5f)));cam.fieldOfView=87;cam.nearClipPlane=.045f;preview.ambientColor=new Color(.68f,.64f,.58f);preview.lights[0].intensity=.7f;preview.lights[1].intensity=.4f;
 var fill=EditorUtility.CreateGameObjectWithHideFlags("Preview fill",HideFlags.HideAndDontSave,typeof(Light));SceneManager.MoveGameObjectToScene(fill,preview.camera.scene);fill.transform.position=h.TransformPoint(new Vector3(0,2.65f,-1.8f));var light=fill.GetComponent<Light>();light.type=LightType.Point;light.intensity=3;light.range=10;
 preview.BeginStaticPreview(new Rect(0,0,1100,800));cam.Render();var tex=preview.EndStaticPreview();File.WriteAllBytes("Docs/locations/previews/house1-furnished-interior.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
 return log.ToString();
 }finally{preview.Cleanup();}
 }
}
