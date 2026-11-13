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
public static class WinterSignalPassManual209
{
    const string Flag="DeadBoat.Editor.SignalPassManual209";
    const string Source="Assets/Editor/WinterSignalPassManual209.cs";
    const string Report="Library/winter209-manual-ready.txt";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool reloadHeld;

    static WinterSignalPassManual209() => EditorApplication.playModeStateChanged+=OnPlayMode;

    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
           Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("The editor must be in clean Edit Mode");
        File.WriteAllText(Report,"Preparing isolated LevelEvent");
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/LoadingScene.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        SessionState.SetBool(Flag,true);
        EditorApplication.EnterPlaymode();
    }

    static async void OnPlayMode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false)) return;
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseBool(Flag);
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
            Debug.LogError("[Signal Pass manual] "+e.Message);
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
        var active=(List<LocationContentSpawner>)typeof(LocationSpawner).GetField("spawnedLocations",Fields).GetValue(owner);
        var generate=typeof(LocationSpawner).GetMethod("SpawnLocation",Fields);
        var point=active.FirstOrDefault(x=>x && x.transform.Find("Signal_Pass"));
        for(int i=0;i<28 && !point;i++)
        {
            generate.Invoke(owner,new object[]{0f});
            point=active.FirstOrDefault(x=>x && x.transform.Find("Signal_Pass"));
        }
        if(!point) throw new InvalidOperationException("Signal Pass did not appear in the location pool");
        var board=BoardController.Instance;
        var player=PlayerMovement.Instance;
        var seat=Object.FindObjectsByType<DriverSeatTrigger>(FindObjectsSortMode.None).First(s=>s.trainController==board);
        float target=point.transform.position.z-30f;
        board.ApplySharedBoat(board.TotalDistanceTraveled+target-board.transform.position.z,0f,
            board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
        player.Teleport(seat.driverSeatTransform.position);
        PauseManager.Instance.SetPause(false);
        ControlManager.Instance.CursorActive=false;
        Physics.SyncTransforms();
        await Task.Delay(425);
        if(!board.PlayerOnSeat)
            typeof(DriverSeatTrigger).GetMethod("EnterDrivingMode",Fields).Invoke(seat,null);
        if(!board.PlayerOnSeat || !PlayerInput.Instance.InTrain())
            throw new InvalidOperationException("The helm is not accepting player input");

        var mouth=point.transform.Find("Signal_Pass/Ice_Mouth_River").position;
        var direction=mouth+Vector3.up*2.2f-Camera.main.transform.position;
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
        var message=$"READY LevelEvent; station {point.ZPosition:F1}; boat {board.TotalDistanceTraveled:F1}; center ahead {point.transform.position.z-board.transform.position.z:F1} m; portal ahead {mouth.z-board.transform.position.z:F1} m; speed {board.SpeedKmh:F1}; helm {board.PlayerOnSeat}; inputs {PlayerInput.Instance.InTrain()}; timeScale {Time.timeScale:F1}; window {gameView.titleContent.text}";
        File.WriteAllText(Report,message);
        Debug.Log("[Signal Pass manual] "+message);
    }
}
