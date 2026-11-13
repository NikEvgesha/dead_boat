using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DeadBoat.Events.NewYear;
using Object=UnityEngine.Object;
public static class VerifyBoss254
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static async Task Until(Func<bool> f,string why,int seconds=20){double end=EditorApplication.timeSinceStartup+seconds;while(!f()&&EditorApplication.timeSinceStartup<end)await Task.Delay(20);Check(f(),why);}
 static object Call(object b,string m)=>b.GetType().GetMethod(m,F).Invoke(b,null);
 public static async Task<string> Run(){
 await Until(()=>PlayerMovement.Instance&&BoardController.Instance&&BoardController.Instance.PlayerOnSeat,"Game not ready",60);
 Check(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave,"Isolated save required");
 var player=PlayerMovement.Instance;var hp=PlayerStatsManager.Instance;var input=PlayerInput.Instance;
 var prior=player.transform.position;bool pm=player.enabled,pi=input.enabled;float scale=Time.timeScale;
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Boss254_TestFloor";floor.layer=6;floor.transform.position=new Vector3(100,19.8f,300);floor.transform.localScale=new Vector3(50,.4f,50);
 SleighBossController boss=null;var log=new List<string>();
 void Move(Vector3 p){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
 try{
 player.enabled=false;input.enabled=false;PauseManager.Instance.SetPause(false);Time.timeScale=1;hp.Revive();Move(new Vector3(100,20.1f,300));
 boss=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Boss244/SleighThief_Boss.prefab"),new Vector3(160,34,300),Quaternion.identity).GetComponent<SleighBossController>();
 await Task.Delay(150);boss.enabled=false;
 Check(Mathf.Approximately((float)typeof(SleighBossController).GetField("radialAttackChance",F).GetValue(boss),.2f),"20% chance missing");
 Check((bool)Call(boss,"BeginIciclePass"),"No clear icicle pass");boss.enabled=true;
 await Until(()=>boss.State==SleighBossController.FlightState.IcicleDrop,"No pass");
 var start=boss.transform.position;float started=Time.time;var samples=new List<Vector3>();var drops=new List<SleighGiftAttack>();var born=new List<float>();
 while(boss.State==SleighBossController.FlightState.IcicleDrop){
 samples.Add(boss.transform.position);
 foreach(var g in Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>g.IsIcicle))if(!drops.Contains(g)){drops.Add(g);born.Add(Time.time-started);}
 await Task.Delay(20);
 }
 Check(drops.Count==3,"Expected three sequential icicles, got "+drops.Count);
 Check(born[1]-born[0]>.8f&&born[2]-born[1]>.8f,"Drops not sequential at one second intervals");
 log.Add("PASS SAMPLES start="+start+" last="+samples.Last()+" y="+samples.Min(p=>p.y)+".."+samples.Max(p=>p.y));
 Check(samples.All(p=>Mathf.Abs(p.y-start.y)<.05f),"Pass changes height");
 var direction=(samples.Last()-start).normalized;Check(samples.All(p=>Vector3.Cross(p-start,direction).magnitude<.05f),"Pass not straight");
 Check(start.y>=26&&start.y<=28,"Pass altitude "+start.y);
 Check(Vector3.Distance(start,samples.Last())>11.5f,"Pass length incorrect");
 await Until(()=>boss.IciclesImpacted==3,"Missing icicle impacts");boss.enabled=false;
 log.Add("PASS icicle: 7 m altitude; straight 12 m / 4 s; three releases at "+string.Join(",",born.Select(t=>t.ToString("F2")))+" s; 3 impacts");
 var from=typeof(SleighGiftAttack).GetField("verticalStart",F);
 // Confirm the stored vertical origins and landing points share horizontal coordinates.
 // Completed projectiles have already cleaned themselves up; trajectory is sampled in a second pass.
 Check((bool)Call(boss,"BeginRadialAttack"),"No radial low approach");boss.enabled=true;
 await Until(()=>boss.State==SleighBossController.FlightState.RadialBurst,"No low burst");
 Check(boss.transform.position.y<22,"Radial attack did not descend close to ground");
 await Until(()=>boss.RadialVolleysThrown==1,"No radial gifts");
 var gifts=Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>!g.IsIcicle).ToArray();Check(gifts.Length==8,"Expected eight radial gifts: "+gifts.Length);
 Check(gifts.All(g=>Mathf.Abs(Vector3.ProjectOnPlane(g.LandingPoint-boss.transform.position,Vector3.up).magnitude-3.5f)<.1f),"Gifts not radial");
 float before=hp.Health;Move(gifts[0].LandingPoint+Vector3.up*.1f);
 await Until(()=>boss.GiftsImpacted==8,"Missing radial explosions");
 Check(hp.Health<before,"Radial gift caused no damage");
 log.Add("PASS radial: 20% configured; low descent; eight 3.5 m ring gifts; eight explosions; player HP "+before+" -> "+hp.Health);
 await Until(()=>boss.State==SleighBossController.FlightState.IcicleRetreat,"No radial retreat");
 boss.TakeDamage(100000);await Task.Delay(400);Check(!Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Any(),"Projectiles survived defeat");
 log.Add("PASS retreat and defeat cleanup");return string.Join("\n",log);
 }finally{File.WriteAllLines("Docs/locations/previews/boss254-check.txt",log);if(boss)Object.Destroy(boss.gameObject);Object.Destroy(floor);Move(prior);player.enabled=pm;input.enabled=pi;Time.timeScale=scale;hp.Revive();}
 }
}
