using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class CompareHouses142{
 static readonly string[] Paths={"Assets/Prefabs/Locations/Enemy/house1_1.prefab","Assets/Prefabs/Workshop/Locations/House1_2_LocationVariant.prefab","Assets/Prefabs/Workshop/Locations/House1_3_LocationVariant.prefab"};
 static GameObject Inst(string path,Scene scene){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);}
 static Transform Home(GameObject g)=>g.GetComponentsInChildren<Transform>().Single(t=>t.name=="Stone_House_Open_Entrance");
 static void Floor(Scene scene,Vector3 p,Vector3 s){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(floor,scene);floor.name="Workshop Ground";floor.transform.position=p;floor.transform.localScale=s;floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/Locations/House1Workshop_Ground.mat");}
 static void Check(string path){
 var preview=EditorSceneManager.NewPreviewScene();
 try{
 var o=Inst(path,preview);var h=Home(o);var entry=h.TransformPoint(new Vector3(0,.514f,0));
 if(h.Find("05_Porch").gameObject.activeSelf||h.Find("Grounded_Stone_Plinth")||Mathf.Abs(entry.y-.2f)>.015f)throw new Exception("Not flush "+path+" "+entry.y);
 Floor(preview,entry+Vector3.down*.5f,new Vector3(45,1,45));
 var go=EditorUtility.CreateGameObjectWithHideFlags("Walk probe",HideFlags.HideAndDontSave);SceneManager.MoveGameObjectToScene(go,preview);go.transform.position=h.TransformPoint(new Vector3(0,.55f,-5));
 var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();var c=go.AddComponent<CharacterController>();c.height=src.height;c.radius=src.radius;c.center=src.center;c.stepOffset=src.stepOffset;c.skinWidth=src.skinWidth;c.slopeLimit=src.slopeLimit;
 Physics.SyncTransforms();
 for(int i=0;i<290&&h.InverseTransformPoint(go.transform.position).z<1.2f;i++)c.Move(h.forward*.05f+Vector3.down*.02f);
 var pos=h.InverseTransformPoint(go.transform.position);if(pos.z<1.1f||!c.isGrounded)throw new Exception("Entry blocked "+path+" "+pos);
 if(o.transform.Find("SpawnItem").childCount!=4||o.transform.Find("SpawnEnemy").childCount!=5)throw new Exception("Spawn points missing");
 }finally{EditorSceneManager.ClosePreviewScene(preview);}
 }
 public static string Main(){
 const string path="Assets/Scenes/House1VariantsWorkshop.unity";
 if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(path))throw new Exception("Workshop already exists or playing");
 foreach(var p in Paths)Check(p);
 var previous=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
 for(int i=0;i<Paths.Length;i++){var o=Inst(Paths[i],scene);var h=Home(o);o.transform.Rotate(0,90,0,Space.World);o.name="House 1-"+(i+1);o.transform.position+=new Vector3((i-1)*31-h.position.x,0,-h.position.z);}
 Floor(scene,new Vector3(3,-.30f,0),new Vector3(104,1,39));
 var sun=new GameObject("Workshop Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(51,-30,0);
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.44f,.47f,.50f);
 const ulong mask=1UL<<58;EditorSceneManager.SetSceneCullingMask(scene,mask);
 var cam=new GameObject("Comparison Camera").AddComponent<Camera>();cam.transform.position=new Vector3(8,59,-65);cam.transform.LookAt(new Vector3(1,1,0));cam.orthographic=true;cam.orthographicSize=29;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.20f,.23f);cam.overrideSceneCullingMask=mask;
 if(!EditorSceneManager.SaveScene(scene,path))throw new Exception("Save failed");
 foreach(var old in previous)if(old.isLoaded)foreach(var r in old.GetRootGameObjects())SceneVisibilityManager.instance.Hide(r,true);
 foreach(var w in Resources.FindObjectsOfTypeAll<EditorWindow>())if(w.titleContent.text=="Прогулка — house1_1")w.Close();
 var view=SceneView.lastActiveSceneView;if(view){view.LookAt(new Vector3(1,0,0),Quaternion.LookRotation(cam.transform.forward),62);view.Focus();view.Repaint();}
 var rt=RenderTexture.GetTemporary(1680,900,24);var last=RenderTexture.active;Texture2D tex=null;
 try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1680,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1680,900),0,0);tex.Apply();File.WriteAllBytes("Docs/locations/previews/house1-three-variants.png",tex.EncodeToPNG());}
 finally{cam.targetTexture=null;RenderTexture.active=last;RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);}
 return "PASS: all three homes flush with ground, controller entered each, 4 loot and 5 enemy points retained; comparison scene saved and open";
 }
}
