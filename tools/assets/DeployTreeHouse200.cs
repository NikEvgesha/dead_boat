using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class DeployTreeHouse200
{
    const string Enemy = "Assets/Prefabs/Locations/Enemy/";
    const string Workshop = "Assets/Prefabs/Workshop/TreeHousePrototypes/";
    const string RefinedPath = Workshop + "TreeHouse_Original_Refined.prefab";
    const string VisualPath = Workshop + "TreeHouse_ProductionVisual.prefab";
    const string VisualName = "TreeHouse_ProductionVisual";

    // Two elevated surfaces retain the placement from the source location.
    static readonly Vector3[] Items = {
        new Vector3(-55.48f,.20f,16.68f), new Vector3(-54.41f,9.55f,4.67f),
        new Vector3(-54.57f,9.45f,7.48f), new Vector3(-57.05f,6.375f,19.94f),
        new Vector3(-63.70f,10.505f,1.35f)
    };
    static readonly Vector3[] Enemies = {
        new Vector3(-58.74f,9.62f,2.76f), new Vector3(-54.87f,9.37f,4.16f),
        new Vector3(-55.67f,5.65f,18.53f), new Vector3(-53.4f,.20f,20f),
        new Vector3(-55.6f,.20f,18.0f)
    };

    static Transform FindModel(GameObject root) =>
        root.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="dom").transform;

    static void Record(UnityEngine.Object obj) => PrefabUtility.RecordPrefabInstancePropertyModifications(obj);

    static void CreateVisual(GameObject authored)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=(GameObject)PrefabUtility.InstantiatePrefab(authored,scene);
            PrefabUtility.UnpackPrefabInstance(copy,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var model=FindModel(copy);
            var visual=new GameObject(VisualName);
            SceneManager.MoveGameObjectToScene(visual,scene);
            visual.transform.position=model.position;
            model.SetParent(visual.transform,true);
            foreach(var name in new[]{
                "185_Stair_And_Landing_Details", "Oak_Canopy_191", "Interior_Lighting",
                "Upper_Room_Furniture", "Lower_Room_Furniture"})
            {
                var child=copy.transform.Find(name);
                if(!child) throw new InvalidOperationException("Missing "+name);
                child.SetParent(visual.transform,true);
            }
            if(!PrefabUtility.SaveAsPrefabAsset(visual,VisualPath)) throw new IOException("Visual prefab save failed");
            if(AssetDatabase.GetDependencies(VisualPath).Contains(RefinedPath) ||
               AssetDatabase.GetDependencies(VisualPath).Contains(Enemy+"TreeHouse_1.prefab"))
                throw new InvalidOperationException("Visual depends on a location prefab");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static float FindSupport(GameObject art, Vector3 point)
    {
        var ray=new Ray(point+Vector3.up*.65f,Vector3.down);
        float top=float.NegativeInfinity;
        foreach(var collider in art.GetComponentsInChildren<Collider>())
            if(collider.enabled && !collider.isTrigger && collider.Raycast(ray,out var hit,1.45f) && hit.normal.y>.55f)
                top=Mathf.Max(top,hit.point.y);
        if(float.IsNegativeInfinity(top))
        {
            if(point.y<.4f) return .20f;
            throw new InvalidOperationException("Unsupported spawn point "+point);
        }
        return top;
    }

    static void UpdateLocation(int bank, Vector3 authorPivot)
    {
        string name="TreeHouse_"+bank;
        string path=Enemy+name+".prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var original=FindModel(root);
            var pivot=original.position;
            var rotation=original.rotation;
            Vector3 Map(Vector3 point)=>pivot+rotation*(point-authorPivot);
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath),original.parent);
            art.transform.SetPositionAndRotation(pivot,rotation);
            Record(art.transform);
            original.gameObject.SetActive(false);
            Record(original.gameObject);
            Physics.SyncTransforms();

            var spawner=root.GetComponent<LocationContentSpawner>();
            if(spawner.itemSpawnPoints.Count!=Items.Length || spawner.enemySpawnPoints.Count!=Enemies.Length)
                throw new InvalidOperationException("Unexpected location point count");
            for(int i=0;i<Items.Length;i++)
            {
                var point=spawner.itemSpawnPoints[i].spawnTransform;
                var p=Map(Items[i]);
                p.y=FindSupport(art,p)+.25f;
                point.position=p;
                Record(point);
            }
            for(int i=0;i<Enemies.Length;i++)
            {
                var p=Map(Enemies[i]);
                p.y=FindSupport(art,p)+.06f;
                spawner.enemySpawnPoints[i].position=p;
                Record(spawner.enemySpawnPoints[i]);
            }

            var nav=root.GetComponent<NavMeshSurface>();
            nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            nav.layerMask=nav.layerMask.value | 1;
            nav.BuildNavMesh();
            var baked=nav.navMeshData;
            if(!baked || AssetDatabase.Contains(baked)) throw new InvalidOperationException("Navigation bake did not produce new data");
            nav.RemoveData();
            var navPath=Enemy+"NavMesh-"+name+"-Refined.asset";
            AssetDatabase.CreateAsset(baked,navPath);
            Record(nav);

            var link=root.GetComponentInChildren<NavMeshLink>();
            var start=Map(new Vector3(-56.72f,.52f,15.25f));
            var end=Map(new Vector3(-56.72f,.20f,17.7f));
            link.startPoint=link.transform.InverseTransformPoint(start);
            link.endPoint=link.transform.InverseTransformPoint(end);
            Record(link);
            if(!PrefabUtility.SaveAsPrefabAsset(root,path)) throw new IOException("Could not save "+name);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void PreserveWalkVersion()
    {
        var root=PrefabUtility.LoadPrefabContents(RefinedPath);
        try
        {
            var art=root.GetComponentsInChildren<Transform>(true).First(t=>t.name==VisualName);
            art.gameObject.SetActive(false);
            Record(art.gameObject);
            var original=FindModel(root);
            original.gameObject.SetActive(true);
            Record(original.gameObject);
            if(!PrefabUtility.SaveAsPrefabAsset(root,RefinedPath)) throw new IOException("Could not save workshop override");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        if(AssetDatabase.LoadMainAssetAtPath(VisualPath)) throw new InvalidOperationException("Production visual already exists");
        Directory.CreateDirectory("Temp/TreeHouseGame200");
        foreach(var path in new[]{Enemy+"TreeHouse_1.prefab",Enemy+"TreeHouse_2.prefab",RefinedPath})
            File.Copy(path,"Temp/TreeHouseGame200/"+System.IO.Path.GetFileName(path));
        var authored=AssetDatabase.LoadAssetAtPath<GameObject>(RefinedPath);
        var pivot=FindModel(authored).position;
        CreateVisual(authored);
        // Publish the left variant last because the workshop inherits from it.
        UpdateLocation(2,pivot);
        UpdateLocation(1,pivot);
        PreserveWalkVersion();
        AssetDatabase.SaveAssets();
        return "Updated both existing gameplay prefabs, rebuilt their NavMesh and mapped five item points each.";
    }
}

