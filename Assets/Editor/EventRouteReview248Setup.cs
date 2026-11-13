using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadBoat.Events.NewYear;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EventRouteReview248Setup
{
    const string Flag="EventRoute248.Pending";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool loading;
    static readonly string[] Names={"Снежный тоннель","Полое бревно","Зал под ледником","Сани с подарками","Арка из елей","Поляна кристаллов","Корабль во льду"};
    static EventRouteReview248Setup(){EditorApplication.update+=Poll;}
    [MenuItem("Tools/New Year/Play Full Event Route")]
    public static void Play()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit current Play Mode first");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Save the open scenes first");
        SessionState.SetString("EventRoute248.PriorStart",EditorSceneManager.playModeStartScene?AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene):"");
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
            {if(!loading&&SceneManager.GetActiveScene().name=="Lobby"){loading=true;LoadingManager.Instance.LoadLocation(Location.Game,"LevelEvent",false);}return;}
            if(!BoardController.Instance||!BoardController.Instance.StartGame||!PlayerMovement.Instance)return;
            SessionState.SetBool(Flag,false);await Setup();
        }
        catch(Exception e){SessionState.SetBool(Flag,false);Debug.LogException(e);Directory.CreateDirectory("Library/EventRoute248");File.WriteAllText("Library/EventRoute248/failure.txt",e.ToString());}
        finally
        {
            if(!SessionState.GetBool(Flag,false))
            {string prior=SessionState.GetString("EventRoute248.PriorStart","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(prior)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(prior);}
        }
    }
    public static Bounds Geometry(GameObject root)
    {
        Bounds bounds=default;bool found=false;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!filter.sharedMesh)continue;
            var b=filter.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var p=filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
            }
        }
        if(!found)throw new InvalidOperationException("No visible geometry: "+root.name);
        return bounds;
    }
    public static async Task<string> Setup()
    {
        if(SceneManager.GetActiveScene().name!="LevelEvent"||!(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave))throw new InvalidOperationException("Isolated event gameplay required");
        if(Object.FindAnyObjectByType<EventRouteReview248>())throw new InvalidOperationException("Route already present");
        var city=Object.FindAnyObjectByType<CitySpawner>();
        var first=GameObject.Find("EventCastle_Winter");if(!first||!city||!city.cityPrefab)throw new InvalidOperationException("Event castle missing");
        var sourceCollection=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>("Assets/Events/NewYear/NY_Locations.asset");
        var entries=new SerializedObject(sourceCollection).FindProperty("spawnItems");
        if(entries.arraySize!=7)throw new InvalidOperationException("Review route expects seven winter-only entries");
        for(int i=0;i<entries.arraySize;i++)if(!AssetDatabase.GetAssetPath(entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue).StartsWith("Assets/Events/NewYear/ShorePOI/",StringComparison.Ordinal))throw new InvalidOperationException("Non-winter event stop");
        foreach(var spawner in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b&&new[]{"LocationSpawner","CitySpawner","ZombieSpawner","BossSpawner"}.Contains(b.GetType().Name)))
        {spawner.StopAllCoroutines();spawner.enabled=false;}
        foreach(var old in Object.FindObjectsByType<LocationContentSpawner>(FindObjectsSortMode.None))
            if(!old.transform.IsChildOf(first.transform))old.gameObject.SetActive(false);
        var preview=GameObject.Find("SleighThief_VisualPreview");if(preview)preview.SetActive(false);
        var root=new GameObject("Event_Route248_All_Points");root.SetActive(false);
        var route=root.AddComponent<EventRouteReview248>();
        // Only the seven approved winter stops; ordinary biome locations are excluded.
        var order=Enumerable.Range(0,entries.arraySize).ToArray();
        float cursor=Mathf.Max(85,Geometry(first).max.z+60);
        var report=new List<string>{"DB-ON-248 deterministic local review route", "First castle: "+first.name+" "+Geometry(first)};
        for(int n=0;n<order.Length;n++)
        {
            int index=order[n];var source=(LocationContentSpawner)entries.GetArrayElementAtIndex(index).FindPropertyRelative("prefab").objectReferenceValue;
            var obj=Object.Instantiate(source.gameObject,root.transform);obj.name=$"{n+1:00}_{source.name}";
            obj.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var bounds=Geometry(obj);float z=cursor-bounds.min.z;obj.transform.position=new Vector3(0,0,z);bounds=Geometry(obj);
            var stop=new EventRouteReview248.Stop{Root=obj,AssetPath=AssetDatabase.GetAssetPath(source),Title=Names[index],Center=z,Start=bounds.min.z,End=bounds.max.z};route.Stops.Add(stop);
            var content=obj.GetComponent<LocationContentSpawner>();content.ZPosition=z;content.SetLevel(1);content.InitializeStableGeneration("review248:"+index);
            route.AddSign(obj.transform,$"{n+1:00}. {Names[index]}",new Vector3(bounds.center.x<0?-8:8,3.5f,z));
            cursor=bounds.max.z+40;
            report.Add($"{n+1:00}|{index}|{stop.Title}|{stop.AssetPath}|Z={z:F2}|visible={bounds.min.z:F2}..{bounds.max.z:F2}|Y=0|rootScale={obj.transform.localScale}");
        }
        var second=Object.Instantiate(city.cityPrefab,root.transform);second.name="Second_Event_Castle248";second.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var secondBounds=Geometry(second);float secondZ=cursor+60-secondBounds.min.z;second.transform.position=new Vector3(0,0,secondZ);secondBounds=Geometry(second);
        route.SecondCastle=second;route.SecondCastleZ=secondZ;
        foreach(var content in second.GetComponentsInChildren<LocationContentSpawner>(true))content.InitializeStableGeneration("review248:castle2");
        route.AddSign(second.transform,"Второй замок",new Vector3(7,4,secondBounds.min.z-5));
        city.baseSpawnDistance=secondZ;city.additionalDistanceIncrement=0;typeof(CitySpawner).GetField("_citys",Private).SetValue(city,2);
        float bossZ=secondBounds.max.z+95;
        var bossRoot=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SleighBossReview244.BossPath),root.transform);
        bossRoot.name="SleighBoss_After_Second_Castle248";bossRoot.SetActive(false);bossRoot.transform.position=new Vector3(8,18,bossZ);
        bossRoot.transform.rotation=Quaternion.Euler(0,180,0);
        route.Boss=bossRoot.GetComponent<SleighBossController>();route.BossEnableDistance=secondBounds.max.z+20;route.BoatStopDistance=bossZ-22;
        route.AddSign(root.transform,"Босс за вторым замком",new Vector3(8,4,bossZ-35));
        var board=BoardController.Instance;board.MaxSpeed=16;board.currentFuel=board.MaxFuel;board.ScenarioDistanceLimit=route.BoatStopDistance;
        route.WarmupUntil=Time.unscaledTime+2;root.SetActive(true);
        // Individual boss was explicitly kept inactive; route progression is the only release gate.
        bossRoot.SetActive(false);
        route.TickRoute();
        report.Add($"Second castle Z={secondZ:F2}, visible={secondBounds.min.z:F2}..{secondBounds.max.z:F2}; boss Z={bossZ:F2}, enable={route.BossEnableDistance:F2}, boat stop={route.BoatStopDistance:F2}");
        var player=PlayerMovement.Instance;
        var rifle=Inventory.Instance.GetItems().FirstOrDefault(i=>i&&i.GetComponentInChildren<RangedWeaponController>(true));
        if(!rifle){rifle=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Rifle.prefab")).GetComponent<PickableItem>();if(!Inventory.Instance.AddItem(rifle))throw new InvalidOperationException("No rifle slot");}
        PlayerAmmoManager.Instance.AddAmmo(WeaponType.Rifle,500);Object.FindAnyObjectByType<ActiveItemManager>().SwitchActiveItem(rifle);rifle.GetComponentInChildren<RangedWeaponController>(true).ApplySharedAmmo(1);
        for(int i=0;i<3;i++)
        {var heal=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Heal.prefab")).GetComponent<PickableItem>();if(!Inventory.Instance.AddItem(heal)){Object.Destroy(heal.gameObject);break;}}
        PlayerStatsManager.Instance.Revive();
        var diagnostics=Object.FindAnyObjectByType<DeadBoat.Online.MenuInputDiagnostics>();if(diagnostics)diagnostics.enabled=false;
        if(DayTime.instanse){typeof(DayTime).GetField("_timeOfDay",Private).SetValue(DayTime.instanse,12f);typeof(DayTime).GetField("_currentOrbitSpeed",Private).SetValue(DayTime.instanse,0f);typeof(DayTime).GetMethod("ProgressTime",Private).Invoke(DayTime.instanse,null);}
        var seat=Object.FindObjectsByType<DriverSeatTrigger>(FindObjectsSortMode.None).First(s=>s.trainController==board);
        player.Teleport(seat.driverSeatTransform.position);await Task.Delay(450);
        typeof(DriverSeatTrigger).GetMethod("EnterDrivingMode",Private).Invoke(seat,null);
        typeof(PlayerMovement).GetField("_startYRotation",Private).SetValue(player,0f);typeof(PlayerMovement).GetField("_currentYRotation",Private).SetValue(player,0f);typeof(PlayerMovement).GetField("_currentXRotation",Private).SetValue(player,0f);
        player.transform.rotation=Quaternion.identity;if(Camera.main)Camera.main.transform.localRotation=Quaternion.identity;
        PauseManager.Instance.SetPause(false);ControlManager.Instance.MoveActive=true;ControlManager.Instance.ForceGameplayCursor();
        route.TickRoute();
        Directory.CreateDirectory("Library/EventRoute248");File.WriteAllLines("Library/EventRoute248/layout.txt",report);
        File.WriteAllLines("Docs/locations/previews/event-route248-layout.txt",report);
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.maximized=true;view.Focus();
        if(!board.PlayerOnSeat||!PlayerInput.Instance.InTrain()||route.Boss.gameObject.activeInHierarchy)throw new InvalidOperationException("Helm or boss gating incorrect");
        return $"READY route: {route.Stops.Count} stops, second castle {secondZ:F0} m, boss {bossZ:F0} m, fuel {board.currentFuel:F0}, helm=True, boss active=False";
    }
}
