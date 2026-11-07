using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class HouseStudy129 {
 const string ScenePath="Assets/Scenes/House1Workshop.unity";
 const string Design="Assets/Prefabs/Workshop/Locations/House1_1_StoneTimber.prefab";
 static Bounds B(GameObject o){var rs=o.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Instance(string path,Scene scene){var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception(path);return (GameObject)PrefabUtility.InstantiatePrefab(source,scene);}
 public static string Main(){
 if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(ScenePath)||File.Exists(Design))throw new Exception("Edit mode and unused output paths required");
 var previous=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
 var original=Instance("Assets/Prefabs/Locations/Enemy/house1_1.prefab",scene);original.name="01_Original_house1_1";
 var old=B(original);original.transform.position+=new Vector3(-22-old.center.x,0,-old.center.z);
 var study=Instance("Assets/Prefabs/Workshop/StoneCottage_01.prefab",scene);study.name="House1_1_StoneTimber";
 var roof=study.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Slate_Tiles");
 var matNames=new[]{"RustTiles_Base","RustTiles_Dark","RustTiles_Light"};
 var roofMats=roof.sharedMaterials.Select((m,i)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/"+matNames[i%3]+".mat")).ToArray();
 if(roofMats.Any(m=>!m))throw new Exception("Roof materials missing");roof.sharedMaterials=roofMats;
 PrefabUtility.RecordPrefabInstancePropertyModifications(roof);
 var asset=PrefabUtility.SaveAsPrefabAssetAndConnect(study,Design,InteractionMode.AutomatedAction);
 if(!asset)throw new Exception("Failed to save study");
 study.name="02_Stone_Timber_Study";var b=B(study);study.transform.position+=new Vector3(16-b.center.x,0,-b.center.z);
 var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Workshop_Floor";ground.transform.position=new Vector3(-2,-.55f,0);ground.transform.localScale=new Vector3(78,1,42);
 var groundMat=new Material(Shader.Find("Standard")){color=new Color(.30f,.37f,.29f)};AssetDatabase.CreateAsset(groundMat,"Assets/Prefabs/Workshop/Locations/House1Workshop_Ground.mat");ground.GetComponent<Renderer>().sharedMaterial=groundMat;
 var light=new GameObject("Workshop_Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;light.transform.rotation=Quaternion.Euler(49,-40,0);
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.47f,.49f,.53f);
 const ulong mask=1UL<<59;EditorSceneManager.SetSceneCullingMask(scene,mask);
 var cam=new GameObject("Workshop_Camera").AddComponent<Camera>();cam.transform.position=new Vector3(16,42,-62);cam.transform.LookAt(new Vector3(-3,1,0));cam.orthographic=true;cam.orthographicSize=22;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.20f,.23f);cam.overrideSceneCullingMask=mask;
 if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Scene save failed");
 foreach(var other in previous)if(other.isLoaded)foreach(var go in other.GetRootGameObjects())SceneVisibilityManager.instance.Hide(go,true);
 Selection.activeGameObject=study;var view=SceneView.lastActiveSceneView;if(view){view.LookAt(B(study).center,Quaternion.Euler(22,-35,0),12);view.Repaint();}
 var rt=RenderTexture.GetTemporary(1440,900,24);var active=RenderTexture.active;Texture2D tex=null;
 try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes("Docs/locations/previews/house1-stone-timber-study.png",tex.EncodeToPNG());}
 finally{cam.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);}
 return "Saved scene and prefab; original linked="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(original)+"; study linked="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(study)+"; study bounds="+B(study).size;
 }
}
