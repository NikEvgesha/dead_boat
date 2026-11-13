// Unity MCP script-execute after LoadingScene boot with SharedPerformanceTestSave.Prepare.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class Sklep1GameplayHandoff
{
    const BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Instance;
    const string Prefab="Assets/Prefabs/Workshop/Sklep1/Sklep1_OvergrownRuins.prefab";
    const string Name="Sklep1_NewDesign_PlayReview";
    public static async Task<string> Open()
    {
        if(!EditorApplication.isPlaying||!LoadingManager.Instance||!SaveManager.Instance||
            !(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave))
            throw new InvalidOperationException("Boot LoadingScene with an isolated preview save first");
        if(SceneManager.GetActiveScene().name!="LevelForest")LoadingManager.Instance.LoadLocation(Location.Game,"LevelForest",false);
        var deadline=DateTime.UtcNow.AddSeconds(85);
        while(SceneManager.GetActiveScene().name!="LevelForest"||!PlayerMovement.Instance||!BoardController.Instance||!BoardController.Instance.StartGame)
        {if(!EditorApplication.isPlaying||DateTime.UtcNow>deadline)throw new Exception("Gameplay did not initialize");await Task.Delay(100);}
        if(GameObject.Find(Name))throw new Exception("Review already exists; inspect live state");
        foreach(var spawner in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b&&
            new[]{"LocationSpawner","DecorationSpawner","CitySpawner","ZombieSpawner","BossSpawner"}.Contains(b.GetType().Name)))
        {spawner.StopAllCoroutines();spawner.enabled=false;}
        foreach(var loc in Object.FindObjectsByType<LocationContentSpawner>(FindObjectsSortMode.None))loc.gameObject.SetActive(false);
        foreach(var enemy in Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),SceneManager.GetActiveScene());
        root.name=Name;root.transform.position=new Vector3(0,0,500);
        root.GetComponent<ZoneCleaner>().enabled=false;root.GetComponent<LocationContentSpawner>().enabled=false;
        // The workshop's own floor and NavMesh support the full gameplay actors.
        var surface=root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();surface.AddData();
        var fill=new GameObject("Sklep_Review_Soft_Fill");fill.transform.rotation=Quaternion.Euler(48,148,0);
        var fillLight=fill.AddComponent<Light>();fillLight.type=LightType.Directional;fillLight.intensity=.25f;fillLight.shadows=LightShadows.None;
        var board=BoardController.Instance;float target=root.transform.position.z+7;
        board.ApplySharedBoat(board.TotalDistanceTraveled+target-board.transform.position.z,0f,board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
        var day=DayTime.instanse;
        typeof(DayTime).GetField("_timeOfDay",Fields).SetValue(day,12f);
        typeof(DayTime).GetField("_currentOrbitSpeed",Fields).SetValue(day,0f);
        typeof(DayTime).GetMethod("ProgressTime",Fields).Invoke(day,null);
        var player=PlayerMovement.Instance;PlayerInput.Instance.SitTrain(false);player.transform.SetParent(null);
        var start=root.transform.Find("Workshop_Walk_Anchors/Start").position;
        start.y=.10f;player.Teleport(start);
        var terrain=Object.FindAnyObjectByType<LendController>();
        if(terrain)typeof(LendController).GetMethod("Movelocation",Fields).Invoke(terrain,new object[]{player.zPositionFix});
        await Task.Delay(650);Physics.SyncTransforms();
        var floor=Physics.RaycastAll(start+Vector3.up*5,Vector3.down,8,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.normal.y>.5f&&h.collider.transform.IsChildOf(root.transform)).OrderBy(h=>h.distance).FirstOrDefault();
        if(!floor.collider)throw new Exception("No workshop floor under player start");
        start.y=floor.point.y+.10f;player.Teleport(start);
        typeof(PlayerMovement).GetField("_startYRotation",Fields).SetValue(player,90f);
        typeof(PlayerMovement).GetField("_currentYRotation",Fields).SetValue(player,0f);
        typeof(PlayerMovement).GetField("_currentXRotation",Fields).SetValue(player,0f);
        player.transform.rotation=Quaternion.Euler(0,90,0);Camera.main.transform.localRotation=Quaternion.identity;
        var items=player.GetComponent<ActiveItemManager>();if(items&&items.Active)items.SwitchActiveItem(null);
        var debug=Object.FindAnyObjectByType<DeadBoat.Online.MenuInputDiagnostics>();if(debug)debug.enabled=false;
        PauseManager.Instance.SetPause(false);ControlManager.Instance.MoveActive=true;ControlManager.Instance.ForceGameplayCursor();
        string previous=SessionState.GetString("Sklep1Play.PriorStart","");
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();view.Repaint();
        await Task.Delay(450);
        return Inspect();
    }
    public static string Inspect()
    {
        var root=GameObject.Find(Name);if(!root)throw new Exception("Review location missing");
        var player=PlayerMovement.Instance;var cc=player.GetComponent<CharacterController>();
        var zone=root.transform.Find("Trap/zoneTrap");var gates=zone.Find("WallActive");
        int enemies=root.transform.Find("Trap").GetComponentsInChildren<ZombieController>().Length;
        var use=root.GetComponentInChildren<Lever>(true).GetComponentInChildren<UseButtonShow>(true);
        var lever=root.GetComponentInChildren<Lever>(true);
        var calls=Enumerable.Range(0,use.Activate.GetPersistentEventCount()).Select(i=>use.Activate.GetPersistentMethodName(i)+" -> "+use.Activate.GetPersistentTarget(i)?.name).ToArray();
        bool nav=NavMesh.SamplePosition(root.transform.Find("Trap/spawnPoints/Point1").position,out _,2,NavMesh.AllAreas);
        var report=$"READY Play={EditorApplication.isPlaying} scene={SceneManager.GetActiveScene().name}; newDesign={Prefab}; player={player.transform.position:F2}; grounded={cc.isGrounded}; move={ControlManager.Instance.MoveActive}; paused={PauseManager.Instance.IsPaused}; touch={ControlManager.Instance.UseTouchControl}; gatesClosed={gates.gameObject.activeSelf}; trapEnemies={enemies}; zoneActive={zone.gameObject.activeSelf}; navmesh={nav}; leverUI={use.enabled&&use.UseButtonShowBool}; leverCalls={string.Join(",",calls)}; save={SaveManager.Instance.PersistentProvider.GetType().Name}; focused={EditorWindow.focusedWindow?.GetType().Name}";
        File.WriteAllText("Library/Sklep1PlayReview/ready.txt",report);
        ScreenCapture.CaptureScreenshot("Library/Sklep1PlayReview/ready.png");
        return report;
    }
}
