using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ScatterCastleTerritory {
 static System.Random rng=new System.Random(73107);
 static float R(float a,float b){return a+(float)rng.NextDouble()*(b-a);}
 static Bounds B(GameObject g){var rr=g.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static bool Safe(Vector2 p){if(Mathf.Abs(p.x)<4.4f||Mathf.Abs(p.x)>21.8f||Mathf.Abs(p.y)>23)return false;foreach(float z in new[]{-14f,0f,14f}){if(Mathf.Abs(Mathf.Abs(p.x)-12)<4.1f&&Mathf.Abs(p.y-z)<5.35f)return false;if(Mathf.Abs(p.x)<9.3f&&Mathf.Abs(p.y-z)<1.5f)return false;}return true;}
 public static void Main(){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected castle Edit Mode");var root=GameObject.Find("Castle_Services_Prototype").transform;var report=new System.Text.StringBuilder();
 var barrier=root.Find("03_Service_Houses/02_Armory/17_Boarded_Entrance");barrier.localPosition=new Vector3(0,-.06f,.135f);barrier.localScale=new Vector3(1.23f,1.12f,1);report.AppendLine("Barricade: root local z +0.135, x scale 1.23, y scale 1.12; plank back surfaces adjacent to facade z=-3.0.");
 var props=root.Find("04_Plaza_Details").Cast<Transform>().Where(t=>t.gameObject.activeInHierarchy&&t.GetComponentsInChildren<Renderer>().Length>0).ToArray();
 var places=new[]{new Vector2(-20.4f,-18.2f),new Vector2(-18.6f,-17.5f),new Vector2(17.7f,-7.6f),new Vector2(19.6f,-5.7f),new Vector2(-18.8f,6.1f),new Vector2(-21f,8.7f),new Vector2(18.6f,18.3f),new Vector2(16.9f,19.8f)};
 var occupied=new System.Collections.Generic.List<Bounds>();int count=0;foreach(var t in props){if(!t.name.StartsWith("Delivery_"))continue;var p=places[count%places.Length];t.rotation=Quaternion.Euler(0,R(-35,35),0);var b=B(t.gameObject);t.position+=new Vector3(p.x-b.center.x,.2f-b.min.y,p.y-b.center.z);occupied.Add(B(t.gameObject));count++;}report.AppendLine("Existing exterior delivery props redistributed="+count+"; interiors and boat cargo untouched.");
 var fixedObjects=root.Find("06_Territory_Decor").GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();occupied.AddRange(fixedObjects.Select(r=>r.bounds));
 var grasses=root.Find("08_Distributed_Greenery").GetComponentsInChildren<Transform>().Where(t=>t.name=="Grass_Tuft"&&t.gameObject.activeInHierarchy&&t.GetComponentsInChildren<Renderer>().Length>0).ToArray();var accepted=new System.Collections.Generic.List<Vector2>();
 foreach(var t in grasses){Vector2 p=Vector2.zero;bool found=false;for(int attempt=0;attempt<2500;attempt++){float side=rng.Next(2)==0?-1:1;int region=rng.Next(4);float x=region<2?R(17,21.6f):region==2?R(4.6f,7.4f):R(9,16);float z=region==3?(rng.Next(2)==0?-7:7)+R(-.9f,.9f):R(-22.5f,22.5f);p=new Vector2(side*x,z);if(!Safe(p)||accepted.Any(a=>Vector2.Distance(a,p)<.57f))continue;bool hit=occupied.Any(b=>p.x>b.min.x-.5f&&p.x<b.max.x+.5f&&p.y>b.min.z-.5f&&p.y<b.max.z+.5f);if(hit)continue;found=true;break;}if(!found)throw new Exception("No grass placement");var scale=t.localScale;t.localScale=scale*R(.8f,1.18f);t.rotation=Quaternion.Euler(0,R(0,360),0);var bounds=B(t.gameObject);t.position+=new Vector3(p.x-bounds.center.x,.21f-bounds.min.y,p.y-bounds.center.z);accepted.Add(p);}
 report.AppendLine("Existing grass clumps scattered="+grasses.Length+"; unique irregular yaw/scale; entrances, homes, river and existing furniture excluded.");
 var probe=new GameObject("Temporary_Player_Size_Probe");try{var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.4f;cc.center=new Vector3(0,.9f,0);cc.skinWidth=.02f;cc.stepOffset=.25f;foreach(Transform h in root.Find("03_Service_Houses")){cc.enabled=false;probe.transform.position=h.TransformPoint(new Vector3(0,.55f,-6.3f));cc.enabled=true;Physics.SyncTransforms();for(int i=0;i<190;i++)cc.Move(h.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);var p=h.InverseTransformPoint(probe.transform.position);bool fisher=h.name=="02_Armory";if(fisher?p.z>-3.3f:p.z<1.1f)throw new Exception("Door check failed "+h.name+" "+p);report.AppendLine(h.name+": player 1.8m / radius .4m "+(fisher?"blocked":"passes")+" "+p);}}finally{UnityEngine.Object.DestroyImmediate(probe);}
 AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");File.WriteAllText("Docs/assets/examples/CASTLE_TERRITORY_WALK_VALIDATION.txt",report.ToString());
 }
}
