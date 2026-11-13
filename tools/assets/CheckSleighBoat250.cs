using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using DeadBoat.Events.NewYear;
using Object=UnityEngine.Object;

public static class CheckSleighBoat250
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static async Task Until(Func<bool> condition,string why,int seconds=20)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!condition()&&EditorApplication.timeSinceStartup<end)await Task.Delay(20);Check(condition(),why);}
    public static async Task<string> Run()
    {
        await Until(()=>BoardController.Instance&&BoardController.Instance.PlayerOnSeat&&BoardController.Instance.TotalDistanceTraveled>49000,"Finish review not ready",50);
        var player=PlayerMovement.Instance;var input=PlayerInput.Instance;var board=BoardController.Instance;var hp=PlayerStatsManager.Instance;
        bool pm=player.enabled,pi=input.enabled;float scale=Time.timeScale;bool audio=AudioListener.pause;
        player.enabled=false;input.enabled=false;
        EditorApplication.CallbackFunction clock=()=>{Time.timeScale=2;AudioListener.pause=false;};EditorApplication.update+=clock;clock();
        SleighBossController boss=null;Action<SleighGiftAttack> onImpact=g=>hp.AddHealth(1000);
        try
        {
            board.TotalDistanceTraveled=GameManager.Instance.PlayDistance-.1f;typeof(BoardController).GetField("currentSpeed",F).SetValue(board,8f);
            await Until(()=>Object.FindAnyObjectByType<SleighBossController>(),"No ordinary finish boss");boss=Object.FindAnyObjectByType<SleighBossController>();boss.GiftImpacted+=onImpact;
            await Until(()=>boss.State==SleighBossController.FlightState.IcicleDrop,"Low icicle pass missing over actual boat",25);
            var wave=Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g=>g.IsIcicle&&!g.Resolved).ToArray();
            Check(wave.Length==3,"Expected three icicles on boat");
            Check(wave.All(g=>g.LandingPoint.y<player.transform.position.y+.55f),"Icicle line targets tall rail instead of deck");
            Check(Vector3.ProjectOnPlane(boss.transform.position-player.transform.position,Vector3.up).magnitude<4.1f,"Low pass missed boat player");
            var parts=new SerializedObject(boss).FindProperty("hangingIcicles");var ice=Enumerable.Range(0,parts.arraySize).Select(i=>(Transform)parts.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Check(ice.All(t=>t.gameObject.activeSelf),"Attached ice absent before release");
            var shot=new GameObject("Boat250_Inspection_Camera");var cam=shot.AddComponent<Camera>();
            try
            {
                cam.CopyFrom(Camera.main);cam.enabled=false;cam.cullingMask=~(1<<LayerMask.NameToLayer("Player"));
                cam.transform.position=player.transform.position+new Vector3(-7,5,-7);cam.transform.LookAt(boss.transform.position+Vector3.up*.4f);cam.fieldOfView=55;
                var rt=RenderTexture.GetTemporary(1400,900,24);var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);var prior=RenderTexture.active;
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1400,900),0,0);File.WriteAllBytes("Docs/locations/previews/sleigh-icicles250-boat.png",tex.EncodeToPNG());}
                finally{cam.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Object.Destroy(tex);}
            }
            finally{Object.Destroy(shot);}
            await Until(()=>ice.All(t=>!t.gameObject.activeSelf),"Source icicles did not detach",4);
            await Until(()=>boss.IciclesImpacted==3,"Three icicles did not impact boat",4);
            await Until(()=>boss.State==SleighBossController.FlightState.IcicleRetreat,"No retreat",4);
            await Until(()=>ice.All(t=>t.gameObject.activeSelf),"Attached icicles did not reload",4);
            string result="PASS actual boat: ordinary finish spawned sleigh; attack 3 descended beside helm crossbeam within 4 m, three marked icicles targeted deck, all source icicles detached, 3/3 impacts, laugh/retreat and attached ice restored.";
            File.AppendAllText("Docs/locations/previews/sleigh-boss250-check.txt",result+"\n");return result;
        }
        finally{if(boss)boss.GiftImpacted-=onImpact;EditorApplication.update-=clock;Time.timeScale=scale;AudioListener.pause=audio;if(player)player.enabled=pm;if(input)input.enabled=pi;}
    }
}
