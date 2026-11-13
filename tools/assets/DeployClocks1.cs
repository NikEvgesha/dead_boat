// Unity MCP script-execute: DeployClocks1.Apply, then Verify with the biome argument.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class DeployClocks1
{
    const string Folder="Assets/Prefabs/Workshop/Clocks1/";
    const string Visual="Clocks1_ProductionVisual";
    const string Workshop=Folder+"Clocks1_OvergrownCourt.prefab";
    const string ArtPath=Folder+Visual+".prefab";
    const string Output="Library/Clocks1ConceptReview/";
    const string Access="Clocks_Bank_Access";
    const float Raise=.03f, Top=4.08f;
    static string Prefab(int bank)=>$"Assets/Prefabs/Locations/Enemy/clocks_{bank}.prefab";
    static string NavPath(int bank)=>$"Assets/Prefabs/Locations/Enemy/NavMesh-clocks_{bank}-Overgrown.asset";
    static readonly Vector3[] EnemyPoints={new(1,0,-7),new(7,0,-3),new(-3,0,6),new(-2.8f,Top,1.8f),new(2.8f,Top,2.8f)};
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static void Record(Object o)=>PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    static Transform Center(GameObject g)=>g.transform.Cast<Transform>().Single(t=>t.name.StartsWith("PointCenter"));
    static Vector3 Map(Transform art,Vector3 p)=>art.TransformPoint(p+Vector3.up*Raise);
    static Collider[] Solids(Transform t)=>t.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
    static float Floor(IEnumerable<Collider> colliders,Vector3 p,float above=.35f)=>
        colliders.Select(c=>c.Raycast(new Ray(p+Vector3.up*above,Vector3.down),out var h,above+1.5f)&&h.normal.y>.6f
            ?h.point.y:float.NegativeInfinity).DefaultIfEmpty(float.NegativeInfinity).Max();
    static Vector3[] Route()
    {
        var ground=new[]{new Vector3(0,0,-20),new Vector3(0,0,-12),new Vector3(2.7f,0,-5.9f),
            new Vector3(0,0,0),new Vector3(0,0,5.5f),new Vector3(5.5f,0,5.5f),new Vector3(5.5f,0,0),
            new Vector3(0,0,0),new Vector3(5.5f,0,0),new Vector3(6.3f,0,-4),
            new Vector3(4.5f,0,-7),new Vector3(-2.4f,0,-10.9f),new Vector3(-5.55f,0,-10.4f)};
        var stair=Enumerable.Range(0,24).Select(i=>new Vector3(-5.55f,(i+1)*.17f,-9.7f+(i+.5f)*.48f));
        var terrace=new[]{new Vector3(-5.55f,Top,2.35f),new Vector3(-4.5f,Top,2.35f),
            new Vector3(-2.8f,Top,1.8f),new Vector3(-2.8f,Top,2.8f),new Vector3(0,Top,2.8f),
            new Vector3(2.85f,Top,2.8f),new Vector3(2.85f,Top,0),new Vector3(2.85f,Top,-2.8f),
            new Vector3(0,Top,-2.8f),new Vector3(-2.85f,Top,-2.8f)};
        var route=ground.Concat(stair).Concat(terrace).ToArray();
        return route.Concat(route.Reverse().Skip(1)).ToArray();
    }

    static GameObject CreateArt()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Workshop),scene);
            PrefabUtility.UnpackPrefabInstance(art,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            art.name=Visual;
            foreach(Transform child in art.transform)
            {
                if(child.name=="Workshop_Sand"){child.gameObject.SetActive(false);continue;}
                child.position+=Vector3.up*Raise;
            }
            art.transform.Find("Workshop_Walk_Anchors").gameObject.SetActive(false);
            var mesh=new Mesh{name="Clocks_Grounded_Apron"};
            var vertices=new List<Vector3>();var indices=new List<int>();
            var inner=new[]{new Vector2(-15,-12.8f),new Vector2(15,-12.8f),new Vector2(16,9),
                new Vector2(12,12.8f),new Vector2(-12,12.8f),new Vector2(-16,9)};
            for(int i=0;i<inner.Length;i++)
            {
                int j=(i+1)%inner.Length;
                Vector3 P(Vector2 p,float y)=>new(p.x,y,p.y);
                var a=P(inner[i],Raise);var b=P(inner[j],Raise);
                var c=P(inner[j]*1.18f,-.20f);var d=P(inner[i]*1.18f,-.20f);
                int first=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            int start=vertices.Count;vertices.AddRange(inner.Select(p=>new Vector3(p.x,Raise,p.y)));
            for(int i=1;i<inner.Length-1;i++)indices.AddRange(new[]{start,start+i+1,start+i});
            mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
            // The ring is ordered clockwise when viewed from above.
            var triangles=mesh.triangles;
            for(int i=0;i<inner.Length*6;i+=3)
            {
                if(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).y<0)
                {int t=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=t;}
            }
            mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var previous=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+mesh.name+".asset");
            if(previous){EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);mesh=previous;}
            else AssetDatabase.CreateAsset(mesh,Folder+mesh.name+".asset");
            var ground=new GameObject("Grounded_Sand_Apron");SceneManager.MoveGameObjectToScene(ground,scene);
            ground.transform.SetParent(art.transform,false);
            ground.AddComponent<MeshFilter>().sharedMesh=mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Court_Sand.mat");
            ground.AddComponent<MeshCollider>().sharedMesh=mesh;
            foreach(var c in Solids(art.transform)){c.gameObject.layer=6;c.gameObject.tag="Untagged";}
            Check(PrefabUtility.SaveAsPrefabAsset(art,ArtPath),"Cannot save Clocks production visual");
            return AssetDatabase.LoadAssetAtPath<GameObject>(ArtPath);
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    static void Bake(GameObject root,Transform art,int bank)
    {
        var nav=root.GetComponent<NavMeshSurface>()??root.AddComponent<NavMeshSurface>();
        nav.collectObjects=CollectObjects.Children;nav.layerMask=1<<6;
        nav.RemoveData();nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        nav.overrideVoxelSize=true;nav.voxelSize=.075f;nav.BuildNavMesh();
        var data=nav.navMeshData;nav.RemoveData();
        Check(data&&!AssetDatabase.Contains(data),"Navigation bake failed");
        AssetDatabase.CreateAsset(data,NavPath(bank));Record(nav);
        var offset=new Vector3(5100,0,5300);
        var instance=NavMesh.AddNavMeshData(data,root.transform.position+offset,root.transform.rotation);
        try
        {
            Vector3 Sample(Vector3 p)
            {
                Check(NavMesh.SamplePosition(p+offset,out var h,.85f,NavMesh.AllAreas),"No Clocks navigation at "+p);
                return h.position-offset;
            }
            var entry=Sample(Map(art,new Vector3(0,0,-11)));
            var content=root.GetComponent<LocationContentSpawner>();
            for(int i=0;i<5;i++)
            {
                var p=Sample(Map(art,EnemyPoints[i]));
                var path=new NavMeshPath();NavMesh.CalculatePath(entry+offset,p+offset,NavMesh.AllAreas,path);
                Check(path.status==NavMeshPathStatus.PathComplete,"Incomplete route to enemy "+i);
                content.enemySpawnPoints[i].position=p+Vector3.up*.06f;Record(content.enemySpawnPoints[i]);
            }
            var linkObject=new GameObject(Access);SceneManager.MoveGameObjectToScene(linkObject,root.scene);
            linkObject.transform.SetParent(root.transform,false);
            var link=linkObject.AddComponent<NavMeshLink>();
            var shore=Map(art,new Vector3(0,0,-18));shore.y=.03f;
            link.startPoint=linkObject.transform.InverseTransformPoint(shore);
            link.endPoint=linkObject.transform.InverseTransformPoint(entry);
            link.width=2.4f;link.bidirectional=true;link.agentTypeID=nav.agentTypeID;
        }
        finally{instance.Remove();}
    }

    public static string Apply()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Use Edit Mode");
        Check(!AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(1)).transform.Find(Visual),"Deployment already exists. Inspect before repeating.");
        Directory.CreateDirectory(Output+"BeforeDeploy");
        for(int bank=1;bank<=2;bank++)if(!File.Exists(Output+"BeforeDeploy/clocks_"+bank+".prefab"))File.Copy(Prefab(bank),Output+"BeforeDeploy/clocks_"+bank+".prefab",false);
        var roots=new GameObject[2];
        try
        {
            var visual=CreateArt();
            for(int bank=1;bank<=2;bank++)
            {
                var root=roots[bank-1]=PrefabUtility.LoadPrefabContents(Prefab(bank));var center=Center(root);
                foreach(var r in root.GetComponentsInChildren<Renderer>()){r.enabled=false;Record(r);}
                foreach(var c in Solids(root.transform)){c.enabled=false;Record(c);}
                foreach(var n in root.GetComponentsInChildren<NavMeshObstacle>()){n.enabled=false;Record(n);}
                foreach(var l in root.GetComponentsInChildren<NavMeshLink>()){l.enabled=false;Record(l);}
                var art=((GameObject)PrefabUtility.InstantiatePrefab(visual,root.transform)).transform;
                art.SetPositionAndRotation(new Vector3(center.position.x,0,center.position.z),Quaternion.Euler(0,bank==1?270:90,0));
                Physics.SyncTransforms();
                var content=root.GetComponent<LocationContentSpawner>();
                Check(content.itemSpawnPoints.Count==5&&content.enemySpawnPoints.Count==5,"Unexpected Clocks slot count");
                var solids=Solids(art);
                for(int i=0;i<5;i++)
                {
                    var p=art.Find($"Workshop_Walk_Anchors/Item{i+1:00}").position;
                    if(i==0)p=Map(art,new Vector3(2.7f,0,-5.9f));
                    if(i==2)p=Map(art,new Vector3(-2.4f,0,-11.4f));
                    if(i>=3)p=Map(art,new Vector3(i==3?-2.8f:2.8f,Top,2.6f));
                    p.y=Floor(solids,p);
                    Check(float.IsFinite(p.y),"Missing support for loot");
                    content.itemSpawnPoints[i].alignVisualBase=true;
                    content.itemSpawnPoints[i].maxVisualFootprint=i>=3?1f:1.5f;
                    content.itemSpawnPoints[i].spawnTransform.SetPositionAndRotation(p+Vector3.up*.008f,art.rotation);
                    Record(content.itemSpawnPoints[i].spawnTransform);
                }
                var bounds=art.GetComponentsInChildren<Renderer>().First().bounds;
                foreach(var r in art.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                var a=root.transform.Find("Point1");var b=root.transform.Find("Point2");
                a.position=new Vector3(bounds.min.x-1,0,bounds.max.z+1);b.position=new Vector3(bounds.max.x+1,0,bounds.min.z-1);
                Record(a);Record(b);
                var cleaner=root.GetComponent<ZoneCleaner>();
                cleaner.halfExtents=new Vector3(cleaner.halfExtents.x,Mathf.Max(cleaner.halfExtents.y,bounds.max.y+1),cleaner.halfExtents.z);
                Record(cleaner);Bake(root,art,bank);
            }
            for(int i=0;i<2;i++)Check(PrefabUtility.SaveAsPrefabAsset(roots[i],Prefab(i+1)),"Could not save Clocks bank");
            AssetDatabase.SaveAssets();return "Both Clocks variants saved with straight stairs and five supported loot slots.";
        }
        finally{foreach(var root in roots)if(root)PrefabUtility.UnloadPrefabContents(root);}
    }

    static int Walk(Transform art,Collider bank)
    {
        var go=new GameObject("Clocks test player");SceneManager.MoveGameObjectToScene(go,art.gameObject.scene);
        var cc=go.AddComponent<CharacterController>();
        var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
        cc.height=reference.height;cc.radius=reference.radius;cc.center=reference.center;
        cc.stepOffset=reference.stepOffset;cc.skinWidth=reference.skinWidth;cc.slopeLimit=reference.slopeLimit;
        var solids=Solids(art).Append(bank).ToArray();
        var route=Route();var start=Map(art,route[0]);start.y=Floor(solids,start)+.08f;
        go.transform.position=start;Physics.SyncTransforms();int count=0;
        foreach(var local in route.Skip(1))
        {
            var goal=Map(art,local);goal.y=Floor(solids,goal,.5f);
            Check(float.IsFinite(goal.y),"No route floor");
            int n=Mathf.CeilToInt(Vector3.Distance(goal,go.transform.position)/.05f)+60;
            float fall=.015f;
            for(int i=0;i<n;i++)
            {
                var delta=goal-go.transform.position;delta.y=0;
                fall=cc.isGrounded?.015f:Mathf.Min(.14f,fall+.005f);
                cc.Move(Vector3.ClampMagnitude(delta,.05f)+Vector3.down*fall);
            }
            var diff=go.transform.position-goal;
            Check(new Vector2(diff.x,diff.z).magnitude<.33f&&Mathf.Abs(diff.y)<.35f&&cc.isGrounded,
                $"Clocks walk blocked at {local:F2}, difference {diff:F3}");
            count++;
        }
        return count;
    }

    static int BankRoutes(GameObject root,Transform art,MeshCollider ground)
    {
        var nav=root.GetComponent<NavMeshSurface>();
        var data=NavMeshBuilder.BuildNavMeshData(nav.GetBuildSettings(),new List<NavMeshBuildSource>{
            new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=ground.sharedMesh,transform=ground.transform.localToWorldMatrix}},
            new Bounds(art.position,new Vector3(70,15,75)),Vector3.zero,Quaternion.identity);
        var offset=new Vector3(8200,0,8100);
        var bank=NavMesh.AddNavMeshData(data,offset,Quaternion.identity);
        var location=NavMesh.AddNavMeshData(nav.navMeshData,root.transform.position+offset,root.transform.rotation);
        var link=root.transform.Find(Access).GetComponent<NavMeshLink>();
        var join=NavMesh.AddLink(new NavMeshLinkData{startPosition=link.transform.TransformPoint(link.startPoint),
            endPosition=link.transform.TransformPoint(link.endPoint),width=link.width,bidirectional=true,
            agentTypeID=link.agentTypeID,area=link.area,costModifier=link.costModifier},offset,Quaternion.identity);
        try
        {
            var approach=Map(art,new Vector3(0,0,-20));approach.y=0;
            Check(NavMesh.SamplePosition(approach+offset,out var from,.8f,NavMesh.AllAreas),"Missing shore navmesh");
            int count=0;
            foreach(var e in root.GetComponent<LocationContentSpawner>().enemySpawnPoints)
            {
                Check(NavMesh.SamplePosition(e.position+offset,out var to,.4f,NavMesh.AllAreas),"Missing enemy navmesh");
                foreach(var pair in new[]{(from.position,to.position),(to.position,from.position)})
                {
                    var path=new NavMeshPath();NavMesh.CalculatePath(pair.Item1,pair.Item2,NavMesh.AllAreas,path);
                    Check(path.status==NavMeshPathStatus.PathComplete,"Incomplete bank route");count++;
                }
            }
            return count;
        }
        finally{join.Remove();location.Remove();bank.Remove();Object.DestroyImmediate(data);}
    }

    public static string Verify(string biome)
    {
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/"+biome+".unity");
        var random=UnityEngine.Random.state;
        var egg=typeof(EggSpawnRuntimeState).GetField("_spawnedInRun",BindingFlags.Static|BindingFlags.NonPublic);
        var savedEgg=egg.GetValue(null);var report=new List<string>();
        try
        {
            var roots=source.GetRootGameObjects();
            var spawner=roots.SelectMany(g=>g.GetComponentsInChildren<LocationSpawner>(true)).First();
            var landRoot=roots.SelectMany(g=>g.GetComponentsInChildren<LendController>(true)).SelectMany(l=>l.locations)
                .SelectMany(l=>l.groundSegments).First(t=>t&&t.name=="Land");
            var landSource=landRoot.GetComponentsInChildren<MeshCollider>(true).First(c=>c.sharedMesh&&!c.isTrigger);
            for(int bank=1;bank<=2;bank++)
            {
                var scene=EditorSceneManager.NewPreviewScene();
                try
                {
                    var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(bank)),scene);
                    root.transform.SetPositionAndRotation(new Vector3(spawner.spawnX,spawner.spawnY,0),Quaternion.identity);
                    var art=root.transform.Find(Visual);Check(art,"New Clocks visual missing");
                    var terrain=new GameObject("Actual gameplay bank");SceneManager.MoveGameObjectToScene(terrain,scene);
                    terrain.layer=6;terrain.transform.SetPositionAndRotation(landSource.transform.position,landSource.transform.rotation);
                    terrain.transform.localScale=landSource.transform.lossyScale;
                    var land=terrain.AddComponent<MeshCollider>();land.sharedMesh=landSource.sharedMesh;
                    Physics.SyncTransforms();int walk=Walk(art,land);
                    var solids=Solids(art);
                    var content=root.GetComponent<LocationContentSpawner>();
                    foreach(var point in content.itemSpawnPoints)
                    {
                        var p=point.spawnTransform.position;
                        Check(point.alignVisualBase&&Mathf.Abs(p.y-Floor(solids,p)-.008f)<.025f,"Unsupported Clocks loot");
                        Check(Route().Any(v=>Vector3.Distance(Map(art,v),p)<1.6f),"Loot unreachable from route");
                    }
                    var profile=Resources.Load<LocationBalanceProfile>("LocationBalanceProfile");
                    var loot=profile.GetBalanceForScene(biome).itemCollectionOverride;
                    UnityEngine.Random.InitState(235+bank);
                    typeof(LocationContentSpawner).GetMethod("SpawnItems",BindingFlags.Instance|BindingFlags.NonPublic)
                        .Invoke(content,new object[]{loot?loot:content.itemCollection});
                    var spawned=root.GetComponentsInChildren<PickableItem>();
                    Check(spawned.Length==5&&content.itemSpawnPoints.All(p=>spawned.Any(i=>Vector2.Distance(new Vector2(i.transform.position.x,i.transform.position.z),new Vector2(p.spawnTransform.position.x,p.spawnTransform.position.z))<.02f)),
                        "Native loot did not fill five slots");
                    var environment=art.GetComponentsInChildren<Renderer>();
                    foreach(var item in spawned)
                    {
                        var rendered=item.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                        var bottom=rendered.Min(r=>r.bounds.min.y);
                        Check(Mathf.Abs(bottom-Floor(solids,new Vector3(item.transform.position.x,bottom,item.transform.position.z)))<.025f,"Native item base unsupported "+item.name+" "+item.transform.position+" base "+bottom);
                        Check(!rendered.Any(r=>environment.Any(other=>r.bounds.Intersects(other.bounds))),
                            "Native pickup intersects the environment: "+item.name+" at "+item.transform.position+" with "+string.Join(",",environment.Where(other=>rendered.Any(r=>r.bounds.Intersects(other.bounds))).Select(r=>r.name+"/"+r.transform.parent.name+" "+r.bounds)));
                    }
                    var plants=art.Find("Grounded_Plants_And_Rocks").Cast<Transform>()
                        .Where(t=>t.name.Contains("Oak")||t.name.Contains("Bush")||t.name.StartsWith("Purple"));
                    foreach(var plant in plants)
                    {
                        var vertices=plant.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh)
                            .SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
                        float low=vertices.Min(v=>v.y);
                        Check(vertices.Where(v=>v.y<low+.02f).All(v=>v.y-Floor(solids.Append(land),v)<.012f
                            &&v.y-Floor(solids.Append(land),v)>-.045f),"Plant is not grounded");
                    }
                    int paths=BankRoutes(root,art,land);
                    report.Add($"{biome}/{root.name}: {walk} walk checkpoints, straight 24 x .17 m stair both ways, native loot 5/5 without renderer overlaps, {paths} complete bank routes, grounded plants.");
                }
                finally{EditorSceneManager.ClosePreviewScene(scene);}
            }
        }
        finally{UnityEngine.Random.state=random;egg.SetValue(null,savedEgg);EditorSceneManager.ClosePreviewScene(source);}
        File.WriteAllLines(Output+"game-check-"+biome+".txt",report);
        return string.Join(Environment.NewLine,report);
    }
}
