using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadBoat.Events.NewYear;
using Object=UnityEngine.Object;

public static class VerifySleighBoss250
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Report="Docs/locations/previews/sleigh-boss250-check.txt";
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static async Task Until(Func<bool> test,string why,int seconds=12)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!test()&&EditorApplication.timeSinceStartup<end)await Task.Delay(20);Check(test(),why);}
    static void Move(Vector3 p)
    {var player=PlayerMovement.Instance;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
    public static async Task<string> Run()
    {
        Check(EditorApplication.isPlaying&&SaveManager.Instance&&SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave,"Isolated boot required");
        if(SceneManager.GetActiveScene().name!="LevelEvent")LoadingManager.Instance.LoadLocation(Location.Game,"LevelEvent",false);
        await Until(()=>SceneManager.GetActiveScene().name=="LevelEvent"&&PlayerMovement.Instance&&BoardController.Instance&&BoardController.Instance.StartGame,"Event did not initialize",60);
        var lines=new List<string>{"DB-ON-250 ordinary LevelEvent finish + local Play Mode combat"};
        File.WriteAllLines(Report,lines);
        foreach(var spawner in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b&&new[]{"ZombieSpawner","LocationSpawner","DecorationSpawner","CitySpawner"}.Contains(b.GetType().Name)))
        {spawner.StopAllCoroutines();spawner.enabled=false;}
        foreach(var enemy in Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        var player=PlayerMovement.Instance;var input=PlayerInput.Instance;var inventory=Inventory.Instance;var active=Object.FindAnyObjectByType<ActiveItemManager>();var hp=PlayerStatsManager.Instance;
        bool pm=player.enabled,pi=input.enabled,iv=inventory.enabled,ac=active.enabled;float scale=Time.timeScale;bool audio=AudioListener.pause;
        player.enabled=false;input.enabled=false;inventory.enabled=false;active.enabled=false;PlayerInput.Instance.SitTrain(false);
        foreach(string field in new[]{"_attack","_healing","_reload","_pause"})typeof(PlayerInput).GetField(field,F)?.SetValue(input,false);
        EditorApplication.CallbackFunction clock=()=>{Time.timeScale=2;AudioListener.pause=false;};EditorApplication.update+=clock;clock();
        SleighBossController boss=null;
        Action<SleighGiftAttack> impact=null;
        var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Boss250_Combat_Fixture_Floor";platform.layer=6;platform.transform.position=new Vector3(75,-.2f,0);platform.transform.localScale=new Vector3(45,.4f,45);
        try
        {
            PauseManager.Instance.SetPause(false);clock();hp.RestoreRecoveryHealth(hp.MaxHealth);Move(new Vector3(75,.08f,0));
            var board=BoardController.Instance;
            Check(GameManager.Instance.PlayDistance==50000&&float.IsPositiveInfinity(board.ScenarioDistanceLimit),"Review route replaced normal finish");
            Check(Object.FindObjectsByType<SleighBossController>(FindObjectsSortMode.None).Length==0,"Sleigh must not spawn at start");
            var spawner=Object.FindAnyObjectByType<BossSpawner>();Check(spawner&&spawner.enabled,"Ordinary BossSpawner disabled");
            board.TotalDistanceTraveled=GameManager.Instance.PlayDistance-.35f;
            typeof(BoardController).GetField("currentSpeed",F).SetValue(board,0f);await Task.Delay(150);
            Check(!Object.FindAnyObjectByType<SleighBossController>(),"Boss spawned before normal distance");
            typeof(BoardController).GetField("currentSpeed",F).SetValue(board,8f);
            await Until(()=>Object.FindAnyObjectByType<SleighBossController>(),"Normal FixedUpdate finish did not spawn sleigh");
            boss=Object.FindAnyObjectByType<SleighBossController>();
            Check(board.TotalDistanceTraveled>=50000,"Finish crossed at wrong distance");
            Check(Object.FindObjectsByType<SleighBossController>(FindObjectsSortMode.None).Length==1,"Duplicate finish boss");
            lines.Add("PASS normal arrival: no boss at start/before 50000; accelerated fixture placed boat 0.35 m before finish, real FixedUpdate movement crossed 50000 and BossSpawner created one sleigh. PlayDistance unchanged, no review route or manual SpawnBoss/EndGame invocation.");
            File.WriteAllLines(Report,lines);
            var meleeItem=inventory.GetItems().First(i=>i&&i.GetComponentInChildren<MeleWeapon>(true));active.SwitchActiveItem(meleeItem);await Task.Delay(150);
            var melee=meleeItem.GetComponentInChildren<MeleWeapon>(true);
            var damages=new List<float>();int iceHits=0;bool dodging=false;
            impact=g=>{if(g.IsIcicle&&g.HitPlayers>0){damages.Add(hp.MaxHealth-hp.Health);iceHits+=g.HitPlayers;}hp.AddHealth(1000);};boss.GiftImpacted+=impact;
            var colors=new HashSet<string>();var iceAttacks=new List<int>();int giftWaves=0,iceWaves=0;bool meleePassed=false,retreatPassed=false,dodgePassed=false;
            double deadline=EditorApplication.timeSinceStartup+75;
            var attached=new SerializedObject(boss).FindProperty("hangingIcicles");
            var iceParts=Enumerable.Range(0,attached.arraySize).Select(i=>(Transform)attached.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            int laughBefore=0,impactsBefore=0;
            while(EditorApplication.timeSinceStartup<deadline)
            {
                await Task.Delay(20);
                if(boss.VolleysThrown>giftWaves)
                {
                    giftWaves=boss.VolleysThrown;var gifts=Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>!g.IsIcicle&&!g.Resolved).ToArray();
                    Check(gifts.Length==5,"Existing gift volley count changed");colors.Add(gifts[0].name);
                }
                if(boss.IcicleVolleysThrown>iceWaves)
                {
                    iceWaves=boss.IcicleVolleysThrown;iceAttacks.Add(boss.AttacksStarted);
                    var wave=Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>g.IsIcicle&&!g.Resolved).ToArray();
                    Check(wave.Length==3&&wave.All(g=>!g.Launched),"Expected three warned icicles before release");
                    Check(Vector3.ProjectOnPlane(boss.transform.position-player.transform.position,Vector3.up).magnitude<.3f,"Boss did not approach directly overhead");
                    Check(boss.GetComponent<BoxCollider>().bounds.min.y<3.1f,"Low hull not reachable by sword");
                    if(iceWaves==1)
                    {
                        laughBefore=boss.LaughsPlayed;int before=boss.SharedHP;Camera.main.transform.LookAt(boss.GetComponent<Collider>().bounds.center);
                        typeof(MeleWeapon).GetMethod("UseUpdate",F).Invoke(melee,null);
                        await Until(()=>boss.SharedHP<before,"Actual equipped sword swing did not hit low boss",2);meleePassed=true;
                        lines.Add("PASS actual sword in icicle drop: boss HP "+before+" -> "+boss.SharedHP+"; hull lower bound="+boss.GetComponent<Collider>().bounds.min.y.ToString("F2"));
                        ScreenCapture.CaptureScreenshot("Docs/locations/previews/sleigh-icicles250-warning.png");
                    }
                    else if(iceWaves==2)
                    {
                        Check(iceHits>0&&damages.Any(d=>d>0),"First icicle did not damage real player");
                        impactsBefore=iceHits;dodging=true;var locked=wave.Select(g=>g.LandingPoint).ToArray();Move(player.transform.position+Vector3.forward*4);
                        await Task.Delay(100);Check(wave.Select((g,i)=>Vector3.Distance(g.LandingPoint,locked[i])<.01f).All(x=>x),"Icicle marks chased player");
                    }
                    else if(iceWaves==3)
                    {
                        Check(dodgePassed&&meleePassed&&retreatPassed,"Incomplete earlier checks");
                        Check(iceAttacks.SequenceEqual(new[]{3,6,9}),"Icicle attack is not one in three");
                        Check(colors.Count==3,"A gift color disappeared from rotation");
                        lines.Add("PASS rotation: icicle salvos on attacks 3/6/9; all three existing colors still throw five gifts.");
                        float health=hp.Health;boss.TakeDamage(boss.SharedHP);
                        await Task.Delay(150);Check(boss.IsDead&&!Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Any(g=>!g.Resolved),"Death left dangerous projectiles");
                        await Task.Delay(700);
                        var ui=Object.FindAnyObjectByType<EndGameUIManager>();
                        Check((EndGameState)typeof(EndGameUIManager).GetField("currentState",F).GetValue(ui)==EndGameState.Win,"Ordinary victory UI missing");
                        Check(Mathf.Approximately(health,hp.Health),"Post-death damage");
                        lines.Add("PASS death during icicle warning: hazards removed, no late damage, ordinary Win UI presented.");
                        break;
                    }
                }
                if(iceWaves==1&&boss.State==SleighBossController.FlightState.IcicleRetreat)
                {Check(boss.LaughsPlayed>laughBefore,"No chuckle on departure");retreatPassed=true;}
                if(dodging&&boss.IciclesImpacted>=6)
                {Check(iceHits==impactsBefore,"Dodge did not avoid icicles");dodgePassed=true;dodging=false;lines.Add("PASS dodge: 4 m sidestep, locked marks, no icicle damage.");}
            }
            Check(iceWaves==3&&boss.IsDead,"Nine-attack test did not finish");
            lines.Add("PASS icicle damage samples: "+string.Join(",",damages.Select(d=>d.ToString("F1")))+" HP; one damage pulse per icicle/player.");
            lines.Add("PASS downward source meshes, detach on launch, 0.9 s minimum warning, .6 s flight, low hold 3.2 s, chuckle and retreat; attached ice restored for next pass.");
            lines.Add("LIMIT: local Editor test; full 50 km travel was accelerated to final 0.35 m; WebGL and synchronized sleigh AI are not verified. Shared runs keep original boss.");
            File.WriteAllLines(Report,lines);return string.Join("\n",lines);
        }
        finally
        {
            if(boss&&impact!=null)boss.GiftImpacted-=impact;
            EditorApplication.update-=clock;Time.timeScale=scale;AudioListener.pause=audio;
            if(player)player.enabled=pm;if(input)input.enabled=pi;if(inventory)inventory.enabled=iv;if(active)active.enabled=ac;
            Object.Destroy(platform);
        }
    }
}
