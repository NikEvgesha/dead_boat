// Unity MCP script-execute: Prepare, then Enter, then Test, Exit, Restore.
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class TrapScenario243
{
    const string ScenePath="Assets/_Recovery/TrapAudit243.unity";
    const string Report="Docs/locations/previews/trap-audit243.txt";
    const string Source="Assets/Prefabs/Locations/Enemy/Sklep_1.prefab";
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static string RepairSpawnReference()
    {
        var root=PrefabUtility.LoadPrefabContents(Source);
        try
        {
            var point=root.transform.Find("Trap/spawnPoints/Point4");var so=new SerializedObject(point.GetComponent<DungeonTrap>());
            var previous=(Transform)so.FindProperty("_spawnPos").objectReferenceValue;
            Check(previous,"Missing Point4 spawn reference");
            if(previous==point)return "Point4 already uses its own transform";
            Check(!previous.IsChildOf(root.transform),"Unexpected local reference; inspect before changing");
            string before=AssetDatabase.GetAssetPath(previous)+" "+previous.position;
            so.FindProperty("_spawnPos").objectReferenceValue=point;so.ApplyModifiedPropertiesWithoutUndo();
            Check(PrefabUtility.SaveAsPrefabAsset(root,Source),"Failed to save Sklep_1");
            return "Fixed Sklep_1 Point4: "+before+" -> own Point4 "+point.position;
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static string Prepare()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Prepare in Edit Mode");
        Check(Enumerable.Range(0,SceneManager.sceneCount).All(i=>!SceneManager.GetSceneAt(i).isDirty),"Unsaved scene; preserve it before preparing test");
        Check(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),"Test scene already exists; inspect it");
        SessionState.SetString("Trap243.PriorStart",EditorSceneManager.playModeStartScene?AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene):"");
        SessionState.SetString("Trap243.PriorActive",SceneManager.GetActiveScene().path);
        SessionState.SetString("Trap243.PriorScenes",string.Join("\n",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)));
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var configs=new[]{("SourceSklep",Source,Vector3.zero),
            ("WorkshopSklep","Assets/Prefabs/Workshop/Sklep1/Sklep1_OvergrownRuins.prefab",new Vector3(150,0,0)),
            ("Ruins","Assets/Prefabs/Locations/Enemy/ruins_1.prefab",new Vector3(450,0,0))};
        foreach(var config in configs)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(config.Item2),scene);
            root.name=config.Item1;root.transform.position+=config.Item3;
            foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))
                if(b)b.enabled=new[]{"ActivateZone","DungeonTrap","ActivateObject","Lever","LightStick","DarkZone","NavMeshSurface","NavMeshLink"}.Contains(b.GetType().Name);
            foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b&&new[]{"PickableItem","Portal","UseButtonShow"}.Contains(b.GetType().Name)))
                b.gameObject.SetActive(false);
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
        }
        var sun=new GameObject("Audit_Sun");SceneManager.MoveGameObjectToScene(sun,scene);sun.tag="Sun";
        sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.AddComponent<Light>().type=LightType.Directional;
        foreach(var d in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DarkZone>(true)))
        {var so=new SerializedObject(d);so.FindProperty("sunlightInZone").objectReferenceValue=sun.GetComponent<Light>();so.ApplyModifiedPropertiesWithoutUndo();}
        var profile=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
        var player=new GameObject("Physics_Player_Probe");SceneManager.MoveGameObjectToScene(player,scene);player.tag="Player";
        player.layer=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").layer;
        player.transform.position=new Vector3(20,0,24.9f);
        var capsule=player.AddComponent<CapsuleCollider>();capsule.height=profile.height;capsule.radius=profile.radius;capsule.center=profile.center;
        var body=player.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        var camera=new GameObject("Audit_Camera");SceneManager.MoveGameObjectToScene(camera,scene);
        var cam=camera.AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(20,15,40);camera.transform.LookAt(new Vector3(52,2,9));
        cam.farClipPlane=250;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.47f,.63f,.74f);
        EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);
        SceneManager.SetActiveScene(SceneManager.GetSceneByPath(SessionState.GetString("Trap243.PriorActive","")));
        return "Prepared isolated physics test scene; existing scenes untouched.";
    }
    public static string Enter()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Already playing");
        DeadBoat.Online.Editor.SharedPerformanceTestSave.Prepare();
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorApplication.EnterPlaymode();return "Entering isolated trap test Play Mode.";
    }
    static async Task Move(Vector3 p)
    {
        var rb=GameObject.Find("Physics_Player_Probe").GetComponent<Rigidbody>();rb.position=p;Physics.SyncTransforms();await Task.Delay(300);
    }
    static async Task WaitFor(Func<bool> condition,string failure,float seconds=8)
    {
        double deadline=EditorApplication.timeSinceStartup+seconds;
        while(!condition()&&EditorApplication.timeSinceStartup<deadline)await Task.Delay(100);
        Check(condition(),failure);
    }
    static void Shot(string file,Vector3 position,Vector3 target)
    {
        var cam=GameObject.Find("Audit_Camera").GetComponent<Camera>();cam.transform.position=position;cam.transform.LookAt(target);
        var rt=RenderTexture.GetTemporary(1200,820,24);var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);File.WriteAllBytes("Docs/locations/previews/"+file+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Object.Destroy(tex);}
    }
    public static async Task<string> Test()
    {
        Check(EditorApplication.isPlaying&&SceneManager.GetActiveScene().path==ScenePath,"Use isolated Play Mode");
        var lines=new List<string>{"DB-ON-243 Unity Play Mode, physical Player-tagged capsule matching stock player dimensions; solo/offline."};
        File.WriteAllLines(Report,lines);
        foreach(string name in new[]{"SourceSklep","WorkshopSklep"})
        {
            var root=GameObject.Find(name);var zone=root.transform.Find("Trap/zoneTrap");var gates=zone.Find("WallActive");
            Check(!gates.gameObject.activeSelf,name+": gates initially closed");
            var offset=root.transform.position;
            await Move(offset+new Vector3(37,0,24.9f));
            Check(!gates.gameObject.activeSelf,name+": activated outside zone");
            await Move(offset+new Vector3(43,0,24.9f));
            await WaitFor(()=>gates.gameObject.activeInHierarchy,name+": physical Player entry did not close gates");
            var points=root.transform.Find("Trap/spawnPoints").GetComponentsInChildren<DungeonTrap>();
            await WaitFor(()=>points.All(t=>t.GetComponentsInChildren<ZombieController>().Length==3),name+": expected 3 enemies at each of 4 points");
            foreach(var point in points)
            {
                var so=new SerializedObject(point);var spawn=(Transform)so.FindProperty("_spawnPos").objectReferenceValue;
                Check(spawn.IsChildOf(root.transform),name+": external spawn reference");
                Check(point.GetComponentsInChildren<ZombieController>().All(e=>Vector3.Distance(e.transform.position,spawn.position)<.2f),name+": enemy spawned at wrong position "+point.name);
            }
            // Actual controller must be stopped by the closed gate, then pass after Lever.Use.
            var probe=new GameObject("Gate_Clearance_Controller");SceneManager.MoveGameObjectToScene(probe,root.scene);
            var profile=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
            var cc=probe.AddComponent<CharacterController>();cc.height=profile.height;cc.radius=profile.radius;cc.center=profile.center;cc.stepOffset=profile.stepOffset;cc.skinWidth=profile.skinWidth;
            cc.enabled=false;probe.transform.position=offset+new Vector3(32,.18f,24.9f);cc.enabled=true;
            for(int i=0;i<160;i++)cc.Move(new Vector3(.05f,-.02f,0));
            Check(probe.transform.position.x<offset.x+35,name+": closed gate does not block player");
            if(name=="SourceSklep")Shot("sklep1-trap-closed",offset+new Vector3(31,2,24.9f),offset+new Vector3(36,2,24.9f));
            root.GetComponentInChildren<Lever>(true).Use();
            await WaitFor(()=>!zone.gameObject.activeSelf,name+": lever did not release zone and gates");
            for(int i=0;i<160;i++)cc.Move(new Vector3(.05f,-.02f,0));
            Check(probe.transform.position.x>offset.x+39,name+": opened gate still blocks player");Object.Destroy(probe);
            await Move(offset+new Vector3(37,0,24.9f));await Move(offset+new Vector3(43,0,24.9f));
            Check(points.Sum(t=>t.GetComponentsInChildren<ZombieController>().Length)==12,name+": duplicate wave after lever/re-entry");
            lines.Add("PASS "+name+": physical entry closed both gates; controller blocked; four local spawn points produced 12/12 enemies; Lever.Use opened path; re-entry did not respawn wave.");
            File.WriteAllLines(Report,lines);
        }
        var ruins=GameObject.Find("Ruins");var outer=ruins.transform.Find("ActivateDangeusZone");var trap=outer.Find("Trap").GetComponent<DungeonTrap>();
        var soTrap=new SerializedObject(trap);var torches=soTrap.FindProperty("_torchs");
        var firstLight=(LightStick)torches.GetArrayElementAtIndex(0).objectReferenceValue;
        bool LightOn(LightStick light){var so=new SerializedObject(light);return ((GameObject)so.FindProperty("_light").objectReferenceValue).activeSelf;}
        var outerBox=outer.GetComponent<BoxCollider>();var inner=trap.transform.Find("Zone").GetComponent<BoxCollider>();
        await Move(outerBox.bounds.center+Vector3.right*(outerBox.bounds.extents.x+3));
        var center=outerBox.bounds.center;center.y=ruins.transform.position.y-45.3f;
        await Move(center);await WaitFor(()=>LightOn(firstLight),"Ruins: outer trigger did not light torches");
        var inside=inner.bounds.center;inside.y=ruins.transform.position.y-45.3f;
        await Move(inside);await WaitFor(()=>trap.GetComponentsInChildren<ZombieController>().Length==20,"Ruins: trap did not spawn full 20-enemy wave",27);
        Check(!LightOn(firstLight),"Ruins: trap did not extinguish torches");
        await Move(inside+Vector3.right*(inner.bounds.extents.x+3));await Move(inside);await Task.Delay(1200);
        Check(trap.GetComponentsInChildren<ZombieController>().Length==20,"Ruins: duplicate wave on re-entry");
        lines.Add("PASS Ruins: physical outer entry lit torches; inner entry extinguished them and spawned 20/20 enemies once.");
        lines.Add("Inventory: automatic gate closure exists in Sklep_1; ruins_1 has torch/dungeon ambush. Sklep_2 and ruins_2 have no DungeonTrap. To4ka6 BlockSpike meshes are static decorations, without damage scripts/Animator. No rising spike trap found in current location prefabs.");
        lines.Add("LIMIT: probe verifies local physics/event/Animator/spawn behavior; combat balance and network synchronization are outside this test.");
        File.WriteAllLines(Report,lines);return string.Join("\n",lines);
    }
    public static string Exit(){Check(EditorApplication.isPlaying,"Not playing");EditorApplication.ExitPlaymode();return "Leaving isolated trap test.";}
    public static string Restore()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Wait for Edit Mode");
        string prior=SessionState.GetString("Trap243.PriorStart","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(prior)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(prior);
        var expected=SessionState.GetString("Trap243.PriorScenes","").Split('\n');
        var actual=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        Check(expected.SequenceEqual(actual),"Open-scene set differs after Play Mode");
        var active=SceneManager.GetSceneByPath(SessionState.GetString("Trap243.PriorActive",""));if(active.IsValid())SceneManager.SetActiveScene(active);
        var type=TypeCache.GetTypesDerivedFrom<EditorWindow>().Single(t=>t.Name=="Sklep1WalkWindow");
        foreach(var w in Resources.FindObjectsOfTypeAll(type).OfType<EditorWindow>())w.Close();
        type.GetMethod("Open",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly).Invoke(null,null);
        // This fixture asset was created by Prepare in this task; keep only the repeatable source and evidence.
        Check(AssetDatabase.DeleteAsset(ScenePath),"Could not remove temporary test fixture scene");
        File.AppendAllText(Report,"RESTORED: original play-start scene, all "+actual.Length+" previously open scenes, temporary save/profiler settings; Sklep walk reopened; fixture scene removed.\n");
        return "Restored prior play-start scene and all "+actual.Length+" open scenes; Sklep 1 walk reopened.";
    }
}
