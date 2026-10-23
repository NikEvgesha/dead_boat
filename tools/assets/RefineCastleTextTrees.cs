using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class RefineCastleTextTrees {
 static int Tris(GameObject g){int n=0;foreach(var mf in g.GetComponentsInChildren<MeshFilter>(true))if(mf.sharedMesh!=null)for(int s=0;s<mf.sharedMesh.subMeshCount;s++)n+=(int)mf.sharedMesh.GetIndexCount(s)/3;return n;}
 static Bounds Bounds(GameObject g){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static int Render(Camera c){var rt=new RenderTexture(256,128,24);var tex=new Texture2D(256,128,TextureFormat.RGB24,false);try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,256,128),0,0);tex.Apply();return tex.GetPixels().Count(p=>p.r>.25f&&p.g>.25f&&p.b>.25f);}finally{c.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 public static void Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected castle in Edit Mode");
 var root=GameObject.Find("Castle_Services_Prototype");var report=new System.Text.StringBuilder();
 AssetDatabase.Refresh();var shader=Shader.Find("Workshop/WorldTextDepth");if(shader==null||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Text shader failed");
 var texts=root.GetComponentsInChildren<TextMesh>(true);foreach(var group in texts.GroupBy(t=>t.font)){var font=group.Key;font.RequestCharactersInTexture(string.Concat(group.Select(t=>t.text))+"DEPTH TEST",48,FontStyle.Normal);var path="Assets/Prefabs/Workshop/GameAligned/WorldTextDepth_"+font.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;material.mainTexture=font.material.mainTexture;foreach(var t in group)t.GetComponent<Renderer>().sharedMaterial=material;}
 report.AppendLine("Depth-tested TextMesh materials: "+texts.Length);
 int before=0,after=0,replaced=0;var trees=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Shade_Tree"&&t.GetComponentsInChildren<Renderer>().Length>0&&Tris(t.gameObject)>0).ToArray();
 string[] assets={"Hilly_Prop_Tree_Cedar_1","Hilly_Prop_Tree_Pine_3","Hilly_Prop_Tree_Cedar_2"};
 foreach(var old in trees){var b=Bounds(old.gameObject);int n=Tris(old.gameObject);var path="Assets/Prefabs/Locations/Decoration/"+assets[replaced%3]+".prefab";var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);go.transform.SetParent(old.parent,false);go.name="Shade_Tree";go.transform.rotation=old.rotation;var nb=Bounds(go);go.transform.localScale*=b.size.y/nb.size.y;nb=Bounds(go);go.transform.position+=new Vector3(b.center.x-nb.center.x,b.min.y-nb.min.y,b.center.z-nb.center.z);int next=Tris(go);if(next>=n)throw new Exception("Tree not simpler");report.AppendLine(old.parent.name+": "+n+" -> "+next+" triangles; "+path);before+=n;after+=next;replaced++;UnityEngine.Object.DestroyImmediate(old.gameObject);}
 report.AppendLine("Replaced visible trees="+replaced+"; triangles="+before+" -> "+after+"; empty/removed trees left untouched.");
 // A native render checks actual text visibility with and without an opaque wall.
 var probe=new GameObject("Temporary_Text_Depth_Probe");var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);var cg=new GameObject("Temporary_Text_Depth_Camera");Material wm=null;
 try{probe.layer=29;probe.transform.position=new Vector3(200,0,0);var tm=probe.AddComponent<TextMesh>();tm.font=texts[0].font;tm.text="DEPTH TEST";tm.fontSize=48;tm.characterSize=.1f;tm.anchor=TextAnchor.MiddleCenter;tm.color=Color.white;probe.GetComponent<Renderer>().sharedMaterial=texts[0].GetComponent<Renderer>().sharedMaterial;
 wall.layer=29;wall.transform.position=new Vector3(200,0,-1);wall.transform.localScale=new Vector3(8,4,.2f);wm=new Material(Shader.Find("Unlit/Color"));wm.color=Color.black;wall.GetComponent<Renderer>().sharedMaterial=wm;
 var c=cg.AddComponent<Camera>();c.transform.position=new Vector3(200,0,-4);c.transform.rotation=Quaternion.identity;c.orthographic=true;c.orthographicSize=1.2f;c.cullingMask=1<<29;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;c.allowHDR=false;
 wall.SetActive(false);int visible=Render(c);wall.SetActive(true);int occluded=Render(c);if(visible<50||occluded!=0)throw new Exception("Depth failed "+visible+" / "+occluded);report.AppendLine("Native depth render PASS: visible pixels="+visible+", behind opaque wall="+occluded);
 }finally{UnityEngine.Object.DestroyImmediate(probe);UnityEngine.Object.DestroyImmediate(wall);UnityEngine.Object.DestroyImmediate(cg);if(wm!=null)UnityEngine.Object.DestroyImmediate(wm);}
 AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");File.WriteAllText("Docs/assets/examples/CASTLE_TEXT_TREES_VALIDATION.txt",report.ToString());
 }
}
