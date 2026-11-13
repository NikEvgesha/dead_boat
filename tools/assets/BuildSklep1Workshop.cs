// Run through Unity MCP script-execute, class BuildSklep1Workshop, method Build.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class BuildSklep1Workshop
{
    const string Folder="Assets/Prefabs/Workshop/Sklep1/";
    public const string Prefab=Folder+"Sklep1_OvergrownRuins.prefab";
    public const string ScenePath="Assets/Scenes/Sklep1Workshop.unity";
    static Transform Child(Transform p,string name)
    {
        var t=new GameObject(name).transform;SceneManager.MoveGameObjectToScene(t.gameObject,p.gameObject.scene);
        t.SetParent(p,false);return t;
    }
    static Material Paint(string name,string hex)
    {
        var m=new Material(Shader.Find("Standard")){name=name};ColorUtility.TryParseHtmlString(hex,out var c);
        m.color=c;m.SetFloat("_Glossiness",.02f);AssetDatabase.CreateAsset(m,Folder+name+".mat");return m;
    }
    static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,bool solid=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(g,p.gameObject.scene);
        g.name=name;g.transform.SetParent(p,false);g.transform.position=pos;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat;g.layer=6;
        if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static Mesh FlatMesh(string name,List<Vector3> vertices,List<int> triangles)
    {
        var m=new Mesh{name=name,vertices=triangles.Select(i=>vertices[i]).ToArray(),triangles=Enumerable.Range(0,triangles.Count).ToArray()};
        m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+name+".asset");return m;
    }
    static void Prism(List<Vector3> v,List<int> t,Vector3[] outline,Vector3 depth)
    {
        int n=outline.Length,b=v.Count;v.AddRange(outline);v.AddRange(outline.Select(p=>p+depth));
        // Top profiles are x-monotone; triangulate the side as strips to the bottom edge.
        for(int i=2;i<n;i++){t.AddRange(new[]{b,b+i-1,b+i,b+n,b+n+i,b+n+i-1});}
        for(int i=0;i<n;i++){int j=(i+1)%n;t.AddRange(new[]{b+i,b+n+i,b+n+j,b+i,b+n+j,b+j});}
    }
    static void WeatherWall(MeshFilter f,int index,Material mat)
    {
        var r=f.GetComponent<Renderer>();var b=r.bounds;bool alongX=b.size.x>b.size.z;
        float lo=alongX?b.min.x:b.min.z,hi=alongX?b.max.x:b.max.z;
        var points=new List<Vector3>();
        Vector3 P(float u,float y)=>alongX?new Vector3(u,y,b.min.z):new Vector3(b.max.x,y,u);
        points.Add(P(lo,b.min.y));points.Add(P(hi,b.min.y));points.Add(P(hi,b.max.y));
        for(int k=7;k>=0;k--)
        {
            float u=Mathf.Lerp(lo,hi,k/8f),y=b.max.y-(k==0?0:((index*11+k*7)%5)*.22f);
            points.Add(P(u,y));
        }
        var v=new List<Vector3>();var t=new List<int>();
        Prism(v,t,points.ToArray(),alongX?Vector3.forward*b.size.z:Vector3.left*b.size.x);
        for(int i=0;i<t.Count;i+=3){int tmp=t[i+1];t[i+1]=t[i+2];t[i+2]=tmp;}
        for(int i=0;i<v.Count;i++)v[i]=f.transform.InverseTransformPoint(v[i]);
        var mesh=FlatMesh("Chipped_Wall_"+index,v,t);f.sharedMesh=mesh;r.sharedMaterial=mat;
        var c=f.GetComponent<MeshCollider>();if(c)c.sharedMesh=mesh;
    }
    static void MeshObject(Transform parent,string name,Mesh mesh,Material mat)
    {
        var g=Child(parent,name).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.layer=6;
    }
    static void Prop(Transform parent,string path,string name,Vector3 p,float height,float yaw)
    {
        var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
        g.name=name;g.transform.SetPositionAndRotation(new Vector3(p.x,0,p.z),Quaternion.Euler(0,yaw,0));
        var rs=g.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
        g.transform.localScale*=height/b.size.y;
        var vertices=g.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
        float low=vertices.Min(v=>v.y);var feet=vertices.Where(v=>v.y<low+.02f).ToArray();var center=feet.Aggregate(Vector3.zero,(s,v)=>s+v)/feet.Length;
        g.transform.position+=new Vector3(p.x-center.x,p.y-low-.012f,p.z-center.z);
        foreach(var r in rs)
        {
            if(!path.EndsWith("/Flower.prefab"))continue;
            bool head=new[]{"Mesh7","Mesh9","Mesh11","Mesh13","Mesh15"}.Contains(r.GetComponent<MeshFilter>().sharedMesh.name);
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/SharedVegetation/Flower_"+(head?"Petals_Violet":"Leaves_Green")+".mat");
            r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
        }
        foreach(var c in g.GetComponentsInChildren<Collider>())c.gameObject.layer=6;
    }
    static Mesh LeafMesh()
    {
        var v=new List<Vector3>{new Vector3(0,.58f,0),new Vector3(-.37f,.12f,0),new Vector3(0,-.5f,0),new Vector3(.37f,.12f,0),new Vector3(0,.06f,-.16f)};
        return FlatMesh("Faceted_Ivy_Leaf",v,new List<int>{0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3});
    }
    static void Vine(Transform parent,int id,Vector3 top,Vector3 outward,float length,Mesh mesh,Material[] leaves,Material stem)
    {
        var vine=Child(parent,"Attached_Ivy_"+id);var rotation=Quaternion.LookRotation(-outward,Vector3.up);
        var side=Vector3.Cross(Vector3.up,outward);
        Box(vine,"Stem",top-Vector3.up*length*.5f,new Vector3(.06f,length,.06f),stem,false);
        int n=Mathf.CeilToInt(length/.46f);
        for(int i=0;i<n;i++)for(int j=0;j<2;j++)
        {
            var g=Child(vine,"Leaf_"+i+"_"+j).gameObject;
            g.transform.position=top-Vector3.up*(i*.46f)+side*((j==0?-.30f:.30f)+Mathf.Sin(i*2.1f)*.11f)+outward*.08f;
            g.transform.rotation=rotation*Quaternion.Euler(0,0,j==0?26:-26);g.transform.localScale=Vector3.one*(1.20f-i*.025f);
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=leaves[(i+j+id)%leaves.Length];
        }
    }
    static void Moss(Transform parent,Renderer wall,int index,Material[] mats)
    {
        var b=wall.bounds;bool alongX=b.size.x>b.size.z;
        float lo=alongX?b.min.x:b.min.z,hi=alongX?b.max.x:b.max.z;
        for(int side=0;side<2;side++)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<5;i++)
            {
                float start=Mathf.Lerp(lo,hi,(i+.08f)/5),end=Mathf.Min(hi,start+1.8f+(index+i)%3*.55f);
                float y=.28f+(index*3+i*7)%5*.10f;
                Vector3 P(float u,float h)=>alongX?new Vector3(u,h,side==0?b.min.z-.012f:b.max.z+.012f):new Vector3(side==0?b.min.x-.012f:b.max.x+.012f,h,u);
                int a=v.Count;v.AddRange(new[]{P(start,.17f),P(end,.17f),P((start+end)/2,y+.38f)});
                t.AddRange(new[]{a,a+1,a+2,a+2,a+1,a});
            }
            MeshObject(parent,"Wall_Foot_Moss_"+index+"_"+side,FlatMesh("Moss_"+index+"_"+side,v,t),mats[(index+side)%mats.Length]);
        }
    }
    public static string Polish()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var tree=root.transform.Find("Overgrown_Concept_02/Grounded_Plants_And_Rocks/Outer_Oak");
            var old=tree.GetComponentsInChildren<MeshRenderer>();var b=old[0].bounds;foreach(var r in old.Skip(1))b.Encapsulate(r.bounds);
            tree.localScale*=15/b.size.y;
            var vertices=tree.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
            var feet=vertices.Where(p=>p.y<vertices.Min(v=>v.y)+.02f).ToArray();var center=feet.Aggregate(Vector3.zero,(s,p)=>s+p)/feet.Length;
            tree.position+=new Vector3(96-center.x,-.012f-vertices.Min(p=>p.y),24-center.z);
            foreach(var ivy in root.transform.Find("Overgrown_Concept_02/Wall_Attached_Ivy_And_Moss").Cast<Transform>().Where(t=>t.name.StartsWith("Attached_Ivy_")))
                foreach(var leaf in ivy.Cast<Transform>().Where(t=>t.name.StartsWith("Leaf_")))leaf.localScale*=1.25f;
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.GetRootGameObjects().Any(g=>g.name=="Soft_Outdoor_Fill"))
        {
            var fill=new GameObject("Soft_Outdoor_Fill");SceneManager.MoveGameObjectToScene(fill,scene);fill.transform.rotation=Quaternion.Euler(48,148,0);
            var light=fill.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.25f;light.shadows=LightShadows.None;
        }
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Increased oak canopy and ivy; added soft outdoor fill to workshop scene.";
    }
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit Mode");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab))throw new Exception("Workshop already exists; inspect first");
        AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","Sklep1");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/Sklep_1.prefab"),scene);
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="Sklep1_OvergrownRuins";
            var stone=Paint("Ruins_Stone","#929BA8");var pale=Paint("Pale_Edges","#B3BDCA");var dark=Paint("Dark_Caps","#424A53");
            var sand=Paint("Warm_Sand","#C6B083");var red=Paint("Red_Band","#BD4941");var blue=Paint("Blue_Band","#518CB7");
            var leaf=new[]{Paint("Ivy_Light","#8CB957"),Paint("Ivy_Mid","#74A346"),Paint("Ivy_Shade","#597C38")};
            var moss=new[]{Paint("Moss_Olive","#7D8842"),Paint("Moss_Dark","#67743B")};var stem=Paint("Ivy_Stem","#546839");
            var decor=Child(root.transform,"Overgrown_Concept_02");
            Box(decor,"Workshop_Sand_Ground",new Vector3(61,-.55f,7),new Vector3(93,1.1f,82),sand);
            var walls=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Wall_Stone_")).ToArray();int index=0;
            foreach(var f in walls){if(f.name.StartsWith("Wall_Stone_Normal"))WeatherWall(f,index,stone);else f.GetComponent<Renderer>().sharedMaterial=stone;index++;}
            // Original floor meshes include a duplicate tile and have no colliders. Keep their objects for provenance.
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Floor_Tile_")))r.enabled=false;
            var floor=Child(decor,"Checker_Stone_Floor");
            var tones=new[]{Paint("Floor_A","#A4ADBC"),Paint("Floor_B","#B4BECC"),Paint("Floor_C","#9CA6B4"),Paint("Floor_D","#ABB5C3")};
            const float x0=35.62f,z0=-17.72f,step=3.105f;
            Box(floor,"Floor_Support",new Vector3(x0+step*8,-.08f,z0+step*8),new Vector3(step*16,.5f,step*16),tones[2]);
            for(int color=0;color<4;color++)
            {
                var v=new List<Vector3>();var t=new List<int>();
                for(int x=0;x<16;x++)for(int z=0;z<16;z++)
                {
                    if(((x+z)%2*2+(x*7+z*3)%3%2)!=color)continue;
                    float px=x0+x*step,pz=z0+z*step;int a=v.Count;
                    v.AddRange(new[]{new Vector3(px,.177f,pz),new Vector3(px,.177f,pz+step-.018f),new Vector3(px+step-.018f,.177f,pz+step-.018f),new Vector3(px+step-.018f,.177f,pz)});
                    t.AddRange(new[]{a,a+1,a+2,a,a+2,a+3});
                }
                MeshObject(floor,"Paving_"+color,FlatMesh("Paving_"+color,v,t),tones[color]);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Braz")||t.name.StartsWith("Pillar02")))t.gameObject.SetActive(false);
            var corners=new[]{new Vector3(34.99f,0,-19.01f),new Vector3(85.52f,0,-17.68f),new Vector3(34.57f,0,32.44f),new Vector3(85.96f,0,31.98f)};
            for(int i=0;i<corners.Length;i++)
            {
                var c=Child(decor,"Corner_Tower_"+i);var p=corners[i];var signal=i==0||i==3?red:blue;
                Box(c,"Stone_Foot",p+Vector3.up*.65f,new Vector3(3.4f,1.3f,3.4f),stone);
                Box(c,"Square_Shaft",p+Vector3.up*4.8f,new Vector3(2.6f,7.1f,2.6f),stone);
                Box(c,"Signal_Band",p+Vector3.up*3.5f,new Vector3(2.69f,1.75f,2.69f),signal);
                Box(c,"Neck",p+Vector3.up*8.6f,new Vector3(2.9f,.6f,2.9f),pale);
                Box(c,"Dark_Lantern",p+Vector3.up*9.35f,new Vector3(3.25f,.9f,3.25f),dark);
                Box(c,"Capstone",p+Vector3.up*9.93f,new Vector3(3.55f,.3f,3.55f),dark);
                for(int j=-1;j<=1;j++)foreach(int s in new[]{-1,1})
                {
                    Box(c,"Front_Stud_"+j+"_"+s,p+new Vector3(j*.82f,9.4f,s*1.64f),new Vector3(.33f,.32f,.025f),signal,false);
                    Box(c,"Side_Stud_"+j+"_"+s,p+new Vector3(s*1.64f,9.4f,j*.82f),new Vector3(.025f,.32f,.33f),signal,false);
                }
            }
            var ivy=Child(decor,"Wall_Attached_Ivy_And_Moss");var leafMesh=LeafMesh();int vine=0;
            foreach(var spec in new[]{(new Vector3(41,8.9f,32.79f),Vector3.forward,6.3f),(new Vector3(43,8.9f,32.79f),Vector3.forward,4.7f),
                (new Vector3(85.53f,8.9f,23),Vector3.right,5.7f),(new Vector3(85.53f,8.9f,21.6f),Vector3.right,7.0f),
                (new Vector3(48.99f,8.8f,16),Vector3.right,5.1f),(new Vector3(49.0f,8.8f,14.7f),Vector3.right,3.8f),
                (new Vector3(72.17f,8.8f,-1.9f),Vector3.left,5.6f),(new Vector3(34.67f,8.8f,.5f),Vector3.left,6.7f),
                (new Vector3(34.67f,8.8f,2.0f),Vector3.left,4.6f),(new Vector3(77,8.8f,-18.48f),Vector3.back,6.5f)})
                Vine(ivy,vine++,spec.Item1,spec.Item2,spec.Item3,leafMesh,leaf,stem);
            index=0;foreach(var f in walls.Where(f=>f.name.StartsWith("Wall_Stone_Normal")))Moss(ivy,f.GetComponent<Renderer>(),index++,moss);
            var plants=Child(decor,"Grounded_Plants_And_Rocks");const string pdir="Assets/Prefabs/Locations/Decoration/";
            Prop(plants,pdir+"Hilly_Prop_Tree_Oak_4.prefab","Outer_Oak",new Vector3(96,0,24),11.5f,25);
            Prop(plants,pdir+"Hilly_Prop_Bush_2.prefab","North_Bush",new Vector3(55,0,36),1.7f,90);
            Prop(plants,pdir+"Hilly_Prop_Bush_3.prefab","East_Bush",new Vector3(90,0,3),1.5f,25);
            Prop(plants,pdir+"Hilly_Prop_Bush_2.prefab","West_Bush",new Vector3(30,0,4),1.35f,120);
            Prop(plants,"Assets/Import/3D Cartoon Village/Prefabs/Flower.prefab","West_Flowers",new Vector3(28,0,-5),2.2f,20);
            Prop(plants,"Assets/Import/3D Cartoon Village/Prefabs/Flower.prefab","South_Flowers",new Vector3(72,0,-24),2.1f,70);
            foreach(var p in new[]{new Vector3(92,0,29),new Vector3(91,0,31),new Vector3(29,0,-7),new Vector3(74,0,-24)})
                Prop(plants,pdir+"Shared_Prop_Boulder_3.prefab","Edge_Rock_"+p.x+"_"+p.z,p,.6f,35);
            // Keep all five trap behaviours; fix only the copied Point4's external spawn-transform reference.
            var last=root.transform.Find("Trap/spawnPoints/Point4");var trap=new SerializedObject(last.GetComponent<DungeonTrap>());
            trap.FindProperty("_spawnPos").objectReferenceValue=last;trap.ApplyModifiedPropertiesWithoutUndo();
            var anchors=Child(root.transform,"Workshop_Walk_Anchors");
            Child(anchors,"Start").position=new Vector3(26,.02f,24.9f);
            var pts=new[]{new Vector3(43,.185f,27),new Vector3(58,.185f,26),new Vector3(68,.185f,24),new Vector3(79,.185f,12),new Vector3(80,.185f,-13)};
            for(int i=0;i<pts.Length;i++)Child(anchors,"Item"+(i+1).ToString("00")).position=pts[i];
            var surface=root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();surface.BuildNavMesh();
            if(surface.navMeshData)AssetDatabase.CreateAsset(surface.navMeshData,Folder+"NavMesh-Sklep1-Workshop.asset");
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new Exception("Cannot save workshop prefab");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var workshop=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(workshop);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),workshop);
        var sun=new GameObject("Workshop_Sun");SceneManager.MoveGameObjectToScene(sun,workshop);sun.transform.rotation=Quaternion.Euler(48,-32,0);
        var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.95f;light.shadows=LightShadows.Soft;
        EditorSceneManager.SaveScene(workshop,ScenePath);AssetDatabase.SaveAssets();
        Polish();return "Created Sklep1 overgrown ruins: original maze and five trap components retained, four signal towers, chipped walls, ivy/moss, checker floor, grounded surroundings. Point4 spawn position made local in copy.";
    }
}
