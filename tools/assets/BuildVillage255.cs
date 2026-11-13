// Unity MCP script-execute, class BuildVillage255, method Build.
// Builds a NEW clean inspection asset; source gameplay prefabs and active pools remain intact.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class BuildVillage255
{
    const string Folder="Assets/Prefabs/Workshop/Village255";
    public const string Prefab=Folder+"/Village_Unified.prefab";
    public const string ScenePath="Assets/Scenes/VillageWorkshop.unity";
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static string Path(Transform t,Transform root)=>AnimationUtility.CalculateTransformPath(t,root);
    public static string Build()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode required");
        Check(!AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),"Unified village already exists; inspect before rebuilding");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","Village255");
        var scene=EditorSceneManager.NewPreviewScene();
        var report=new List<string>();
        try
        {
            var sources=new[]{"house1_1","house2_1","house3_1"};var labels=new[]{"House_Grey","House_Brown","House_Green"};
            for(int i=0;i<3;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/"+sources[i]+".prefab");
                var home=source.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Stone_House_Open_Entrance");
                var copy=Object.Instantiate(home.gameObject);SceneManager.MoveGameObjectToScene(copy,scene);
                try
                {
                    copy.name="Stone_House_Open_Entrance";copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);copy.transform.localScale=home.lossyScale;
                    Check(PrefabUtility.SaveAsPrefabAsset(copy,Folder+"/"+labels[i]+".prefab"),"Cannot save current house "+labels[i]);
                }
                finally{Object.DestroyImmediate(copy);}
            }
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/Village_1.prefab");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(original,scene);
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="Village_Unified";
            var content=root.GetComponent<LocationContentSpawner>();
            var oldPaths=content.itemSpawnPoints.Select(p=>Path(p.spawnTransform,root.transform)).ToArray();
            var enemyPaths=content.enemySpawnPoints.Select(p=>Path(p,root.transform)).ToArray();
            var constraints=content.itemSpawnPoints.Select(p=>$"{p.allowedType}|{p.allowedSize}|{p.alignVisualBase}|{p.maxVisualFootprint}").ToArray();
            var center=root.transform.Cast<Transform>().Single(t=>t.name.StartsWith("PointCenter"));var town=center.Find("To4ka7");
            var oldHouses=town.Cast<Transform>().Where(t=>t.name=="dom"||t.name.StartsWith("dom (")||t.name.StartsWith("GunStore")).ToArray();
            Check(oldHouses.Length==10,"Expected ten legacy house roots");
            // All of these objects belong to the new clone created above, never to the source assets.
            foreach(var old in oldHouses){report.Add("Removed legacy building from new clone: "+Path(old,root.transform));Object.DestroyImmediate(old.gameObject);}
            var courts=center.Cast<Transform>().Where(t=>t.name.StartsWith("Village_NewHouse_")).OrderBy(t=>int.Parse(t.name.Substring("Village_NewHouse_".Length))).ToArray();
            Check(courts.Length==10,"Expected ten new building slots");
            var homes=new List<Transform>();
            foreach(var court in courts)
            {
                int number=int.Parse(court.name.Substring("Village_NewHouse_".Length));
                var oldHome=court.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Stone_House_Open_Entrance");
                var floor=oldHome.TransformPoint(new Vector3(0,.514f,0));var rotation=oldHome.rotation;
                var home=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+labels[(number-1)%3]+".prefab"),center);
                home.name="Village_House_"+number.ToString("00");home.transform.rotation=rotation;
                home.transform.position+=floor-home.transform.TransformPoint(new Vector3(0,.514f,0));homes.Add(home.transform);
                Object.DestroyImmediate(court.gameObject);
            }
            // Retain every spawn Transform and its order/constraints. New furnishings may have moved in the latest house.
            for(int i=0;i<content.itemSpawnPoints.Count;i++)
            {
                var point=content.itemSpawnPoints[i].spawnTransform;
                Transform house=homes[Mathf.Min(i/2,homes.Count-1)];
                if(i<20)
                {
                    var support=house.Find(i%2==0?"08_Interior_Props/Dining_Table":"09_Castle_Guardroom/Equipment_Rack");
                    Check(support,"Missing original furnishing support");
                    var rs=support.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                    point.position=new Vector3(b.center.x,b.max.y+.08f,b.center.z);
                }
                else point.position=homes[(i-20)%homes.Count].TransformPoint(new Vector3(0,.56f,-1.2f));
            }
            for(int i=0;i<content.enemySpawnPoints.Count;i++)
                content.enemySpawnPoints[i].position=homes[i%homes.Count].TransformPoint(new Vector3(i<10?-1.3f:1.3f,.54f,-4.5f));
            Check(content.itemSpawnPoints.Select(p=>Path(p.spawnTransform,root.transform)).SequenceEqual(oldPaths),"Spawn references/order changed");
            Check(content.enemySpawnPoints.Select(p=>Path(p,root.transform)).SequenceEqual(enemyPaths),"Enemy references/order changed");
            Check(content.itemSpawnPoints.Select(p=>$"{p.allowedType}|{p.allowedSize}|{p.alignVisualBase}|{p.maxVisualFootprint}").SequenceEqual(constraints),"Loot constraints changed");
            Check(content.itemSpawnPoints.Count==25&&content.enemySpawnPoints.Count==15,"Gameplay point count changed");
            var surface=root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if(surface){surface.BuildNavMesh();if(surface.navMeshData)AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/NavMesh-Village-Unified.asset");}
            Check(PrefabUtility.SaveAsPrefabAsset(root,Prefab),"Cannot save unified village");
            report.Add("Saved one village, ten current houses, 25 item slots, 15 enemy slots, original item collection and enemy definitions.");
            report.Add("Source Village_1/2 and current generation pools were not edited.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var workshop=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(workshop);
        var stage=new GameObject("Village_Review_Set");SceneManager.MoveGameObjectToScene(stage,workshop);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),workshop);instance.transform.SetParent(stage.transform,false);
        var mat=new Material(Shader.Find("Standard")){name="Village_Workshop_Ground",color=new Color(.42f,.49f,.33f)};mat.SetFloat("_Glossiness",0);AssetDatabase.CreateAsset(mat,Folder+"/Workshop_Ground.mat");
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(ground,workshop);ground.name="Workshop_Ground";ground.transform.SetParent(stage.transform,false);
        ground.transform.position=new Vector3(-63,-.5f,8);ground.transform.localScale=new Vector3(140,1,104);ground.GetComponent<Renderer>().sharedMaterial=mat;ground.layer=6;
        var sun=new GameObject("Workshop_Sun");SceneManager.MoveGameObjectToScene(sun,workshop);sun.transform.SetParent(stage.transform,false);sun.transform.rotation=Quaternion.Euler(48,-35,0);
        var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.70f,.75f);RenderSettings.fog=false;
        var camera=new GameObject("Village_Overview_Camera");SceneManager.MoveGameObjectToScene(camera,workshop);camera.transform.SetParent(stage.transform,false);
        camera.transform.position=new Vector3(-63,75,-62);camera.transform.LookAt(new Vector3(-63,1,8));
        var cam=camera.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=53;cam.farClipPlane=300;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.48f,.63f,.72f);cam.scene=workshop;
        Check(EditorSceneManager.SaveScene(workshop,ScenePath),"Cannot save village workshop");AssetDatabase.SaveAssets();
        SceneVisibilityManager.instance.Isolate(stage,true);Selection.activeGameObject=instance;
        if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.LookAt(new Vector3(-63,2,8),Quaternion.Euler(48,0,0),65);SceneView.lastActiveSceneView.Focus();}
        File.WriteAllLines("Library/Village255/build.txt",report);return string.Join("\n",report);
    }
}
