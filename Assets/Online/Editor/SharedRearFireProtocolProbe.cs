using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
namespace DeadBoat.Online.Editor
{
 public static class SharedRearFireProtocolProbe
 {
  public static string Result {get;private set;} = "not run";
  static readonly BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
  static async Task<NetworkRunner> Connect(string room,long id,bool create) =>
   await (Task<NetworkRunner>)typeof(SharedAdProtocolProbe).GetMethod("Connect",Private).Invoke(null,new object[]{room,id,create});
  static LobbyNetworkAvatar Spawn(NetworkRunner r, Vector3 position) {
   var o=r.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab"), position, Quaternion.identity, onBeforeSpawned:(_,n)=>Set(n.GetComponent<LobbyNetworkAvatar>(),"Health",100f));
   r.SetPlayerObject(r.LocalPlayer,o);return o.GetComponent<LobbyNetworkAvatar>();
  }
  public static async void Run() {
   if(!EditorApplication.isPlaying || Result=="running")return;
   Result="running";NetworkRunner first=null,second=null;
   float previousScale=Time.timeScale;
   EditorApplication.CallbackFunction clock=()=>Time.timeScale=1;
   EditorApplication.update+=clock; Time.timeScale=1;
   try {
    Require(!SharedRunContext.Active && LoadingManager.Instance==null,"empty isolated scene");
    string room="rear-fire-test-"+Guid.NewGuid().ToString("N");
    first=await Connect(room,601,true);var safe=Spawn(first, Vector3.zero);
    var state=first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),onBeforeSpawned:(_,o)=>{var s=o.GetComponent<SharedDepartureState>();s.Initialize(0,2);Set(s,"BoatInitialized",(NetworkBool)true);}).GetComponent<SharedDepartureState>();
    second=await Connect(room,602,false);var far=Spawn(second, new Vector3(0,0,-1100));
    await Until(()=>state.Phase==2 && Replica(second)?.Phase==2,"crew");
    var remote=Replica(second);state.RPC_SceneReady();remote.RPC_SceneReady();
    await Until(()=>state.Phase==3 && remote.Phase==3,"playing");
    int slot=state.CrewSlot(second.LocalPlayer),safeSlot=state.CrewSlot(first.LocalPlayer);
    far.GetComponent<NetworkTransform>().Teleport(new Vector3(0,0,-1100),Quaternion.identity);
    await Until(()=>remote.RearFireTimers[slot].IsRunning,"remote timer replicated");
    Require(!state.RearFireTimers[safeSlot].IsRunning,"safe player no timer");
    remote.RPC_AdBegin(6060);
    await Until(()=>remote.AdShowing(second.LocalPlayer) && !remote.RearFireTimers[slot].IsRunning,"protected timer cleared");
    remote.RPC_AdEnd(6060);
    await Until(()=>remote.RearFireTimers[slot].IsRunning,"grace expired and timer restarted");
    state.RearFireTimers.Set(slot,TickTimer.CreateFromSeconds(first,30));
    var expected=state.RearFireTimers[slot];
    await Until(()=>remote.RearFireTimers[slot].Equals(expected),"long timer replica");
    await first.Shutdown();first=null;
    await Until(()=>remote.Object.HasStateAuthority,"authority transfer");
    Require(remote.RearFireTimers[slot].Equals(expected),"timer survives takeover without restarting");
    far.GetComponent<NetworkTransform>().Teleport(Vector3.zero,Quaternion.identity);
    await Task.Delay(200);
    Debug.Log("[Rear fire protocol probe] transfer diagnostics: phase="+remote.Phase+" scale="+Time.timeScale+" z="+far.LogicalPosition.z+" authority="+remote.Object.HasStateAuthority+" frame="+second.Tick);
    await Until(()=>!remote.RearFireTimers[slot].IsRunning,"safe exit clears on new master");
    far.GetComponent<NetworkTransform>().Teleport(new Vector3(0,0,-1100),Quaternion.identity);
    try { await Until(()=>remote.RearFireTimers[slot].IsRunning,"new master rearms"); } catch(Exception e) {throw new InvalidOperationException(e.Message+" phase="+remote.Phase+" z="+far.LogicalPosition.z+" hp="+far.Health+" initialized="+remote.BoatInitialized+" protected="+remote.AdProtected(second.LocalPlayer)+" won="+remote.RunWon+" scale="+Time.timeScale); }
    Set(remote,"RunWon",(NetworkBool)true);
    await Until(()=>!remote.RearFireTimers[slot].IsRunning,"victory clears timer");
    Result="PASS: remote timer, safe crew, ad/grace, takeover preserves deadline, safe exit/rearm, victory";
    Debug.Log("[Rear fire protocol probe] "+Result);
   }catch(Exception e){Result="FAIL: "+e;Debug.LogWarning("[Rear fire protocol probe] "+Result);}
   finally {foreach(var r in new[]{first,second})if(r!=null)try{await r.Shutdown();}catch(Exception){} EditorApplication.update-=clock;Time.timeScale=previousScale;}
  }
  static SharedDepartureState Replica(NetworkRunner r)=>UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s=>s.Runner==r);
  static void Set(object o,string p,object v)=>o.GetType().GetProperty(p,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
  static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
  static async Task Until(Func<bool> c,string m){double e=Time.realtimeSinceStartupAsDouble+15;while(!c()){Require(Time.realtimeSinceStartupAsDouble<e,"timeout: "+m);await Task.Delay(50);}}
 }
}
