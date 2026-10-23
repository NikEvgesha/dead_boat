using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class MakeCastleRiver
{
 const string Dir="Assets/Prefabs/Workshop/CastleServices/";
 static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool solid=true){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 static Material Mat(string n,Color c,float gloss){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+n+".mat");if(m!=null)return m;m=new Material(Shader.Find("Standard")){name=n,color=c};m.SetFloat("_Glossiness",gloss);AssetDatabase.CreateAsset(m,Dir+n+".mat");return m;}
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");
  var root=GameObject.Find("Castle_Services_Prototype").transform;if(root.Find("05_River")!=null)throw new Exception("River already installed");
  var courtyard=root.Find("02_Courtyard");var stone=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Paving_Gray.mat");var trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/CastleStone_Trim.mat");
  foreach(var t in courtyard.Cast<Transform>().Where(t=>t.name=="Solid_Ground"||t.name=="Central_Avenue"||t.name=="Avenue_Border").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  foreach(float side in new[]{-1f,1f})Box(side<0?"West_Bank":"East_Bank",courtyard,new Vector3(side*16.25f,-.05f,0),new Vector3(24.5f,.5f,53),stone);
  var river=new GameObject("05_River").transform;river.SetParent(root,false);
  var water=Mat("River_Blue",new Color(.075f,.34f,.47f),.65f);var bed=Mat("Riverbed_Dark",new Color(.12f,.18f,.2f),.05f);var ripple=Mat("Water_Ripples",new Color(.3f,.62f,.68f),.5f);
  Box("Riverbed",river,new Vector3(0,-1.25f,0),new Vector3(8,.3f,69),bed);
  Box("Water_Surface",river,new Vector3(0,-.3f,0),new Vector3(8,.035f,69),water,false);
  foreach(float side in new[]{-1f,1f})Box("Stone_Embankment",river,new Vector3(side*4.075f,-.25f,0),new Vector3(.15f,.9f,53),trim);
  for(int i=0;i<38;i++){float x=Mathf.Sin(i*4.1f)*2.9f;var go=Box("Current_Ripple",river,new Vector3(x,-.278f,-33+i*1.75f),new Vector3(.35f+(i%4)*.17f,.005f,.024f),ripple,false);go.transform.localRotation=Quaternion.Euler(0,9+(i%3)*7,0);}
  var sb=new System.Text.StringBuilder();
  foreach(Transform h in root.Find("03_Service_Houses"))
  {
   float top=h.Find("01_Floor").GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y);float delta=.2f-top;h.position+=Vector3.up*delta;
   foreach(var stair in h.Find("05_Porch").Cast<Transform>().Where(t=>t.name=="stairsWood"||t.name=="Stone_Steps").ToArray())UnityEngine.Object.DestroyImmediate(stair.gameObject);
   // Interaction anchors must remain just above the newly aligned interior floor.
   foreach(Transform anchor in h.Find("14_Interaction_Anchors"))if(anchor.name=="Approach")anchor.position=new Vector3(anchor.position.x,.23f,anchor.position.z);
   sb.AppendLine(h.name+" lowered="+(-delta).ToString("F3")+" floorTop="+h.Find("01_Floor").GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y).ToString("F3"));
  }
  Physics.SyncTransforms();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/castle-river-result.txt",sb.ToString());
  SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(42,-35,0),40,false,true);SceneView.lastActiveSceneView?.Focus();
 }
}
