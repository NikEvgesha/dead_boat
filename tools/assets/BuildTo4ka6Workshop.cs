// Unity MCP script-execute: BuildTo4ka6Workshop.Build.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BuildTo4ka6Workshop
{
    const string Folder="Assets/Prefabs/Workshop/To4ka6/";
    const string Prefab=Folder+"To4ka6_OvergrownCourt.prefab";
    const string ScenePath="Assets/Scenes/To4ka6Workshop.unity";
    const float Top=6.4f;
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
        if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;RefreshMesh(mesh);}
        else AssetDatabase.CreateAsset(mesh,path);
        var g=Child(parent,name).gameObject;
        g.AddComponent<MeshFilter>().sharedMesh=mesh;
        g.AddComponent<MeshRenderer>().sharedMaterials=materials;
        if(solid)g.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void RefreshMesh(Mesh mesh)
    {
        // CopySerialized changes CPU data but can leave old GPU topology visible in Unity.
        var vertices=mesh.vertices;var triangles=mesh.triangles;var normals=mesh.normals;var uv=mesh.uv;
        mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;
        if(normals.Length==vertices.Length)mesh.normals=normals;else mesh.RecalculateNormals();
        if(uv.Length==vertices.Length)mesh.uv=uv;
        mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
    }
    public static string RefreshWorkshopMeshes()
    {
        var meshes=AssetDatabase.FindAssets("t:Mesh",new[]{Folder.TrimEnd('/')})
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).Where(m=>m).ToArray();
        foreach(var mesh in meshes)RefreshMesh(mesh);
        AssetDatabase.SaveAssets();
        foreach(var mesh in meshes)AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(mesh),ImportAssetOptions.ForceUpdate);
        return "Refreshed and reimported workshop GPU buffers for "+meshes.Length+" meshes.";
    }
    static Mesh TransformMesh(MeshRenderer r,Func<Vector3,Vector3> map)
    {
        var mesh=Object.Instantiate(r.GetComponent<MeshFilter>().sharedMesh);
        mesh.vertices=mesh.vertices.Select(p=>map(r.transform.TransformPoint(p))).ToArray();
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Polygon(Transform parent,string name,Vector2[] outline,float low,float high,Material mat)
    {
        MeshObject(parent,name,PolygonMesh(name,outline,low,high),new[]{mat},true);
    }
    static Mesh PolygonMesh(string name,Vector2[] outline,float low,float high)
    {
        var top=outline.Select(p=>new Vector3(p.x,high,p.y)).ToArray();
        float signed=0;for(int i=0;i<top.Length;i++)signed+=Vector3.Cross(top[i],top[(i+1)%top.Length]).y;
        if(signed<0)Array.Reverse(top);
        var bottom=top.Select(p=>new Vector3(p.x,low,p.z)).ToArray();
        var vertices=new List<Vector3>();var triangles=new List<int>();
        void Face(Vector3[] face)
        {
            int first=vertices.Count;vertices.AddRange(face);
            var normal=Vector3.zero;
            for(int i=0;i<face.Length;i++)normal+=Vector3.Cross(face[i],face[(i+1)%face.Length]);
            normal.Normalize();var remaining=Enumerable.Range(0,face.Length).ToList();
            bool Inside(Vector3 p,Vector3 a,Vector3 b,Vector3 c)=>
                Vector3.Dot(Vector3.Cross(b-a,p-a),normal)>=-1e-7f&&
                Vector3.Dot(Vector3.Cross(c-b,p-b),normal)>=-1e-7f&&
                Vector3.Dot(Vector3.Cross(a-c,p-c),normal)>=-1e-7f;
            while(remaining.Count>3)
            {
                bool found=false;
                for(int j=0;j<remaining.Count;j++)
                {
                    int a=remaining[(j+remaining.Count-1)%remaining.Count],b=remaining[j],c=remaining[(j+1)%remaining.Count];
                    if(Vector3.Dot(Vector3.Cross(face[b]-face[a],face[c]-face[b]),normal)<1e-8f)continue;
                    if(remaining.Any(k=>k!=a&&k!=b&&k!=c&&Inside(face[k],face[a],face[b],face[c])))continue;
                    triangles.AddRange(new[]{first+a,first+b,first+c});remaining.RemoveAt(j);found=true;break;
                }
                if(!found)throw new InvalidOperationException("Cannot triangulate polygon "+name);
            }
            triangles.AddRange(remaining.Select(i=>first+i));
        }
        Face(top);Face(bottom.Reverse().ToArray());
        for(int i=0;i<top.Length;i++){int j=(i+1)%top.Length;Face(new[]{top[j],top[i],bottom[i],bottom[j]});}
        var mesh=new Mesh{name=name,vertices=vertices.ToArray(),triangles=triangles.ToArray()};
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
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
    static void AddFurnitureColliders(GameObject root)
    {
        foreach(var filter in root.transform.Find("Furniture").GetComponentsInChildren<MeshFilter>())
        {
            if(!filter.sharedMesh||filter.GetComponent<Collider>())continue;
            filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;filter.gameObject.layer=6;
        }
    }
    static float LootSurface(GameObject root,Vector3 p)=>root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger)
        .Select(c=>c.Raycast(new Ray(new Vector3(p.x,5,p.z),Vector3.down),out var h,6)&&h.normal.y>.65f&&h.point.y<2
            ?h.point.y:float.NegativeInfinity).Max();
    public static string RepairLootSupports()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            AddFurnitureColliders(root);Physics.SyncTransforms();
            foreach(var anchor in root.transform.Find("Workshop_Walk_Anchors").Cast<Transform>().Where(t=>t.name.StartsWith("Item")))
            {var p=anchor.position;p.y=LootSurface(root,p)+.008f;anchor.position=p;}
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Cannot save loot supports");
            AssetDatabase.SaveAssets();return "Furniture collider surfaces and five pickup bases saved.";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab))throw new InvalidOperationException("Workshop already exists; inspect first");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","To4ka6");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("To4ka6_OvergrownCourt");SceneManager.MoveGameObjectToScene(root,preview);
            var stone=Color("Court_Stone","#B1B8C0");var lighter=Color("Pale_Stone","#C4CAD0");
            var shade=Color("Stone_Courses","#A3ABB4");var iron=Color("Dark_Iron_Grid","#343A40");
            var sand=Color("Court_Sand","#C8B18B");var leaf=Color("Ivy_Green","#629441");var stem=Color("Ivy_Stem","#4D6333");
            Box(root.transform,"Workshop_Ground",new Vector3(0,-.5f,0),new Vector3(55,1,55),sand);
            var court=Child(root.transform,"Court");
            for(int course=0;course<6;course++)
            {
                float y=(course+.5f)*.52f;var material=course%2==0?stone:shade;
                Box(court,"Left_Course_"+course,new Vector3(-4.2f,y,0),new Vector3(.52f,.52f,8.4f),material);
                Box(court,"Right_Course_"+course,new Vector3(4.2f,y,0),new Vector3(.52f,.52f,8.4f),material);
                Box(court,"Back_Course_"+course,new Vector3(0,y,4.2f),new Vector3(8.4f,.52f,.52f),material);
            }
            int corner=0;
            foreach(float x in new[]{-4.2f,4.2f})foreach(float z in new[]{-4.2f,4.2f})
            {
                Box(court,"Corner_Pier_"+corner,new Vector3(x,1.78f,z),new Vector3(.68f,3.56f,.68f),stone);
                Box(court,"Corner_Coping_"+corner,new Vector3(x,3.65f,z),new Vector3(.83f,.18f,.83f),lighter);corner++;
            }
            var front=Child(court,"Broken_Front_Wall");
            Box(front,"Left_High",new Vector3(-3.6f,.675f,-4.2f),new Vector3(1.2f,1.35f,.55f),stone);
            Box(front,"Left_Middle",new Vector3(-2.6f,.475f,-4.2f),new Vector3(.8f,.95f,.55f),stone);
            Box(front,"Left_Low",new Vector3(-1.9f,.30f,-4.2f),new Vector3(.6f,.60f,.55f),stone);
            Box(front,"Right_Low",new Vector3(2.2f,.34f,-4.2f),new Vector3(1.2f,.68f,.55f),stone);
            Box(front,"Right_High",new Vector3(3.5f,.53f,-4.2f),new Vector3(1.4f,1.06f,.55f),stone);
            var grid=Child(court,"Overhead_Iron_Grid");
            Box(grid,"Front_Rim",new Vector3(0,3.30f,-4.2f),new Vector3(8.4f,.19f,.19f),iron);
            Box(grid,"Back_Rim",new Vector3(0,3.30f,4.2f),new Vector3(8.4f,.19f,.19f),iron);
            Box(grid,"Left_Rim",new Vector3(-4.2f,3.30f,0),new Vector3(.19f,.19f,8.2f),iron);
            Box(grid,"Right_Rim",new Vector3(4.2f,3.30f,0),new Vector3(.19f,.19f,8.2f),iron);
            var lines=Enumerable.Range(0,6).Select(i=>-3.5f+i*1.4f).ToArray();
            foreach(float z in lines)
            {
                float end=z< -3?2.02f:4.105f;
                Box(grid,"Cross_Beam_"+z,new Vector3((-4.105f+end)*.5f,3.30f,z),new Vector3(end+4.105f,.19f,.18f),iron);
            }
            foreach(float x in lines)
            {
                float from=-4.105f;
                for(int i=0;i<=lines.Length;i++)
                {
                    float to=i==lines.Length?4.105f:lines[i]-.09f;
                    if(!(x>2.0f&&to< -2.0f))Box(grid,$"Long_Beam_{x}_{i}",new Vector3(x,3.30f,(from+to)*.5f),new Vector3(.18f,.19f,to-from),iron);
                    if(i<lines.Length)from=lines[i]+.09f;
                }
            }
            var ivy=Child(court,"Wall_Ivy");
            for(int vine=0;vine<3;vine++)
            {
                bool right=vine==2;float height=vine==0?1.65f:2.25f;
                var p=right?new Vector3(3.91f,0,2.7f):new Vector3(vine==0?-1.0f:2.9f,0,3.91f);
                Box(ivy,"Attached_Stem_"+vine,p+Vector3.up*(3.0f-height*.5f),new Vector3(.035f,height,.035f),stem);
                int count=vine==0?5:7;
                for(int i=0;i<count;i++)
                {
                    var pos=p+Vector3.up*(3.02f-i*.31f)+(right?new Vector3(-.05f,0,(i%2==0?.13f:-.13f)):new Vector3(i%2==0?.13f:-.13f,0,-.05f));
                    Box(ivy,$"Attached_Leaf_{vine}_{i}",pos,new Vector3(.32f,.34f,.07f),leaf,right?90:0);
                    ivy.Find($"Attached_Leaf_{vine}_{i}").Rotate(0,0,i%2==0?28:-28,Space.Self);
                }
            }
            var props=Child(root.transform,"Grounded_Plants_And_Rocks");
            const string decor="Assets/Prefabs/Locations/Decoration/";
            Prop(props,decor+"Hilly_Prop_Tree_Oak_4.prefab","Courtyard_Oak",new Vector3(8.0f,0,5.5f),5.8f,30);
            Prop(props,decor+"Hilly_Prop_Bush_2.prefab","West_Bush",new Vector3(-6.8f,0,-4.4f),1.4f,25);
            Prop(props,decor+"Hilly_Prop_Bush_3.prefab","Rear_Bush",new Vector3(-6.2f,0,5.8f),1.10f,110);
            Prop(props,decor+"Shared_Prop_Boulder_3.prefab","Front_Rock",new Vector3(6.1f,0,-5.0f),.5f,40);
            var furniture=Child(root.transform,"Furniture");
            Prop(furniture,"Assets/Import/3D Cartoon Village/Prefabs/Box.prefab","Back_Crate",new Vector3(2.5f,0,2.65f),1.05f,5);
            Prop(furniture,"Assets/Import/3D Cartoon Village/Prefabs/Bench.prefab","Left_Bench",new Vector3(-2.55f,0,.7f),.72f,90);
            Box(furniture,"Stone_Plinth",new Vector3(-1.7f,.44f,2.55f),new Vector3(1.0f,.88f,1.0f),lighter);
            Box(furniture,"Side_Ledge",new Vector3(3.43f,.88f,-.8f),new Vector3(.9f,.20f,1.15f),stone);
            Box(furniture,"Ledge_Support",new Vector3(3.7f,.38f,-.8f),new Vector3(.32f,.76f,.70f),stone);
            for(int i=0;i<4;i++)
            {
                float x=i%2==0?-.18f:.20f,z=-7.8f+i*.90f;
                Polygon(root.transform,"Path_Stone_"+i,new[]{new Vector2(x-.65f,z-.30f),new Vector2(x+.63f,z-.31f),new Vector2(x+.7f,z+.23f),new Vector2(x-.45f,z+.32f)},-.025f,.055f,lighter);
            }
            float Surface(Vector3 p)
            {
                return root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger)
                    .Select(c=>c.Raycast(new Ray(p+Vector3.up*5,Vector3.down),out var h,6)&&h.normal.y>.65f&&h.point.y<2.0f?h.point.y:float.NegativeInfinity).Max();
            }
            AddFurnitureColliders(root);Physics.SyncTransforms();var anchors=Child(root.transform,"Workshop_Walk_Anchors");
            void Anchor(string n,Vector3 p){Child(anchors,n).localPosition=p;}
            Anchor("Start",new Vector3(0,.05f,-13));
            var points=new[]{new Vector3(1.15f,0,-2.7f),new Vector3(-1.7f,0,2.55f),new Vector3(2.5f,0,2.65f),new Vector3(3.43f,0,-.8f),new Vector3(-2.55f,0,.7f)};
            for(int i=0;i<points.Length;i++){var p=points[i];p.y=Surface(p)+.008f;if(!float.IsFinite(p.y))throw new InvalidOperationException("Missing loot support");Anchor($"Item{i+1:00}",p);}
            foreach(var c in root.GetComponentsInChildren<Collider>()){c.gameObject.layer=6;c.gameObject.tag="Untagged";}
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Cannot save To4ka6 workshop");
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
        var sun=new GameObject("Workshop_Sun");SceneManager.MoveGameObjectToScene(sun,scene);sun.transform.rotation=Quaternion.Euler(45,-30,0);
        var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.9f;light.shadows=LightShadows.Soft;
        if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Cannot save workshop scene");
        AssetDatabase.SaveAssets();return "To4ka6 courtyard saved without flowers, with 3.2 m entrance and five supported pickup anchors.";
    }
}
