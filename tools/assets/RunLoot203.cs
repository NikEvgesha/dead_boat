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
public static class RunLoot203
{
    const string Phase="DBON203.PlayCheck", LogPath="Docs/locations/previews/loot203/run-validation.txt";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Note(string s){File.AppendAllText(LogPath,s+Environment.NewLine);Debug.Log("[Loot203] "+s);}
    static void Require(bool condition,string name){if(!condition)throw new Exception(name);}
    static RunLoot203(){EditorApplication.playModeStateChanged+=State;}
    public static void Main(){
        Directory.CreateDirectory(IntegrateLoot203.Report);
        bool eventOnly=Environment.GetCommandLineArgs().Contains("-loot203Event");
        if(!eventOnly)File.WriteAllText(LogPath,"");else Note("Fresh isolated event session (independent from Forest).");
        try{
            Require(Application.dataPath=="E:/Fork/dead_boat/Assets","Unexpected Unity project");
            if(!AssetDatabase.IsValidFolder("Assets/Prefabs/Items/Loot203")){
                Note(BuildLootConcept201.Revise203());Note(IntegrateLoot203.Install());
            }else{Note(IntegrateLoot203.Verify());Note(BuildLootConcept201.Revise203());IntegrateLoot203.RefreshRevisedIcons();}
            try{PreviewLootConcept201.Run();Note("Refreshed 3D renders.");}catch(Exception e){Note("Preview render issue: "+e.Message);}
            Require(!PlayerPrefs.HasKey("DeadBoat.SoloBranch.v1"),"Existing shared solo branch present; skip loader to preserve it");
            EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
            SharedPerformanceTestSave.Prepare();SessionState.SetInt(Phase,1);EditorApplication.EnterPlaymode();
        }catch(Exception e){Note("FAIL "+e);EditorApplication.Exit(1);}
    }
    static async void State(PlayModeStateChange change){
        int phase=SessionState.GetInt(Phase,0);
        if(change==PlayModeStateChange.EnteredEditMode&&phase>=2){SessionState.EraseInt(Phase);EditorApplication.Exit(phase==2?0:1);return;}
        if(change!=PlayModeStateChange.EnteredPlayMode||phase!=1)return;
        EditorApplication.CallbackFunction clock=()=>Time.timeScale=1f;EditorApplication.update+=clock;
        try{await VerifyPlay();Note("PLAY PASS");SessionState.SetInt(Phase,2);}
        catch(Exception e){Note("PLAY FAIL "+e);SessionState.SetInt(Phase,3);}
        finally{EditorApplication.update-=clock;EditorApplication.ExitPlaymode();}
    }
    static async Task Until(Func<bool> ok,string what,double seconds=22){double stop=EditorApplication.timeSinceStartup+seconds;while(!ok()){if(EditorApplication.timeSinceStartup>stop)throw new Exception("Timeout: "+what);await Task.Yield();}}
    static async Task Frame(){int f=Time.frameCount;await Until(()=>Time.frameCount>f,"next frame",4);}
    static async Task Run(string map){LoadingManager.Instance.LoadLocation(Location.Game,map,false);await Until(()=>SceneManager.GetActiveScene().name==map&&LoadingManager.Instance.CurrentLocation==Location.Game&&BoardController.Instance&&BoardController.Instance.StartGame,"load "+map,45);await Frame();Note(map+" loaded via LoadingManager; isolated provider="+SaveManager.Instance.PersistentProvider.GetType().Name);}
    static async Task VerifyPlay(){
        await Until(()=>LoadingManager.Instance&&LoadingManager.Instance.CurrentLocation==Location.Lobby,"boot Lobby",65);
        Require(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave,"Temporary save provider required");
        var tables=IntegrateLoot203.Tables();var profile=Resources.Load<LocationBalanceProfile>("LocationBalanceProfile");
        foreach(var entry in tables){
            var table=entry.Value;var seen=new HashSet<string>();var random=new RunRandom(20481,"loot203:"+entry.Key,0);
            for(int i=0;i<4500;i++){var p=table.GetRandomItem(ItemType.Any,ItemSize.Any,random);if(IntegrateLoot203.NewItem(p))seen.Add(IntegrateLoot203.IdOf(p));}
            var expected=table.spawnEntries.Where(e=>IntegrateLoot203.NewItem(e.itemPrefab)).Select(e=>IntegrateLoot203.IdOf(e.itemPrefab)).ToArray();
            Require(seen.SetEquals(expected),"Random table coverage "+entry.Key);Note("Random rolls "+entry.Key+": "+string.Join(", ",seen.OrderBy(x=>x)));
        }
        foreach(var e in tables["LevelEvent"].spawnEntries.Where(e=>IntegrateLoot203.NewItem(e.itemPrefab)))
            Require(ItemsManager.Instance.GetItem(e.itemPrefab.Data.Name)==e.itemPrefab&&SharedItemCatalog.Load().Identify(e.itemPrefab)!=0,"Loaded catalogs "+e.itemPrefab.name);
        if(!Environment.GetCommandLineArgs().Contains("-loot203Event")){
            await Run("LevelForest");Require(profile.ResolveItemCollection(null)==tables["LevelForest"],"Forest uses dedicated table");
            await MainMapSpawn(tables["LevelForest"]);return;
        }
        await Run("LevelEvent");Require(profile.ResolveItemCollection(null)==tables["LevelEvent"],"Event uses dedicated table");
        await EventActions(tables["LevelEvent"]);
    }
    static PickableItem Spawn(LocationItemSpawnEntry entry,Vector3 at){
        var probe=new GameObject("Loot203 controlled spawn point");var mark=new GameObject("Probe spawn");mark.transform.SetParent(probe.transform);mark.transform.position=at;
        var s=probe.AddComponent<LocationContentSpawner>();s.enabled=false;s.itemSpawnPoints=new List<LocationSpawnPoint>{new LocationSpawnPoint{spawnTransform=mark.transform,allowedType=entry.itemType,allowedSize=entry.itemSize}};
        var table=ScriptableObject.CreateInstance<LocationItemSpawnCollection>();table.spawnEntries=new List<LocationItemSpawnEntry>{entry};
        typeof(LocationContentSpawner).GetMethod("SpawnItems",Flags).Invoke(s,new object[]{table});
        var pick=probe.GetComponentInChildren<PickableItem>();Require(pick&&pick.Data==entry.itemPrefab.Data,"Actual spawner returned "+entry.itemPrefab.name);
        pick.transform.SetParent(null,true);Object.Destroy(probe);Object.Destroy(table);return pick;
    }
    static async Task MainMapSpawn(LocationItemSpawnCollection table){
        foreach(var entry in table.spawnEntries.Where(e=>IntegrateLoot203.NewItem(e.itemPrefab))){
            var p=Spawn(entry,new Vector3(-75,1,-20));await Frame();Require(p&&p.IsSellable&&!p.IsFuel,"Main map spawned sellable "+entry.itemPrefab.name);Object.Destroy(p.gameObject);Note("Forest LocationContentSpawner spawned "+IntegrateLoot203.IdOf(entry.itemPrefab));}
    }
    static Collider Trigger(Component c)=>c.GetComponents<Collider>().FirstOrDefault(x=>x.enabled&&x.isTrigger);
    static void Put(PickableItem p,Collider trigger){
        var r=p.GetComponent<Rigidbody>();r.isKinematic=false;r.useGravity=false;r.linearVelocity=Vector3.zero;r.angularVelocity=Vector3.zero;r.position=trigger.bounds.center;p.transform.position=r.position;Physics.SyncTransforms();
    }
    static async Task EventActions(LocationItemSpawnCollection table){
        var board=BoardController.Instance;
        var sell=Object.FindObjectsByType<SellPoint>(FindObjectsSortMode.None).FirstOrDefault(p=>Trigger(p)&& (StoreType)typeof(SellPoint).GetField("_type",Flags).GetValue(p)==StoreType.Items);
        var fuel=board.GetComponentsInChildren<FuelDeposit>().FirstOrDefault(p=>Trigger(p));
        Require(sell&&fuel,"Real castle sale point and boat furnace available");
        var st=Trigger(sell);var ft=Trigger(fuel);var pickup=PlayerManager.Instance.transform;
        int total=0;float totalFuel=0;
        foreach(var entry in table.spawnEntries.Where(e=>IntegrateLoot203.NewItem(e.itemPrefab))){
            string id=IntegrateLoot203.IdOf(entry.itemPrefab);var pos=pickup.position+Vector3.right*1.4f+Vector3.up*.4f;
            var item=Spawn(entry,pos);await Frame();Require(item&&item.Status==ItemStatus.Free,"spawn free "+id);
            item.PickUp(pickup);Require(item.Status==ItemStatus.Grabbed,"pickup "+id);item.Drop();Require(item.Status==ItemStatus.Free,"drop "+id);
            if(id.StartsWith("T")){
                int before=sell.CheckpointBag?sell.CheckpointBag.Money:0;int reward=item.GetComponent<SellableItem>().GetReward();Require(reward>0&&item.IsSellable&&!item.IsFuel,"treasure tags "+id);
                Put(item,st);await Until(()=>!item,"sell trigger "+id,5);Require(sell.CheckpointBag&&sell.CheckpointBag.Money==before+reward,"coin payout "+id);
                total+=reward;Note(id+" spawn → pickup → drop → real SellPoint collision: +"+reward+" coins in bag");
            }else{
                board.currentFuel=10;float amount=item.GetComponent<FuelItem>().fuelValue;
                if(LevelStatManager.Instance)amount*=LevelStatManager.Instance.Stats.AddMultFuel;
                amount=EggAnimalBuffService.ApplyFuelFill(ProfessionService.ApplyFuelFill(amount));Require(amount>0&&item.IsFuel&&!item.IsSellable,"fuel tags "+id);
                Put(item,ft);await Until(()=>!item,"furnace trigger "+id,5);
                float added=board.currentFuel-10;Require(Mathf.Abs(added-amount)<.05f,"fuel delta "+id+" expected="+amount+" actual="+added);
                totalFuel+=added;Note(id+" spawn → pickup → drop → real FuelDeposit collision: +"+added.ToString("F2")+" fuel");
            }
        }
        Note("LevelEvent totals: "+total+" coins and "+totalFuel.ToString("F2")+" fuel across 20 items; no actual save rewards.");
    }
}
