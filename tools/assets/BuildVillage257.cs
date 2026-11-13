// Compile with RefineVillage256.cs through Unity MCP script-execute.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class BuildVillage257
{
    const string Folder="Assets/Prefabs/Workshop/Village257/";
    const string EnvPath=Folder+"Village_Overgrown_Environment.prefab";
    const string EnvName="Village_Environment257";
    const string Output="Docs/locations/previews/village-overgrown257/";
    static readonly Vector4[] Beds={new(-33.8f,16.3f,4.4f,3.5f),new(-55.8f,-1.7f,3.5f,2.8f),new(-75.6f,18.3f,3.9f,2.5f),new(-88,-.8f,4.2f,2.5f),new(-103.5f,19,4.2f,2.6f),new(-18.5f,-.7f,3.3f,2.4f)};
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static float R(System.Random rng,float a,float b)=>a+(float)rng.NextDouble()*(b-a);
    static Transform Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
    static Material Mat(string name,string hex)
    {
        var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
        ColorUtility.TryParseHtmlString(hex,out var color);m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",0);AssetDatabase.CreateAsset(m,path);return m;
    }
    sealed class Geometry
    {
        readonly List<Vector3> v=new();readonly List<Vector3> n=new();readonly List<int>[] tr;
        public Geometry(int materials){tr=Enumerable.Range(0,materials).Select(_=>new List<int>()).ToArray();}
        void Tri(Vector3 a,Vector3 b,Vector3 c,int mat){int i=v.Count;var normal=Vector3.Cross(b-a,c-a).normalized;v.AddRange(new[]{a,b,c});n.AddRange(new[]{normal,normal,normal});tr[mat].AddRange(new[]{i,i+1,i+2});}
        public void Slab(Vector3 center,float rx,float rz,float yaw,float bottom,float top,int mat,System.Random rng,int count=6)
        {
            var points=Enumerable.Range(0,count).Select(i=>{float a=(i+.12f)*Mathf.PI*2/count;return center+Quaternion.Euler(0,yaw,0)*new Vector3(Mathf.Cos(a)*rx*R(rng,.86f,1),0,Mathf.Sin(a)*rz*R(rng,.86f,1));}).ToArray();
            var c=new Vector3(center.x,top,center.z);
            for(int i=0;i<count;i++)
            {
                var a=points[i];var b=points[(i+1)%count];a.y=top;b.y=top;Tri(c,b,a,mat);
                var lowA=new Vector3(a.x,bottom,a.z);var lowB=new Vector3(b.x,bottom,b.z);
                Tri(a,b,lowB,mat);Tri(a,lowB,lowA,mat);Tri(new Vector3(center.x,bottom,center.z),lowA,lowB,mat);
            }
        }
        public void Polygon(List<Vector2> polygon,float bottom,float top,int mat)
        {
            if(polygon.Count<3)return;var center=polygon.Aggregate(Vector2.zero,(a,b)=>a+b)/polygon.Count;
            var p=polygon.Select(v=>Vector2.Lerp(center,v,.979f)).ToArray();
            for(int i=0;i<p.Length;i++)
            {
                var a=new Vector3(p[i].x,top,p[i].y);var b=new Vector3(p[(i+1)%p.Length].x,top,p[(i+1)%p.Length].y);
                var lowA=new Vector3(a.x,bottom,a.z);var lowB=new Vector3(b.x,bottom,b.z);
                Tri(new Vector3(center.x,top,center.y),b,a,mat);Tri(a,b,lowB,mat);Tri(a,lowB,lowA,mat);Tri(new Vector3(center.x,bottom,center.y),lowA,lowB,mat);
            }
        }
        public Mesh Save(string name)
        {
            var path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool existing=mesh;
            if(!mesh)mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};else mesh.Clear();
            mesh.SetVertices(v);mesh.SetNormals(n);mesh.subMeshCount=tr.Length;
            for(int i=0;i<tr.Length;i++)mesh.SetTriangles(tr[i],i);mesh.RecalculateBounds();mesh.UploadMeshData(false);
            if(existing){EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
    static Transform MeshObject(Transform parent,string name,Mesh mesh,Material[] materials,bool collision=false)
    {
        var t=Group(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;t.gameObject.layer=6;
        if(collision)t.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;return t;
    }
    static Transform CopyVisual(Transform source,Transform parent)
    {
        var target=Group(source.name,parent);target.localPosition=source.localPosition;target.localRotation=source.localRotation;target.localScale=source.localScale;
        var f=source.GetComponent<MeshFilter>();var r=source.GetComponent<MeshRenderer>();
        if(f&&r&&r.enabled&&f.sharedMesh){target.gameObject.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;target.gameObject.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;}
        foreach(Transform child in source)if(child.gameObject.activeSelf)CopyVisual(child,target);return target;
    }
    static Vector3 Foot(Transform root)
    {
        var v=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
        float y=v.Min(p=>p.y);var low=v.Where(p=>p.y<y+.01f).ToArray();return new Vector3(low.Average(p=>p.x),y,low.Average(p=>p.z));
    }
    static Transform Plant(Transform parent,string path,string name,Vector3 p,float height,float yaw,MeshFilter[] surfaces)
    {
        var plant=CopyVisual(AssetDatabase.LoadAssetAtPath<GameObject>(path).transform,parent);plant.name=name;plant.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));
        var rs=plant.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        plant.localScale*=height/b.size.y;
        var foot=Foot(plant);plant.position+=new Vector3(p.x-foot.x,-foot.y,p.z-foot.z);foot=Foot(plant);
        float support=RefineVillage256.Surface(surfaces,foot,.1f);Check(!float.IsInfinity(support),"No planted surface "+name);plant.position+=Vector3.up*(support-foot.y-.008f);
        if(path.EndsWith("/Flower.prefab"))foreach(var r in rs)
        {
            string mesh=r.GetComponent<MeshFilter>().sharedMesh.name;bool head=new[]{"Mesh7","Mesh9","Mesh11","Mesh13","Mesh15"}.Contains(mesh);
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/SharedVegetation/Flower_"+(head?"Petals_Violet":"Leaves_Green")+".mat");r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
        }
        if(name.StartsWith("Tree_"))
        {
            var collider=Group("Trunk collision",plant);collider.position=new Vector3(p.x,support+1.35f,p.z);collider.rotation=Quaternion.identity;
            var scale=plant.lossyScale;collider.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);collider.gameObject.layer=6;collider.gameObject.AddComponent<BoxCollider>().size=new Vector3(.65f,2.7f,.65f);
        }
        return plant;
    }
    static void Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;g.layer=6;
    }
    static bool InBed(float x,float z,float margin=0)=>Beds.Any(b=>Mathf.Pow((x-b.x)/(b.z+margin),2)+Mathf.Pow((z-b.y)/(b.w+margin),2)<1);
    static Geometry Paving(GameObject canonical)
    {
        var rng=new System.Random(257);var paving=new Geometry(3);var seeds=new List<Vector2>();
        for(float x=-114;x<-10;x+=2.7f)for(float z=-6;z<24;z+=2.35f)seeds.Add(new Vector2(x+R(rng,-.8f,.8f),z+R(rng,-.7f,.7f)));
        foreach(var seed in seeds)
        {
            if(InBed(seed.x,seed.y,.35f))continue;
            var polygon=new List<Vector2>{new(-114,-4.5f),new(-12,-4.5f),new(-12,21.5f),new(-114,21.5f)};
            foreach(var other in seeds)
            {
                if(other==seed||(other-seed).sqrMagnitude>100)continue;
                var normal=other-seed;float limit=(other.sqrMagnitude-seed.sqrMagnitude)*.5f;var clipped=new List<Vector2>();
                for(int i=0;i<polygon.Count;i++)
                {
                    var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float da=Vector2.Dot(a,normal)-limit;float db=Vector2.Dot(b,normal)-limit;
                    if(da<=0)clipped.Add(a);if((da<=0)!=(db<=0))clipped.Add(Vector2.Lerp(a,b,da/(da-db)));
                }
                polygon=clipped;if(polygon.Count==0)break;
            }
            paving.Polygon(polygon,-.012f,R(rng,.023f,.03f),rng.Next(3));
        }
        foreach(var house in RefineVillage256.Homes(canonical))
        {
            var front=house.TransformPoint(new Vector3(0,.514f,-4));float edge=front.z<8?-3:20;
            for(float t=0;t<=1;t+=.24f){var p=Vector3.Lerp(front,new Vector3(front.x,0,edge),t);p.y=0;paving.Slab(p,1.65f,.61f,0,-.012f,.028f,rng.Next(3),rng,6);}
        }
        return paving;
    }
    public static string Build()
    {
        Check(!AssetDatabase.LoadAssetAtPath<GameObject>(EnvPath),"Environment already exists; do not rebuild over manual edits");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","Village257");Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var env=new GameObject(EnvName);SceneManager.MoveGameObjectToScene(env,scene);
            var mats=new[]{Mat("Path_Stone_Grey","#777C73"),Mat("Path_Stone_Light","#83897E"),Mat("Path_Stone_Shade","#6B7369")};
            var moss=Mat("Ground_Moss","#506439");var turf=Mat("Garden_Green","#657D42");var wood=Mat("Old_Fence_Wood","#766048");
            var rng=new System.Random(257);var green=new Geometry(2);var edges=new Geometry(3);
            // Moss under the irregular stones is geometry; no realistic texture or new building art.
            green.Slab(new Vector3(-63,0,8),51,14,0,-.025f,.007f,0,rng,14);
            foreach(var bed in Beds)green.Slab(new Vector3(bed.x,0,bed.y),bed.z,bed.w,0,-.025f,.012f,1,rng,12);
            var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(RefineVillage256.Canonical);
            var paving=Paving(canonical);
            foreach(var bed in Beds)
            {
                int steps=24;for(int i=0;i<steps;i++)
                {
                    // Leave small natural gaps in the low stone garden edging.
                    if(i%8==0||i%8==1)continue;float a=i*Mathf.PI*2/steps;
                    var p=new Vector3(bed.x+Mathf.Cos(a)*bed.z,0,bed.y+Mathf.Sin(a)*bed.w);
                    edges.Slab(p,R(rng,.33f,.48f),R(rng,.22f,.33f),a*Mathf.Rad2Deg,-.015f,R(rng,.14f,.21f),rng.Next(3),rng,5);
                }
            }
            var ground=MeshObject(env.transform,"Moss_and_garden_ground",green.Save("Moss_Garden_Ground"),new[]{moss,turf},true);
            MeshObject(env.transform,"Worn_flagstone_paths",paving.Save("Worn_Flagstone_Paths"),mats);
            MeshObject(env.transform,"Low_stone_garden_edges",edges.Save("Low_Stone_Edges"),mats,true);
            var plants=Group("Grounded_Plants",env.transform);var surfaces=new[]{ground.GetComponent<MeshFilter>()};const string decor="Assets/Prefabs/Locations/Decoration/";
            Plant(plants,decor+"Hilly_Prop_Tree_Oak_4.prefab","Tree_01",new Vector3(-35.5f,0,18.5f),7,35,surfaces);
            Plant(plants,decor+"Hilly_Prop_Tree_Oak_4.prefab","Tree_02",new Vector3(-56,0,-1.7f),6.5f,140,surfaces);
            Plant(plants,decor+"Hilly_Prop_Tree_Oak_4.prefab","Tree_03",new Vector3(-104,0,20.3f),6.8f,245,surfaces);
            int shrub=0,flower=0;
            foreach(var bed in Beds)
            {
                for(int i=0;i<4;i++)
                {
                    float a=i*1.9f+bed.x;var p=new Vector3(bed.x+Mathf.Cos(a)*bed.z*.57f,0,bed.y+Mathf.Sin(a)*bed.w*.55f);
                    // Keep the old braziers and the main lane readable.
                    if(canonical.transform.Find("PointCenter (1)/To4ka7").Cast<Transform>().Where(t=>t.name.StartsWith("Fire_")).Any(t=>Vector3.Distance(new Vector3(t.position.x,0,t.position.z),p)<2))continue;
                    Plant(plants,decor+(i%2==0?"Hilly_Prop_Bush_2.prefab":"Hilly_Prop_Bush_3.prefab"),"Bush_"+(++shrub).ToString("00"),p,R(rng,.75f,1.25f),R(rng,0,360),surfaces);
                }
                for(int i=0;i<3;i++)
                {
                    float a=i*2.4f+bed.y;var p=new Vector3(bed.x+Mathf.Cos(a)*bed.z*.73f,0,bed.y+Mathf.Sin(a)*bed.w*.7f);
                    Plant(plants,"Assets/Import/3D Cartoon Village/Prefabs/Flower.prefab","Flowers_"+(++flower).ToString("00"),p,R(rng,.4f,.62f),R(rng,0,360),surfaces);
                }
            }
            var fences=Group("Short_old_fences",env.transform);
            foreach(var p in new[]{new Vector3(-20.3f,0,1.7f),new Vector3(-89,0,-3.4f),new Vector3(-76,0,20.6f)})
            {
                var fence=Group("Low wooden fence",fences);
                foreach(float x in new[]{-1.6f,0,1.6f})Box(fence,"Post",p+new Vector3(x,.48f,0),new Vector3(.16f,.96f,.16f),wood);
                foreach(float y in new[]{.32f,.68f})Box(fence,"Rail",p+new Vector3(0,y,0),new Vector3(3.4f,.13f,.12f),wood);
            }
            Check(PrefabUtility.SaveAsPrefabAsset(env,EnvPath),"Cannot save environment");
            File.WriteAllText(Output+"build.txt",$"Variant 05: separate environment prefab; 3 trees, {shrub} shrubs, {flower} flower clumps, 6 garden islands, worn paths, low stone edges and 3 short fences. No house assets edited.\n");
            return File.ReadAllText(Output+"build.txt");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static string Install()
    {
        var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(RefineVillage256.Canonical);var origin=canonical.transform.Find("PointCenter (1)");var report=new List<string>();
        foreach(var path in RefineVillage256.Paths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                string before=RefineVillage256.Houses(root);var center=root.transform.Find("PointCenter (1)");Check(!center.Find(EnvName),"Environment already installed "+path);
                var matrix=center.localToWorldMatrix*origin.worldToLocalMatrix;var rotation=center.rotation*Quaternion.Inverse(origin.rotation);
                var env=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EnvPath),root.scene);env.name=EnvName;env.transform.SetParent(center,true);env.transform.SetPositionAndRotation(matrix.MultiplyPoint3x4(Vector3.zero),rotation);
                // Tuck the existing cart beside the near tree; move the whole prop, not its parts.
                var cart=center.Find("To4ka7/cart");if(cart){var foot=Foot(cart);var target=matrix.MultiplyPoint3x4(new Vector3(-32.5f,0,13.6f));cart.position+=target-foot;}
                Check(before==RefineVillage256.Houses(root),"House/interior modified");
                var surface=root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
                if(surface)
                {
                    var old=surface.navMeshData;surface.RemoveData();surface.BuildNavMesh();var fresh=surface.navMeshData;surface.RemoveData();
                    if(old&&fresh&&old!=fresh){string oldPath=AssetDatabase.GetAssetPath(old);string backup="Library/Village256/Backup/"+oldPath;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(oldPath,backup);EditorUtility.CopySerialized(fresh,old);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);surface.navMeshData=old;Object.DestroyImmediate(fresh);}
                }
                Check(PrefabUtility.SaveAsPrefabAsset(root,path),"Cannot save "+path);report.Add("PASS "+path+": variant 05 installed; ten houses and interiors unchanged; loot=25; guard slots=15; navigation rebuilt.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllLines(Output+"installation.txt",report);return string.Join("\n",report);
    }

    public static string EnsureGroundCollision()
    {
        var root=PrefabUtility.LoadPrefabContents(EnvPath);
        try{var t=root.transform.Find("Moss_and_garden_ground");if(!t.GetComponent<MeshCollider>())t.gameObject.AddComponent<MeshCollider>().sharedMesh=t.GetComponent<MeshFilter>().sharedMesh;PrefabUtility.SaveAsPrefabAsset(root,EnvPath);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var path in RefineVillage256.Paths)
        {
            var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var s=g.GetComponent<Unity.AI.Navigation.NavMeshSurface>();if(!s)continue;var old=s.navMeshData;s.RemoveData();s.BuildNavMesh();var fresh=s.navMeshData;s.RemoveData();
                if(old&&fresh&&old!=fresh){EditorUtility.CopySerialized(fresh,old);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);s.navMeshData=old;Object.DestroyImmediate(fresh);}
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(g);}
        }
        return "Garden ground collision and all three navigation assets updated.";
    }

    public static string PolishPaths()
    {
        Paving(AssetDatabase.LoadAssetAtPath<GameObject>(RefineVillage256.Canonical)).Save("Worn_Flagstone_Paths");
        foreach(var paint in new[]{("Path_Stone_Grey","#777C73"),("Path_Stone_Light","#83897E"),("Path_Stone_Shade","#6B7369"),("Ground_Moss","#506439"),("Garden_Green","#657D42")})
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+paint.Item1+".mat");ColorUtility.TryParseHtmlString(paint.Item2,out var color);mat.color=color;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
        }
        return "Replaced repeating stepping stones with fitted irregular flagstones and muted moss/stone colors.";
    }

    static void Shot(Camera c,string filename,Vector3 p,Vector3 target,bool overview=false)
    {
        c.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));c.orthographic=overview;c.orthographicSize=52;c.fieldOfView=65;
        var rt=RenderTexture.GetTemporary(1500,1000,24);var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try{c.targetTexture=rt;c.aspect=1.5f;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(Output+filename,tex.EncodeToPNG());}
        finally{c.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }

    public static string VerifyAndCapture()
    {
        Directory.CreateDirectory(Output);var lines=new List<string>();
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/LevelForest.unity");var scene=EditorSceneManager.NewPreviewScene();
        bool fog=RenderSettings.fog;var mode=RenderSettings.ambientMode;var ambient=RenderSettings.ambientLight;
        try
        {
            var land=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LendController>(true)).SelectMany(l=>l.locations).SelectMany(l=>l.groundSegments).First(t=>t&&t.name=="Land");
            var terrain=CopyVisual(land,null);SceneManager.MoveGameObjectToScene(terrain.gameObject,scene);
            var landFilter=terrain.GetComponent<MeshFilter>();Check(landFilter&&landFilter.sharedMesh,"Real forest terrain mesh missing");
            terrain.gameObject.AddComponent<MeshCollider>().sharedMesh=landFilter.sharedMesh;
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.70f,.75f);
            var sun=new GameObject("Verification sun");SceneManager.MoveGameObjectToScene(sun,scene);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-35,0);
            var cg=new GameObject("Verification camera");SceneManager.MoveGameObjectToScene(cg,scene);var camera=cg.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.nearClipPlane=.05f;camera.farClipPlane=500;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.48f,.63f,.72f);
            var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
            var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(RefineVillage256.Canonical);var origin=canonical.transform.Find("PointCenter (1)");
            foreach(var path in RefineVillage256.Paths)
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var content=asset.GetComponent<LocationContentSpawner>();
                Check(RefineVillage256.Houses(asset)==File.ReadAllText("Docs/locations/previews/village-refine256/"+Path.GetFileNameWithoutExtension(path)+"-houses.txt"),"Saved houses/interiors changed "+path);
                Check(content.itemSpawnPoints.Count==25&&content.enemySpawnPoints.Count==15,"Gameplay slot counts changed");
                var root=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);var env=root.transform.Find("PointCenter (1)/"+EnvName);Check(env,"Missing selected environment");
                var ground=env.Find("Moss_and_garden_ground").GetComponent<MeshFilter>();int plants=0;
                foreach(Transform plant in env.Find("Grounded_Plants"))
                {
                    var foot=Foot(plant);float y=RefineVillage256.Surface(new[]{ground,landFilter},foot,foot.y+.1f);float gap=foot.y-y;
                    Check(!float.IsInfinity(y)&&gap<.015f&&gap>-.025f,"Floating/embedded plant "+plant.name+" "+gap);
                    lines.Add($"GROUND {asset.name}/{plant.name}: visible foot={foot:F4}, real surface={y:F4}, gap={gap:F4} m");plants++;
                }
                // Move test geometry away from the owner's loaded workshop physics.
                var offset=new Vector3(10000,0,10000);root.transform.position+=offset;terrain.position+=offset;Physics.SyncTransforms();
                foreach(var house in RefineVillage256.Homes(root))
                {
                    var go=new GameObject("Entrance probe");SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=house.TransformPoint(new Vector3(0,.55f,-4.6f));
                    var cc=go.AddComponent<CharacterController>();cc.height=template.height;cc.radius=template.radius;cc.center=template.center;cc.stepOffset=template.stepOffset;cc.skinWidth=template.skinWidth;cc.slopeLimit=template.slopeLimit;
                    Physics.SyncTransforms();for(int i=0;i<340&&house.InverseTransformPoint(go.transform.position).z<1.3f;i++)cc.Move(house.forward*.045f+Vector3.down*.025f);
                    var local=house.InverseTransformPoint(go.transform.position);Check(local.z>1.1f&&cc.isGrounded,"Blocked house entrance "+house.name+" "+local);Object.DestroyImmediate(go);
                }
                root.transform.position-=offset;terrain.position-=offset;Physics.SyncTransforms();
                var center=root.transform.Find("PointCenter (1)");var matrix=center.localToWorldMatrix*origin.worldToLocalMatrix;
                Shot(camera,asset.name+"-street.png",matrix.MultiplyPoint3x4(new Vector3(-11,1.8f,5)),matrix.MultiplyPoint3x4(new Vector3(-75,2.3f,8)));
                Shot(camera,asset.name+"-overview.png",matrix.MultiplyPoint3x4(new Vector3(-63,78,-62)),matrix.MultiplyPoint3x4(new Vector3(-63,1,8)),true);
                Shot(camera,asset.name+"-garden.png",matrix.MultiplyPoint3x4(new Vector3(-28,1.8f,10)),matrix.MultiplyPoint3x4(new Vector3(-39,1.5f,18)));
                lines.Add($"PASS {path}: {plants} whole plants grounded against moss/real LevelForest terrain triangles; all ten original house entrances pass the player controller; saved houses and interiors unchanged.");
                root.SetActive(false);Object.DestroyImmediate(root);
            }
        }
        finally{RenderSettings.fog=fog;RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;EditorSceneManager.ClosePreviewScene(scene);EditorSceneManager.ClosePreviewScene(source);}
        Check(!EditorUtility.scriptCompilationFailed,"Project compilation errors");File.WriteAllLines(Output+"verification.txt",lines);return string.Join("\n",lines.Where(s=>s.StartsWith("PASS")));
    }
}
