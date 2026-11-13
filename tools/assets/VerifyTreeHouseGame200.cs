using System;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class VerifyTreeHouseGame200
{
    const string Enemy = "Assets/Prefabs/Locations/Enemy/";
    const string Refined = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    static readonly Vector3[] Route = {
        new Vector3(-56.72f,.20f,16.28f), new Vector3(-56.72f,2.87f,10.80f),
        new Vector3(-53.48f,2.87f,10.80f), new Vector3(-53.48f,5.66f,16.28f),
        new Vector3(-56.90f,5.66f,16.28f), new Vector3(-56.90f,5.66f,18.55f),
        new Vector3(-56.90f,5.66f,16.28f), new Vector3(-58.34f,5.66f,16.28f),
        new Vector3(-58.34f,9.38f,11.20f), new Vector3(-58.34f,9.62f,8.20f),
        new Vector3(-58.34f,9.62f,5f), new Vector3(-61.4f,9.62f,2.60f),
        new Vector3(-62.30f,9.62f,1.75f)
    };

    static void Walk(CharacterController controller, Vector3 goal)
    {
        for (int step=0;step<900;step++)
        {
            var direction=goal-controller.transform.position;
            direction.y=0;
            if(direction.magnitude<.09f) break;
            controller.Move(Vector3.ClampMagnitude(direction,.055f)+Vector3.down*.015f);
        }
        var p=controller.transform.position;
        if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(goal.x,goal.z))>.20f ||
            Mathf.Abs(p.y-goal.y)>.50f || !controller.isGrounded)
            throw new InvalidOperationException("Walk blocked at "+p+" on way to "+goal);
    }

    static string TestBank(int bank, Vector3 authorPivot)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Enemy+"TreeHouse_"+bank+".prefab");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
            // Match LocationSpawner, which instantiates the prefab with an explicit root position and rotation.
            root.transform.SetPositionAndRotation(new Vector3(0,0,1200+bank*350),Quaternion.identity);
            var visual=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="TreeHouse_ProductionVisual");
            if ((bank==1 && visual.position.x>=0) || (bank==2 && visual.position.x<=0))
                throw new InvalidOperationException("Unexpected river side");
            if(root.GetComponentsInChildren<MeshFilter>().Count(f=>f.name=="dom")!=1)
                throw new InvalidOperationException("Expected a single active house model");
            if(visual.GetComponentsInChildren<Transform>().Count(t=>t.name=="Stair_Walk_Surface")!=3)
                throw new InvalidOperationException("Expected three flights");
            Vector3 Map(Vector3 original)=>visual.TransformPoint(original-authorPivot);

            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(ground,scene);
            ground.transform.position=new Vector3(visual.position.x,-.3f,visual.position.z);
            ground.transform.localScale=new Vector3(105,1,105);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
            var probe=new GameObject("Walk probe");
            SceneManager.MoveGameObjectToScene(probe,scene);
            probe.transform.position=Map(new Vector3(-56.716f,.28f,17.42f));
            var controller=probe.AddComponent<CharacterController>();
            controller.height=source.height; controller.radius=source.radius; controller.center=source.center;
            controller.stepOffset=source.stepOffset; controller.skinWidth=source.skinWidth; controller.slopeLimit=source.slopeLimit;
            Physics.SyncTransforms();
            foreach(var waypoint in Route) Walk(controller,Map(waypoint));

            var spawner=root.GetComponent<LocationContentSpawner>();
            var forest=Resources.Load<LocationBalanceProfile>("LocationBalanceProfile")?.GetBalanceForScene("LevelForest");
            var pool=forest?.itemCollectionOverride ? forest.itemCollectionOverride : spawner.itemCollection;
            typeof(LocationContentSpawner).GetMethod("SpawnItems",BindingFlags.NonPublic|BindingFlags.Instance)
                .Invoke(spawner,new object[]{pool});
            var pickups=root.GetComponentsInChildren<PickableItem>();
            if(pickups.Length!=5) throw new InvalidOperationException("Spawned "+pickups.Length+" items instead of five");
            var surfaces=visual.GetComponentsInChildren<Collider>().Where(c=>c.enabled && !c.isTrigger)
                .Concat(new[]{ground.GetComponent<Collider>()}).ToArray();
            foreach(var point in spawner.itemSpawnPoints)
            {
                var p=point.spawnTransform.position;
                if(pickups.Count(item=>(item.transform.position-p).sqrMagnitude<.001f)!=1)
                    throw new InvalidOperationException("No uniquely spawned item at "+p);
                if(!surfaces.Any(c=>c.Raycast(new Ray(p,Vector3.down),out var hit,.5f) && hit.normal.y>.55f))
                    throw new InvalidOperationException("Spawn point has no support "+p);
            }
            if(Vector3.Distance(spawner.itemSpawnPoints[3].spawnTransform.position,Map(new Vector3(-57.05f,6.60f,19.94f)))>.15f ||
               Vector3.Distance(spawner.itemSpawnPoints[4].spawnTransform.position,Map(new Vector3(-63.70f,10.73f,1.35f)))>.15f)
                throw new InvalidOperationException("Crate and table loot points are misplaced");

            var navigation=root.GetComponent<NavMeshSurface>();
            if(!navigation.navMeshData) throw new InvalidOperationException("Missing baked navmesh");
            var data=NavMesh.AddNavMeshData(navigation.navMeshData,root.transform.position,root.transform.rotation);
            try
            {
                int onMesh=spawner.enemySpawnPoints.Count(p=>NavMesh.SamplePosition(p.position,out _,1.25f,NavMesh.AllAreas));
                return asset.name+" | 3 flights and all 5 spawns verified | enemy anchors near NavMesh "+onMesh+"/5";
            }
            finally { data.Remove(); }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode for isolated check");
        var workshop=AssetDatabase.LoadAssetAtPath<GameObject>(Refined);
        if(workshop.GetComponentsInChildren<Transform>().Any(t=>t.name=="TreeHouse_ProductionVisual"))
            throw new InvalidOperationException("Workshop shows two copies; finish its override before verification");
        var authorPivot=workshop.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="dom").transform.position;
        var state=UnityEngine.Random.state;
        try { return TestBank(1,authorPivot)+"\n"+TestBank(2,authorPivot); }
        finally { UnityEngine.Random.state=state; }
    }
}
