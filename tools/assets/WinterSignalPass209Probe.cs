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
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class WinterSignalPass209Probe
{
    const string Flag = "WinterSignalPass209Probe";
    const string Output = "Docs/locations/previews/event-winter209-";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    static WinterSignalPass209Probe() => EditorApplication.playModeStateChanged+=Run;

    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("A clean Edit Mode scene is required");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/ShorePOI/POI_01_Snow_Tunnel.prefab");
        var pool=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>("Assets/Events/NewYear/NY_Locations.asset");
        var entry=new SerializedObject(pool).FindProperty("spawnItems").GetArrayElementAtIndex(16);
        Require(entry.FindPropertyRelative("prefab").objectReferenceValue==prefab.GetComponent<LocationContentSpawner>(),"Original event pool reference changed");
        Require(prefab.transform.Find("Signal_Pass/Signal_Tower/Walkable_Deck"),"Missing tower prefab");
        File.WriteAllText(Output+"play.txt","");
        EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        SessionState.SetBool(Flag,true);
        EditorApplication.EnterPlaymode();
    }

    static async void Run(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag,false)) return;
        EditorApplication.CallbackFunction clock=()=>Time.timeScale=1f;
        EditorApplication.update+=clock;
        try
        {
            await Until(()=>LoadingManager.Instance && LoadingManager.Instance.CurrentLocation==Location.Lobby);
            Require(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave,"Save must be isolated");
            LoadingManager.Instance.LoadLocation(Location.Game,"LevelEvent",false);
            await Until(()=>SceneManager.GetActiveScene().name=="LevelEvent" && BoardController.Instance && BoardController.Instance.StartGame);
            await Verify();
            Note("PASS");
        }
        catch(Exception e) { Note("FAIL "+e); }
        finally
        {
            EditorApplication.update-=clock;
            SessionState.EraseBool(Flag);
            EditorApplication.ExitPlaymode();
        }
    }

    static async Task Until(Func<bool> condition)
    {
        var deadline=DateTime.UtcNow.AddSeconds(175);
        while(!condition())
        {
            if(DateTime.UtcNow>deadline) throw new TimeoutException("Timed out loading LevelEvent");
            await Task.Delay(45);
        }
    }

    static async Task Verify()
    {
        var owner=Object.FindAnyObjectByType<LocationSpawner>();
        var spawns=(List<LocationContentSpawner>)typeof(LocationSpawner).GetField("spawnedLocations",Fields).GetValue(owner);
        var spawn=typeof(LocationSpawner).GetMethod("SpawnLocation",Fields);
        var point=spawns.FirstOrDefault(p=>p && p.transform.Find("Signal_Pass"));
        for(int i=0;i<28 && !point;i++)
        {
            spawn.Invoke(owner,new object[]{0f});
            point=spawns.FirstOrDefault(p=>p && p.transform.Find("Signal_Pass"));
        }
        Require(point,"The normal location spawner did not create the signal pass");

        var board=BoardController.Instance;
        var player=PlayerMovement.Instance;
        float cameraZ=point.transform.position.z-16f;
        board.ApplySharedBoat(board.TotalDistanceTraveled+cameraZ-board.transform.position.z,0,board.SharedBaseFuel,
            board.SharedBaseFuel,board.SharedBaseSpeed,false);
        player.Teleport(board.transform);
        await Task.Delay(420);
        Physics.SyncTransforms();
        var pass=point.transform.Find("Signal_Pass");
        var terrain=Object.FindAnyObjectByType<LendController>().locations[0].groundSegments;
        var zone=point.GetComponent<BoxCollider>().bounds;
        Require(zone.max.x<-4.5f && !Object.FindAnyObjectByType<CitySpawner>().IsWithinCityClearance(point.ZPosition,60f),"Signal Pass violates shore or castle clearance");
        Require(point.transform.Cast<Transform>().Count(t=>t.GetComponent<PickableItem>())==2,"Expected two generated pickups");
        foreach(var mark in point.itemSpawnPoints.Select(s=>s.spawnTransform))
        {
            var hits=Physics.RaycastAll(mark.position+Vector3.up*.2f,Vector3.down,.8f,~0,QueryTriggerInteraction.Ignore);
            Require(hits.Any(hit=>hit.collider.transform.IsChildOf(pass) || terrain.Any(t=>hit.collider.transform.IsChildOf(t))),"No support beneath "+mark.name);
        }
        var entry=board.transform.position+new Vector3(-1.1f,2.35f,0);
        Capture("boat",entry,pass.TransformPoint(new Vector3(1.1f,4.6f,-1.0f)),64f);
        Capture("trail",pass.TransformPoint(new Vector3(30,20,-20)),pass.TransformPoint(new Vector3(2,5,0)),49f);

        var pawn=new GameObject("Signal Pass traversal capsule") { layer=player.gameObject.layer };
        var cc=pawn.AddComponent<CharacterController>();
        var original=player.GetComponent<CharacterController>();
        cc.radius=original.radius; cc.height=original.height; cc.center=original.center;
        cc.stepOffset=original.stepOffset; cc.slopeLimit=original.slopeLimit; cc.skinWidth=original.skinWidth;
        try
        {
            foreach(float side in new[]{-1f,1f})
                foreach(float x in new[]{-.65f,-1.45f,.15f})
                {
                    Place(cc,pass.TransformPoint(new Vector3(x,.08f,side*13.9f)));
                    Walk(cc,pass.TransformPoint(new Vector3(x,.02f,-side*13.9f)),240);
                    var reached=pass.InverseTransformPoint(cc.transform.position);
                    Require(reached.z*-side>13.6f && reached.y<.32f,$"Tunnel blocked at offset {x:F2}, reached {reached:F2}");
                }
            Note("Six full tunnel crossings, three offsets in each direction, with the player capsule.");
            var path=pass.Find("Trail_Checkpoints").Cast<Transform>().ToArray();
            Place(cc,path[0].position+Vector3.up*.07f);
            foreach(var checkpoint in path.Skip(1))
            {
                Walk(cc,checkpoint.position,150);
                var reached=cc.transform.position;
                Require(Flat(reached-checkpoint.position).magnitude<.18f && Mathf.Abs(reached.y-checkpoint.position.y)<.38f,
                    $"Trail blocked at {checkpoint.name}; local capsule {pass.InverseTransformPoint(reached):F2}, expected {checkpoint.localPosition:F2}");
            }
            for(int i=0;i<8;i++) cc.Move(Vector3.down*.035f);
            var deck=pass.Find("Signal_Tower/Walkable_Deck").GetComponent<BoxCollider>();
            Require(deck.Raycast(new Ray(cc.transform.position+Vector3.up*.25f,Vector3.down),out var floor,.5f) &&
                Mathf.Abs(floor.point.y-cc.transform.position.y)<.15f,"Player capsule did not stand on the tower deck");
            Note($"Player capsule climbed all {path.Length-1} trail sections without jumping and stood on the tower deck at {floor.point.y:F2} m.");
        }
        finally { Object.Destroy(pawn); }
        Note("Generated POI has two supported loot pickups and stays clear of the river and castles. Boat and trail frames captured.");
    }

    static Vector3 Flat(Vector3 p) => Vector3.ProjectOnPlane(p,Vector3.up);

    static void Place(CharacterController capsule,Vector3 position)
    {
        capsule.enabled=false; capsule.transform.position=position; capsule.enabled=true;
        Physics.SyncTransforms();
    }

    static void Walk(CharacterController capsule,Vector3 target,int budget)
    {
        for(int i=0;i<budget;i++)
        {
            var delta=Flat(target-capsule.transform.position);
            if(delta.magnitude<.055f) break;
            capsule.Move(Vector3.ClampMagnitude(delta,.155f)+Vector3.down*.026f);
        }
    }

    static void Capture(string name,Vector3 position,Vector3 target,float fieldOfView)
    {
        var camera=new GameObject("Signal Pass review camera").AddComponent<Camera>();
        camera.CopyFrom(Camera.main); camera.enabled=false; camera.fieldOfView=fieldOfView;
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
        var previous=RenderTexture.active;
        var frame=new RenderTexture(1440,900,24);
        var pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=frame; camera.Render(); RenderTexture.active=frame;
            pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);
            pixels.Apply(); File.WriteAllBytes(Output+name+".png",pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null; RenderTexture.active=previous; frame.Release();
            Object.Destroy(frame); Object.Destroy(pixels); Object.Destroy(camera.gameObject);
        }
    }

    static void Note(string message)
    {
        File.AppendAllText(Output+"play.txt",message+Environment.NewLine);
        Debug.Log("[Signal Pass 209] "+message);
    }

    static void Require(bool condition,string message)
    {
        if(!condition) throw new InvalidOperationException(message);
    }
}
