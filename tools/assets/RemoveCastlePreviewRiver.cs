using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class RemoveCastlePreviewRiver
{
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");
  var root=GameObject.Find("Castle_Services_Prototype").transform;var river=root.Find("05_River");if(river==null||root.Find("05_RuntimeWaterGap_7m")!=null)throw new Exception("Expected preview river; correction already applied?");
  UnityEngine.Object.DestroyImmediate(river.gameObject);
  var ground=root.Find("02_Courtyard");foreach(float side in new[]{-1f,1f}){var bank=ground.Find(side<0?"West_Bank":"East_Bank");bank.localPosition=new Vector3(side*16,-.05f,0);bank.localScale=new Vector3(25,.5f,53);}
  foreach(Transform h in root.Find("03_Service_Houses"))h.position+=Vector3.up*.01f;
  var anchor=new GameObject("05_RuntimeWaterGap_7m").transform;anchor.SetParent(root,false);anchor.localPosition=new Vector3(0,-.67f,0);
  Physics.SyncTransforms();PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(42,-35,0),40,false,true);SceneView.lastActiveSceneView?.Focus();
 }
}
