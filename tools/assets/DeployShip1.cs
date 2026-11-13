// Run through Unity MCP script-execute: DeployShip1.Apply, then Verify.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DeployShip1
{
    const string Folder = "Assets/Prefabs/Workshop/Ship1/";
    const string Workshop = Folder + "Ship1_Overgrown.prefab";
    const string ArtName = "Ship1_ProductionVisual";
    const string ArtPath = Folder + ArtName + ".prefab";
    const string Enemy = "Assets/Prefabs/Locations/Enemy/";
    const string Evidence = "Library/Ship1ConceptReview/";
    const string Access = "Ship_Bank_Access";
    static readonly Vector3 Origin = new(-72.66f, 0, 5.90f);
    static readonly Vector3[] Route =
    {
        new(-73.85f,0,-40),new(-73.85f,1.39f,-29.45f),
        new(-73.4f,1.45f,-24.4f),new(-73.05f,1.5f,-21.33f),new(-72.75f,1.64f,-19.24f),
        new(-71.95f,1.81f,-17.15f),new(-71.16f,2.01f,-15.03f),new(-71.15f,2.16f,-14.03f),
        new(-71.15f,3.69f,-8.1f),new(-71.15f,4.56f,-3.9f),new(-71.15f,6.91f,.85f),
        new(-71.15f,7.14f,2.62f),new(-74.55f,10.14f,2.62f),new(-77.95f,10.58f,2.62f),
        new(-77.95f,15.52f,16.40f),new(-74.40f,17.96f,16.29f),new(-72.75f,17.61f,16.12f),
        new(-70.75f,17.57f,16.05f)
    };
    static readonly int[] Slots = { 3, 6, 8, 11, 14, 17 };
    static string Prefab(int bank) => Enemy + $"ship_{bank}.prefab";
    static string NavPath(int bank) => Enemy + $"NavMesh-ship_{bank}-Overgrown.asset";
    static void Require(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
    static void Record(Object o) => PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    static Transform Center(GameObject root) => root.transform.Cast<Transform>().Single(t=>t.name.StartsWith("PointCenter"));
    static Vector3 Map(Transform art,Vector3 p) => art.TransformPoint(p-Origin);
    static Collider[] Solids(Transform t) => t.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
    static float Floor(IEnumerable<Collider> cs,Vector3 p,float above=.35f) =>
        cs.Select(c=>c.Raycast(new Ray(p+Vector3.up*above,Vector3.down),out var h,above+1.5f)&&h.normal.y>.5f
            ?h.point.y:float.NegativeInfinity).DefaultIfEmpty(float.NegativeInfinity).Max();

    static GameObject MakeArt()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Workshop),scene);
            PrefabUtility.UnpackPrefabInstance(copy,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var art=new GameObject(ArtName);
            SceneManager.MoveGameObjectToScene(art,scene);
            art.transform.position=Origin;
            Center(copy).Find("ship").SetParent(art.transform,true);
            copy.transform.Find("Overgrown wreck workshop").SetParent(art.transform,true);
            AddFringeCollision(art.transform);
            foreach(var obstacle in art.GetComponentsInChildren<NavMeshObstacle>(true))Object.DestroyImmediate(obstacle);
            foreach(var collider in Solids(art.transform))
            {
                collider.gameObject.layer=6;
                collider.gameObject.tag="Untagged";
            }
            // A broad, solid wedge joins the raised workshop approach to the normal bank.
            var mesh=new Mesh {name="Ship_Bank_Approach"};
            float x=-73.85f, a=-41, b=-28.7f, width=3.2f;
            mesh.vertices=new[]{
                new Vector3(x-width,-.025f,a),new Vector3(x-width,1.39f,b),
                new Vector3(x+width,1.39f,b),new Vector3(x+width,-.025f,a),
                new Vector3(x-width,-.25f,a),new Vector3(x-width,-.25f,b),
                new Vector3(x+width,-.25f,b),new Vector3(x+width,-.25f,a)};
            mesh.vertices=mesh.vertices.Select(v=>v-Origin).ToArray();
            mesh.triangles=new[]{0,1,2,0,2,3,0,4,5,0,5,1,3,2,6,3,6,7,1,5,6,1,6,2,0,3,7,0,7,4};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Folder+mesh.name+".asset");
            var ramp=new GameObject("Grounded shore approach");
            SceneManager.MoveGameObjectToScene(ramp,scene);
            ramp.transform.SetParent(art.transform,false);
            ramp.AddComponent<MeshFilter>().sharedMesh=mesh;
            ramp.AddComponent<MeshRenderer>().sharedMaterial=art.GetComponentsInChildren<Renderer>()
                .First(r=>r.name=="Dry bank approach").sharedMaterial;
            ramp.AddComponent<MeshCollider>().sharedMesh=mesh;ramp.layer=6;
            art.transform.position=Vector3.zero;
            Require(PrefabUtility.SaveAsPrefabAsset(art,ArtPath),"Failed to save shared ship art");
            Require(!AssetDatabase.GetDependencies(ArtPath).Contains(Prefab(1)),"Cyclic ship prefab dependency");
            return AssetDatabase.LoadAssetAtPath<GameObject>(ArtPath);
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
    }

    static void Bake(GameObject root,Transform art,int bank)
    {
        var surface=root.GetComponent<NavMeshSurface>();
        surface.RemoveData();surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize=true;surface.voxelSize=.075f;
        surface.BuildNavMesh();
        var data=surface.navMeshData;surface.RemoveData();
        Require(data&&!AssetDatabase.Contains(data),"Ship navigation bake failed");
        var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath(bank));
        if(existing)
        {
            EditorUtility.CopySerialized(data,existing);EditorUtility.SetDirty(existing);
            surface.navMeshData=existing;
        }
        else AssetDatabase.CreateAsset(data,NavPath(bank));
        Record(surface);
        var offset=new Vector3(5000,0,5000);
        var instance=NavMesh.AddNavMeshData(data,root.transform.position+offset,root.transform.rotation);
        try
        {
            Vector3 Sample(Vector3 p) {
                Require(NavMesh.SamplePosition(p+offset,out var h,1.25f,NavMesh.AllAreas),"Missing navigation near "+p);
                return h.position-offset;
            }
            var start=Sample(Map(art,Route[1]));
            var content=root.GetComponent<LocationContentSpawner>();
            for(int i=0;i<6;i++)
            {
                var p=Sample(Map(art,Route[Slots[i]]));
                var path=new NavMeshPath();
                NavMesh.CalculatePath(start+offset,p+offset,NavMesh.AllAreas,path);
                Require(path.status==NavMeshPathStatus.PathComplete,"Ship enemy route incomplete at "+Slots[i]);
                content.enemySpawnPoints[i].position=p+Vector3.up*.06f;
                Record(content.enemySpawnPoints[i]);
            }
            var link=root.transform.Find(Access)?.GetComponent<NavMeshLink>();
            if(!link)
            {
                var obj=new GameObject(Access);SceneManager.MoveGameObjectToScene(obj,root.scene);
                obj.transform.SetParent(root.transform,false);
                link=obj.AddComponent<NavMeshLink>();
            }
            var outer=Map(art,Route[0]);outer.y=.03f;
            link.startPoint=link.transform.InverseTransformPoint(outer);
            link.endPoint=link.transform.InverseTransformPoint(start);
            link.width=2.4f;link.bidirectional=true;link.agentTypeID=surface.agentTypeID;
        }
        finally {instance.Remove();}
    }

    static void AddFringeCollision(Transform art)
    {
        foreach(var filter in art.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Grass fringe")))
        {
            if(!filter.GetComponent<MeshCollider>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            filter.gameObject.layer=6;
        }
    }

    public static string GroundFringes()
    {
        var art=PrefabUtility.LoadPrefabContents(ArtPath);
        try
        {
            AddFringeCollision(art.transform);
            Require(PrefabUtility.SaveAsPrefabAsset(art,ArtPath),"Could not save fringe collision");
        }
        finally{PrefabUtility.UnloadPrefabContents(art);}
        for(int i=1;i<=2;i++)
        {
            var root=PrefabUtility.LoadPrefabContents(Prefab(i));
            try
            {
                Bake(root,root.transform.Find(ArtName),i);
                Require(PrefabUtility.SaveAsPrefabAsset(root,Prefab(i)),"Could not save fringe navigation");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        return "Fringe surfaces solid on both banks.";
    }

    public static string Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Ship deployment requires Edit Mode");
        Require(!AssetDatabase.LoadAssetAtPath<GameObject>(ArtPath),"Ship art already exists, inspect before repeating deployment");
        Directory.CreateDirectory(Evidence+"BeforeDeploy");
        for(int i=1;i<=2;i++)File.Copy(Prefab(i),Evidence+"BeforeDeploy/ship_"+i+".prefab",false);
        var roots=new GameObject[2];
        try
        {
            var visual=MakeArt();
            for(int bank=1;bank<=2;bank++)
            {
                var root=roots[bank-1]=PrefabUtility.LoadPrefabContents(Prefab(bank));
                var center=Center(root);
                foreach(var r in root.GetComponentsInChildren<Renderer>()){r.enabled=false;Record(r);}
                foreach(var c in Solids(root.transform)){c.enabled=false;Record(c);}
                foreach(var n in root.GetComponentsInChildren<NavMeshObstacle>()){n.enabled=false;Record(n);}
                foreach(var l in root.GetComponentsInChildren<NavMeshLink>()){l.enabled=false;Record(l);}
                var art=((GameObject)PrefabUtility.InstantiatePrefab(visual,root.transform)).transform;
                art.SetPositionAndRotation(new Vector3(center.position.x,0,center.position.z),Quaternion.Euler(0,bank==1?270:90,0));
                Physics.SyncTransforms();
                var sp=root.GetComponent<LocationContentSpawner>();
                Require(sp.itemSpawnPoints.Count==6&&sp.enemySpawnPoints.Count==6,"Ship slot count differs");
                var solids=Solids(art);
                for(int i=0;i<6;i++)
                {
                    var p=Map(art,Route[Slots[i]]);p.y=Floor(solids,p,.55f);
                    Require(float.IsFinite(p.y),"Missing loot support");
                    sp.itemSpawnPoints[i].spawnTransform.SetPositionAndRotation(p+Vector3.up*.22f,art.rotation);
                    Record(sp.itemSpawnPoints[i].spawnTransform);
                }
                var bounds=art.GetComponentsInChildren<Renderer>().First().bounds;
                foreach(var r in art.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                var a=root.transform.Find("Point1");var b=root.transform.Find("Point2");
                a.position=new Vector3(bounds.min.x-1,0,bounds.max.z+1);
                b.position=new Vector3(bounds.max.x+1,0,bounds.min.z-1);Record(a);Record(b);
                var cleaner=root.GetComponent<ZoneCleaner>();
                cleaner.halfExtents=new Vector3(cleaner.halfExtents.x,Mathf.Max(cleaner.halfExtents.y,bounds.max.y+1),cleaner.halfExtents.z);
                Record(cleaner);
                Bake(root,art,bank);
            }
            for(int i=0;i<2;i++)Require(PrefabUtility.SaveAsPrefabAsset(roots[i],Prefab(i+1)),"Ship prefab save failed");
            AssetDatabase.SaveAssets();
            return "Both ship gameplay prefabs saved with six supported loot points and six connected enemy points.";
        }
        finally{foreach(var root in roots)if(root)PrefabUtility.UnloadPrefabContents(root);}
    }

    static int Walk(Transform art,Collider land)
    {
        var go=new GameObject("Ship verification player");
        SceneManager.MoveGameObjectToScene(go,art.gameObject.scene);
        var cc=go.AddComponent<CharacterController>();
        var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
        cc.height=p.height;cc.radius=p.radius;cc.center=p.center;cc.skinWidth=p.skinWidth;
        cc.stepOffset=p.stepOffset;cc.slopeLimit=p.slopeLimit;
        var solids=Solids(art).Append(land).ToArray();
        var start=Map(art,Route[0]);
        start.y=Floor(solids,start,.5f)+.08f;
        go.transform.position=start;Physics.SyncTransforms();
        int count=0;
        foreach(var v in Route.Skip(1).Concat(Route.Reverse().Skip(1)))
        {
            var goal=Map(art,v);goal.y=Floor(solids,goal,.55f);
            Require(float.IsFinite(goal.y),"No floor at route checkpoint "+v);
            int steps=Mathf.CeilToInt(Vector3.Distance(goal,go.transform.position)/.052f)+90;
            float fall=.015f;
            for(int i=0;i<steps;i++)
            {
                var movement=goal-go.transform.position;movement.y=0;
                fall=cc.isGrounded?.015f:Mathf.Min(.14f,fall+.005f);
                cc.Move(Vector3.ClampMagnitude(movement,.052f)+Vector3.down*fall);
            }
            var diff=go.transform.position-goal;
            Require(new Vector2(diff.x,diff.z).magnitude<.36f&&Mathf.Abs(diff.y)<.40f&&cc.isGrounded,
                $"Ship route blocked at {v:F2}, difference {diff:F3}");
            count++;
        }
        return count;
    }

    static int CheckBankRoutes(GameObject root,Transform art,MeshCollider land)
    {
        var nav=root.GetComponent<NavMeshSurface>();
        var settings=nav.GetBuildSettings();
        var data=NavMeshBuilder.BuildNavMeshData(settings,new List<NavMeshBuildSource>{
            new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Mesh,sourceObject=land.sharedMesh,
                transform=land.transform.localToWorldMatrix}},new Bounds(art.position,new Vector3(110,12,125)),
            Vector3.zero,Quaternion.identity);
        var offset=new Vector3(8200,0,8100);
        var bank=NavMesh.AddNavMeshData(data,offset,Quaternion.identity);
        var ship=NavMesh.AddNavMeshData(nav.navMeshData,root.transform.position+offset,root.transform.rotation);
        var link=root.transform.Find(Access).GetComponent<NavMeshLink>();
        var join=NavMesh.AddLink(new NavMeshLinkData {
            startPosition=link.transform.TransformPoint(link.startPoint),endPosition=link.transform.TransformPoint(link.endPoint),
            width=link.width,bidirectional=true,agentTypeID=link.agentTypeID,area=link.area,costModifier=link.costModifier
        },offset,Quaternion.identity);
        try
        {
            var approach=Map(art,new Vector3(-73.85f,0,-44));
            Require(NavMesh.SamplePosition(approach+offset,out var from,.8f,NavMesh.AllAreas),"Missing external bank navigation");
            int count=0;
            foreach(var enemy in root.GetComponent<LocationContentSpawner>().enemySpawnPoints)
            {
                Require(NavMesh.SamplePosition(enemy.position+offset,out var to,.4f,NavMesh.AllAreas),"Missing enemy navmesh");
                foreach(var pair in new[]{(from.position,to.position),(to.position,from.position)})
                {
                    var path=new NavMeshPath();
                    NavMesh.CalculatePath(pair.Item1,pair.Item2,NavMesh.AllAreas,path);
                    Require(path.status==NavMeshPathStatus.PathComplete,"Incomplete shore-to-ship navigation");
                    count++;
                }
            }
            return count;
        }
        finally{join.Remove();ship.Remove();bank.Remove();Object.DestroyImmediate(data);}
    }

    static void Capture(GameObject root,Transform art,string name)
    {
        var sun=new GameObject("Audit sun").AddComponent<Light>();
        SceneManager.MoveGameObjectToScene(sun.gameObject,root.scene);
        sun.type=LightType.Directional;sun.intensity=.65f;sun.shadows=LightShadows.Soft;
        sun.transform.rotation=Quaternion.Euler(42,-28,0);
        var cam=new GameObject("Audit camera").AddComponent<Camera>();
        SceneManager.MoveGameObjectToScene(cam.gameObject,root.scene);
        cam.scene=root.scene;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.47f,.63f,.74f);
        cam.transform.position=Map(art,new Vector3(-105,34,-46));
        cam.transform.LookAt(Map(art,new Vector3(-72,6,0)));
        var rt=RenderTexture.GetTemporary(1200,760,24);var previous=RenderTexture.active;
        var pic=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        try
        {
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            pic.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
            File.WriteAllBytes(Evidence+name,pic.EncodeToPNG());
        }
        finally{cam.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(pic);}
    }

    public static string Verify(string biome)
    {
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/"+biome+".unity");
        var report=new List<string>();
        var random=UnityEngine.Random.state;
        var egg=typeof(EggSpawnRuntimeState).GetField("_spawnedInRun",BindingFlags.Static|BindingFlags.NonPublic);
        var eggState=egg.GetValue(null);
        try
        {
            var roots=source.GetRootGameObjects();
            var spawner=roots.SelectMany(g=>g.GetComponentsInChildren<LocationSpawner>(true)).First();
            var landRoot=roots.SelectMany(g=>g.GetComponentsInChildren<LendController>(true))
                .SelectMany(l=>l.locations).SelectMany(l=>l.groundSegments).First(t=>t&&t.name=="Land");
            var original=landRoot.GetComponentsInChildren<MeshCollider>(true).First(c=>!c.isTrigger&&c.sharedMesh);
            for(int bank=1;bank<=2;bank++)
            {
                var scene=EditorSceneManager.NewPreviewScene();
                try
                {
                    var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(bank)),scene);
                    root.transform.SetPositionAndRotation(new Vector3(spawner.spawnX,spawner.spawnY,0),Quaternion.identity);
                    var art=root.transform.Find(ArtName);
                    Require(art,"Production ship visual missing");
                    var go=new GameObject("Actual bank");SceneManager.MoveGameObjectToScene(go,scene);go.layer=6;
                    go.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);
                    go.transform.localScale=original.transform.lossyScale;
                    var land=go.AddComponent<MeshCollider>();land.sharedMesh=original.sharedMesh;
                    var visible=new GameObject("Actual bank visual");SceneManager.MoveGameObjectToScene(visible,scene);
                    visible.transform.SetPositionAndRotation(landRoot.position,landRoot.rotation);visible.transform.localScale=landRoot.lossyScale;
                    visible.AddComponent<MeshFilter>().sharedMesh=landRoot.GetComponent<MeshFilter>().sharedMesh;
                    visible.AddComponent<MeshRenderer>().sharedMaterials=landRoot.GetComponent<MeshRenderer>().sharedMaterials;
                    Physics.SyncTransforms();
                    int walk=Walk(art,land);
                    var solids=Solids(art);
                    var content=root.GetComponent<LocationContentSpawner>();
                    Require(content.itemSpawnPoints.Count==6&&content.enemySpawnPoints.Count==6,"Lost ship slots");
                    foreach(var slot in content.itemSpawnPoints)
                    {
                        var point=slot.spawnTransform.position;
                        Require(Mathf.Abs(point.y-Floor(solids,point)-.22f)<.035f,"Unsupported loot slot");
                        Require(Route.Any(v=>Vector3.Distance(Map(art,v),point)<1.4f),"Loot too far from walked route");
                    }
                    var reeds=art.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Reed blade").ToArray();
                    var gaps=reeds.SelectMany(f=>f.sharedMesh.vertices.Take(2).Select(f.transform.TransformPoint))
                        .Select(p=>p.y-Floor(solids.Append(land),p)).ToArray();
                    Require(gaps.All(v=>v<.025f&&v>-.07f),"Reeds not grounded: "+gaps.Min()+".."+gaps.Max());
                    var profile=Resources.Load<LocationBalanceProfile>("LocationBalanceProfile");
                    var loot=profile.GetBalanceForScene(biome).itemCollectionOverride;
                    UnityEngine.Random.InitState(232+bank);
                    typeof(LocationContentSpawner).GetMethod("SpawnItems",BindingFlags.Instance|BindingFlags.NonPublic)
                        .Invoke(content,new object[]{loot?loot:content.itemCollection});
                    var spawned=root.GetComponentsInChildren<PickableItem>();
                    Require(spawned.Length==6&&content.itemSpawnPoints.All(s=>spawned.Any(p=>
                        Vector3.Distance(p.transform.position,s.spawnTransform.position)<.02f)),"Native loot did not fill 6/6 slots");
                    int paths=CheckBankRoutes(root,art,land);
                    float riverGap=art.GetComponentsInChildren<Renderer>().Where(r=>!r.GetComponentInParent<PickableItem>())
                        .Min(r=>bank==1?-r.bounds.max.x:r.bounds.min.x);
                    Require(riverGap>10,"Ship extends too close to river center");
                    if(biome=="LevelDesert")Capture(root,art,"game-bank-"+bank+".png");
                    report.Add($"{biome}/{root.name}: {walk} walk checkpoints round trip; native loot 6/6, all reachable; {paths} complete bank routes; reed feet {gaps.Min():F3}..{gaps.Max():F3} m; river gap {riverGap:F1} m.");
                }
                finally{EditorSceneManager.ClosePreviewScene(scene);}
            }
        }
        finally{egg.SetValue(null,eggState);UnityEngine.Random.state=random;EditorSceneManager.ClosePreviewScene(source);}
        File.WriteAllLines(Evidence+"game-check-"+biome+".txt",report);
        return string.Join(Environment.NewLine,report);
    }
}
