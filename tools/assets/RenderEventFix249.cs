using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class RenderEventFix249
{
 static Transform Copy(Transform src,Transform parent,Scene scene){
 var g=new GameObject(src.name);SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(parent,false);g.transform.localPosition=src.localPosition;g.transform.localRotation=src.localRotation;g.transform.localScale=src.localScale;
 var f=src.GetComponent<MeshFilter>();var r=src.GetComponent<MeshRenderer>();if(f&&r&&r.enabled){g.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;}
 foreach(Transform t in src)if(t.gameObject.activeSelf)Copy(t,g.transform,scene);return g.transform;
 }
 public static string Run(){bool fog=RenderSettings.fog;RenderSettings.fog=false;try{
 string[] paths={"Assets/Events/NewYear/ShorePOI/POI_05_Gift_Sled.prefab","Assets/Events/NewYear/NY_Ruins_1.prefab","Assets/Prefabs/Workshop/Ship1/Ship1_ProductionVisual.prefab","Assets/Prefabs/Locations/Enemy/castle_1.prefab"};
 string suffix=File.Exists("Library/EventFix249/applied.txt")?"after":"before";
 for(int i=0;i<paths.Length;i++){
 var scene=EditorSceneManager.NewPreviewScene();try{
 var root=Copy(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]).transform,null,scene);root.position=Vector3.zero;
 var rs=root.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 var g=new GameObject("Camera");SceneManager.MoveGameObjectToScene(g,scene);var c=g.AddComponent<Camera>();c.scene=scene;c.fieldOfView=50;c.nearClipPlane=.05f;c.farClipPlane=1000;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.25f,.4f,.5f);
 if(i==0){c.transform.position=new Vector3(12,2,-10);c.transform.LookAt(new Vector3(20,2,0));}
 else{c.transform.position=b.center+new Vector3(-.65f,.7f,-1)*b.size.magnitude*.85f;c.transform.LookAt(b.center);}
 var light=new GameObject("Sun");SceneManager.MoveGameObjectToScene(light,scene);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.15f;l.transform.rotation=Quaternion.Euler(40,-35,0);
 var rt=RenderTexture.GetTemporary(1200,850,24);var tex=new Texture2D(1200,850,TextureFormat.RGB24,false);var prior=RenderTexture.active;
 try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes($"Docs/locations/previews/event-fix249-{i}-{suffix}.png",tex.EncodeToPNG());}
 finally{c.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
 }finally{EditorSceneManager.ClosePreviewScene(scene);}}
 return "Four geometry views saved: "+suffix;
 }finally{RenderSettings.fog=fog;}
 }
}
