using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CheckEventMaterials249
{
 static bool On(Transform t,Transform root){for(;t&&t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
 public static string Run(){var lines=new List<string>();int bad=0,total=0;var meshes=new HashSet<Mesh>();
 var pool=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>("Assets/Events/NewYear/NY_Locations.asset");var entries=new SerializedObject(pool).FindProperty("spawnItems");
 var roots=Enumerable.Range(0,entries.arraySize).Select(i=>((Component)entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue).gameObject).Concat(new[]{AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab")});
 foreach(var root in roots){int count=0,start=bad;
 foreach(var f in root.GetComponentsInChildren<MeshFilter>(true).Where(f=>On(f.transform,root.transform)&&f.sharedMesh&&f.TryGetComponent<MeshRenderer>(out var r)&&r.enabled)){
 var r=f.GetComponent<MeshRenderer>();var mesh=f.sharedMesh;count++;total++;
 if(r.sharedMaterials.Length<mesh.subMeshCount){bad++;lines.Add("FAIL material slots "+root.name+"/"+f.name+" "+r.sharedMaterials.Length+"/"+mesh.subMeshCount);}
 foreach(var m in r.sharedMaterials){if(!m||!m.shader||!m.shader.isSupported){bad++;lines.Add("FAIL missing/unsupported material "+root.name+"/"+f.name);continue;}
 if(m.HasProperty("_MainTex")&&m.mainTexture&&m.mainTextureScale.sqrMagnitude>.000001f&&mesh.uv.Length!=mesh.vertexCount){bad++;lines.Add("FAIL textured mesh without full UV "+root.name+"/"+f.name+" mat="+AssetDatabase.GetAssetPath(m));}}
 if(!meshes.Add(mesh))continue;var v=mesh.vertices;var n=mesh.normals;var tri=mesh.triangles;int reversed=0;
 if(v.Any(p=>float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))){bad++;lines.Add("FAIL non-finite vertices "+AssetDatabase.GetAssetPath(mesh));}
 if(n.Length==v.Length)for(int i=0;i<tri.Length;i+=3){var a=tri[i];var b=tri[i+1];var c=tri[i+2];var normal=Vector3.Cross(v[b]-v[a],v[c]-v[a]);if(normal.sqrMagnitude>.00000001f&&Vector3.Dot(normal.normalized,(n[a]+n[b]+n[c]).normalized)<-.3f)reversed++;}
 if(reversed>0)lines.Add("NORMALS opposing winding="+reversed+" "+mesh.name+" @ "+AssetDatabase.GetAssetPath(mesh));
 }lines.Add($"SUMMARY {root.name} active mesh renderers={count} material/UV failures={bad-start}");}
 lines.Add($"TOTAL {entries.arraySize} points + event castle; renderers={total}; unique meshes={meshes.Count}; material/UV failures={bad}");
 Directory.CreateDirectory("Library/EventFix249");File.WriteAllLines("Library/EventFix249/materials.txt",lines);return string.Join("\n",lines);
 }
}
