// Unity MCP script-execute: DeployPond1.Apply, then DeployPond1.Verify.
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

public static class DeployPond1
{
    const string Workshop = "Assets/Prefabs/Workshop/Pond1/Pond1_ExpandedOasis.prefab";
    const string EnemyDir = "Assets/Prefabs/Locations/Enemy/";
    const string ArtDir = "Assets/Prefabs/Workshop/Pond1/";
    const string ArtName = "Pond1_Riverbank_Oasis";
    const string BankLinks = "Oasis_Bank_Access";
    const string ArtPath = ArtDir + ArtName + ".prefab";
    const string Evidence = "Library/Pond1ConceptReview/";
    const float Raise = 1.33f;
    const float WaterY = -.22f;
    const int GroundLayer = 6;
    static readonly Vector2[] EnemyPoints =
    {
        new(-22,-11), new(-24,8), new(-15,19), new(19,11), new(25,-8)
    };
    static readonly Vector2[] DryRoute =
    {
        new(5,-30),new(5,-17),new(-10,-18.3f),new(-15,-16.6f),new(-22,-11),
        new(-24.2f,3.2f),new(-24,12),new(-19,17.5f),new(-10,19.2f),new(-1,17),
        new(12,16),new(23,12),new(24.9f,2.1f),new(25,-8),new(18,-16),new(5,-17)
    };

    static string PathFor(int bank) => EnemyDir + $"pond_{bank}.prefab";
    static string NavPath(int bank) => EnemyDir + $"NavMesh-pond_{bank}-ExpandedOasis.asset";
    static Transform Center(GameObject root) => root.transform.Find("PointCenter (1)");
    static void Record(Object obj) => PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
    static void Require(bool ok,string failure) { if(!ok) throw new InvalidOperationException(failure); }
    static Vector3 World(Transform t,Vector2 p) => t.TransformPoint(new Vector3(p.x,0,p.y));

    static float Floor(Collider c, Vector3 point)
    {
        Require(c.Raycast(new Ray(new Vector3(point.x,point.y+12,point.z),Vector3.down),out var hit,24),
            "Pond ground missing near "+point.ToString("F3"));
        return hit.point.y;
    }

    static Vector3[] VisibleBase(Transform prop)
    {
        var vertices=prop.GetComponentsInChildren<MeshFilter>()
            .Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
        float y=vertices.Min(v=>v.y);
        return vertices.Where(v=>v.y-y<.025f).ToArray();
    }

    static void GroundProp(Transform prop,Collider terrain)
    {
        float high=VisibleBase(prop).Max(v=>v.y-Floor(terrain,v));
        prop.position+=Vector3.down*(high+.013f);
        Record(prop);
    }

    static GameObject MakeArt()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Workshop);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var art=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
            PrefabUtility.UnpackPrefabInstance(art,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            art.name=ArtName;
            var terrain=art.transform.Find("Pond_Basin_And_Shore");
            var filter=terrain.GetComponent<MeshFilter>();
            var col=terrain.GetComponent<MeshCollider>();
            var water=art.transform.Find("Pond_Water").GetComponent<MeshFilter>().sharedMesh;
            var mesh=Object.Instantiate(filter.sharedMesh);
            mesh.name="Pond_Riverbank_Basin";
            var vertices=mesh.vertices;
            int rim=(water.vertexCount-1)/3;
            Require(vertices.Length==6*rim+1 && rim>=8,"Unexpected workshop ring topology");
            // Land is continuous at y=0. Keep the whole bowl above it, and let only the
            // outside edge run beneath the bank for a clean, walkable join.
            for(int i=0;i<rim;i++) vertices[i]=new Vector3(vertices[i].x*.90f,-Raise-.065f,vertices[i].z*.89f);
            mesh.vertices=vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,ArtDir+mesh.name+".asset");
            filter.sharedMesh=mesh;
            col.sharedMesh=mesh;
            terrain.gameObject.layer=GroundLayer;
            Physics.SyncTransforms();
            foreach(Transform prop in art.transform.Find("Oasis_Decoration"))
            {
                // The rear palm goes on the flatter shoulder of the blended bank.
                if(prop.name=="Palm_2") prop.localPosition+=Vector3.back*3.05f;
                GroundProp(prop,col);
                foreach(var solid in prop.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                {
                    solid.gameObject.layer=GroundLayer;
                    solid.gameObject.tag="Untagged";
                    Record(solid.gameObject);
                }
            }
            art.transform.Find("Workshop_Walk_Anchors").gameObject.SetActive(false);
            Require(PrefabUtility.SaveAsPrefabAsset(art,ArtPath),"Could not save riverbank art");
            return AssetDatabase.LoadAssetAtPath<GameObject>(ArtPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void RetireLegacy(GameObject root)
    {
        void Disable(GameObject go) {go.SetActive(false);Record(go);}
        foreach(Transform t in Center(root))
            if(t.name=="hole"||t.name=="PolyShape"||t.name.StartsWith("palm_",StringComparison.Ordinal))
                Disable(t.gameObject);
        foreach(var link in root.GetComponentsInChildren<NavMeshLink>(true))
            if(link.transform.parent.name!=BankLinks) Disable(link.gameObject);
    }

    static void SetCoverage(GameObject root,Transform art)
    {
        var cleaner=root.GetComponent<ZoneCleaner>();
        var start=root.transform.Find("Point1");
        var end=root.transform.Find("Point2");
        var b=art.Find("Pond_Basin_And_Shore").GetComponent<Renderer>().bounds;
        foreach(var r in art.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        var minimum=b.min-Vector3.one;
        var maximum=b.max+Vector3.one;
        Vector3 Corner(Vector3 a,Vector3 other)=>new(
            a.x<other.x ? minimum.x : maximum.x, a.y, a.z<other.z ? minimum.z : maximum.z);
        var p=start.position;var q=end.position;
        start.position=Corner(p,q);end.position=Corner(q,p);
        Record(start);Record(end);
        var half=cleaner.halfExtents;
        half.y=Mathf.Max(half.y,maximum.y);
        cleaner.halfExtents=half;
        Record(cleaner);

        var box=Center(root).GetComponent<BoxCollider>();
        var water=art.Find("Pond_Water").GetComponent<MeshFilter>();
        var pts=water.sharedMesh.vertices.Select(v=>box.transform.InverseTransformPoint(water.transform.TransformPoint(v))).ToArray();
        float minX=pts.Min(v=>v.x),maxX=pts.Max(v=>v.x),minZ=pts.Min(v=>v.z),maxZ=pts.Max(v=>v.z);
        box.center=new Vector3((minX+maxX)*.5f,box.center.y,(minZ+maxZ)*.5f);
        box.size=new Vector3(maxX-minX+3,box.size.y,maxZ-minZ+3);
        Record(box);
    }

    static void Navigation(GameObject root,Transform art,MeshCollider ground,int bank)
    {
        var nav=root.GetComponent<NavMeshSurface>();
        nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        nav.RemoveData();
        nav.BuildNavMesh();
        var data=nav.navMeshData;
        nav.RemoveData();
        Require(data && !AssetDatabase.Contains(data),root.name+" has no freshly baked NavMesh");
        var delta=new Vector3(4200,0,4600);
        var handle=NavMesh.AddNavMeshData(data,root.transform.position+delta,root.transform.rotation);
        Require(handle.valid,"Invalid navigation bake for "+root.name);
        try
        {
            Vector3 Sample(Vector2 p)
            {
                var v=World(art,p);v.y=Floor(ground,v);
                Require(NavMesh.SamplePosition(v+delta,out var hit,1.1f,NavMesh.AllAreas),
                    "NavMesh missing at "+p.ToString("F2")+" on "+root.name);
                return hit.position;
            }
            var start=Sample(new Vector2(5,-17));
            var sp=root.GetComponent<LocationContentSpawner>();
            for(int i=0;i<EnemyPoints.Length;i++)
            {
                var point=Sample(EnemyPoints[i]);
                var route=new NavMeshPath();
                NavMesh.CalculatePath(start,point,NavMesh.AllAreas,route);
                Require(route.status==NavMeshPathStatus.PathComplete,"Enemy path incomplete on "+root.name);
                sp.enemySpawnPoints[i].position=point-delta+Vector3.up*.07f;
                Record(sp.enemySpawnPoints[i]);
            }
        }
        finally { handle.Remove(); }
        AssetDatabase.CreateAsset(data,NavPath(bank));
        Record(nav);
    }

    static void ConnectBank(GameObject root,Transform art,MeshCollider ground)
    {
        var nav=root.GetComponent<NavMeshSurface>();
        var offset=new Vector3(7600,0,7700);
        var handle=NavMesh.AddNavMeshData(nav.navMeshData,root.transform.position+offset,root.transform.rotation);
        Require(handle.valid,"Cannot load pond mesh for shore links");
        try
        {
            var group=root.transform.Find(BankLinks);
            if(!group)
            {
                var g=new GameObject(BankLinks);SceneManager.MoveGameObjectToScene(g,root.scene);
                group=g.transform;group.SetParent(root.transform,false);
            }
            var entries=new[]{(new Vector2(5,-28.5f),new Vector2(5,-23.1f)),
                (new Vector2(-8,-28f),new Vector2(-8,-22.2f))};
            for(int i=0;i<entries.Length;i++)
            {
                var a=World(art,entries[i].Item1);a.y=.04f;
                var b=World(art,entries[i].Item2);b.y=Floor(ground,b);
                Require(NavMesh.SamplePosition(b+offset,out var hit,1.1f,NavMesh.AllAreas),"Missing inland link endpoint");
                var name="Pond bank crossing "+(i+1);
                var t=group.Find(name);
                if(!t)
                {
                    var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,root.scene);
                    t=go.transform;t.SetParent(group,false);go.AddComponent<NavMeshLink>();
                }
                var link=t.GetComponent<NavMeshLink>();
                link.agentTypeID=nav.agentTypeID;
                link.startPoint=t.InverseTransformPoint(a);
                link.endPoint=t.InverseTransformPoint(hit.position-offset);
                link.width=2f;
                link.bidirectional=true;
            }
        }
        finally { handle.Remove(); }
    }

    public static string Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Open Unity in Edit Mode to deploy Pond 1");
        Require(!AssetDatabase.LoadAssetAtPath<GameObject>(ArtPath) &&
            !Enumerable.Range(1,2).Any(i=>AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath(i))),
            "Deployment assets already exist; inspect the current prefab state before applying again.");
        var roots=new GameObject[2];
        var notes=new List<string>();
        Directory.CreateDirectory(Evidence+"BeforeDeploy");
        for(int i=1;i<=2;i++)
        {
            var backup=Evidence+"BeforeDeploy/"+Path.GetFileName(PathFor(i));
            if(!File.Exists(backup)) File.Copy(PathFor(i),backup);
        }
        try
        {
            var prefab=MakeArt();
            for(int i=1;i<=2;i++)
            {
                var root=roots[i-1]=PrefabUtility.LoadPrefabContents(PathFor(i));
                var center=Center(root);
                var art=((GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform)).transform;
                // Face the gentle beach toward the river on either bank; yaw rather
                // than negative scale keeps mesh winding and navigation consistent.
                art.SetPositionAndRotation(new Vector3(center.position.x,Raise,center.position.z),
                    Quaternion.Euler(0,i==1?270:90,0));
                RetireLegacy(root);
                SetCoverage(root,art);
                Physics.SyncTransforms();
                var ground=art.Find("Pond_Basin_And_Shore").GetComponent<MeshCollider>();
                var sp=root.GetComponent<LocationContentSpawner>();
                Require(sp.itemCollection && sp.itemSpawnPoints.Count==5 && sp.enemySpawnPoints.Count==5,
                    "Existing spawn configuration differs for "+root.name);
                for(int j=0;j<5;j++)
                {
                    var marker=sp.itemSpawnPoints[j].spawnTransform;
                    var p=art.Find($"Workshop_Walk_Anchors/Item{j+1:00}").position;
                    p.y=Floor(ground,p)+.34f;
                    marker.position=p;
                    Record(marker);
                }
                Navigation(root,art,ground,i);
                ConnectBank(root,art,ground);
                notes.Add($"{root.name}: shallow bank toward river, 5 retained loot slots and 5 connected enemy positions");
            }
            for(int i=1;i<=2;i++)
                Require(PrefabUtility.SaveAsPrefabAsset(roots[i-1],PathFor(i)),"Could not update "+PathFor(i));
            AssetDatabase.SaveAssets();
            return string.Join("\n",notes);
        }
        finally {foreach(var root in roots) if(root) PrefabUtility.UnloadPrefabContents(root);}
    }

    static bool Hit(Collider c,Vector3 p,out RaycastHit hit) =>
        c.Raycast(new Ray(new Vector3(p.x,20,p.z),Vector3.down),out hit,27);

    static int Walk(GameObject root,Transform art,Collider terrain,Collider land)
    {
        var go=new GameObject("Verification player");
        SceneManager.MoveGameObjectToScene(go,root.scene);
        var c=go.AddComponent<CharacterController>();
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
        c.radius=player.radius;c.height=player.height;c.center=player.center;c.skinWidth=player.skinWidth;
        c.stepOffset=player.stepOffset;c.slopeLimit=player.slopeLimit;
        float Height(Vector3 p) => Mathf.Max(Hit(terrain,p,out var t)?t.point.y:float.NegativeInfinity,
            Hit(land,p,out var l)?l.point.y:float.NegativeInfinity);
        var water=art.Find("Pond_Water").GetComponent<MeshFilter>().sharedMesh;
        var edge=water.vertices.Take((water.vertexCount-1)/3).ToArray();
        bool InWater(Vector3 p)
        {
            var a=art.InverseTransformPoint(p);
            bool inside=false;
            for(int i=0,j=edge.Length-1;i<edge.Length;j=i++)
                if((edge[i].z>a.z)!=(edge[j].z>a.z) &&
                    a.x<(edge[j].x-edge[i].x)*(a.z-edge[i].z)/(edge[j].z-edge[i].z)+edge[i].x)
                    inside=!inside;
            return inside;
        }
        Vector3 Target(Vector2 p) {var q=World(art,p);q.y=Height(q);return q;}
        c.enabled=false;
        go.transform.position=Target(DryRoute[0])+Vector3.up*.08f;
        c.enabled=true;
        Physics.SyncTransforms();
        int n=0;
        foreach(var p in DryRoute.Skip(1).Concat(new[]{new Vector2(5,-12.65f),new Vector2(5,-17),DryRoute[0]}))
        {
            var q=Target(p);
            float fall=.015f;
            int steps=Mathf.CeilToInt(Vector3.Distance(q,go.transform.position)/.055f)+60;
            for(int i=0;i<steps;i++)
            {
                var v=q-go.transform.position;v.y=0;
                fall=c.isGrounded?.015f:Mathf.Min(.13f,fall+.005f);
                c.Move(Vector3.ClampMagnitude(v,.055f)+Vector3.down*fall);
                if(n<DryRoute.Length-1)
                    Require(!InWater(go.transform.position),$"Dry path enters water before {p:F2}");
            }
            var diff=go.transform.position-q;
            Require(new Vector2(diff.x,diff.z).magnitude<.35f&&Mathf.Abs(diff.y)<.40f&&c.isGrounded,
                $"{root.name}: player stuck at {p:F2}, difference {diff:F3}");
            if(n==DryRoute.Length-1)
                Require(InWater(go.transform.position)&&q.y<art.position.y+WaterY,"Expected a reachable shallow wading point");
            n++;
        }
        return n;
    }

    static void Capture(GameObject root,Transform art,string filename)
    {
        var light=new GameObject("Audit sun").AddComponent<Light>();
        SceneManager.MoveGameObjectToScene(light.gameObject,root.scene);
        light.type=LightType.Directional;light.intensity=.98f;light.shadows=LightShadows.Soft;
        light.transform.rotation=Quaternion.Euler(43,-37,0);
        var cam=new GameObject("Audit player view").AddComponent<Camera>();
        SceneManager.MoveGameObjectToScene(cam.gameObject,root.scene);
        cam.scene=root.scene;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.48f,.66f,.80f);
        cam.fieldOfView=67;cam.farClipPlane=160;
        cam.transform.position=art.TransformPoint(new Vector3(-9,.58f,-18.6f))+Vector3.up*1.23f;
        cam.transform.LookAt(art.TransformPoint(new Vector3(1,.2f,8)));
        var rt=RenderTexture.GetTemporary(1200,675,24);
        var active=RenderTexture.active;
        var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        try
        {
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
            File.WriteAllBytes(Evidence+filename,tex.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture=null;RenderTexture.active=active;
            RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);
        }
    }

    static int VerifyBankAccess(GameObject root,Transform art,MeshCollider land,NavMeshLink[] links)
    {
        // Add the two separately baked surfaces away from the open scenes.
        // The biome normally builds the flat ground surface at runtime.
        var offset=new Vector3(8200,0,8100);
        var nav=root.GetComponent<NavMeshSurface>();
        var source=new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Mesh,sourceObject=land.sharedMesh,
            transform=land.transform.localToWorldMatrix,area=0};
        var region=new Bounds(new Vector3(art.position.x,0,art.position.z),new Vector3(100,16,145));
        var data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(nav.agentTypeID),new List<NavMeshBuildSource>{source},
            region,Vector3.zero,Quaternion.identity);
        var outside=NavMesh.AddNavMeshData(data,offset,Quaternion.identity);
        var inside=NavMesh.AddNavMeshData(nav.navMeshData,root.transform.position+offset,root.transform.rotation);
        var access=links.Select(l=>NavMesh.AddLink(new NavMeshLinkData {
            startPosition=l.transform.TransformPoint(l.startPoint),endPosition=l.transform.TransformPoint(l.endPoint),
            width=l.width,bidirectional=l.bidirectional,area=l.area,agentTypeID=l.agentTypeID,costModifier=l.costModifier
        },offset,Quaternion.identity)).ToArray();
        try
        {
            var approach=World(art,new Vector2(5,-31));approach.y=0;
            Require(NavMesh.SamplePosition(approach+offset,out var from,1f,NavMesh.AllAreas),"Missing external bank navigation");
            int count=0;
            foreach(var marker in root.GetComponent<LocationContentSpawner>().enemySpawnPoints)
            {
                Require(NavMesh.SamplePosition(marker.position+offset,out var to,.4f,NavMesh.AllAreas),"Missing enemy navigation");
                foreach(var pair in new[]{(from.position,to.position),(to.position,from.position)})
                {
                    var path=new NavMeshPath();
                    NavMesh.CalculatePath(pair.Item1,pair.Item2,NavMesh.AllAreas,path);
                    Require(path.status==NavMeshPathStatus.PathComplete,$"{root.name}: bank route incomplete for {marker.name}");
                    count++;
                }
            }
            return count;
        }
        finally
        {
            foreach(var link in access) link.Remove();
            inside.Remove();outside.Remove();Object.DestroyImmediate(data);
        }
    }

    public static string Verify()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Verification uses isolated Unity preview scenes");
        var result=new List<string>();
        var method=typeof(LocationContentSpawner).GetMethod("SpawnItems",BindingFlags.Instance|BindingFlags.NonPublic);
        var egg=typeof(EggSpawnRuntimeState).GetField("_spawnedInRun",BindingFlags.Static|BindingFlags.NonPublic);
        var eggState=egg.GetValue(null);
        var random=UnityEngine.Random.state;
        try
        {
            foreach(string biome in new[]{"LevelForest","LevelDesert","LevelWinter","LevelEvent","Lizzi"})
            {
                var path="Assets/Scenes/"+biome+".unity";
                var sourceScene=EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var r=sourceScene.GetRootGameObjects();
                    var spawner=r.SelectMany(g=>g.GetComponentsInChildren<LocationSpawner>(true)).First();
                    var pool=new SerializedObject(spawner.locationCollection).FindProperty("spawnItems");
                    var poolNames=Enumerable.Range(0,pool.arraySize).Select(i=>pool.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("prefab").objectReferenceValue).Where(x=>x).Select(x=>x.name).ToArray();
                    result.Add($"{biome} pool {spawner.locationCollection.name}: {string.Join(",",poolNames.Where(p=>p.StartsWith("pond_")))}; spacing {spawner.minDistance:F0}–{spawner.maxDistance:F0} m");
                    var landRoot=r.SelectMany(g=>g.GetComponentsInChildren<LendController>(true))
                        .SelectMany(l=>l.locations).SelectMany(l=>l.groundSegments).First(t=>t&&t.name=="Land");
                    var landSource=landRoot.GetComponentsInChildren<MeshCollider>(true).First(c=>!c.isTrigger&&c.sharedMesh);
                    for(int i=1;i<=2;i++)
                    {
                        var scene=EditorSceneManager.NewPreviewScene();
                        try
                        {
                            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(i)),scene);
                            root.transform.SetPositionAndRotation(new Vector3(spawner.spawnX,spawner.spawnY,0),Quaternion.identity);
                            var art=root.transform.Find(ArtName);
                            Require(art&&art.gameObject.activeInHierarchy,"Missing deployment on "+root.name);
                            var links=root.GetComponentsInChildren<NavMeshLink>();
                            Require(links.Length==2&&links.All(l=>l.transform.parent.name==BankLinks),"Unexpected active links");
                            Require(!root.GetComponentsInChildren<Renderer>().Any(x=>x.name=="hole"||x.name=="PolyShape"),"Legacy pond geometry active");
                            var ground=art.Find("Pond_Basin_And_Shore").GetComponent<MeshCollider>();
                            var bank=new GameObject("Actual "+biome+" bank");
                            SceneManager.MoveGameObjectToScene(bank,scene);
                            bank.layer=GroundLayer;
                            bank.transform.SetPositionAndRotation(landSource.transform.position,landSource.transform.rotation);
                            bank.transform.localScale=landSource.transform.lossyScale;
                            var land=bank.AddComponent<MeshCollider>();land.sharedMesh=landSource.sharedMesh;
                            var landFilter=landRoot.GetComponent<MeshFilter>();
                            var landRender=new GameObject("Actual "+biome+" visible bank");
                            SceneManager.MoveGameObjectToScene(landRender,scene);
                            landRender.transform.SetPositionAndRotation(landFilter.transform.position,landFilter.transform.rotation);
                            landRender.transform.localScale=landFilter.transform.lossyScale;
                            landRender.AddComponent<MeshFilter>().sharedMesh=landFilter.sharedMesh;
                            landRender.AddComponent<MeshRenderer>().sharedMaterials=landRoot.GetComponent<MeshRenderer>().sharedMaterials;
                            Physics.SyncTransforms();

                            var bounds=ground.bounds;
                            float clearance=i==1?-bounds.max.x:bounds.min.x;
                            Require(clearance>15f,root.name+" extends too close to river center");
                            var floor=World(art,Vector2.zero);
                            float aboveBank=Floor(ground,floor)-Floor(land,floor);
                            Require(aboveBank>.04f,root.name+" bank occludes the pond bowl");
                            var plant=root.GetComponentsInChildren<Transform>().Where(t=>t.parent==art.Find("Oasis_Decoration") &&
                                (t.name.StartsWith("Palm_")||t.name.StartsWith("Reeds_"))).ToArray();
                            Require(plant.Length==6,"Missing plants");
                            var props=art.Find("Oasis_Decoration").Cast<Transform>().ToArray();
                            var range=new List<float>();
                            var contacts=new List<string>();
                            foreach(var t in plant)
                            {
                                var bases=VisibleBase(t);
                                var offsets=bases.Select(v=>v.y-Mathf.Max(Floor(ground,v),Floor(land,v))).ToArray();
                                range.AddRange(offsets);
                                contacts.Add($"{t.name} {offsets.Min():F3}..{offsets.Max():F3}");
                                var renderers=t.GetComponentsInChildren<Renderer>();
                                foreach(var p in props.Where(x=>x!=t))
                                    Require(!renderers.Any(a=>p.GetComponentsInChildren<Renderer>().Any(b=>a.bounds.Intersects(b.bounds))),
                                        $"{root.name}: plant {t.name} intersects {p.name}");
                            }
                            Require(range.Max()<.008f&&range.Min()>-.20f,root.name+" plant bases lack valid contact: "+string.Join(", ",contacts));

                            var content=root.GetComponent<LocationContentSpawner>();
                            var nav=root.GetComponent<NavMeshSurface>();
                            Require(nav.navMeshData && content.itemSpawnPoints.Count==5 && content.enemySpawnPoints.Count==5 &&
                                content._enemyPrefabs.All(e=>e),root.name+" lost a gameplay reference");
                            var refPath=PathFor(i);
                            Require(AssetDatabase.GetDependencies(path).Contains(refPath),root.name+" missing from scene dependencies");
                            foreach(var p in content.itemSpawnPoints)
                            {
                                var t=p.spawnTransform.position;
                                Require(Mathf.Abs(t.y-Floor(ground,t)-.34f)<.025f,root.name+" bad loot support");
                            }
                            int waypoints=Walk(root,art,ground,land);
                            UnityEngine.Random.InitState(203+i);
                            method.Invoke(content,new object[]{content.itemCollection});
                            var spawned=root.GetComponentsInChildren<PickableItem>();
                            Require(spawned.Length==5 && content.itemSpawnPoints.All(p=>spawned.Any(k=>
                                Vector3.Distance(k.transform.position,p.spawnTransform.position)<.02f)),root.name+" did not instantiate loot at all five slots");
                            int access=0;
                            if(biome=="LevelDesert")
                            {
                                access=VerifyBankAccess(root,art,land,links);
                                Capture(root,art,$"game-bank-{i}.png");
                            }
                            result.Add($"{biome}/{root.name}: {waypoints} walking checkpoints, native loot 5/5, 6 grounded plants ({range.Min():F3}..{range.Max():F3} m); bowl above ground {aboveBank:F3} m; inner sand edge |X|={clearance:F1} m."+
                                (access>0?$" Complete enemy routes between pond and bank: {access}.":""));
                        }
                        finally { EditorSceneManager.ClosePreviewScene(scene); }
                    }
                }
                finally { EditorSceneManager.ClosePreviewScene(sourceScene); }
            }
        }
        finally {UnityEngine.Random.state=random;egg.SetValue(null,eggState);}
        File.WriteAllLines(Evidence+"game-check.txt",result);
        return string.Join("\n",result);
    }
}
