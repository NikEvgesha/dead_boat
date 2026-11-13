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
public static class WinterSleighManual219
{
    const string Flag="DeadBoat.Editor.SleighManual219";
    const string Source="Assets/Editor/WinterSleighManual219.cs";
    const string Report="Library/winter219-manual-ready.txt";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool reloadHeld;

    static WinterSleighManual219() => EditorApplication.playModeStateChanged+=OnPlayMode;

    public static void QueueStart()
    {
        EditorApplication.update += StartWhenReady;
    }

    static void StartWhenReady()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= StartWhenReady;
        try { Start(); }
        catch(Exception e) { File.WriteAllText(Report,"FAIL "+e); Debug.LogException(e); }
    }

    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
           Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("The editor must be in clean Edit Mode");
        File.WriteAllText(Report,"Preparing isolated LevelEvent");
        SessionState.SetString(Flag+".PreviousStartScene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? ""); EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        SessionState.SetBool(Flag,true);
        EditorApplication.EnterPlaymode();
    }

    static async void OnPlayMode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false)) return;
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseBool(Flag); EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Flag+".PreviousStartScene","")); SessionState.EraseString(Flag+".PreviousStartScene");
            if(reloadHeld) { reloadHeld=false; EditorApplication.UnlockReloadAssemblies(); }
            EditorApplication.delayCall+=()=>AssetDatabase.DeleteAsset(Source);
            return;
        }
        if(state!=PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            await Until(()=>LoadingManager.Instance && LoadingManager.Instance.CurrentLocation==Location.Lobby);
            if(!(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave))
                throw new InvalidOperationException("The run is not using the isolated save");
            LoadingManager.Instance.LoadLocation(Location.Game,"LevelEvent",false);
            await Until(()=>SceneManager.GetActiveScene().name=="LevelEvent" && BoardController.Instance &&
                BoardController.Instance.StartGame && PlayerMovement.Instance && PlayerInput.Instance);
            await PositionBoat();
        }
        catch(Exception e)
        {
            File.WriteAllText(Report,"FAIL "+e);
            Debug.LogError("[Sleigh manual] "+e.Message);
        }
    }

    static async Task Until(Func<bool> ready)
    {
        var deadline=DateTime.UtcNow.AddSeconds(145);
        while(!ready())
        {
            if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)
                throw new TimeoutException("LevelEvent did not reach its playable state");
            await Task.Delay(45);
        }
    }

    static async Task PositionBoat()
    {
        var owner=Object.FindAnyObjectByType<LocationSpawner>();
        var city=Object.FindAnyObjectByType<CitySpawner>();
        var land=Object.FindAnyObjectByType<LendController>();
        var active=(List<LocationContentSpawner>)typeof(LocationSpawner).GetField("spawnedLocations",Fields).GetValue(owner);
        var generate=typeof(LocationSpawner).GetMethod("SpawnLocation",Fields);
        var lastField=typeof(LocationSpawner).GetField("lastSpawnZ",Fields);
        var original=owner.locationCollection;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/ShorePOI/POI_05_Gift_Sled.prefab");
        var source=prefab.GetComponent<LocationContentSpawner>();
        var originalEntries=new SerializedObject(original).FindProperty("spawnItems");
        if(!Enumerable.Range(0,originalEntries.arraySize).Any(i=>originalEntries.GetArrayElementAtIndex(i)
            .FindPropertyRelative("prefab").objectReferenceValue==source))
            throw new InvalidOperationException("Sleigh is missing from the event location pool");
        var fixture=Object.Instantiate(original);
        var previousMin=owner.minDistance;
        var previousMax=owner.maxDistance;
        var board=BoardController.Instance;
        float z=Mathf.Max(145f,(float)lastField.GetValue(owner)+85f);
        while(city.IsWithinCityClearance(z,source.CityClearance)) z+=85f;
        LocationContentSpawner point;
        try
        {
            var serialized=new SerializedObject(fixture);
            var entries=serialized.FindProperty("spawnItems");
            entries.arraySize=1;
            var entry=entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("prefab").objectReferenceValue=source;
            entry.FindPropertyRelative("chance").floatValue=30f;
            entry.FindPropertyRelative("HaveMinSpawnDistence").boolValue=false;
            entry.FindPropertyRelative("HaveMaxSpawnDistence").boolValue=false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            typeof(LocationSpawnCollection).GetMethod("OnEnable",Fields).Invoke(fixture,null);
            owner.locationCollection=fixture;
            owner.minDistance=owner.maxDistance=85f;
            lastField.SetValue(owner,z-85f);
            int before=active.Count;
            generate.Invoke(owner,new object[]{0f});
            if(active.Count!=before+1) throw new InvalidOperationException("Could not generate POI 05");
            point=active.Last();
        }
        finally
        {
            owner.locationCollection=original;
            owner.minDistance=previousMin;
            owner.maxDistance=previousMax;
            Object.Destroy(fixture);
        }
        var shape=point.transform.GetChild(0).Find("Painted_Gift_Sleigh");
        var collider=shape.GetComponent<MeshCollider>();
        if(!collider || !collider.enabled || collider.isTrigger || collider.convex ||
           collider.sharedMesh!=shape.GetComponent<MeshFilter>().sharedMesh)
            throw new InvalidOperationException("Missing solid sleigh and present collider");
        var renderer=shape.GetComponent<MeshRenderer>();
        var approach=shape.TransformPoint(new Vector3(-2.10f,0,.18f));
        var red=shape.TransformPoint(new Vector3(-2.10f,0,.82f));
        var blue=shape.TransformPoint(new Vector3(-1.50f,0,.82f));
        float target=renderer.bounds.min.z-22f;
        board.ApplySharedBoat(board.TotalDistanceTraveled+target-board.transform.position.z,0f,
            board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
        var seat=Object.FindObjectsByType<DriverSeatTrigger>(FindObjectsSortMode.None).First(s=>s.trainController==board);
        var player=PlayerMovement.Instance;
        player.Teleport(seat.driverSeatTransform.position);
        PauseManager.Instance.SetPause(false);
        ControlManager.Instance.CursorActive=false;
        Physics.SyncTransforms();
        await Task.Delay(425);
        if(!board.PlayerOnSeat)
            typeof(DriverSeatTrigger).GetMethod("EnterDrivingMode",Fields).Invoke(seat,null);
        if(!board.PlayerOnSeat || !PlayerInput.Instance.InTrain())
            throw new InvalidOperationException("The helm is not accepting player input");
        bool GroundReady() => Physics.RaycastAll(approach+Vector3.up*2f,Vector3.down,4f,~0,QueryTriggerInteraction.Ignore)
            .Any(h=>land.locations[0].groundSegments.Any(s=>h.collider.transform.IsChildOf(s)));
        await Until(()=>Camera.main && GroundReady());
        bool CheckLid(Vector3 p,float height) => collider.Raycast(new Ray(p+Vector3.up*5f,Vector3.down),out var hit,5f) &&
            Mathf.Abs(hit.point.y-p.y-height)<.08f;
        if(!CheckLid(red,1.744f) || !CheckLid(blue,2.624f))
            throw new InvalidOperationException("Expected stepping gift lids not found");
        var direction=renderer.bounds.center+Vector3.down*.5f-Camera.main.transform.position;
        var aim=Quaternion.LookRotation(direction).eulerAngles;
        float yaw=Mathf.DeltaAngle(0f,aim.y);
        float pitch=Mathf.DeltaAngle(0f,aim.x);
        typeof(PlayerMovement).GetField("_startYRotation",Fields).SetValue(player,yaw);
        typeof(PlayerMovement).GetField("_currentYRotation",Fields).SetValue(player,0f);
        typeof(PlayerMovement).GetField("_currentXRotation",Fields).SetValue(player,pitch);
        player.transform.rotation=Quaternion.Euler(0,yaw,0);
        Camera.main.transform.localRotation=Quaternion.Euler(pitch,0,0);

        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        gameView.Focus();
        gameView.Repaint();
        EditorApplication.LockReloadAssemblies();
        reloadHeld=true;
        var message=$"READY LevelEvent; {point.name}; sleigh edge ahead {renderer.bounds.min.z-board.transform.position.z:F1} m; sleigh x {shape.position.x:F1}; boat speed {board.SpeedKmh:F1}; helm {board.PlayerOnSeat}; inputs {PlayerInput.Instance.InTrain()}; timeScale {Time.timeScale:F1}; snow True; red and blue lids True; window {gameView.titleContent.text}";
        File.WriteAllText(Report,message);
        Debug.Log("[Sleigh manual] "+message);
    }
}
