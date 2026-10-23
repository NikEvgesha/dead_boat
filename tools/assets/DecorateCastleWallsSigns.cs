using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class DecorateCastleWallsSigns {
 const string Dir="Assets/Prefabs/Workshop/GameAligned/";
 static Material wood,iron,gold,red,teal;
 static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
 static Material Mat(string n,Color c){var path=Dir+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Glossiness",.1f);return m;}
 static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static void Beam(string n,Transform p,Vector3 a,Vector3 b,float width,Material m){var g=Box(n,p,(a+b)*.5f,new Vector3(width,(b-a).magnitude,.08f),m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
 static Mesh Polygon(string n,Vector3[] points){var mesh=new Mesh{name=n};mesh.vertices=points.Concat(points).ToArray();var tri=new System.Collections.Generic.List<int>();for(int i=1;i<points.Length-1;i++){tri.Add(0);tri.Add(i);tri.Add(i+1);tri.Add(points.Length);tri.Add(points.Length+i+1);tri.Add(points.Length+i);}mesh.triangles=tri.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+n+".asset");return mesh;}
 static void MeshObj(string n,Transform p,Mesh mesh,Material mat,Vector3 pos){var t=Group(n,p);t.localPosition=pos;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;}
 public static void Main(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected castle Edit Mode");var root=GameObject.Find("Castle_Services_Prototype").transform;if(root.Find("11_Inner_Wall_Banners")!=null)throw new Exception("Already decorated");
 wood=Mat("Barricade_Weathered_Oak",new Color(.32f,.23f,.15f));iron=Mat("Banner_Iron",new Color(.19f,.21f,.23f));gold=Mat("Banner_Ochre",new Color(.67f,.5f,.23f));red=Mat("Banner_Burgundy",new Color(.4f,.11f,.12f));teal=Mat("Banner_Deep_Teal",new Color(.1f,.3f,.27f));var report=new System.Text.StringBuilder();
 foreach(Transform h in root.Find("03_Service_Houses")){
  var s=h.Find("13_Service_Sign");var text=s.Find("Service_Name").GetComponent<Renderer>();var board=s.Find("Signboard");var b=text.bounds;float minx=float.MaxValue,maxx=float.MinValue,miny=float.MaxValue,maxy=float.MinValue;
  foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})foreach(float z in new[]{b.min.z,b.max.z}){var p=s.InverseTransformPoint(new Vector3(x,y,z));minx=Mathf.Min(minx,p.x);maxx=Mathf.Max(maxx,p.x);miny=Mathf.Min(miny,p.y);maxy=Mathf.Max(maxy,p.y);}
  float w=Mathf.Max(board.localScale.x,maxx-minx+.58f),height=Mathf.Max(board.localScale.y,maxy-miny+.28f);board.localScale=new Vector3(w,height,board.localScale.z);board.localPosition=new Vector3((minx+maxx)*.5f,(miny+maxy)*.5f,board.localPosition.z);
  foreach(string n in new[]{"Left_Trim","Right_Trim"}){var t=s.Find(n);t.localPosition=new Vector3(board.localPosition.x+(n=="Left_Trim"?-1:1)*(w*.5f-.035f),board.localPosition.y,t.localPosition.z);t.localScale=new Vector3(t.localScale.x,height-.03f,t.localScale.z);}
  foreach(float side in new[]{-1f,1f})Box("Sign_Horizontal_Trim",s,board.localPosition+new Vector3(0,side*(height*.5f-.025f),-.10f),new Vector3(w,.045f,.025f),gold);
  if(minx<board.localPosition.x-w*.5f+.25f||maxx>board.localPosition.x+w*.5f-.25f)throw new Exception("Sign overflow");report.AppendLine(h.name+": board "+w.ToString("F2")+" x "+height.ToString("F2")+"; text "+(maxx-minx).ToString("F2")+" x "+(maxy-miny).ToString("F2")+" fits, font unchanged.");
 }
 var fisher=root.Find("03_Service_Houses/02_Armory");var barrier=Group("17_Boarded_Entrance",fisher);
 for(int i=0;i<5;i++){var board=Box("Rough_Plank_"+i,barrier,new Vector3((i%2==0?.04f:-.04f),.76f+i*.38f,-3.2f),new Vector3(1.48f,.26f,.12f),wood);board.transform.localRotation=Quaternion.Euler(0,0,i%2==0?4:-5);foreach(float x in new[]{-.52f,.52f}){var nail=Box("Square_Iron_Nail",board.transform,Vector3.zero,Vector3.one,iron);nail.transform.position=barrier.TransformPoint(new Vector3(x,.76f+i*.38f,-3.28f));nail.transform.rotation=barrier.rotation;nail.transform.localScale=new Vector3(.028f/1.48f,.028f/.26f,.035f/.12f);}}
 Beam("Diagonal_Brace",barrier,new Vector3(-.63f,.52f,-3.31f),new Vector3(.63f,2.55f,-3.31f),.19f,wood);Beam("Diagonal_Brace_2",barrier,new Vector3(.62f,.53f,-3.33f),new Vector3(-.62f,2.54f,-3.33f),.16f,wood);
 var col=barrier.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,1.54f,-3.22f);col.size=new Vector3(1.53f,2.12f,.28f);
 var probe=new GameObject("Temporary_Fisher_Door_Probe");try{var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.3f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.25f;cc.skinWidth=.02f;probe.transform.position=fisher.TransformPoint(new Vector3(0,.55f,-6.3f));Physics.SyncTransforms();for(int i=0;i<190;i++)cc.Move(fisher.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);var p=fisher.InverseTransformPoint(probe.transform.position);if(p.z>-3.5f)throw new Exception("Fisher entrance not blocked "+p);report.AppendLine("Fisher house retained; entrance physically blocked: controller local="+p);}finally{UnityEngine.Object.DestroyImmediate(probe);}
 var decor=Group("11_Inner_Wall_Banners",root);
 var cloth=Polygon("Castle_Banner_Cloth",new[]{new Vector3(-.8f,1.45f,0),new Vector3(.8f,1.45f,0),new Vector3(.8f,-1.05f,-.03f),new Vector3(.38f,-1.45f,.04f),new Vector3(0,-1.15f,0),new Vector3(-.38f,-1.45f,.04f),new Vector3(-.8f,-1.05f,-.03f)});
 var shield=Polygon("Castle_Banner_Shield",new[]{new Vector3(-.32f,.42f,0),new Vector3(.32f,.42f,0),new Vector3(.32f,-.14f,0),new Vector3(0,-.48f,0),new Vector3(-.32f,-.14f,0)});
 int count=0;foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-16.3f,-3.3f,3.2f,16.3f}){
  var banner=Group("Wall_Banner_"+count,decor);banner.position=new Vector3(side*22.73f,5.5f,z);banner.rotation=Quaternion.Euler(0,side<0?-90:90,0);MeshObj("Cloth",banner,cloth,count%2==0?red:teal,Vector3.zero);MeshObj("Shield_Emblem",banner,shield,gold,new Vector3(0,.15f,-.06f));Box("Emblem_Cross_V",banner,new Vector3(0,.17f,-.09f),new Vector3(.06f,.47f,.025f),iron);Box("Emblem_Cross_H",banner,new Vector3(0,.24f,-.10f),new Vector3(.36f,.06f,.025f),iron);Box("Top_Hanging_Rod",banner,new Vector3(0,1.54f,0),new Vector3(1.95f,.075f,.08f),iron);foreach(float x in new[]{-.63f,.63f})Box("Rod_Wall_Bracket",banner,new Vector3(x,1.54f,.13f),new Vector3(.075f,.075f,.34f),iron);count++;
 }
 report.AppendLine("8 inside-facing wall banners: burgundy/teal, ochre shield emblems; 608 triangles total, no colliders, bottom y=4.05.");
 AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");File.WriteAllText("Docs/assets/examples/CASTLE_SIGNS_WALLS_VALIDATION.txt",report.ToString());
 }
}
