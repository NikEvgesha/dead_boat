using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeadBoat.FirstRun.Editor
{
    [InitializeOnLoad]
    public static class FirstRunPrototypeTools
    {
        private const string Backup="DeadBoat.FirstRunSmoke.";
        static FirstRunPrototypeTools()
        {
            EditorApplication.playModeStateChanged += change => {
                if(change!=PlayModeStateChange.EnteredEditMode)return;
                if(SessionState.GetBool(Backup+"clock",false)) {Time.timeScale=SessionState.GetFloat(Backup+"scale",1);SessionState.SetBool(Backup+"clock",false);}
                if(SessionState.GetBool(Backup+"manual",false))
                { EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Backup+"start",""));SessionState.SetBool(Backup+"manual",false); }
                if(!SessionState.GetBool(Backup+"active",false))return;
                if(SessionState.GetBool(Backup+"exists",false))PlayerPrefs.SetString(FirstRunPrototype.CheckpointKey,SessionState.GetString(Backup+"value",""));
                else PlayerPrefs.DeleteKey(FirstRunPrototype.CheckpointKey);
                SessionState.SetBool(Backup+"active",false);PlayerPrefs.Save();
            };
        }
        [MenuItem("Tools/Dead Boat/First Run/Play Prototype")]
        public static void PlayPrototype()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            Create();SessionState.SetFloat(Backup+"scale",Time.timeScale);SessionState.SetBool(Backup+"clock",true);SessionState.SetString(Backup+"start",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool(Backup+"manual",true);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;
        }
        public static string PrepareSmoke()
        {
            Create();
            SessionState.SetFloat(Backup+"scale",Time.timeScale);SessionState.SetBool(Backup+"clock",true);
            DeadBoat.Online.Editor.SharedRewardGameplayProbe.Prepare();
            SessionState.SetBool(Backup+"exists",PlayerPrefs.HasKey(FirstRunPrototype.CheckpointKey));
            SessionState.SetString(Backup+"value",PlayerPrefs.GetString(FirstRunPrototype.CheckpointKey,""));SessionState.SetBool(Backup+"active",true);
            PlayerPrefs.SetString(FirstRunPrototype.CheckpointKey,"");
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            return "prepared isolated first-run scene; prototype and owner preferences backed up";
        }
        public static string Smoke()
        {
            var r=UnityEngine.Object.FindAnyObjectByType<FirstRunPrototype>();if(r==null||r.Progress==null||Time.timeScale<=0)throw new Exception("Prototype not started");
            r.enabled=false;
            var move=typeof(FirstRunPrototype).GetMethod("SetPlayer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            r.Progress.Advance(FirstRunStage.Walk);
            r.Interact();if(r.Progress.Stage!=FirstRunStage.PickUp)throw new Exception("Remote interaction allowed");
            move.Invoke(r,new object[]{new Vector3(0,1,7)});r.Interact();r.Bag();r.Bag();
            if(r.Progress.Stage!=FirstRunStage.Fuel)throw new Exception("Can/bag flow");
            move.Invoke(r,new object[]{new Vector3(0,1,12)});r.Interact();move.Invoke(r,new object[]{new Vector3(0,1,15)});r.Interact();
            r.Progress.Sail(50);r.Progress.Hit();r.Progress.Hit();r.Progress.Sail(180);
            if(r.Progress.Stage!=FirstRunStage.Completed||UnityEngine.Object.FindObjectsByType<Fusion.NetworkRunner>(FindObjectsSortMode.None).Length!=0||SaveManager.Instance!=null)throw new Exception("Completion/isolation");
            r.enabled=true;
            return "PASS: runtime pickup proximity, bag in/out, refuel, board, threat/finish; no SaveManager or Photon runners";
        }
        public const string ScenePath="Assets/Scenes/FirstRunPrototype.unity";
        [MenuItem("Tools/Dead Boat/First Run/Create Prototype Scene")]
        public static string Create()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)return ScenePath;
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try { var root=new GameObject("First run greybox");SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<FirstRunPrototype>();if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Scene save failed"); }
            finally {SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            return ScenePath;
        }
        public static string Validate()
        {
            var a=new DeadBoat.Online.RunRandom(FirstRunProgress.Seed,"first-run-decor-v1",0);
            var b=new DeadBoat.Online.RunRandom(FirstRunProgress.Seed,"first-run-decor-v1",0);
            for(int i=0;i<100;i++){new DeadBoat.Online.RunRandom(88,"unrelated",i).Value.ToString();if(a.Value!=b.Value)throw new Exception("Seed drift");}
            var p=new FirstRunProgress();
            if(p.Advance(FirstRunStage.Board))throw new Exception("Skipped gates");
            for(int i=0;i<6;i++)if(!p.Advance((FirstRunStage)i))throw new Exception("Stage gate");
            p.Sail(1000);if(p.Stage!=FirstRunStage.Threat||p.Distance!=50)throw new Exception("Threat gate");
            if(!p.Damage(100)||p.Health!=100||p.Stage!=FirstRunStage.Threat)throw new Exception("Retry checkpoint");
            p.Hit();var restored=FirstRunPrototype.ReadCheckpoint(JsonUtility.ToJson(p));restored.Hit();restored.Sail(1000);
            if(restored.Stage!=FirstRunStage.Completed||restored.Advance(FirstRunStage.Walk))throw new Exception("Finish gate");
            restored.Skip();if(restored.Stage!=FirstRunStage.Completed)throw new Exception("Terminal state");
            p=new FirstRunProgress();p.Skip();if(!FirstRunPrototype.ReadCheckpoint(JsonUtility.ToJson(p)).Closed)throw new Exception("Skip persistence");
            if(FirstRunPrototype.ReadCheckpoint("invalid").Stage!=FirstRunStage.Walk)throw new Exception("Invalid recovery");
            return "PASS: ordered steps, threat gate, checkpoint retry, resume, completion and skip terminal states; no profile writes";
        }
    }
}
