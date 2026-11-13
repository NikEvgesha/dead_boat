using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class TestEventFix249
{
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly List<string> lines=new();
 static void Note(string line){lines.Add(line);File.WriteAllLines("Docs/locations/previews/event-fix249-runtime.txt",lines);}
 static bool On(Transform t,Transform root){for(;t&&t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
 static Collider[] Solids(GameObject root)=>root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger&&On(c.transform,root.transform)&&!c.GetComponentInParent<EnemyCore>()&&!c.GetComponentInParent<PickableItem>()).ToArray();
 static IEnumerable<Vector3> Candidates(GameObject root){
 var points=new List<(Vector3 p,float area)>();
 foreach(var c in Solids(root)){
 if(c is MeshCollider m&&m.sharedMesh){var v=m.sharedMesh.vertices;var tr=m.sharedMesh.triangles;
 for(int i=0;i<tr.Length;i+=3){var a=m.transform.TransformPoint(v[tr[i]]);var b=m.transform.TransformPoint(v[tr[i+1]]);var d=m.transform.TransformPoint(v[tr[i+2]]);var n=Vector3.Cross(b-a,d-a);var point=(a+b+d)/3;if(m.sharedMesh.name.Contains("Mountain")&&point.y<root.transform.position.y+1.5f)continue;if(n.normalized.y>.8f&&n.magnitude>.01f)points.Add((point,n.magnitude));}}
 else if(c.bounds.size.x>2&&c.bounds.size.z>2)points.Add((new Vector3(c.bounds.center.x,c.bounds.max.y,c.bounds.center.z),c.bounds.size.x*c.bounds.size.z));}
 foreach(var p in points.OrderByDescending(x=>x.area).Take(55))yield return p.p;
 var bounds=EventRouteReview248Setup.Geometry(root);foreach(float x in new[]{0f,-2f,2f})foreach(float z in new[]{0f,-2f,2f})yield return new Vector3(bounds.center.x+x,0,bounds.center.z+z);
 }
 static bool Floor(Vector3 p,out RaycastHit hit){
 hit=default;var hits=Physics.RaycastAll(p+Vector3.up*.6f,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.65f&&!h.collider.GetComponentInParent<EnemyCore>()&&!h.collider.GetComponentInParent<CharacterController>()&&!h.collider.GetComponentInParent<PickableItem>()).OrderBy(h=>h.distance).ToArray();if(hits.Length==0)return false;hit=hits[0];return true;
 }
 static void Place(CharacterController cc,Vector3 point){cc.enabled=false;cc.transform.SetParent(null);cc.transform.position=point+Vector3.up*.04f;cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.down*.09f);}
 static async Task<bool> Walk(GameObject root,CharacterController cc,FootstepController foot,AudioSource audio){
 foreach(var point in Candidates(root))foreach(var direction in new[]{Vector3.right,Vector3.forward,Vector3.left,Vector3.back}){
 if(!Floor(point,out var a)||!Floor(point+direction*3,out var b)||Mathf.Abs(a.point.y-b.point.y)>.5f)continue;
 if(!a.collider.transform.IsChildOf(root.transform)&&Solids(root).Length>0&&Mathf.Abs(point.y)>.001f)continue;
 Place(cc,a.point);typeof(FootstepController).GetMethod("ResetDistance",Private).Invoke(foot,null);await Task.Delay(30);
 int before=foot.StepsPlayed;bool played=false;var start=cc.transform.position;
 for(int i=0;i<54;i++){cc.Move(direction*.06f+Vector3.down*.08f);played|=audio.isPlaying;await Task.Delay(18);}
 float travelled=Vector2.Distance(new Vector2(start.x,start.z),new Vector2(cc.transform.position.x,cc.transform.position.z));
 if(foot.StepsPlayed>before&&played){Note($"PASS {root.name}: surface={a.collider.name}, layer={a.collider.gameObject.layer}, tag={a.collider.tag}, at={a.point:F2}, travel={travelled:F2}, steps={foot.StepsPlayed-before}, AudioSource.isPlaying=True");return true;}
 if(travelled<.5f)continue;
 }
 Note("FAIL no successful physical footstep route "+root.name);return false;
 }
 static void Capture(string name,Vector3 position,Vector3 target){
 var c=Camera.main;var prior=c.targetTexture;var pos=c.transform.position;var rot=c.transform.rotation;float aspect=c.aspect;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,850,24);var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
 try{c.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));c.targetTexture=rt;c.aspect=(float)rt.width/rt.height;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes("Docs/locations/previews/event-fix249-"+name+".png",tex.EncodeToPNG());}
 finally{c.targetTexture=prior;c.aspect=aspect;c.transform.SetPositionAndRotation(pos,rot);RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(tex);}
 }
 public static async Task<string> Details(){
 var route=Object.FindAnyObjectByType<EventRouteReview248>();var player=PlayerMovement.Instance;var input=PlayerInput.Instance;var source=player.GetComponent<CharacterController>();
 bool move=player.enabled,enabled=input.enabled,collide=source.enabled;input.SitTrain(false);input.enabled=false;player.enabled=false;source.enabled=false;
 var obj=new GameObject("Landing_Connection_Probe249");var cc=obj.AddComponent<CharacterController>();cc.height=source.height;cc.radius=source.radius;cc.center=source.center;cc.stepOffset=source.stepOffset;cc.slopeLimit=source.slopeLimit;cc.skinWidth=source.skinWidth;cc.minMoveDistance=0;
 EditorApplication.CallbackFunction clock=()=>Time.timeScale=1;EditorApplication.update+=clock;
 try{
 Note("DETAIL controller stepOffset="+cc.stepOffset+" slopeLimit="+cc.slopeLimit);
 foreach(var stop in route.Stops.Where(s=>s.Root.name.Contains("castle_")||s.Root.name.Contains("ship_"))){
 player.transform.position=new Vector3(0,1,stop.Center);typeof(LendController).GetMethod("Movelocation",Private).Invoke(Object.FindAnyObjectByType<LendController>(),new object[]{stop.Center});route.TickRoute();Physics.SyncTransforms();await Task.Delay(65);
 if(stop.Root.name.Contains("ship_")){
 var art=stop.Root.transform.Find("Ship1_ProductionVisual");Vector3 Map(Vector3 p)=>art.TransformPoint(p-new Vector3(-72.66f,0,5.9f));var position=Map(new Vector3(-72.75f,3.34f,-19.24f));var target=Map(new Vector3(-71.15f,5.39f,-8.1f));
 Capture("ship-approach-"+stop.Root.name,position,target);
 var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);var shadows=sun.shadows;sun.shadows=LightShadows.Soft;Capture("ship-soft-"+stop.Root.name,position,target);sun.shadows=shadows;
 continue;}
 foreach(var f in stop.Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="wall-narrow-stairs")){
 var v=f.sharedMesh.vertices.Select(f.transform.TransformPoint).ToArray();var tr=f.sharedMesh.triangles;var faces=new List<(float y,float area,Vector3 p)>();
 for(int i=0;i<tr.Length;i+=3){var a=v[tr[i]];var b=v[tr[i+1]];var c=v[tr[i+2]];var n=Vector3.Cross(b-a,c-a);if(n.normalized.y>.99f)faces.Add(((a.y+b.y+c.y)/3,n.magnitude*.5f,(a+b+c)/3));}
 float top=faces.GroupBy(x=>Mathf.Round(x.y*100)/100).OrderByDescending(g=>g.Sum(x=>x.area)).First().Key;
 var step=faces.Where(x=>x.y<top-.1f).GroupBy(x=>Mathf.Round(x.y*100)/100).OrderByDescending(g=>g.Key).First();var end=step.Aggregate(Vector3.zero,(a,b)=>a+b.p*b.area)/step.Sum(x=>x.area);
 var pad=stop.Root.transform.Find("Stair_Landings249").Cast<Transform>().OrderBy(t=>Vector3.Distance(t.position,end)).First();
 var start=end-pad.forward*.7f;if(!Floor(start,out var support)){Note("FAIL landing start floor");continue;}Place(cc,support.point);await Task.Delay(30);
 for(int i=0;i<76;i++){cc.Move(pad.forward*.05f+Vector3.down*.08f);await Task.Delay(18);}
 bool ok=cc.transform.position.y>top-.12f;Note((ok?"PASS ":"FAIL ")+stop.Root.name+" stairs -> landing, start="+support.point.ToString("F3")+" end="+cc.transform.position.ToString("F3")+" deckY="+top.ToString("F3"));
 }
 }
 var hospital=GameObject.Find("EventCastle_Winter").transform.Find("03_Service_Houses/06_AnimalMerge");
 foreach(var p in hospital.GetComponentsInChildren<StorePoint>(true)){var so=new SerializedObject(p);((GameObject)so.FindProperty("_BuyInfoCanvas").objectReferenceValue).SetActive(true);((GameObject)so.FindProperty("_PriceCanvas").objectReferenceValue).SetActive(true);typeof(StorePoint).GetMethod("CheckMoney",Private).Invoke(p,null);}
 var ui=(GameObject)new SerializedObject(hospital.GetComponentInChildren<StorePoint>(true)).FindProperty("_BuyInfoCanvas").objectReferenceValue;
 Capture("hospital",ui.transform.position+ui.transform.forward*2.1f,ui.transform.position+Vector3.up*.2f);
 return string.Join("\n",lines.Where(x=>x.Contains("stairs ->")||x.StartsWith("DETAIL")));
 }finally{EditorApplication.update-=clock;Object.Destroy(obj);player.enabled=move;input.enabled=enabled;source.enabled=collide;}
 }
 public static async Task<string> Run(){
 var route=Object.FindAnyObjectByType<EventRouteReview248>();if(!route||!EditorApplication.isPlaying)throw new Exception("Prepared route required");
 var player=PlayerMovement.Instance;var input=PlayerInput.Instance;var stats=PlayerStatsManager.Instance;var original=player.GetComponent<CharacterController>();var source=player.GetComponent<FootstepController>();
 if(!source)throw new Exception("Player has no footstep controller");
 var controllerObject=new GameObject("Footstep_Verification249");var cc=controllerObject.AddComponent<CharacterController>();cc.height=original.height;cc.radius=original.radius;cc.center=original.center;cc.stepOffset=original.stepOffset;cc.slopeLimit=original.slopeLimit;cc.skinWidth=original.skinWidth;cc.minMoveDistance=0;
 var audio=controllerObject.AddComponent<AudioSource>();var foot=controllerObject.AddComponent<FootstepController>();EditorUtility.CopySerialized(source,foot);
 var serialized=new SerializedObject(foot);serialized.FindProperty("audioSource").objectReferenceValue=audio;serialized.FindProperty("controller").objectReferenceValue=cc;serialized.ApplyModifiedPropertiesWithoutUndo();
 var sourceAudio=(AudioSource)new SerializedObject(source).FindProperty("audioSource").objectReferenceValue;EditorUtility.CopySerialized(sourceAudio,audio);audio.playOnAwake=false;audio.spatialBlend=0;audio.ignoreListenerPause=true;
 bool movement=player.enabled,inputs=input.enabled,character=original.enabled,inGame=stats.InGame;float volume=AudioListener.volume;
 EditorApplication.CallbackFunction clock=()=>{Time.timeScale=1;AudioListener.pause=false;};EditorApplication.update+=clock;
 int passed=0;try{
 input.SitTrain(false);input.enabled=false;player.enabled=false;original.enabled=false;stats.InGame=false;AudioListener.volume=0;
 foreach(var seat in Object.FindObjectsByType<DriverSeatTrigger>(FindObjectsSortMode.None))typeof(DriverSeatTrigger).GetMethod("ExitDrivingMode",Private).Invoke(seat,null);
 Note("DB-ON-249: physical CharacterController copied from player, actual FootstepController and serialized clips, all real POI supports; isolated save.");
 await Task.Delay(70);
 // Check the live hospital, including source-instance propagation and both simultaneous panels.
 var hospital=GameObject.Find("EventCastle_Winter").transform.Find("03_Service_Houses/06_AnimalMerge");
 foreach(var point in hospital.GetComponentsInChildren<StorePoint>(true)){
 var so=new SerializedObject(point);var buy=(GameObject)so.FindProperty("_BuyInfoCanvas").objectReferenceValue;var price=(GameObject)so.FindProperty("_PriceCanvas").objectReferenceValue;
 var rect=(RectTransform)buy.transform;float width=rect.rect.width*rect.lossyScale.x;Note((width<2?"PASS ":"FAIL ")+"hospital purchase panel world width="+width.ToString("F3")+" m");buy.SetActive(true);price.SetActive(true);}
 var firstCanvas=(GameObject)new SerializedObject(hospital.GetComponentInChildren<StorePoint>(true)).FindProperty("_BuyInfoCanvas").objectReferenceValue;
 Capture("hospital",firstCanvas.transform.position+firstCanvas.transform.forward*2.1f,firstCanvas.transform.position+Vector3.up*.2f);
 foreach(var stop in route.Stops){
 player.transform.position=new Vector3(0,1,stop.Center);var land=Object.FindAnyObjectByType<LendController>();typeof(LendController).GetMethod("Movelocation",Private).Invoke(land,new object[]{stop.Center});route.TickRoute();Physics.SyncTransforms();await Task.Delay(55);
 foreach(var enemy in Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None))enemy.enabled=false;
 if(await Walk(stop.Root,cc,foot,audio))passed++;
 if(stop.Root.name.Contains("Snow_Tunnel")){
 var mountain=stop.Root.GetComponentsInChildren<MeshCollider>().FirstOrDefault(m=>m.sharedMesh&&m.sharedMesh.name.Contains("Signal_Pass_Mountain"));
 if(mountain)await Walk(mountain.gameObject,cc,foot,audio);else Note("FAIL missing physical mountain");}
 if(stop.Root.name.Contains("Gift_Sled")){var b=EventRouteReview248Setup.Geometry(stop.Root);Capture("sleigh-player",new Vector3(11,1.8f,stop.Center-6),b.center);}
 if(stop.Root.name.Contains("NY_Ruins")){var decor=stop.Root.transform.Find("Winter_Ruins249");var p=decor.Find("Garland grounded post").position;Capture("ruins-player",p+new Vector3(12,.15f,-7),p+new Vector3(7,1,0));}
 if(stop.Root.name.Contains("ship_")){
 var art=stop.Root.transform.Find("Ship1_ProductionVisual");Vector3 Map(Vector3 p)=>art.TransformPoint(p-new Vector3(-72.66f,0,5.9f));
 Capture("ship-stairs-"+stop.Root.name,Map(new Vector3(-71.15f,8.8f,-3.9f)),Map(new Vector3(-77.95f,12.2f,2.62f)));
 Capture("ship-deck-"+stop.Root.name,Map(new Vector3(-77.95f,12.2f,2.62f)),Map(new Vector3(-77.95f,17.2f,16.4f)));}
 if(stop.Root.name.Contains("castle_"))foreach(Transform pad in stop.Root.transform.Find("Stair_Landings249")){
 if(await Walk(pad.gameObject,cc,foot,audio))passed++;var p=pad.position+Vector3.up*.18f;Capture("landing-"+stop.Root.name+"-"+pad.GetSiblingIndex(),p-pad.forward*2.3f+Vector3.up*1.65f,p+pad.forward*1.4f);}
 }
 // Suppression checks use the last verified solid support.
 int initial=foot.StepsPlayed;input.SitTrain(true);for(int i=0;i<45;i++){cc.Move(Vector3.right*.06f+Vector3.down*.08f);await Task.Delay(18);}Note((foot.StepsPlayed==initial?"PASS ":"FAIL ")+"no steps in driving mode");input.SitTrain(false);
 Place(cc,cc.transform.position+Vector3.up*4);initial=foot.StepsPlayed;for(int i=0;i<45;i++){cc.Move(Vector3.right*.06f);await Task.Delay(18);}Note((foot.StepsPlayed==initial?"PASS ":"FAIL ")+"no steps in air");
 Note("RESULT successful surfaces="+passed+" (23 POIs + four landings)");return string.Join("\n",lines);
 }finally{
 input.enabled=inputs;player.enabled=movement;original.enabled=character;stats.InGame=inGame;AudioListener.volume=volume;EditorApplication.update-=clock;Object.Destroy(controllerObject);
 }
 }
}
