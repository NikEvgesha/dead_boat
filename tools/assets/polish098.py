from pathlib import Path
import json
full=Path('tools/assets/Finish098.cs.txt').read_text(encoding='utf-8')
prefix=full.split(' public static string Run()')[0].replace('class Finish098','class Polish098')
prefix=prefix.replace('static List<Seg> segs=new List<Seg>();static int mid;','static List<Seg> segs=new List<Seg>();static List<List<Seg>> routes=new List<List<Seg>>();static int mid;')
prefix=prefix.replace('int n=closed?p.Length:p.Length-1;','int start=segs.Count;int n=closed?p.Length:p.Length-1;')
prefix=prefix.replace('last=next;}}}', 'last=next;}}routes.Add(segs.Skip(start).ToList());}')
start=prefix.index(' static float Field('); end=prefix.index(' struct Pnt',start)
prefix=prefix[:start]+''' static float Field(Vector2 p){float f=100;foreach(var route in routes){float r=100;foreach(var s in route){if(p.x<Mathf.Min(s.a.x,s.b.x)-s.r-1||p.x>Mathf.Max(s.a.x,s.b.x)+s.r+1||p.y<Mathf.Min(s.a.y,s.b.y)-s.r-1||p.y>Mathf.Max(s.a.y,s.b.y)+s.r+1)continue;r=Mathf.Min(r,PointSegment(p,s.a,s.b)-s.r);}float h=Mathf.Max(.65f-Mathf.Abs(f-r),0)/.65f;f=Mathf.Min(f,r)-h*h*.65f*.25f;}return f;}
'''+prefix[end:]
routecode=full.split(' Curve(2.8f,true,')[1].split(' if(piers!=8)')[0]
routecode=' Curve(2.8f,true,'+routecode
endcode=full.split(' Shot(main.TransformPoint')[1]
code=prefix+''' public static string Run(){var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var root=GameObject.Find("Lobby_Island_Visual_Prototype").transform;var main=root.Find("01_Central_Island");var lab=main.Find("Animal_Laboratory097");var hospital=main.Find("02_Open_Market/04_Hospital_Source_Home_Hospital");UnityEngine.Object.DestroyImmediate(main.Find("Park_Paths098").gameObject);foreach(var p in new[]{"Park_Path_Edging098.asset","Park_Path_Surface098.asset"})AssetDatabase.DeleteAsset(D+p);
'''+routecode+'''
 var paths=G("Park_Paths098",main,Vector3.zero);Surface(paths,"Park_Path_Edging098",.18f,.035f,AssetDatabase.LoadAssetAtPath<Material>(D+"Park_Edge098.mat"));Surface(paths,"Park_Path_Surface098",0,.045f,AssetDatabase.LoadAssetAtPath<Material>(D+"Park_Limestone098.mat"));
 var sign=hospital.Find("Service_Sign");var signPos=sign.localPosition;signPos.y=3.15f;sign.localPosition=signPos;var text=hospital.Find("Sign_Text");signPos=text.localPosition;signPos.y=3.16f;text.localPosition=signPos;foreach(var n in new[]{"Medical_Cross_H","Medical_Cross_V"}){var t=hospital.Find(n);signPos=t.localPosition;signPos.x=-4.7f;signPos.y=3.15f;t.localPosition=signPos;}
 // Static visual entrance network connectivity and preserved chambers.
 var cells=new HashSet<Vector2Int>();for(int x=-82;x<=82;x++)for(int z=-62;z<=62;z++)if(Field(Q(x*.5f,z*.5f))<=0)cells.Add(new Vector2Int(x,z));var visited=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();var seed=cells.First();visited.Add(seed);queue.Enqueue(seed);while(queue.Count>0){var c=queue.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var q=c+d;if(cells.Contains(q)&&visited.Add(q))queue.Enqueue(q);}}if(cells.Count!=visited.Count)throw new Exception("Disconnected path surface "+visited.Count+"/"+cells.Count);if(piers!=8)throw new Exception("Expected8 piers");report.AppendLine("All8 current piers connected; continuous network BFS="+visited.Count+"/"+cells.Count+". Smooth blended junctions; width2.5-3.2m. Bank=Банк; Hospital original bandage+Heal,6counter2reserve copies, header lifted for product visibility. Animal chambers unchanged; beige floor/reactor/result hidden; slender angled tablet replaces box. Physics="+root.GetComponentsInChildren<Collider>(true).Length);
'''+ ' Shot(main.TransformPoint'+endcode
Path('tools/assets/Polish098.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-098/polish.json').write_text(json.dumps({'csharpCode':code,'className':'Polish098','methodName':'Run'}),encoding='utf-8')

