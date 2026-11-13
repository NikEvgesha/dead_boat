// Unity MCP script-execute: FixPoiFlowerColors.Apply, then Audit.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixPoiFlowerColors
{
    const string Flower="Assets/Import/3D Cartoon Village/Models/Flower.fbx";
    const string Folder="Assets/Prefabs/Workshop/SharedVegetation/";
    static readonly HashSet<string> Heads=new(){"Mesh7","Mesh9","Mesh11","Mesh13","Mesh15"};
    static string[] Prefabs()=>AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Locations","Assets/Prefabs/Workshop"})
        .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
    static MeshFilter[] Flowers(GameObject root)=>root.GetComponentsInChildren<MeshFilter>(true)
        .Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh)==Flower).ToArray();
    static Material Material(string name,string hex)
    {
        var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard")){name=name};AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString(hex,out var color);m.color=color;m.SetFloat("_Glossiness",.02f);EditorUtility.SetDirty(m);return m;
    }
    static int Paint(GameObject root,Material green,Material violet)
    {
        int changed=0;
        foreach(var filter in Flowers(root))
        {
            var r=filter.GetComponent<MeshRenderer>();var target=Heads.Contains(filter.sharedMesh.name)?violet:green;
            var materials=Enumerable.Repeat(target,Mathf.Max(filter.sharedMesh.subMeshCount,r.sharedMaterials.Length)).ToArray();
            if(r.sharedMaterials.SequenceEqual(materials))continue;
            var position=filter.transform.position;var rotation=filter.transform.rotation;var scale=filter.transform.lossyScale;var mesh=filter.sharedMesh;
            Undo.RecordObject(r,"Green leaves and violet flowers");r.sharedMaterials=materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            if(position!=filter.transform.position||rotation!=filter.transform.rotation||scale!=filter.transform.lossyScale||mesh!=filter.sharedMesh)
                throw new InvalidOperationException("Flower geometry or transform changed");
            changed++;
        }
        return changed;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","SharedVegetation");
        var green=Material("Flower_Leaves_Green","#557D3B");var violet=Material("Flower_Petals_Violet","#8555A2");
        var report=new List<string>();int prefabCount=0,sceneCount=0,renderers=0;
        foreach(var path in Prefabs())
        {
            if(Flowers(AssetDatabase.LoadAssetAtPath<GameObject>(path)).Length==0)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                int count=Paint(root,green,violet);
                if(count>0){if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new InvalidOperationException("Cannot save "+path);report.Add(path+": "+count+" renderers");prefabCount++;renderers+=count;}
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        var active=SceneManager.GetActiveScene();
        foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            if(!AssetDatabase.GetDependencies(path,true).Contains(Flower))continue;
            var scene=SceneManager.GetSceneByPath(path);bool already=scene.isLoaded;
            if(!already)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            bool dirty=scene.isDirty;
            try
            {
                int count=scene.GetRootGameObjects().Sum(g=>Paint(g,green,violet));
                if(count==0)continue;
                EditorSceneManager.MarkSceneDirty(scene);
                if(dirty&&already)report.Add("PENDING SAVE (preserved existing edits): "+path+": "+count);
                else if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Cannot save "+path);
                else report.Add(path+": "+count+" direct/overridden renderers");
                sceneCount++;renderers+=count;
            }
            finally{if(!already)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
        AssetDatabase.SaveAssets();
        report.Insert(0,$"Changed {prefabCount} prefabs, {sceneCount} scenes, {renderers} renderer material assignments. Positions, scales, meshes and colliders preserved.");
        File.WriteAllLines("Library/To4ka6ConceptReview/flower-colors-change.txt",report);
        return string.Join("\n",report);
    }
    public static string Audit()
    {
        var green=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Flower_Leaves_Green.mat");
        var violet=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Flower_Petals_Violet.mat");
        var report=new List<string>();int leaves=0,heads=0;
        foreach(var path in Prefabs())
        {
            var filters=Flowers(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            foreach(var f in filters)
            {
                bool flower=Heads.Contains(f.sharedMesh.name);var expected=flower?violet:green;
                if(f.GetComponent<MeshRenderer>().sharedMaterials.Any(m=>m!=expected))throw new InvalidOperationException("Wrong flower part material: "+path+"/"+f.name);
                if(flower)heads++;else leaves++;
            }
            if(filters.Length>0)report.Add(path+": correct "+filters.Length+" part renderers");
        }
        report.Insert(0,$"PASS {leaves} green leaf/stem renderers and {heads} violet flower-head renderers across authored POI prefabs.");
        File.WriteAllLines("Library/To4ka6ConceptReview/flower-colors-check.txt",report);return string.Join("\n",report);
    }
}
