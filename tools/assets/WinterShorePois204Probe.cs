using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DeadBoat.Online;
using DeadBoat.Online.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WinterShorePois204Probe
{
    const string Flag = "WinterShorePois204Probe";
    const string Report = "Docs/locations/previews/event-winter206-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Names =
    {
        "POI_01_Snow_Tunnel","POI_02_Rooted_Log","POI_03_Frozen_Grotto",
        "POI_05_Gift_Sled","POI_06_Fir_Arch","POI_07_Ice_Crystal_Field"
    };

    static WinterShorePois204Probe() => EditorApplication.playModeStateChanged += StateChanged;

    public static void Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        File.WriteAllText(Report,"");
        VerifyPools();
        EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        SessionState.SetBool(Flag,true);
        EditorApplication.EnterPlaymode();
    }

    static async void StateChanged(PlayModeStateChange state)
    {
        if (state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag,false)) return;
        EditorApplication.CallbackFunction clock=()=>Time.timeScale=1f;
        EditorApplication.update+=clock;
        try
        {
            await Until(()=>LoadingManager.Instance && LoadingManager.Instance.CurrentLocation==Location.Lobby);
            Require(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave,"Isolated save");
            LoadingManager.Instance.LoadLocation(Location.Game,"LevelEvent",false);
            await Until(()=>SceneManager.GetActiveScene().name=="LevelEvent" && BoardController.Instance && BoardController.Instance.StartGame);
            Note("LevelEvent loaded through the standard loader.");
            await VerifyRun();
            Note("PASS");
        }
        catch(Exception ex) { Note("FAIL "+ex); }
        finally
        {
            EditorApplication.update-=clock;
            SessionState.EraseBool(Flag);
            EditorApplication.ExitPlaymode();
        }
    }

    static async Task Until(Func<bool> ready)
    {
        var deadline=DateTime.UtcNow.AddSeconds(175);
        while (!ready())
        {
            if (DateTime.UtcNow>deadline) throw new TimeoutException("Unity did not finish loading the test scene.");
            await Task.Delay(40);
        }
    }

    static void VerifyPools()
    {
        var source=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>("Assets/Events/NewYear/NY_Locations.asset");
        var decor=AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>("Assets/Events/NewYear/NY_Decor.asset");
        var pool=new SerializedObject(source).FindProperty("spawnItems");
        Require(pool.arraySize==22 && decor.spawnItems.Count==7,"22 locations and seven background entries");
        for (int i=0;i<Names.Length;i++)
        {
            var point=(LocationContentSpawner)pool.GetArrayElementAtIndex(16+i).FindPropertyRelative("prefab").objectReferenceValue;
            Require(point.name==Names[i] && point.CityClearance==60f && point.itemSpawnPoints.Count==2 &&
                point.enemySpawnPoints.Count==(i==5?2:1), "Unexpected setup for "+Names[i]);
            Require(point.CompareTag("Location") && point.GetComponent<BoxCollider>().isTrigger &&
                !point.transform.GetChild(0).GetComponent<BoxCollider>().enabled,"Incorrect ownership or cleanup trigger");
            var art=point.transform.GetChild(0);
            Require(art.localScale==Vector3.one*(i<4?2f:1f) && point.transform.localScale==Vector3.one,
                "Wrong visual scale on "+point.name);
            if (i<4)
            {
                var sourceArt=PrefabUtility.GetCorrespondingObjectFromSource(art.gameObject);
                var renderers=art.GetComponentsInChildren<MeshRenderer>();
                var originals=sourceArt.GetComponentsInChildren<MeshRenderer>();
                Require(renderers.Length==originals.Length,"Renderer count changed in "+point.name);
                for (int r=0;r<renderers.Length;r++)
                {
                    var current=renderers[r];
                    var original=originals[r];
                    Require(current.GetComponent<MeshFilter>().sharedMesh==original.GetComponent<MeshFilter>().sharedMesh &&
                        current.sharedMaterials.SequenceEqual(original.sharedMaterials) &&
                        current.sharedMaterials.All(m=>m && m.shader && m.shader.isSupported),
                        "Mesh, material slots or shader changed in "+point.name+"/"+current.name);
                }
                Note(point.name+": x2 on all axes; original mesh and material slots match across "+renderers.Length+" renderers.");
            }
            if (i==4)
            {
                var fair=point.transform.Find("Winter_Fair");
                Require(fair && fair.Cast<Transform>().Count(t=>t.name.StartsWith("Fir_Gate_"))==7 &&
                    fair.Cast<Transform>().Count(t=>t.name.StartsWith("Market_Stall_"))==4 &&
                    fair.Find("Market_Christmas_Tree"),"Winter fair prefab is incomplete");
            }
        }

        var prior=Object.Instantiate(source);
        var restored=Object.Instantiate(source);
        var oldSnapshot=JsonUtility.FromJson<SharedSoloSnapshot>("{\"Seed\":217}");
        try
        {
            var oldPool=new SerializedObject(prior);
            oldPool.FindProperty("spawnItems").arraySize=16;
            oldPool.ApplyModifiedPropertiesWithoutUndo();
            var enable=typeof(LocationSpawnCollection).GetMethod("OnEnable",Private);
            enable.Invoke(prior,null);
            restored.ApplyPoolRevision(oldSnapshot.LocationPoolRevision);
            float a=0f,b=0f;
            for (int i=0;i<700;i++)
            {
                var expected=LocationLayoutPlan.Advance(i,a,50f,100f,prior,new RunRandom(217,"layout",i));
                var actual=LocationLayoutPlan.Advance(i,b,50f,100f,restored,new RunRandom(217,"layout",i));
                Require(expected.LogicalZ==actual.LogicalZ && expected.Prefab==actual.Prefab,"Legacy layout differs at index "+i);
                a=expected.LogicalZ;
                b=actual.LogicalZ;
            }
        }
        finally { Object.DestroyImmediate(prior); Object.DestroyImmediate(restored); }
        Note("Edit Mode: all six points linked; 700 seeded legacy choices match the original pool.");
    }

    static async Task VerifyRun()
    {
        var spawner=Object.FindAnyObjectByType<LocationSpawner>();
        var city=Object.FindAnyObjectByType<CitySpawner>();
        var terrain=Object.FindAnyObjectByType<LendController>().locations[0].groundSegments;
        var board=BoardController.Instance;
        var player=PlayerMovement.Instance;
        var spawn=typeof(LocationSpawner).GetMethod("SpawnLocation",Private);
        var current=(List<LocationContentSpawner>)typeof(LocationSpawner).GetField("spawnedLocations",Private).GetValue(spawner);
        var saved=UnityEngine.Random.state;
        UnityEngine.Random.InitState(204);
        try
        {
            for (int i=0;i<26;i++) spawn.Invoke(spawner,new object[]{0f});
            await Task.Delay(250);
        }
        finally { UnityEngine.Random.state=saved; }

        var points=current.Where(p=>p && p.name.StartsWith("POI_",StringComparison.Ordinal)).ToArray();
        Require(Names.All(n=>points.Any(p=>p.name==n+"(Clone)")),"LocationSpawner did not generate all six types");
        var sampled=Names.Select(n=>points.First(p=>p.name==n+"(Clone)"))
            .OrderBy(p=>p.transform.position.z).ToArray();
        Require(sampled.Count(p=>p.GetComponent<BoxCollider>().bounds.center.x<0)==3,"Expected three locations on each bank");
        foreach (var point in points)
        {
            var zone=point.GetComponent<BoxCollider>().bounds;
            Require(zone.min.x>3.5f || zone.max.x<-3.5f,"Point encroaches on the channel: "+point.name);
            Require(!city.IsWithinCityClearance(point.ZPosition,60f),"Point entered the castle buffer: "+point.name);
            var items=point.transform.Cast<Transform>().Where(child=>child.GetComponent<PickableItem>()).ToArray();
            Require(items.Length==2,"Expected two generated pickups at "+point.name+", found "+items.Length+
                " ("+string.Join(", ",items.Select(item=>item.name))+")");
        }

        foreach (var point in sampled)
        {
            float z=point.transform.position.z;
            board.ApplySharedBoat(board.TotalDistanceTraveled+z-board.transform.position.z,0,
                board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
            player.Teleport(board.transform);
            await Task.Delay(130);
            Physics.SyncTransforms();
            var model=point.transform.GetChild(0);
            var markers=point.itemSpawnPoints.Select(s=>s.spawnTransform).Concat(point.enemySpawnPoints);
            foreach (var marker in markers)
            {
                var foot=marker.position;
                var hits=Physics.OverlapSphere(foot+Vector3.up*.16f,.23f,~0,QueryTriggerInteraction.Ignore);
                Require(!hits.Any(h=>h.transform.IsChildOf(model)),point.name+" spawn marker intersects solid art: "+marker.name);
                Require(Physics.RaycastAll(foot+Vector3.up*.40f,Vector3.down,1f,~0,QueryTriggerInteraction.Ignore)
                    .Any(h=>terrain.Any(t=>h.collider.transform.IsChildOf(t)) && Mathf.Abs(h.point.y)<.07f),
                    point.name+" has no bank below "+marker.name);
            }
            int guards=point.GetComponentsInChildren<ZombieController>().Length;
            Require(guards<=point.enemySpawnPoints.Count,"Too many guards at "+point.name);
            if (point.name.StartsWith("POI_01",StringComparison.Ordinal) ||
                point.name.StartsWith("POI_02",StringComparison.Ordinal) ||
                point.name.StartsWith("POI_03",StringComparison.Ordinal))
                VerifyPassage(point,player.GetComponent<CharacterController>());
            if (point.transform.Find("Winter_Fair"))
                await VerifyFair(point,player.GetComponent<CharacterController>(),terrain);
            Note(point.name+": two pickups; "+guards+" guards on this sample; safe shoreline footprint.");
        }
        Require(points.Any(p=>p && p.GetComponentInChildren<ZombieController>()),"No winter guard spawned in any sample");

        var decoration=Object.FindAnyObjectByType<DecorationSpawner>().decorationCollection.spawnItems[0].prefab;
        var last=sampled.Last();
        var center=last.GetComponent<BoxCollider>().bounds.center;
        center.y=0;
        var overlap=Object.Instantiate(decoration,center,Quaternion.identity);
        var clear=Object.Instantiate(decoration,center+Vector3.forward*22f,Quaternion.identity);
        await Task.Delay(250);
        Require(!overlap && clear,"Background cleanup failed to distinguish overlapping decoration");
        Object.Destroy(clear);
        Note(points.Length+" shoreline locations generated on both banks; overlapping background decoration cleared.");
        VerifyCastleBuffer(spawner,city,spawn,current);
    }

    static async Task VerifyFair(LocationContentSpawner point,CharacterController source,Transform[] terrain)
    {
        var fair=point.transform.Find("Winter_Fair");
        var gates=fair.Cast<Transform>().Where(t=>t.name.StartsWith("Fir_Gate_")).Prepend(point.transform.GetChild(0)).ToArray();
        var foundations=fair.Cast<Transform>().Where(t=>t.name.StartsWith("Gate_") || t.name.StartsWith("Market_Stall_"));
        foreach (var foundation in foundations)
        {
            var ground=Physics.RaycastAll(foundation.position+Vector3.up*2f,Vector3.down,4f,~0,QueryTriggerInteraction.Ignore);
            Require(ground.Any(hit=>terrain.Any(t=>hit.collider.transform.IsChildOf(t)) && Mathf.Abs(hit.point.y)<.1f),
                "No streamed snow bank under "+foundation.name);
        }
        var solid=point.GetComponentsInChildren<Collider>().Where(c=>c.enabled && !c.isTrigger).ToArray();
        foreach (var spawn in point.itemSpawnPoints.Select(s=>s.spawnTransform).Concat(point.enemySpawnPoints))
        {
            var hits=Physics.OverlapSphere(spawn.position+Vector3.up*.12f,.30f,~0,QueryTriggerInteraction.Ignore);
            Require(!hits.Any(c=>solid.Contains(c) && c.transform.IsChildOf(fair)),"Fair spawn obstructed: "+spawn.name);
        }
        var traveler=new GameObject("Fair passage test") {layer=source.gameObject.layer};
        try
        {
            var capsule=traveler.AddComponent<CharacterController>();
            capsule.radius=source.radius;
            capsule.height=source.height;
            capsule.center=Vector3.up*source.height*.5f;
            capsule.stepOffset=source.stepOffset;
            capsule.skinWidth=source.skinWidth;
            foreach (var gate in gates)
            {
                var outward=(gate.position-fair.position).normalized;
                capsule.enabled=false;
                traveler.transform.position=fair.position+outward*14.15f+Vector3.up*.06f;
                capsule.enabled=true;
                Physics.SyncTransforms();
                for(int step=0;step<36;step++) capsule.Move(-outward*.25f);
                Require(Vector3.Distance(fair.position,traveler.transform.position)<5.7f,"Fair passage blocked at "+gate.name);
            }
        }
        finally { Object.Destroy(traveler); }
        CaptureFair("overview",fair.position+new Vector3(17f,27f,-21f),fair.position+Vector3.up*.9f,48f);
        CaptureFair("river",new Vector3(-1.25f,3.1f,point.transform.position.z-17f),fair.position+Vector3.up*2.45f,60f);
        var decor=Object.FindAnyObjectByType<DecorationSpawner>().decorationCollection.spawnItems[0].prefab;
        var overlap=Object.Instantiate(decor,fair.position+Vector3.forward*11.2f,Quaternion.identity);
        await Task.Delay(225);
        Require(!overlap,"Background decor remained inside the fair perimeter");
        Note("Fair 06: eight gate passages, grounded stall supports, clear loot and guard positions, cleared decoration; overview and river frames captured.");
    }

    static void CaptureFair(string view,Vector3 position,Vector3 target,float fov)
    {
        var camera=new GameObject("Fair review camera").AddComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled=false;
        camera.fieldOfView=fov;
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
        var previousActive=RenderTexture.active;
        var frame=new RenderTexture(1280,800,24);
        var pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=frame;
            camera.Render();
            RenderTexture.active=frame;
            pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);
            pixels.Apply();
            File.WriteAllBytes("Docs/locations/previews/event-winter206-"+view+".png",pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null;
            RenderTexture.active=previousActive;
            frame.Release();
            Object.Destroy(frame);
            Object.Destroy(pixels);
            Object.Destroy(camera.gameObject);
        }
    }

    static void VerifyCastleBuffer(LocationSpawner spawner,CitySpawner city,MethodInfo spawn,List<LocationContentSpawner> current)
    {
        var collectionField=typeof(LocationSpawner).GetField("locationCollection",Private | BindingFlags.Public);
        var zField=typeof(LocationSpawner).GetField("lastSpawnZ",Private);
        var generationField=typeof(LocationSpawner).GetField("generationIndex",Private);
        var original=(LocationSpawnCollection)collectionField.GetValue(spawner);
        var pool=Object.Instantiate(original);
        var serialized=new SerializedObject(pool);
        var items=serialized.FindProperty("spawnItems");
        var chosen=(LocationContentSpawner)items.GetArrayElementAtIndex(16).FindPropertyRelative("prefab").objectReferenceValue;
        items.arraySize=1;
        var entry=items.GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("prefab").objectReferenceValue=chosen;
        entry.FindPropertyRelative("HaveMinSpawnDistence").boolValue=false;
        entry.FindPropertyRelative("HaveMaxSpawnDistence").boolValue=false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        typeof(LocationSpawnCollection).GetMethod("OnEnable",Private).Invoke(pool,null);
        var previousZ=zField.GetValue(spawner);
        var previousGeneration=generationField.GetValue(spawner);
        float previousMin=spawner.minDistance;
        float previousMax=spawner.maxDistance;
        try
        {
            Require(city.IsWithinCityClearance(city.baseSpawnDistance,60f),"City schedule did not mark the second castle: "+city.Cities);
            int previousCount=current.Count;
            collectionField.SetValue(spawner,pool);
            zField.SetValue(spawner,city.baseSpawnDistance-75f);
            spawner.minDistance=spawner.maxDistance=75f;
            spawn.Invoke(spawner,new object[]{0f});
            float planned=(float)zField.GetValue(spawner);
            Require(planned==city.baseSpawnDistance && current.Count==previousCount,
                $"LocationSpawner did not skip a point beside the second castle ({planned}, {previousCount} to {current.Count})");
            Note("A forced location within the second castle buffer was skipped.");
        }
        finally
        {
            collectionField.SetValue(spawner,original);
            zField.SetValue(spawner,previousZ);
            generationField.SetValue(spawner,previousGeneration);
            spawner.minDistance=previousMin;
            spawner.maxDistance=previousMax;
            Object.Destroy(pool);
        }
    }

    static void VerifyPassage(LocationContentSpawner point,CharacterController source)
    {
        var art=point.transform.GetChild(0);
        var shell=art.GetComponentsInChildren<MeshCollider>().First(c=>!c.isTrigger).transform;
        var test=new GameObject("Shore POI traversal test") {layer=source.gameObject.layer};
        try
        {
            var capsule=test.AddComponent<CharacterController>();
            capsule.radius=source.radius;
            capsule.height=source.height;
            capsule.center=Vector3.up*(source.height*.5f);
            capsule.stepOffset=source.stepOffset;
            capsule.skinWidth=source.skinWidth;
            foreach (float direction in new[]{-1f,1f})
            {
                capsule.enabled=false;
                test.transform.position=shell.TransformPoint(new Vector3(0f,.05f,-5.3f*direction));
                capsule.enabled=true;
                Physics.SyncTransforms();
                int steps=Mathf.CeilToInt(shell.TransformVector(Vector3.forward*11f).magnitude/.24f);
                for (int i=0;i<steps;i++) capsule.Move(shell.forward*(.24f*direction));
                Require(shell.InverseTransformPoint(test.transform.position).z*direction>5.25f,
                    "Passage blocked in "+point.name);
            }
            Note(point.name+": player capsule crossed both entrances.");
        }
        finally { Object.Destroy(test); }
    }

    static void Require(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void Note(string message)
    {
        File.AppendAllText(Report,message+Environment.NewLine);
        Debug.Log("[Shore POI] "+message);
    }
}
