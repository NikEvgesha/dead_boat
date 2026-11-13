// Unity MCP full script-execute, Run. Real local gameplay, input and focus clock held for probe only.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DeadBoat.Events.NewYear;
using Object=UnityEngine.Object;

public static class SleighVolley246
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string why){if(!value)throw new InvalidOperationException(why);}
    static void Move(PlayerMovement player,Vector3 point)
    {var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=point;cc.enabled=true;Physics.SyncTransforms();}
    public static async Task<string> Run()
    {
        double deadline=EditorApplication.timeSinceStartup+50;
        while(!PlayerMovement.Instance||!SaveManager.Instance||!Object.FindAnyObjectByType<SleighBossController>())
        {Check(EditorApplication.timeSinceStartup<deadline,"Review not ready");await Task.Delay(100);}
        Check(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave,"Isolated save required");
        var boss=Object.FindAnyObjectByType<SleighBossController>();Check(boss.State==SleighBossController.FlightState.Waiting,"Fresh boss required");
        var player=PlayerMovement.Instance;var input=PlayerInput.Instance;var inventory=Inventory.Instance;
        var active=Object.FindAnyObjectByType<ActiveItemManager>();var hp=PlayerStatsManager.Instance;
        bool movement=player.enabled,inputs=input.enabled,inv=inventory.enabled,itemControl=active.enabled;
        float scale=Time.timeScale;bool audioPause=AudioListener.pause;
        player.enabled=false;input.enabled=false;inventory.enabled=false;active.enabled=false;
        foreach(var field in new[]{"_attack","_healing","_reload","_pause"})typeof(PlayerInput).GetField(field,Private)?.SetValue(input,false);
        EditorApplication.CallbackFunction clock=()=>{Time.timeScale=1;AudioListener.pause=false;};EditorApplication.update+=clock;clock();
        var lines=new List<string>{"DB-ON-246 real local Play Mode: "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")};
        try
        {
            var so=new SerializedObject(boss);
            var laughs=so.FindProperty("laughClips");
            for(int i=0;i<laughs.arraySize;i++)
            {
                var clip=(AudioClip)laughs.GetArrayElementAtIndex(i).objectReferenceValue;Check(clip&&clip.length>.7f&&clip.length<.96f,"New hi-hi clip missing");
                var data=new float[clip.samples*clip.channels];Check(clip.GetData(data,0),"Cannot read voice samples");
                float peak=data.Max(v=>Mathf.Abs(v));Check(peak>.1f&&peak<.99f,"Voice silent/clipped");
                lines.Add("PASS new voiced hi-hi "+clip.name+": "+clip.length.ToString("F3")+" s, peak="+peak.ToString("F3"));
            }
            Vector3 approach=boss.transform.position+Vector3.ProjectOnPlane(player.transform.position-boss.transform.position,Vector3.up).normalized*27.5f;
            Check(boss.FindSurface(new Vector3(approach.x,player.transform.position.y,approach.z),out var floor),"No approach floor");
            approach.y=floor.point.y+.08f;Move(player,approach);
            double start=Time.timeAsDouble,prior=start;Vector3 previous=boss.transform.position;
            float air=0,low=0,firstApproach=-1,bombTime=0,bombDistance=0,fastTime=0,fastDistance=0;
            bool recovery=false,dodged=false,blueChecked=false;float beforeBlue=0;int seenVolleys=0;
            var waveCounts=new List<int>();var colors=new HashSet<string>();
            while(Time.timeAsDouble-start<40)
            {
                await Task.Delay(20);double now=Time.timeAsDouble;float dt=(float)(now-prior);prior=now;
                float distance=Vector3.Distance(previous,boss.transform.position);previous=boss.transform.position;
                var state=boss.State;
                bool lowState=state==SleighBossController.FlightState.Descending||state==SleighBossController.FlightState.Recovery;
                if(lowState)low+=dt;else air+=dt;
                if(state==SleighBossController.FlightState.Approach){fastTime+=dt;fastDistance+=distance;}
                if(state==SleighBossController.FlightState.Bombardment){bombTime+=dt;bombDistance+=distance;if(firstApproach<0)firstApproach=(float)(now-start);}
                if(boss.VolleysThrown>seenVolleys)
                {
                    seenVolleys=boss.VolleysThrown;
                    var wave=Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>!g.Resolved).ToArray();
                    Check(wave.Length==5,"Expected five presents in wave "+seenVolleys+", got "+wave.Length);
                    Check(wave.All(g=>!g.Launched),"Five warnings must precede launches");waveCounts.Add(wave.Length);
                    colors.Add(wave[0].name);
                    for(int a=0;a<wave.Length;a++)for(int b=a+1;b<wave.Length;b++)
                        Check(Vector3.ProjectOnPlane(wave[a].LandingPoint-wave[b].LandingPoint,Vector3.up).magnitude>wave[a].Radius+wave[b].Radius+.8f,"Five gifts overlap and close dodge gaps");
                    lines.Add("PASS wave "+seenVolleys+": five "+wave[0].name+", distinct separated landing marks");
                    if(seenVolleys==1)
                    {
                        Camera.main.transform.LookAt(wave.OrderByDescending(g=>Vector3.Distance(g.LandingPoint,player.transform.position)).First().LandingPoint);
                        ScreenCapture.CaptureScreenshot("Docs/locations/previews/sleigh-five-warning246.png");
                    }
                    if(seenVolleys==2)
                    {
                        beforeBlue=hp.Health;
                        // The midpoint between marks on the blue line is a genuine escape gap.
                        Vector3 escape=player.transform.position+Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized*2.95f;
                        Check(boss.FindSurface(escape,out var exitFloor),"No escape floor");Move(player,exitFloor.point+Vector3.up*.08f);dodged=true;
                    }
                    if(seenVolleys==3){Check(dodged&&Mathf.Approximately(beforeBlue,hp.Health),"Blue gap did not allow dodging");blueChecked=true;}
                }
                if(state==SleighBossController.FlightState.Descending)Check(seenVolleys==3,"Premature descent");
                if(state==SleighBossController.FlightState.Recovery)recovery=true;
                if(recovery&&state==SleighBossController.FlightState.Approach)break;
            }
            Check(waveCounts.Count==3&&colors.Count==3,"Must exercise all three five-present patterns");
            Check(firstApproach>1.4f&&firstApproach<1.8f,"First transit is not twice as quick: "+firstApproach);
            Check(recovery&&blueChecked,"Complete cycle/dodge not observed");
            float fast=fastDistance/fastTime,slow=bombDistance/bombTime;
            Check(fast>slow*2,"No visible slowdown during drops");
            float share=air/(air+low);Check(share>=.7f&&share<=.8f,"Airborne share outside prior 70-80% request");
            Check(boss.ThrowSoundsPlayed==15,"Each of the 15 presents must actually launch with sound");
            Check(boss.PlayerHits>0,"No actual player hits");
            lines.Add("PASS transit "+firstApproach.ToString("F2")+" s vs previous 3.2 s; avg transit "+fast.ToString("F2")+" m/s, dropping "+slow.ToString("F2")+" m/s");
            lines.Add("PASS cycle: air "+air.ToString("F2")+" s, low "+low.ToString("F2")+" s, airborne "+(share*100).ToString("F1")+"%; 15 launched presents, laughs="+boss.LaughsPlayed);
            lines.Add("PASS real player damage + blue dodge gap, final player HP="+hp.Health);
            boss.TakeDamage(boss.SharedHP);await Task.Delay(150);
            Check(boss.IsDead&&Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Length==0,"Hazards remain after death");
            lines.Add("PASS defeat clears hazards; sound character/tempo await owner review. Focus clock held only for fixture and restored in finally.");
            File.WriteAllLines("Docs/locations/previews/sleigh-volley246.txt",lines);return string.Join("\n",lines);
        }
        catch(Exception e){lines.Add("FAIL "+e.Message);File.WriteAllLines("Docs/locations/previews/sleigh-volley246.txt",lines);throw;}
        finally{EditorApplication.update-=clock;Time.timeScale=scale;AudioListener.pause=audioPause;player.enabled=movement;input.enabled=inputs;inventory.enabled=inv;active.enabled=itemControl;}
    }
}
