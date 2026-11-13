using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;
public static class ApplyEventFix249
{
 const string Winter="Assets/Events/NewYear/";
 static readonly List<string> log=new();
 static readonly Dictionary<Mesh,Mesh> repaired=new();
 static void Backup(string path){string p="Library/EventFix249/Backup/"+path;Directory.CreateDirectory(Path.GetDirectoryName(p));if(!File.Exists(p))File.Copy(path,p);}
 static void Edit(string path,Func<GameObject,bool> action){Backup(path);var g=PrefabUtility.LoadPrefabContents(path);try{if(action(g)){PrefabUtility.SaveAsPrefabAsset(g,path);log.Add("SAVED "+path);}}finally{PrefabUtility.UnloadPrefabContents(g);}}
 static Material Mat(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path);
 static Transform Shape(Transform parent,string name,PrimitiveType kind,Vector3 position,Vector3 size,Material material,bool solid=false){
 var g=GameObject.CreatePrimitive(kind);g.name=name;g.transform.SetParent(parent,false);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
 if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());else{g.layer=6;g.tag="Stone";}return g.transform;
 }
 static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float thickness,Material material){var t=Shape(parent,name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(thickness,thickness,Vector3.Distance(a,b)),material);t.rotation=Quaternion.LookRotation(b-a);}
 static bool Hospital(GameObject g){int n=0;
 foreach(var point in g.GetComponentsInChildren<StorePoint>(true).Where(p=>p.GetComponentsInParent<Transform>(true).Any(t=>t.name.Contains("Hospital")||t.name=="06_AnimalMerge"))){
 var so=new SerializedObject(point);var buy=(GameObject)so.FindProperty("_BuyInfoCanvas").objectReferenceValue;var price=(GameObject)so.FindProperty("_PriceCanvas").objectReferenceValue;
 foreach(var cv in new[]{buy,price}){if(!cv)continue;var rect=cv.transform as RectTransform;if(!rect||rect.rect.width*Mathf.Abs(rect.lossyScale.x)<4.5f)continue;
 rect.localScale=Vector3.one*.0055f;if(cv==price){var p=rect.position;p.y=buy.transform.position.y+.55f;rect.position=p;}PrefabUtility.RecordPrefabInstancePropertyModifications(rect);n++;}
 }
 log.Add("HOSPITAL "+g.name+" resized="+n);return n>0;
 }
 static bool Sled(GameObject g){var bank=g.transform.Find("Bank_05_Gift_Sled");if(!bank||bank.localScale.x<1.5f)return false;
 var anchor=new Vector3(20,0,0);foreach(Transform child in g.transform){child.localPosition=anchor+(child.localPosition-anchor)*.5f;child.localScale*=.5f;PrefabUtility.RecordPrefabInstancePropertyModifications(child);}
 var box=g.GetComponent<BoxCollider>();box.center=anchor+(box.center-anchor)*.5f;box.size*=.5f;log.Add("SLED all geometry, collision, loot and guard positions scaled 0.5 about bank center X20");return true;}
 static bool Ruins(GameObject g){if(g.transform.Find("Winter_Ruins249"))return false;var old=g.transform.Find("NewYear_Decoration");if(old)old.gameObject.SetActive(false);
 var parent=new GameObject("Winter_Ruins249").transform;parent.SetParent(g.transform,false);var wood=Mat(Winter+"Route179/Log_Bark_Ridges.mat");var gold=Mat(Winter+"Gold.mat");var red=Mat(Winter+"Ruby.mat");var blue=Mat(Winter+"WinterTown170/Gift_Blue.mat");
 var a=new Vector3(-119,3.3f,-5);var b=new Vector3(-104,3.3f,-5);
 foreach(var p in new[]{a,b}){Shape(parent,"Garland grounded post",PrimitiveType.Cube,new Vector3(p.x,1.65f,p.z),new Vector3(.16f,3.3f,.16f),wood);Shape(parent,"Post cap",PrimitiveType.Sphere,p,Vector3.one*.23f,gold);}
 Vector3 previous=a;for(int i=0;i<=24;i++){float u=i/24f;var p=Vector3.Lerp(a,b,u)-Vector3.up*Mathf.Sin(u*Mathf.PI)*.45f;if(i>0)Beam(parent,"Suspended cord",previous,p,.035f,wood);Shape(parent,"Festive light",PrimitiveType.Sphere,p,Vector3.one*.16f,i%3==0?red:gold);previous=p;}
 foreach(var item in new[]{(p:new Vector3(-119,0,-6.2f),s:.65f,m:red),(p:new Vector3(-118.15f,0,-6.1f),s:.8f,m:blue),(p:new Vector3(-104,0,-6.2f),s:.7f,m:red)}){
 var p=item.p+Vector3.up*item.s*.5f;Shape(parent,"Wrapped present",PrimitiveType.Cube,p,Vector3.one*item.s,item.m);
 Shape(parent,"Gift ribbon",PrimitiveType.Cube,p+Vector3.up*(item.s*.51f),new Vector3(item.s*1.02f,.025f,item.s*.15f),gold);
 Shape(parent,"Gift ribbon cross",PrimitiveType.Cube,p+Vector3.up*(item.s*.52f),new Vector3(item.s*.15f,.025f,item.s*1.02f),gold);}
 log.Add("RUINS unsupported line/remote gift grid disabled; lit cord attached to two grounded posts beside actual arches");return true;
 }
 static bool Landings(GameObject g){if(g.transform.Find("Stair_Landings249")){Bake(g);return true;}
 var group=new GameObject("Stair_Landings249").transform;group.SetParent(g.transform,false);
 var stairs=g.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="wall-narrow-stairs").ToArray();if(stairs.Length!=2)throw new Exception("Expected two stairs: "+g.name);
 var court=g.GetComponentsInChildren<Transform>(true).First(t=>t.name.StartsWith("PointCenter"));
 foreach(var f in stairs){var v=f.sharedMesh.vertices.Select(f.transform.TransformPoint).ToArray();var tr=f.sharedMesh.triangles;var faces=new List<(float y,float area,Vector3 c)>();
 for(int i=0;i<tr.Length;i+=3){var a=v[tr[i]];var b=v[tr[i+1]];var c=v[tr[i+2]];var n=Vector3.Cross(b-a,c-a);if(n.normalized.y>.99f)faces.Add(((a.y+b.y+c.y)/3,n.magnitude*.5f,(a+b+c)/3));}
 var levels=faces.GroupBy(x=>Mathf.Round(x.y*100)/100).OrderByDescending(x=>x.Sum(f=>f.area)).ToArray();var deck=levels[0];float y=deck.Key;
 Vector3 Centroid(IEnumerable<(float y,float area,Vector3 c)> fs)=>fs.Aggregate(Vector3.zero,(a,b)=>a+b.c*b.area)/fs.Sum(x=>x.area);
 var top=faces.Where(x=>x.y<y-.1f).GroupBy(x=>Mathf.Round(x.y*100)/100).OrderByDescending(x=>x.Key).Take(2).ToArray();
 var end=Centroid(top[0]);var below=Centroid(top[1]);var direction=Vector3.ProjectOnPlane(end-below,Vector3.up).normalized;var inward=Vector3.ProjectOnPlane(court.position-end,Vector3.up).normalized;
 var center=end+direction*1.9f+inward*1f;center.y=y-.18f;
 var pad=Shape(group,"Broad stair landing",PrimitiveType.Cube,center,new Vector3(4,.36f,3.6f),Mat("Assets/Prefabs/Workshop/Castle1/Sunworn_Sandstone.mat"),true);pad.rotation=Quaternion.LookRotation(direction);
 log.Add($"LANDING {g.name}/{f.transform.parent.name} top={y:F3} size=4x3.6 center={center:F3} stairEnd={end:F3}");
 }Bake(g);return true;}
 static void Bake(GameObject root){
 var surface=root.GetComponent<NavMeshSurface>();if(!surface)return;var old=surface.navMeshData;if(old)Backup(AssetDatabase.GetAssetPath(old));
 surface.RemoveData();surface.BuildNavMesh();var generated=surface.navMeshData;surface.RemoveData();if(!generated)throw new Exception("Landing navigation bake failed");
 if(old&&generated!=old){EditorUtility.CopySerialized(generated,old);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);surface.navMeshData=old;Object.DestroyImmediate(generated);}
 var offset=new Vector3(5000,0,5000);var instance=NavMesh.AddNavMeshData(surface.navMeshData,root.transform.position+offset,root.transform.rotation);
 try{foreach(var pad in root.transform.Find("Stair_Landings249").Cast<Transform>()){
 var p=pad.position+Vector3.up*.18f;bool ok=NavMesh.SamplePosition(p+offset,out var hit,.6f,NavMesh.AllAreas);if(!ok)throw new Exception("No navigation on new landing: "+p);log.Add("NAV "+root.name+" landing reachable area at "+(hit.position-offset));}}
 finally{instance.Remove();}
 }
 static Mesh RepairNormals(Mesh source){
 if(repaired.TryGetValue(source,out var result))return result;
 var v=source.vertices;var n=source.normals;if(n.Length!=v.Length){repaired[source]=source;return source;}
 // Shared vertices on opposite faces can cancel to zero; their dot product is not negative.
 bool bad=n.Any(normal=>normal.sqrMagnitude<.01f);foreach(int sub in Enumerable.Range(0,source.subMeshCount)){var indices=source.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3){int a=indices[i],b=indices[i+1],c=indices[i+2];var face=Vector3.Cross(v[b]-v[a],v[c]-v[a]).normalized;if(Vector3.Dot(face,(n[a]+n[b]+n[c]).normalized)<-.3f){bad=true;break;}}}
 if(!bad){repaired[source]=source;return source;}
 var uv=source.uv;var uv2=source.uv2;var colors=source.colors;var points=new List<Vector3>();var normals=new List<Vector3>();var coords=new List<Vector2>();var lightmap=new List<Vector2>();var paint=new List<Color>();var triangles=new List<int[]>();
 for(int sub=0;sub<source.subMeshCount;sub++){var indices=source.GetTriangles(sub);var fresh=new List<int>();for(int i=0;i<indices.Length;i+=3){var face=Vector3.Cross(v[indices[i+1]]-v[indices[i]],v[indices[i+2]]-v[indices[i]]).normalized;
 for(int k=0;k<3;k++){int old=indices[i+k];fresh.Add(points.Count);points.Add(v[old]);normals.Add(Vector3.Dot(n[old],face)<.05f?face:n[old]);if(uv.Length==v.Length)coords.Add(uv[old]);if(uv2.Length==v.Length)lightmap.Add(uv2[old]);if(colors.Length==v.Length)paint.Add(colors[old]);}}
 triangles.Add(fresh.ToArray());}
 var mesh=new Mesh{name=source.name+"_Surface249",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetNormals(normals);if(coords.Count>0)mesh.SetUVs(0,coords);if(lightmap.Count>0)mesh.SetUVs(1,lightmap);if(paint.Count>0)mesh.SetColors(paint);mesh.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();if(coords.Count>0)mesh.RecalculateTangents();
 AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);string safe=System.Text.RegularExpressions.Regex.Replace(source.name,@"[^A-Za-z0-9_]","_");if(safe.Length>35)safe=safe.Substring(0,35);string path=Winter+"Route249/"+safe+"_"+id+".asset";
 var exists=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(exists){EditorUtility.CopySerialized(mesh,exists);Object.DestroyImmediate(mesh);mesh=exists;EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
 log.Add("NORMALS repaired "+source.name+" using independent back-face vertices; positions, UV and submesh paints retained");repaired[source]=mesh;return mesh;
 }
 static bool Surfaces(GameObject g){bool changed=false;foreach(var f in g.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh)){
 var old=f.sharedMesh;var mesh=RepairNormals(old);if(mesh==old)continue;f.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(f);
 foreach(var c in f.GetComponents<MeshCollider>().Where(c=>c.sharedMesh==old)){c.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}changed=true;
 }return changed;}
 public static string Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
 if(!AssetDatabase.IsValidFolder(Winter+"Route249"))AssetDatabase.CreateFolder(Winter.TrimEnd('/'),"Route249");
 foreach(var path in new[]{Winter+"Castle174/EventCastle_Winter.prefab","Assets/Prefabs/Workshop/Hospital/Hospital_Stone_01.prefab","Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab","Assets/Prefabs/Locations/Castle.prefab",Winter+"NY_Castle.prefab","Assets/Prefabs/Locations/Bilding/Hospital.prefab"})Edit(path,Hospital);
 Edit(Winter+"ShorePOI/POI_05_Gift_Sled.prefab",Sled);
 Edit(Winter+"Route181/Bank_05_Gift_Sled.prefab",g=>{if(g.transform.localScale.x<1.5f)return false;g.transform.localScale=Vector3.one;return true;});
 Edit(Winter+"NY_Ruins_1.prefab",Ruins);
 foreach(int i in new[]{1,2})Edit($"Assets/Prefabs/Locations/Enemy/castle_{i}.prefab",Landings);
 // Shared ship art propagates to both banks; the mountain and ruins are independent POI assets.
 foreach(var path in new[]{"Assets/Prefabs/Workshop/Ship1/Ship1_ProductionVisual.prefab",Winter+"ShorePOI/POI_01_Snow_Tunnel.prefab",Winter+"NY_Ruins_1.prefab","Assets/Prefabs/Locations/Enemy/house1_1.prefab","Assets/Prefabs/Locations/Enemy/house1_2.prefab","Assets/Prefabs/Locations/Enemy/house3_1.prefab","Assets/Prefabs/Locations/Enemy/house3_2.prefab"})Edit(path,Surfaces);
 File.AppendAllLines("Library/EventFix249/applied.txt",log);return string.Join("\n",log);
 }
}
