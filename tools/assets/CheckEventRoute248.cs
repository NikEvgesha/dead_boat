using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Read-only inspection of the live route. Does not move the player, activate stops or change time.
public static class CheckEventRoute248
{
    static readonly Regex Plant=new Regex(@"prop_(bush|tree)|courtyard_(bush|tree)|shade_tree|tall_pine|small_oak|(^|_)palm|(^|_)tree( |\d|_|$)|(^|_)bush( |\d|_|$)|(^|_)shrub( |\d|$)|(^|_)fir( |\d|$)|(^|_)spruce|yolka",RegexOptions.IgnoreCase);
    static readonly Regex Ground=new Regex(@"Courtyard|Terrain|Ground|Grass_Terrace|Soft_Sand_Beach|Grass_|Grassy|Floor|Bank|Land|Rock|Stone|Sand",RegexOptions.IgnoreCase);
    static bool IsPlant(Transform t)=>(Plant.IsMatch(t.name)||t.name=="NY_FestiveFir")&&!t.name.StartsWith("TreeHouse",StringComparison.OrdinalIgnoreCase)&&t.name!="Tree_Topper";
    static bool On(Transform t,Transform root){for(;t&&t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
    static bool PlantParent(Transform t){for(t=t.parent;t;t=t.parent)if(IsPlant(t))return true;return false;}
    static Bounds Bounds(MeshFilter f)
    {
        var b=f.sharedMesh.bounds;Bounds world=default;
        for(int i=0;i<8;i++){
            var p=f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            if(i==0)world=new Bounds(p,Vector3.zero);else world.Encapsulate(p);
        }return world;
    }
    static float Surface(MeshFilter f,Vector3 foot)
    {
        var b=Bounds(f);if(foot.x<b.min.x-.001f||foot.x>b.max.x+.001f||foot.z<b.min.z-.001f||foot.z>b.max.z+.001f||b.min.y>foot.y+.25f)return float.NegativeInfinity;
        float best=float.NegativeInfinity;var v=f.sharedMesh.vertices;var tri=f.sharedMesh.triangles;
        for(int i=0;i<tri.Length;i+=3){
            var a=f.transform.TransformPoint(v[tri[i]]);var c=f.transform.TransformPoint(v[tri[i+1]]);var d=f.transform.TransformPoint(v[tri[i+2]]);
            if(Vector3.Cross(c-a,d-a).y<=.00001f)continue;
            float den=(c.z-d.z)*(a.x-d.x)+(d.x-c.x)*(a.z-d.z);if(Mathf.Abs(den)<.000001f)continue;
            float u=((c.z-d.z)*(foot.x-d.x)+(d.x-c.x)*(foot.z-d.z))/den;
            float w=((d.z-a.z)*(foot.x-d.x)+(a.x-d.x)*(foot.z-d.z))/den;
            if(u<-.0001f||w<-.0001f||u+w>1.0001f)continue;
            float y=u*a.y+w*c.y+(1-u-w)*d.y;if(y<=foot.y+.25f&&y>best)best=y;
        }return best;
    }
    static IEnumerable<MeshFilter> Meshes(GameObject root)=>root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&On(f.transform,root.transform)&&f.TryGetComponent<MeshRenderer>(out var r)&&r.enabled);
    static void Plants(GameObject root,LendController land,List<string> report)
    {
        var surfaces=Meshes(root).Where(f=>Ground.IsMatch(f.name)&&!IsPlant(f.transform)&&!PlantParent(f.transform)).ToArray();
        var banks=land.locations[0].groundSegments.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh&&f.TryGetComponent<MeshRenderer>(out var r)&&r.enabled).ToArray();
        int count=0,issues=0;
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>On(t,root.transform)&&IsPlant(t)&&!PlantParent(t))){
            var v=Meshes(t.gameObject).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();if(v.Length==0)continue;
            float y=v.Min(p=>p.y);var low=v.Where(p=>p.y<=y+.015f).ToArray();var foot=new Vector3(low.Average(p=>p.x),y,low.Average(p=>p.z));
            float ground=float.NegativeInfinity;foreach(var f in surfaces.Where(f=>!f.transform.IsChildOf(t)))ground=Mathf.Max(ground,Surface(f,foot));
            // Match the actual repeating terrain mesh at this Z without moving the live terrain/player.
            foreach(var f in banks){var sample=foot;sample.z-=Mathf.Round((sample.z-Bounds(f).center.z)/land.segmentLength)*land.segmentLength;ground=Mathf.Max(ground,Surface(f,sample));}
            float gap=foot.y-ground;count++;if(float.IsInfinity(gap)||gap>.035f)issues++;
            report.Add($"PLANT {root.name}/{t.name} foot={foot:F3} supportY={ground:F3} gap={gap:F3}");
        }
        report.Add($"PLANTS {root.name} tested={count} issues={issues}; repeated real bank triangles, no pivot/flat-Y fallback");
    }
    static void Check(bool ok,string text,List<string> report){report.Add((ok?"PASS ":"FAIL ")+text);}
    static void CastlePlants(Transform root,List<string> report)
    {
        var court=root.Find("02_Courtyard").GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh).ToArray();
        var beds=root.Find("08_Distributed_Greenery").GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Small_Flowers").GroupBy(t=>t.parent).Select(g=>g.Select(t=>t.position).Aggregate(Vector3.zero,(a,b)=>a+b)/g.Count()).ToArray();
        var firs=new[]{new Vector3(-18.12f,0,-18.45f),new Vector3(-7.6f,0,-5.2f),new Vector3(-18.24f,0,9),new Vector3(7.3f,0,-6.9f),new Vector3(17.5f,0,5.2f),new Vector3(17.35f,0,19.05f)}.Select(root.TransformPoint).ToArray();
        foreach(var batch in new[]{(name:"fir trunks",path:"Winter_176/Festive_Firs/Festive_Firs_Wood_Dark",centers:firs,radius:.32f),(name:"shrub groups",path:"Winter_Leafy_Shrubs/Winter_Leafy_Shrubs_Shrub_Green",centers:beds,radius:1f)}){
            var f=root.Find(batch.path).GetComponent<MeshFilter>();var v=f.sharedMesh.vertices.Select(f.transform.TransformPoint).ToArray();int issues=0;
            // A shrub bed has several offset shoots. Test the lowest actual branch base in the full bed,
            // rather than assuming the old flower marker coincides with its base.
            foreach(var center in batch.centers){
                var low=v.Where(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(center.x,center.z))<batch.radius).OrderBy(p=>p.y).Take(1).ToArray();
                if(low.Length==0){issues++;report.Add("CASTLE missing base "+root.name+" "+batch.name+" "+center);continue;}
                var foot=low[0];float support=court.Max(c=>Surface(c,foot));float gap=foot.y-support;
                if(float.IsInfinity(gap)||gap>.035f)issues++;
                report.Add($"CASTLE {root.name} {batch.name} foot={foot:F3} supportY={support:F3} gap={gap:F3}");
            }
            report.Add($"BAKED {root.name} {batch.name} tested={batch.centers.Length} issues={issues}");
        }
    }
    public static string Run()
    {
        var r=Object.FindAnyObjectByType<EventRouteReview248>();if(!r)throw new InvalidOperationException("No live route");
        var report=new List<string>{"DB-ON-248 live read-only inspection "+DateTime.UtcNow.ToString("O")};
        var pool=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>("Assets/Events/NewYear/NY_Locations.asset");
        var items=new SerializedObject(pool).FindProperty("spawnItems");
        var paths=Enumerable.Range(0,items.arraySize).Select(i=>AssetDatabase.GetAssetPath(items.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue)).OrderBy(p=>p);
        Check(r.Stops.Count==7&&r.Stops.Select(s=>s.AssetPath).OrderBy(p=>p).SequenceEqual(paths),"Seven winter stops exactly match NY_Locations",report);
        Check(r.Stops.Select(s=>s.AssetPath).Distinct().Count()==7,"No duplicate variants",report);
        Check(r.Stops.All(s=>s.AssetPath.Contains("/ShorePOI/")),"Every stop is a winter ShorePOI",report);
        Check(r.Stops.All(s=>s.Root.transform.position.y==0&&s.Root.transform.localScale==Vector3.one&&s.Root.transform.rotation==Quaternion.identity),"All full compositions preserved at Y=0, scale 1, authored bank X",report);
        float gap=r.Stops.Zip(r.Stops.Skip(1),(a,b)=>b.Start-a.End).Min();
        Check(gap>=39.99f,"Visible bounds between stops: minimum gap "+gap.ToString("F2")+" m",report);
        var castle=EventRouteReview248Setup.Geometry(r.SecondCastle);
        Check(castle.min.z-r.Stops.Last().End>=99.9f,"Second castle follows final stop with "+(castle.min.z-r.Stops.Last().End).ToString("F2")+" m gap",report);
        Check(r.Boss.transform.position.z>castle.max.z&&r.BossEnableDistance>castle.max.z&&r.BoatStopDistance>r.BossEnableDistance,"Boss and release gate are beyond second castle",report);
        Check(BoardController.Instance.ScenarioDistanceLimit==r.BoatStopDistance&&r.BoatStopDistance-r.Boss.transform.position.z==-22,"Boat limit ends 22 m before boss (28 m aggro)",report);
        Check(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave,"Isolated owner save",report);
        var boss=new SerializedObject(r.Boss);
        Check(boss.FindProperty("giftsPerVolley").intValue==5&&Mathf.Approximately(boss.FindProperty("flightSpeedMultiplier").floatValue,2),"Boss: five gifts and double flight speed",report);
        var clips=boss.FindProperty("laughClips");
        var durations=Enumerable.Range(0,clips.arraySize).Select(i=>((AudioClip)clips.GetArrayElementAtIndex(i).objectReferenceValue).length).ToArray();
        Check(durations.Length==3&&durations.All(d=>d>.7f&&d<1),"Three performed chuckles: "+string.Join(", ",durations.Select(d=>d.ToString("F2")))+" s",report);
        report.Add($"LIVE boat distance={BoardController.Instance.TotalDistanceTraveled:F2}, player={PlayerMovement.Instance.transform.position}, helm={BoardController.Instance.PlayerOnSeat}, fuel={BoardController.Instance.currentFuel:F2}/{BoardController.Instance.MaxFuel:F2}, health={PlayerStatsManager.Instance.Health:F0}/{PlayerStatsManager.Instance.MaxHealth:F0}, rifle reserve={PlayerAmmoManager.Instance.GetAmmo(WeaponType.Rifle)}");
        report.Add($"LIVE boss released={r.BossReleased}, active={r.Boss.gameObject.activeInHierarchy}, second castle={r.SecondCastleZ:F2}, boss={r.Boss.transform.position.z:F2}");
        foreach(var s in r.Stops.Where(s=>s.Root.activeInHierarchy))report.Add($"ACTIVE {s.Root.name} items={s.Root.GetComponentsInChildren<PickableItem>().Length}, enemies={s.Root.GetComponentsInChildren<EnemyCore>().Length}");
        var land=Object.FindAnyObjectByType<LendController>();foreach(var s in r.Stops)Plants(s.Root,land,report);
        CastlePlants(GameObject.Find("EventCastle_Winter").transform,report);
        CastlePlants(r.SecondCastle.transform,report);
        File.WriteAllLines("Docs/locations/previews/event-route248-check.txt",report);
        return string.Join("\n",report.Where(x=>!x.StartsWith("PLANT ")&&!x.StartsWith("CASTLE ")));
    }
}
