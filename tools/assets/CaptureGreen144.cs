using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class CaptureGreen144 {
 public static string Main(){
 var s=SceneManager.GetSceneByPath("Assets/Scenes/House1VariantsWorkshop.unity");if(!s.isLoaded)throw new Exception("Scene not open");
 var roots=s.GetRootGameObjects();var cam=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single(c=>c.name=="Comparison Camera");
 ulong mask=1UL<<58;EditorSceneManager.SetSceneCullingMask(s,mask);cam.overrideSceneCullingMask=mask;
 var log=new System.Text.StringBuilder();foreach(var r in roots.Where(r=>r.name.StartsWith("House 1-"))){var h=r.GetComponentsInChildren<Transform>().Single(t=>t.name=="Stone_House_Open_Entrance");log.AppendLine(r.name+" floor="+h.TransformPoint(new Vector3(0,.514f,0)).y+" trees="+r.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Extra_Tree")));}
 var rt=RenderTexture.GetTemporary(1680,900,24);var active=RenderTexture.active;Texture2D tex=null;var old=cam.targetTexture;
 try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1680,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1680,900),0,0);tex.Apply();File.WriteAllBytes("Docs/locations/previews/house1-greenery144.png",tex.EncodeToPNG());}finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);}
 return log.ToString();
 }}
