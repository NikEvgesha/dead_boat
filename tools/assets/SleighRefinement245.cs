// Unity MCP script-execute: Assets in Edit Mode, then Run in isolated event Play Mode.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeadBoat.Events.NewYear;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SleighRefinement245
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Report="Docs/locations/previews/sleigh-refinement245.txt";
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static string Assets()
    {
        Check(!EditorApplication.isPlaying,"Check assets in Edit Mode");
        // Build twice to verify that the expression survives the actual rebuild path.
        SleighBossReview244.Build(); SleighBossReview244.Build();
        var lines=new List<string>();
        foreach(var path in new[]{"Assets/Events/NewYear/Models/SleighThief_Visual.prefab",SleighBossReview244.BossPath})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var brow in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Brow_")))
            {
                var bounds=brow.GetComponent<MeshFilter>().sharedMesh.bounds;
                Vector3 a=root.transform.InverseTransformPoint(brow.TransformPoint(new Vector3(bounds.min.x,bounds.center.y,bounds.center.z)));
                Vector3 b=root.transform.InverseTransformPoint(brow.TransformPoint(new Vector3(bounds.max.x,bounds.center.y,bounds.center.z)));
                Vector3 inner=Mathf.Abs(a.x)<Mathf.Abs(b.x)?a:b,outer=Mathf.Abs(a.x)<Mathf.Abs(b.x)?b:a;
                Check(outer.y-inner.y>.1f,"Inner eyebrow tip does not point down: "+path+" "+brow.name);
                lines.Add("PASS persisted angry eyebrow "+root.name+"/"+brow.name+": outer above inner="+(outer.y-inner.y).ToString("F3")+" m");
            }
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SleighBossReview244.BossPath);
        var so=new SerializedObject(prefab.GetComponent<SleighBossController>());
        var clips=so.FindProperty("laughClips"); Check(clips.arraySize==3,"Three chuckles required");
        var all=Enumerable.Range(0,clips.arraySize).Select(i=>(AudioClip)clips.GetArrayElementAtIndex(i).objectReferenceValue)
            .Concat(new[]{(AudioClip)so.FindProperty("throwClip").objectReferenceValue});
        foreach(var clip in all)
        {
            Check(clip&&clip.samples>6000,"Missing/empty audio clip");
            clip.LoadAudioData();var data=new float[clip.samples*clip.channels];Check(clip.GetData(data,0),"Cannot read audio");
            float peak=data.Max(v=>Mathf.Abs(v));Check(peak>.1f&&peak<.99f,"Silent or clipped audio");
            lines.Add("PASS audio "+clip.name+": "+clip.length.ToString("F3")+" s, peak="+peak.ToString("F3"));
        }
        foreach(var sound in prefab.GetComponentsInChildren<AudioSource>(true))
            Check(sound.spatialBlend==1&&sound.outputAudioMixerGroup&&sound.outputAudioMixerGroup.name=="Sound","Sound must use 3D Sound mixer");
        File.WriteAllLines(Report,lines);return string.Join("\n",lines);
    }

    static void Move(PlayerMovement p,Vector3 at)
    {
        var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=at;cc.enabled=true;Physics.SyncTransforms();
    }

    public static async Task<string> Run()
    {
        double deadline=EditorApplication.timeSinceStartup+50;
        while(!PlayerMovement.Instance||!SaveManager.Instance||!Object.FindAnyObjectByType<SleighBossController>())
        {Check(EditorApplication.timeSinceStartup<deadline,"Event review not initialized");await Task.Delay(100);}
        Check(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave,"Isolated save required");
        var boss=Object.FindAnyObjectByType<SleighBossController>();
        Check(boss.State==SleighBossController.FlightState.Waiting,"Requires a fresh boss");
        var p=PlayerMovement.Instance;var hp=PlayerStatsManager.Instance;var active=Object.FindAnyObjectByType<ActiveItemManager>();
        var input=PlayerInput.Instance;var inventory=Inventory.Instance;
        bool moveEnabled=p.enabled,inputEnabled=input.enabled,activeEnabled=active.enabled,inventoryEnabled=inventory.enabled;
        p.enabled=false;input.enabled=false;active.enabled=false;inventory.enabled=false;
        foreach(var name in new[]{"_attack","_healing","_reload"})typeof(PlayerInput).GetField(name,Private)?.SetValue(input,false);
        var lines=File.ReadAllLines(Report).ToList();
        float priorScale=Time.timeScale;bool priorAudioPause=AudioListener.pause;
        EditorApplication.CallbackFunction clock=()=>{Time.timeScale=1;AudioListener.pause=false;};
        EditorApplication.update+=clock;clock();
        try
        {
            int initialHP=boss.SharedHP;
            var source=(AudioSource)typeof(SleighBossController).GetField("laughSource",Private).GetValue(boss);
            var throws=(AudioSource)typeof(SleighBossController).GetField("throwSource",Private).GetValue(boss);
            Vector3 toward=Vector3.ProjectOnPlane(boss.transform.position-p.transform.position,Vector3.up).normalized;
            Vector3 start=boss.transform.position-toward*27.5f;start.y=p.transform.position.y;
            Check(boss.FindSurface(start,out var floor),"Approach floor missing");start.y=floor.point.y+.08f;
            float horizontal=Vector3.ProjectOnPlane(start-boss.transform.position,Vector3.up).magnitude;
            float actual=Vector3.Distance(start,boss.transform.position);
            double beginning=Time.timeAsDouble;Move(p,start);
            await Task.Delay(150);
            Check(boss.State!=SleighBossController.FlightState.Waiting&&boss.SharedHP==initialHP,"Approach must aggro before player attacks");
            Check(horizontal<28&&actual>28,"Must exercise altitude-independent distance");
            Check(boss.LaughsPlayed==1&&source.isPlaying,"Aggro chuckle did not play");
            lines.Add("PASS automatic first attack without any shot: horizontal="+horizontal.ToString("F2")+" m, 3D="+actual.ToString("F2")+" m");
            float airborne=0,low=0;double prior=Time.timeAsDouble;bool sawRecovery=false,sawThrow=false,sawLaugh=true,dodged=false,blueSafe=false,blueDamageChecked=false;
            int beforeDescent=0;float blueHP=0;Vector3[] bluePoints=null;SleighGiftAttack[] blue=null;
            Vector3? coastStart=null;float coastDistance=0;double dodgeTime=0;
            while(Time.timeAsDouble-beginning<40)
            {
                await Task.Delay(20);
                var state=boss.State;double now=Time.timeAsDouble;float dt=(float)(now-prior);prior=now;
                bool lowState=state==SleighBossController.FlightState.Descending||state==SleighBossController.FlightState.Recovery;
                if(lowState)low+=dt;else airborne+=dt;
                sawThrow|=throws.isPlaying;sawLaugh|=source.isPlaying;
                if(lowState&&beforeDescent==0){beforeDescent=boss.VolleysThrown;Check(beforeDescent==3,"Boss descended before three aerial volleys");}
                if(state==SleighBossController.FlightState.Recovery)sawRecovery=true;
                if(boss.VolleysThrown==1&&state==SleighBossController.FlightState.Bombardment)
                {if(!coastStart.HasValue)coastStart=boss.transform.position;coastDistance=Mathf.Max(coastDistance,Vector3.Distance(coastStart.Value,boss.transform.position));}
                if(!dodged&&boss.VolleysThrown==2)
                {
                    blue=Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>!g.Resolved&&g.name.Contains("Blue")).ToArray();
                    if(blue.Length>0)
                    {
                        blueHP=hp.Health;bluePoints=blue.Select(g=>g.LandingPoint).ToArray();
                        Vector3 escape=p.transform.position+Vector3.forward*9;
                        Check(boss.FindSurface(escape,out var exitFloor),"Dodge floor missing");
                        Move(p,exitFloor.point+Vector3.up*.08f);dodged=true;dodgeTime=now;
                    }
                }
                if(dodged&&!blueSafe&&now-dodgeTime>.3)
                {
                    Check(blue.Select((g,i)=>g&&Vector3.Distance(g.LandingPoint,bluePoints[i])<.01f).All(v=>v),"Blue marks chased player during coast");
                    blueSafe=true;
                }
                if(dodged&&!blueDamageChecked&&boss.VolleysThrown==2&&state==SleighBossController.FlightState.Approach)
                {Check(Mathf.Approximately(hp.Health,blueHP),"Dodge failed after airborne cadence change");blueDamageChecked=true;}
                if(sawRecovery&&state==SleighBossController.FlightState.Approach)break;
            }
            Check(sawRecovery,"No low recovery during complete cycle");
            float share=airborne/(airborne+low);
            Check(share>=.7f&&share<=.8f,"Measured airborne share outside 70–80%: "+share);
            Check(coastDistance>2,"Sleigh must move while dropping gifts");
            Check(sawThrow&&sawLaugh&&boss.ThrowSoundsPlayed>=3&&boss.LaughsPlayed>=2,"Live attack audio was not observed");
            Check(dodged&&blueSafe&&blueDamageChecked,"Blue dodge check was not exercised");
            Check(boss.PlayerHits>=1,"No real player damage observed");
            lines.Add("PASS measured complete cycle: airborne="+airborne.ToString("F2")+" s, low="+low.ToString("F2")+" s, airborne share="+(share*100).ToString("F1")+"%; first descent after "+beforeDescent+" volleys");
            lines.Add("PASS moving gift pass: travelled "+coastDistance.ToString("F2")+" m during first bombardment; blue locked marks still dodgeable");
            lines.Add("PASS live 3D sounds: laughs="+boss.LaughsPlayed+", throws="+boss.ThrowSoundsPlayed+"; actual AudioSource.isPlaying observed");
            // Freeze on leaving the low pause and exercise the normal gun path again.
            boss.enabled=false;
            var item=inventory.GetItems().First(i=>i&&i.GetComponentInChildren<RangedWeaponController>(true));active.SwitchActiveItem(item);
            await Task.Delay(150);var rifle=item.GetComponentInChildren<RangedWeaponController>(true);
            rifle.ApplySharedAmmo(1);PauseManager.Instance.SetPause(false);ControlManager.Instance.ForceGameplayCursor();
            Camera.main.transform.LookAt(boss.GetComponent<Collider>().bounds.center);Physics.SyncTransforms();
            int before=boss.SharedHP;typeof(RangedWeaponController).GetMethod("UseUpdate",Private).Invoke(rifle,null);
            await Task.Delay(150);Check(boss.SharedHP<before,"Rifle stopped damaging boss");
            lines.Add("PASS normal rifle after cycle: HP "+before+" -> "+boss.SharedHP);
            boss.enabled=true;boss.PlayThrowSound();
            typeof(SleighBossController).GetField("nextLaughTime",Private).SetValue(boss,0f);
            typeof(SleighBossController).GetMethod("PlayLaugh",Private).Invoke(boss,null);
            boss.TakeDamage(boss.SharedHP);await Task.Delay(100);
            Check(!source.isPlaying&&!throws.isPlaying&&Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Length==0,"Audio/hazards continued after death");
            lines.Add("PASS death stops voice, throw sound and remaining hazards");
            lines.Add("Fixture: input held and background-focus time/audio pause overridden only for this automated probe; restored in finally. Manual sound/balance acceptance remains with owner.");
            File.WriteAllLines(Report,lines);return string.Join("\n",lines);
        }
        catch(Exception e){lines.Add("FAIL "+e.Message);File.WriteAllLines(Report,lines);throw;}
        finally{EditorApplication.update-=clock;Time.timeScale=priorScale;AudioListener.pause=priorAudioPause;p.enabled=moveEnabled;input.enabled=inputEnabled;active.enabled=activeEnabled;inventory.enabled=inventoryEnabled;}
    }
}
