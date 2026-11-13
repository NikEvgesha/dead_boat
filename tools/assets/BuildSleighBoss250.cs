using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadBoat.Events.NewYear;
using Object=UnityEngine.Object;

public static class BuildSleighBoss250
{
    public static string Build()
    {
        const string folder="Assets/Events/NewYear/Boss250";
        const string bossPath="Assets/Events/NewYear/Boss244/SleighThief_Boss.prefab";
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Events/NewYear","Boss250");
        var root=PrefabUtility.LoadPrefabContents(bossPath);
        try
        {
            var ice=root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name.StartsWith("Engine_Ice",StringComparison.Ordinal)).OrderBy(m=>m.name).ToArray();
            if(ice.Length!=3)throw new Exception("Expected three attached icicles");
            string path=folder+"/Icicle_Falling.prefab";
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))
            {
                var mesh=Object.Instantiate(ice[0].sharedMesh);mesh.name="Sleigh_Falling_Icicle";
                var bounds=ice[0].GetComponent<MeshRenderer>().bounds;
                mesh.vertices=mesh.vertices.Select(v=>ice[0].transform.TransformPoint(v)-bounds.center).ToArray();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,folder+"/Icicle_Mesh.asset");
                var warning=new Material(Shader.Find("Unlit/Color")){name="Icicle_Warning_Cyan",color=new Color(.1f,.85f,1)};
                AssetDatabase.CreateAsset(warning,folder+"/Icicle_Warning.mat");
                var projectile=new GameObject("Icicle_Falling");SceneManager.MoveGameObjectToScene(projectile,root.scene);
                try
                {
                    var visual=new GameObject("Falling_Ice");visual.transform.SetParent(projectile.transform,false);
                    visual.AddComponent<MeshFilter>().sharedMesh=mesh;
                    visual.AddComponent<MeshRenderer>().sharedMaterials=ice[0].GetComponent<MeshRenderer>().sharedMaterials;
                    var attack=projectile.AddComponent<SleighGiftAttack>();var so=new SerializedObject(attack);
                    so.FindProperty("giftVisual").objectReferenceValue=visual.transform;so.FindProperty("warningMaterial").objectReferenceValue=warning;
                    so.FindProperty("icicle").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(projectile,path);
                }
                finally{Object.DestroyImmediate(projectile);}
            }
            var controller=root.GetComponent<SleighBossController>();var data=new SerializedObject(controller);
            data.FindProperty("icicleProjectile").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<SleighGiftAttack>();
            var attached=data.FindProperty("hangingIcicles");attached.arraySize=3;
            for(int i=0;i<3;i++)attached.GetArrayElementAtIndex(i).objectReferenceValue=ice[i].transform;
            data.FindProperty("icicleAttackInterval").intValue=3;data.FindProperty("icicleHoverHeight").floatValue=2.8f;
            data.FindProperty("icicleDropDuration").floatValue=3.2f;data.FindProperty("icicleDamage").intValue=18;data.ApplyModifiedPropertiesWithoutUndo();
            if(!root.GetComponent<SleighBossEncounter>())root.AddComponent<SleighBossEncounter>();
            PrefabUtility.SaveAsPrefabAsset(root,bossPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/Scenes/LevelEvent.unity",OpenSceneMode.Additive);
        try
        {
            var spawner=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BossSpawner>(true)).Single();
            var so=new SerializedObject(spawner);var previous=so.FindProperty("_boss").objectReferenceValue;
            so.FindProperty("_soloBossOverride").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(bossPath).GetComponent<SleighBossEncounter>();
            so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);
            foreach(var preview in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="SleighThief_VisualPreview"))preview.gameObject.SetActive(false);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save event boss binding");
            AssetDatabase.SaveAssets();return "LevelEvent solo finish now uses sleigh; existing synchronized boss retained: "+previous.name+". Three source icicles configured, every third attack, 18 damage.";
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);}
    }
}
