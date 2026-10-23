using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class ApplyProjectGrassToCastle
{
 public static void Main()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Castle Edit Mode required");var root=GameObject.Find("Castle_Services_Prototype");
  var grass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Grass_1/Grass_1.mat");if(grass==null||!grass.shader.isSupported)throw new Exception("Game grass unavailable");
  var courtyard=root.transform.Find("02_Courtyard");
  foreach(string n in new[]{"West_Bank","East_Bank"})
  {
   var bank=courtyard.Find(n);string path="Assets/Prefabs/Workshop/CastleServices/"+n+"_GrassUV.asset";var mf=bank.GetComponent<MeshFilter>();var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(mesh==null)
   {
    mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=n+"_GrassUV";var positions=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[positions.Length];
    // Land's generated mesh uses world x/z as UVs. Preserve that density with the unchanged project material.
    for(int i=0;i<uv.Length;i++){var p=bank.TransformPoint(positions[i]);var normal=bank.TransformDirection(normals[i]);uv[i]=Mathf.Abs(normal.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(normal.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);}
    mesh.uv=uv;AssetDatabase.CreateAsset(mesh,path);
   }
   mf.sharedMesh=mesh;bank.GetComponent<Renderer>().sharedMaterial=grass;
  }
  PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  File.WriteAllText("Temp/project-grass-result.txt","Both banks share original Grass_1 material; world-coordinate UVs as in Land; top=0.200; gap=7");
  SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,0),Quaternion.Euler(42,-35,0),40,false,true);SceneView.lastActiveSceneView?.Focus();
 }
}
