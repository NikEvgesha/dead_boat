// Unity MCP script-execute: BuildClocks1Workshop.Build.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BuildClocks1Workshop
{
    const string Folder="Assets/Prefabs/Workshop/Clocks1/";
    const string Prefab=Folder+"Clocks1_OvergrownCourt.prefab";
    const string ScenePath="Assets/Scenes/Clocks1Workshop.unity";
    const float Top=4.08f;
    static Transform Child(Transform parent,string name)
    {
        var t=new GameObject(name).transform;
        SceneManager.MoveGameObjectToScene(t.gameObject,parent.gameObject.scene);
        t.SetParent(parent,false);
        return t;
    }
    static Material Color(string name,string hex)
    {
        var path=Folder+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard")){name=name};AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString(hex,out var c);m.color=c;m.SetFloat("_Glossiness",.02f);EditorUtility.SetDirty(m);
        return m;
    }
    static void Box(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,float yaw=0)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(g,parent.gameObject.scene);
        g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;
        g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static void MeshObject(Transform parent,string name,Mesh mesh,Material[] materials,bool solid)
    {
        if(name.StartsWith("Clock_Arch_Mesh_")||name.StartsWith("Smooth_"))
        {
            // Separate face vertices so perpendicular stone faces keep sharp normals.
            var source=mesh.vertices;var indices=mesh.triangles;
            var uv=mesh.uv;var vertices=indices.Select(i=>source[i]).ToArray();
            var faceUv=uv.Length==source.Length?indices.Select(i=>uv[i]).ToArray():null;
            mesh.Clear();mesh.vertices=vertices;
            if(faceUv!=null)mesh.uv=faceUv;
            mesh.triangles=Enumerable.Range(0,vertices.Length).ToArray();
            mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        var path=Folder+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
        else AssetDatabase.CreateAsset(mesh,path);
        var g=Child(parent,name).gameObject;
        g.AddComponent<MeshFilter>().sharedMesh=mesh;
        g.AddComponent<MeshRenderer>().sharedMaterials=materials;
        if(solid)g.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static Mesh TransformMesh(MeshRenderer r,Func<Vector3,Vector3> map)
    {
        var mesh=Object.Instantiate(r.GetComponent<MeshFilter>().sharedMesh);
        mesh.vertices=mesh.vertices.Select(p=>map(r.transform.TransformPoint(p))).ToArray();
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Polygon(Transform parent,string name,Vector2[] outline,float low,float high,Material mat)
    {
        var top=outline.Select(p=>new Vector3(p.x,high,p.y)).ToArray();
        if(Vector3.Cross(top[1]-top[0],top[2]-top[0]).y<0)Array.Reverse(top);
        var bottom=top.Select(p=>new Vector3(p.x,low,p.z)).ToArray();
        var vertices=new List<Vector3>();var triangles=new List<int>();
        void Face(Vector3[] face)
        {
            int first=vertices.Count;vertices.AddRange(face);
            for(int i=1;i<face.Length-1;i++)triangles.AddRange(new[]{first,first+i,first+i+1});
        }
        Face(top);Face(bottom.Reverse().ToArray());
        for(int i=0;i<top.Length;i++){int j=(i+1)%top.Length;Face(new[]{top[j],top[i],bottom[i],bottom[j]});}
        var mesh=new Mesh{name=name,vertices=vertices.ToArray(),triangles=triangles.ToArray()};
        mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject(parent,name,mesh,new[]{mat},true);
    }
    static void StairWall(Transform parent,string name,float x,float from,float to,float lowTop,float highTop,Material mat)
    {
        var mesh=new Mesh{name=name};
        mesh.vertices=new[]{new Vector3(x-.16f,-.02f,from),new Vector3(x-.16f,-.02f,to),
            new Vector3(x-.16f,highTop,to),new Vector3(x-.16f,lowTop,from),
            new Vector3(x+.16f,-.02f,from),new Vector3(x+.16f,-.02f,to),
            new Vector3(x+.16f,highTop,to),new Vector3(x+.16f,lowTop,from)};
        mesh.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4};
        var triangles=mesh.triangles;
        for(int i=0;i<triangles.Length;i+=3){int second=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=second;}
        mesh.triangles=triangles;
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        MeshObject(parent,name,mesh,new[]{mat},true);
    }
    static void Prop(Transform parent,string path,string name,Vector3 position,float height,float yaw,Material material=null)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!source)throw new InvalidOperationException("Missing prop "+path);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);g.name=name;
        g.transform.SetPositionAndRotation(new Vector3(position.x,0,position.z),Quaternion.Euler(0,yaw,0));
        var renderers=g.GetComponentsInChildren<MeshRenderer>();
        var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
        g.transform.localScale*=height/b.size.y;
        var vertices=g.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh)
            .SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
        float low=vertices.Min(p=>p.y);
        var feet=vertices.Where(p=>p.y<low+.02f).ToArray();
        var center=feet.Aggregate(Vector3.zero,(sum,p)=>sum+p)/feet.Length;
        g.transform.position+=new Vector3(position.x-center.x,position.y-low-.012f,position.z-center.z);
        if(path.EndsWith("/Flower.prefab",StringComparison.Ordinal))
        {
            foreach(var r in renderers)
            {
                var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;
                bool head=mesh&&new[]{"Mesh7","Mesh9","Mesh11","Mesh13","Mesh15"}.Contains(mesh.name);
                var paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/SharedVegetation/Flower_"+(head?"Petals_Violet":"Leaves_Green")+".mat");
                if(paint)r.sharedMaterials=r.sharedMaterials.Select(_=>paint).ToArray();
            }
        }
        else if(material)foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();
    }
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Clocks workshop requires Edit Mode");
        if(SceneManager.GetSceneByPath(ScenePath).isDirty)throw new InvalidOperationException("Save manual scene edits before rebuilding");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","Clocks1");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Clocks1_OvergrownCourt");
            SceneManager.MoveGameObjectToScene(root,preview);
            var stone=Color("Tower_Stone","#B5B1A7");
            var cap=Color("Tower_Coping","#C7C2B9");
            var sand=Color("Court_Sand","#D2AB78");
            var wall=Color("Weathered_Sandstone","#BF945D");
            var red=Color("Red_Flag","#993D32");
            red.shader=Shader.Find("Unlit/Color");
            red.color=new UnityEngine.Color(.60f,.19f,.15f);
            var wood=Color("Flag_Pole","#6F4E30");
            var violet=Color("Flower_Violet","#8959A4");
            var rock=Color("Warm_Rock","#B9AB96");
            Box(root.transform,"Workshop_Sand",new Vector3(0,-.5f,0),new Vector3(65,1,64),sand);
            var tower=Child(root.transform,"Tower");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/clocks_1.prefab");
            var visual=source.transform.Find("PointCenter (1)/To4ka2");
            var arc=visual.Find("Arc4").GetComponentInChildren<MeshRenderer>();
            for(int i=0;i<4;i++)
            {
                var t=Child(tower,"Stone_Arch_"+i);
                t.localPosition=Quaternion.Euler(0,90*i,0)*new Vector3(0,0,-3.70f);
                t.localRotation=Quaternion.Euler(0,90*i,0);
                var b=arc.bounds;
                var mesh=TransformMesh(arc,p=>new Vector3((p.x-b.center.x)*8f/b.size.x,(p.y-b.min.y)*3.86f/b.size.y,(p.z-b.center.z)*.80f/b.size.z));
                MeshObject(t,"Clock_Arch_Mesh_"+i,mesh,new[]{stone,stone},true);
            }
            Box(tower,"Terrace_Floor",new Vector3(0,Top-.11f,0),new Vector3(8.6f,.22f,8.6f),cap);
            Box(tower,"Clock_Support",new Vector3(0,Top+.5f,0),new Vector3(3.5f,1f,3.5f),stone);
            var clock=Child(tower,"Original_Clock_And_Red_Roof");
            var original=visual.Find("TownHall (3)");
            var parts=original.GetComponentsInChildren<MeshRenderer>().Where(r=>
                (PrefabUtility.GetCorrespondingObjectFromSource(r)?.enabled??r.enabled)&&r.bounds.min.y>27).ToArray();
            if(parts.Length<5)throw new InvalidOperationException("Original clock parts missing");
            var full=parts[0].bounds;foreach(var r in parts.Skip(1))full.Encapsulate(r.bounds);
            int part=0;
            foreach(var r in parts)
            {
                var mesh=TransformMesh(r,p=>(p-new Vector3(full.center.x,full.min.y,full.center.z))*.25f+Vector3.up*(Top+1f));
                MeshObject(clock,"Original_Clock_Part_"+part++,mesh,r.sharedMaterials,false);
            }
            // The west parapet leaves a clear two-metre opening at the stair landing.
            for(int side=0;side<4;side++)
            {
                var t=Child(tower,"Terrace_Parapet_"+side);t.localRotation=Quaternion.Euler(0,side*90,0);
                for(int i=0;i<6;i++)
                {
                    float x=-3.55f+i*1.42f;
                    if(side==1&&x>-2.9f&&x<-.6f)continue;
                    Box(t,"Low_Wall_"+i,new Vector3(x,Top+.33f,-4.05f),new Vector3(1.42f,.66f,.48f),stone);
                    Box(t,"Merlon_"+i,new Vector3(x,Top+.93f,-4.05f),new Vector3(.65f,.54f,.57f),cap);
                }
            }
            var stairs=Child(root.transform,"Low_Straight_Stair");
            for(int i=0;i<24;i++)
            {
                float a=-9.70f+i*.48f,b=a+.48f;
                Polygon(stairs,"Step_"+i,new[]{new Vector2(-6.85f,a),new Vector2(-4.25f,a),
                    new Vector2(-4.25f,b),new Vector2(-6.85f,b)},-.04f,(i+1)*.17f,stone);
            }
            StairWall(stairs,"Smooth_Outer_Parapet",-6.99f,-9.7f,1.82f,.75f,Top+.75f,cap);
            Box(stairs,"Upper_Outer_Parapet",new Vector3(-6.99f,Top*.5f+.365f,2.36f),new Vector3(.32f,Top+.75f,1.08f),cap);
            StairWall(stairs,"Smooth_Lower_Inner_Parapet",-4.12f,-9.7f,-1.06f,.75f,3.06f+.75f,cap);
            Box(stairs,"Upper_Stair_Landing",new Vector3(-5.55f,Top-.15f,2.36f),new Vector3(2.6f,.30f,1.08f),cap);
            Box(stairs,"Landing_To_Terrace",new Vector3(-3.925f,Top-.15f,1.75f),new Vector3(.65f,.30f,2.30f),cap);
            Box(stairs,"Landing_Back_Parapet",new Vector3(-5.15f,Top+.36f,3.02f),new Vector3(3.65f,.72f,.24f),stone);
            var walls=Child(root.transform,"Broken_Court_Walls");
            for(int i=0;i<6;i++)
            {
                if(i==2)continue;
                float h=i%2==0?1.7f:1.1f;
                Box(walls,"Rear_Block_"+i,new Vector3(-11.6f+i*3.4f,h*.5f,9.3f),new Vector3(3.15f,h,.75f),wall);
                Box(walls,"Rear_Cap_"+i,new Vector3(-11.6f+i*3.4f,h+.09f,9.3f),new Vector3(3.2f,.18f,.83f),cap);
            }
            Box(walls,"West_Remnant",new Vector3(-11.8f,.60f,5.7f),new Vector3(.8f,1.2f,4),wall);
            var props=Child(root.transform,"Grounded_Plants_And_Rocks");
            const string decor="Assets/Prefabs/Locations/Decoration/";
            Prop(props,decor+"Hilly_Prop_Tree_Oak_4.prefab","Courtyard_Oak",new Vector3(11.8f,0,4.3f),6.8f,30);
            Prop(props,decor+"Hilly_Prop_Bush_2.prefab","West_Bush",new Vector3(-10.2f,0,-5.9f),1.45f,25);
            Prop(props,decor+"Hilly_Prop_Bush_3.prefab","East_Bush",new Vector3(8.3f,0,-6.4f),1.35f,140);
            Prop(props,decor+"Shared_Prop_Boulder_2.prefab","East_Rock",new Vector3(10.6f,0,-7.5f),.76f,30,rock);
            Prop(props,decor+"Shared_Prop_Boulder_5.prefab","West_Rock",new Vector3(-12.4f,0,-6.5f),.65f,96,rock);
            Prop(props,decor+"Shared_Prop_Boulder_3.prefab","Rear_Rock",new Vector3(-4,0,8.2f),.50f,40,rock);
            for(int i=0;i<6;i++)
            {
                float sign=i<3?-1:1;
                Prop(props,"Assets/Import/3D Cartoon Village/Prefabs/Flower.prefab","Purple_Flowers_"+i,
                    new Vector3(sign*(7.8f+(i%3)*1.65f),0,-9.3f-(i%3)*.30f),.64f+(i%2)*.18f,35*i,violet);
            }
            for(int i=0;i<5;i++)
            {
                var p=new Vector2(-.2f+i*.14f,-9.7f+i*1.12f);
                Polygon(root.transform,"Path_Stone_"+i,new[]{p+new Vector2(-.75f,-.38f),p+new Vector2(.62f,-.44f),
                    p+new Vector2(.82f,.21f),p+new Vector2(.3f,.48f),p+new Vector2(-.69f,.35f)},-.03f,.085f,cap);
            }
            Box(tower,"Flag_Pole",new Vector3(3.65f,Top+1.65f,-3.65f),new Vector3(.13f,3.3f,.13f),wood);
            var flagMesh=new Mesh{name="Red_Court_Flag"};
            flagMesh.vertices=new[]{new Vector3(3.72f,Top+2.95f,-3.65f),new Vector3(5.30f,Top+2.75f,-3.54f),
                new Vector3(4.95f,Top+2.36f,-3.45f),new Vector3(5.30f,Top+1.96f,-3.54f),new Vector3(3.72f,Top+2.15f,-3.65f)};
            flagMesh.triangles=new[]{0,1,2,0,2,4,4,2,3,2,1,0,4,2,0,3,2,4};flagMesh.RecalculateNormals();flagMesh.RecalculateBounds();
            MeshObject(tower,"Red_Court_Flag",flagMesh,new[]{red},false);
            var anchors=Child(root.transform,"Workshop_Walk_Anchors");
            void Anchor(string name,Vector3 p,float yaw=0){var t=Child(anchors,name);t.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));}
            Anchor("Start",new Vector3(-2.8f,.02f,-23.5f),7);
            Anchor("Item01",new Vector3(1.7f,.008f,-5.9f));
            Anchor("Item02",new Vector3(6.3f,.008f,-4.0f));
            Anchor("Item03",new Vector3(-3.35f,.008f,-10.9f));
            Anchor("Item04",new Vector3(-2.8f,Top+.008f,1.8f));
            Anchor("Item05",new Vector3(2.85f,Top+.008f,2.8f));
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Clocks prefab save failed");
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)
            ?EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        if(!scene.GetRootGameObjects().Any(g=>g.name=="Clocks1_OvergrownCourt"))
        {
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
            var sun=new GameObject("Workshop_Sun");SceneManager.MoveGameObjectToScene(sun,scene);
            sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.9f;light.shadows=LightShadows.Soft;
        }
        if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Clocks scene save failed");
        AssetDatabase.SaveAssets();return "Clocks1Workshop saved. 24 risers of .17 m, terrace at 4.08 m, five pickup anchors.";
    }
}
