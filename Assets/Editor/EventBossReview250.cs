using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Only skips travel; the ordinary boat finish and BossSpawner remain in control.</summary>
[InitializeOnLoad]
public static class EventBossReview250
{
    const string Flag="EventBoss250.Pending";
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static bool loading;
    static EventBossReview250(){EditorApplication.update+=Poll;}
    [MenuItem("Tools/New Year/Play Event Boss Finish")]
    public static void Play()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit current Play Mode first");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save open scenes first");
        SessionState.SetString("EventBoss250.PriorStart",EditorSceneManager.playModeStartScene?AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene):"");
        DeadBoat.Online.Editor.SharedPerformanceTestSave.Prepare();
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/LoadingScene.unity");
        SessionState.SetBool(Flag,true);loading=false;EditorApplication.EnterPlaymode();
    }
    static async void Poll()
    {
        if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(!LoadingManager.Instance||!SaveManager.Instance||!(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave))return;
            if(SceneManager.GetActiveScene().name!="LevelEvent")
            {
                if(!loading&&SceneManager.GetActiveScene().name=="Lobby"){loading=true;LoadingManager.Instance.LoadLocation(Location.Game,"LevelEvent",false);}return;
            }
            if(!BoardController.Instance||!BoardController.Instance.StartGame||!PlayerMovement.Instance)return;
            SessionState.SetBool(Flag,false);await Setup();
        }
        catch(Exception ex){SessionState.SetBool(Flag,false);Debug.LogException(ex);}
        finally
        {
            if(!SessionState.GetBool(Flag,false))
            {string prior=SessionState.GetString("EventBoss250.PriorStart","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(prior)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(prior);}
        }
    }
    public static async Task<string> Setup()
    {
        if(SceneManager.GetActiveScene().name!="LevelEvent"||!(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave))throw new Exception("Isolated event gameplay required");
        foreach(var spawner in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b&&new[]{"LocationSpawner","CitySpawner","DecorationSpawner","ZombieSpawner"}.Contains(b.GetType().Name)))
        {spawner.StopAllCoroutines();spawner.enabled=false;}
        foreach(var enemy in Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        foreach(var loc in Object.FindObjectsByType<LocationContentSpawner>(FindObjectsSortMode.None))loc.gameObject.SetActive(false);
        var preview=GameObject.Find("SleighThief_VisualPreview");if(preview)preview.SetActive(false);
        var board=BoardController.Instance;board.transform.position+=Vector3.forward*(300-board.transform.position.z);
        board.TotalDistanceTraveled=GameManager.Instance.PlayDistance-35;board.currentFuel=board.MaxFuel;
        board.SwitchFuel?.Invoke(board.currentFuel,board.MaxFuel);
        typeof(BoardController).GetField("currentSpeed",F).SetValue(board,0f);
        if(DayTime.instanse){typeof(DayTime).GetField("_timeOfDay",F).SetValue(DayTime.instanse,12f);typeof(DayTime).GetField("_currentOrbitSpeed",F).SetValue(DayTime.instanse,0f);typeof(DayTime).GetMethod("ProgressTime",F).Invoke(DayTime.instanse,null);}
        var player=PlayerMovement.Instance;var seat=Object.FindObjectsByType<DriverSeatTrigger>(FindObjectsSortMode.None).First(s=>s.trainController==board);
        player.Teleport(seat.driverSeatTransform.position);
        var terrain=Object.FindAnyObjectByType<LendController>();if(terrain)typeof(LendController).GetMethod("Movelocation",F).Invoke(terrain,new object[]{player.zPositionFix});
        await Task.Delay(450);typeof(DriverSeatTrigger).GetMethod("EnterDrivingMode",F).Invoke(seat,null);
        typeof(PlayerMovement).GetField("_startYRotation",F).SetValue(player,0f);typeof(PlayerMovement).GetField("_currentYRotation",F).SetValue(player,0f);typeof(PlayerMovement).GetField("_currentXRotation",F).SetValue(player,0f);
        player.transform.rotation=Quaternion.identity;Camera.main.transform.localRotation=Quaternion.identity;
        var rifle=Inventory.Instance.GetItems().FirstOrDefault(i=>i&&i.GetComponentInChildren<RangedWeaponController>(true));
        if(!rifle){rifle=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Rifle.prefab")).GetComponent<PickableItem>();if(!Inventory.Instance.AddItem(rifle))throw new Exception("No rifle slot");}
        PlayerAmmoManager.Instance.AddAmmo(WeaponType.Rifle,500);Object.FindAnyObjectByType<ActiveItemManager>().SwitchActiveItem(rifle);rifle.GetComponentInChildren<RangedWeaponController>(true).ApplySharedAmmo(1);
        PlayerStatsManager.Instance.Revive();var diagnostics=Object.FindAnyObjectByType<DeadBoat.Online.MenuInputDiagnostics>();if(diagnostics)diagnostics.enabled=false;
        PauseManager.Instance.SetPause(false);ControlManager.Instance.MoveActive=true;ControlManager.Instance.ForceGameplayCursor();
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.maximized=true;view.Focus();view.Repaint();
        return "READY normal event finish: 35 m remaining, boss not yet spawned, helm active, full fuel, rifle and sword, isolated save.";
    }
}
