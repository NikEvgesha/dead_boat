// Unity MCP script-execute: BuildBoshka1Workshop.Build.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BuildBoshka1Workshop
{
    const string Folder = "Assets/Prefabs/Workshop/Boshka1/";
    const string PrefabPath = Folder + "Boshka1_StoneCourt.prefab";
    const string ScenePath = "Assets/Scenes/Boshka1Workshop.unity";
    const string SourcePath = "Assets/Prefabs/Locations/visual/boshka.prefab";
    const string Rocks = "Assets/Prefabs/Locations/Decoration/Shared_Prop_Boulder_";
    const float Begin = -18, End = -231, DeckEnd = -318;
    const int Steps = 26;
    const float Rise = .17f, CourtY = .04f, Inner = 5.3f, Outer = 8.75f, DeckInner = 3.45f;
    const float Top = CourtY + Steps * Rise;
    const int EntrySteps = 6;
    const float RimY = CourtY + EntrySteps * Rise;
    const float EntryLeft = -6.4f, EntryRight = -.6f;
    const float EntryTopZ = -15.6f, EntryBottomZ = -12f;

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Boshka 1 workshop requires Edit Mode.");
        if (SceneManager.GetSceneByPath(ScenePath).isDirty)
            throw new InvalidOperationException("Save Boshka 1 workshop before rebuilding.");
        if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Prefabs/Workshop", "Boshka1");

        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Boshka1_StoneCourt");
            SceneManager.MoveGameObjectToScene(root, preview);
            var sand = Paint("Court_Sand", "#D6A369");
            var courtyard = Paint("Court_Fine_Sand", "#ECC592");
            var ochre = Paint("Niche_Sandstone", "#CC985A");
            var cap = Paint("Niche_Edges", "#E9B77C");
            var step = Paint("Stairs_Slate", "#858990");
            var deck = Paint("Deck_Slate", "#92979D");
            var coping = Paint("Parapet_Coping", "#A5A6A9");
            var rock = Paint("Loose_Sandstone", "#C6AA85");
            var grass = Paint("Dry_Grass", "#85834D");
            var cutSand = Paint("Court_Cut_Sand", "#C58F55");

            var court = Child(root.transform, "Sandstone_Court");
            AddSunkenCourt(court,sand,courtyard,cutSand,cap);

            AddSculpture(root.transform);

            var stair = new SolidMesh();
            for (int i = 0; i < Steps; i++)
            {
                float a = Mathf.Lerp(Begin,End,i/(float)Steps);
                float b = Mathf.Lerp(Begin,End,(i+1f)/Steps);
                stair.Prism(Wedge(a,b,Inner,Outer), -.035f, CourtY + Rise*(i+1));
            }
            stair.Make(root.transform,"Curved_Stair_26_Risers",step,true);

            var landing = new SolidMesh();
            var parapet = new SolidMesh();
            var copingMesh = new SolidMesh();
            const int segments = 10;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.Lerp(End,DeckEnd,i/(float)segments);
                float b = Mathf.Lerp(End,DeckEnd,(i+1f)/segments);
                landing.Prism(Wedge(a,b,DeckInner,Outer), Top-.5f, Top);
                parapet.Prism(Wedge(a,b,Outer-.46f,Outer),Top,Top+.60f);
                copingMesh.Prism(Wedge(a,b,Outer-.53f,Outer+.06f),Top+.60f,Top+.72f);
                if (i%2 == 1)
                {
                    parapet.Prism(Wedge(a+2.15f,a-2.15f,Outer-.60f,Outer+.10f),Top+.01f,Top+.94f);
                    copingMesh.Prism(Wedge(a+2.35f,a-2.35f,Outer-.66f,Outer+.16f),Top+.94f,Top+1.04f);
                }
            }
            parapet.Prism(Wedge(DeckEnd+3.5f,DeckEnd,DeckInner,Outer),Top,Top+.65f);
            copingMesh.Prism(Wedge(DeckEnd+3.7f,DeckEnd-.18f,DeckInner-.05f,Outer+.06f),Top+.65f,Top+.78f);
            landing.Make(root.transform,"Upper_Landing",deck,true);
            parapet.Make(root.transform,"Upper_Parapet",step,true);
            copingMesh.Make(root.transform,"Parapet_Caps",coping,true);

            // Supports sit under the open landing and outside the central pedestal.
            foreach (float a in new[]{-252f,-283f,-308f})
                Box(root.transform,"Landing_Support",Polar(a,6.55f,Top*.5f-.12f),
                    new Vector3(1.18f,Top-.17f,1.26f),step,-a);

            // Broken low walls leave the foreground and both sides of the staircase open.
            Wall(court,new Vector2(-14,-6),new Vector2(-15.1f,3.5f),3,ochre,cap);
            Wall(court,new Vector2(-14.9f,6),new Vector2(-10.8f,12.1f),2,ochre,cap);
            Wall(court,new Vector2(-8.8f,12.9f),new Vector2(6.6f,13.8f),5,ochre,cap);
            Wall(court,new Vector2(9.2f,13),new Vector2(14.3f,9),2,ochre,cap);
            Wall(court,new Vector2(15,6.6f),new Vector2(15,-5),4,ochre,cap);
            Niche(court,-13,4,90,ochre,cap);
            Niche(court,13.35f,4,-90,ochre,cap);

            var props = Child(court,"Grounded_Props");
            const string jug = "Assets/Import/Dungeon_Deco_Set-Proto_Series/Prefabs/pot_3.prefab";
            const string pot = "Assets/Import/Dungeon_Deco_Set-Proto_Series/Prefabs/pot_2.prefab";
            Place(props,jug,"Clay_Jar",new Vector3(-7.0f,CourtY,-9.15f),.94f,16,null);
            Place(props,pot,"Niche_Pot",new Vector3(12.98f,.94f,3.85f),.49f,24,null);
            Place(props,Rocks+"2.prefab","West_Boulder",new Vector3(-10.8f,CourtY,-7.4f),.91f,30,rock);
            Place(props,Rocks+"6.prefab","West_Stone",new Vector3(-11.7f,CourtY,-7.2f),.35f,68,rock);
            Place(props,Rocks+"3.prefab","East_Boulder",new Vector3(10.8f,CourtY,-6.1f),.8f,57,rock);
            Place(props,Rocks+"4.prefab","East_Stone",new Vector3(11.6f,CourtY,-6.8f),.29f,120,rock);
            Place(props,Rocks+"3.prefab","Outside_Stone",new Vector3(18.7f,RimY,3.7f),.61f,72,rock);
            Place(props,Rocks+"5.prefab","Back_Stone",new Vector3(-6.8f,CourtY,11.1f),.58f,135,rock);
            for (int i=0;i<6;i++)
            {
                var tile = Child(props, "Approach_Flagstone_"+(i+1));
                tile.localPosition = i<2
                    ? new Vector3(-3.35f,CourtY,-9.55f-i*1.43f)
                    : new Vector3(-3.55f-(i-2)*.25f,RimY,-16.55f-(i-2)*1.46f);
                tile.localRotation = Quaternion.Euler(0,i%2==0?-5:7,0);
                var flagstone = new SolidMesh();
                flagstone.Prism(new[]{new Vector2(-1.45f,-.31f),new Vector2(-1.18f,-.60f),
                    new Vector2(.95f,-.57f),new Vector2(1.48f,-.21f),new Vector2(1.35f,.31f),
                    new Vector2(.93f,.53f),new Vector2(-1.14f,.51f)},-.025f,.105f);
                flagstone.Make(tile,"Slate",coping,true);
            }
            Tuft(props,new Vector3(-10.8f,CourtY,-8.6f),.82f,grass);
            Tuft(props,new Vector3(9.1f,CourtY,-7.3f),.77f,grass);
            Tuft(props,new Vector3(-12.2f,CourtY,8.4f),.90f,grass);
            Tuft(props,new Vector3(18.5f,RimY,1.9f),.67f,grass);
            Tuft(props,new Vector3(3.2f,CourtY,11.8f),.76f,grass);

            var markers = Child(root.transform,"Workshop_Walk_Anchors");
            Marker(markers,"Start",new Vector3(-5.2f,RimY+.015f,-22.0f),13);
            Marker(markers,"Item01",new Vector3(-6.4f,CourtY+.008f,-8.85f));
            Marker(markers,"Item02",new Vector3(-12.60f,.948f,4));
            Marker(markers,"Item03",new Vector3(8.1f,CourtY+.008f,-6.6f));
            Marker(markers,"Item04",Polar(-265,6.65f,Top+.008f));
            Marker(markers,"Item05",Polar(-304,6.65f,Top+.008f));

            if (!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))
                throw new InvalidOperationException("Could not save Boshka 1 workshop prefab.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }

        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        if (!scene.GetRootGameObjects().Any(g=>g.name=="Boshka1_StoneCourt"))
        {
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            var sun = new GameObject("Workshop_Sun");
            SceneManager.MoveGameObjectToScene(sun,scene);
            sun.transform.rotation = Quaternion.Euler(48,-32,0);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }
        if (!EditorSceneManager.SaveScene(scene,ScenePath))
            throw new InvalidOperationException("Could not save Boshka 1 workshop scene.");
        AssetDatabase.SaveAssets();
        return $"{ScenePath}: court {RimY-CourtY:F2} m below sand, {EntrySteps} entrance steps, {Steps} upper steps x {Rise:F2} m.";
    }

    static void AddSunkenCourt(Transform court,Material sand,Material floor,Material cutSand,Material step)
    {
        var bottom = new[]{new Vector2(-14,-9),new Vector2(-16,-3),new Vector2(-15,9),
            new Vector2(-10,13),new Vector2(6,14),new Vector2(13,11),new Vector2(16,4),
            new Vector2(15,-7),new Vector2(9,EntryBottomZ),new Vector2(EntryRight,EntryBottomZ),
            new Vector2(EntryLeft,EntryBottomZ),new Vector2(-8,EntryBottomZ)};
        var rim = new[]{new Vector2(-15,-9.7f),new Vector2(-17.1f,-3.2f),new Vector2(-16.1f,9.5f),
            new Vector2(-10.7f,14.0f),new Vector2(6.4f,15.2f),new Vector2(13.9f,11.8f),new Vector2(17.2f,4.2f),
            new Vector2(16,-7.7f),new Vector2(10,EntryTopZ),new Vector2(EntryRight,EntryTopZ),
            new Vector2(EntryLeft,EntryTopZ),new Vector2(-8.8f,EntryTopZ)};
        // The unbroken sand surface stops at the rim, so it cannot cover the recess.
        var sandRing = new SolidMesh();
        var slopes = new SolidMesh();
        for(int i=0;i<rim.Length;i++)
        {
            int j=(i+1)%rim.Length;
            sandRing.Surface(rim[i],rim[j],Distant(rim[j]),Distant(rim[i]),RimY,RimY);
            if(i!=9)
                slopes.Surface(bottom[i],bottom[j],rim[j],rim[i],CourtY,RimY);
        }
        sandRing.Make(court,"Surrounding_Sand",sand,true);
        var apron = new SolidMesh();
        // The intermediate front points serve the stair opening, but the flat
        // apron itself needs just one straight edge across that opening.
        apron.Prism(bottom.Where((_,i)=>i!=9&&i!=10).ToArray(),-.025f,CourtY);
        apron.Make(court,"Court_Apron",floor,true);
        var stairs = new SolidMesh();
        stairs.CutFace(new Vector2(EntryLeft,EntryTopZ),new Vector2(EntryRight,EntryTopZ),RimY-Rise,RimY);
        for(int i=0;i<EntrySteps;i++)
        {
            float a=Mathf.Lerp(EntryTopZ,EntryBottomZ,i/(float)EntrySteps);
            float b=Mathf.Lerp(EntryTopZ,EntryBottomZ,(i+1f)/EntrySteps);
            float tread=RimY-(i+1)*Rise;
            stairs.Prism(new[]{new Vector2(EntryLeft,a),new Vector2(EntryRight,a),
                new Vector2(EntryRight,b),new Vector2(EntryLeft,b)},-.025f,tread);
            slopes.StairCheek(EntryLeft,a,b,tread,tread+Rise,true);
            slopes.StairCheek(EntryRight,a,b,tread,tread+Rise,false);
        }
        slopes.Make(court,"Cut_Sand_Banks",cutSand,true);
        stairs.Make(court,"Court_Entry_6_Risers",step,true);
    }

    static Vector2 Distant(Vector2 p) =>
        p*Mathf.Min(40f/Mathf.Abs(p.x),38f/Mathf.Abs(p.y));

    static void AddSculpture(Transform parent)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        var original = source.transform;
        var sculpture = Child(parent,"Original_Boshka_Sculpture");
        sculpture.localRotation = Quaternion.Euler(0,-90,0);
        var skull = CopyMesh(original.Find("StatueD"),sculpture,"Original_Horned_Skull",true);
        var baseModel = CopyMesh(original.Find("TowerD"),sculpture,"Original_Stone_Plinth",true);
        skull.localScale *= .55f;
        var scale = baseModel.localScale;
        baseModel.localScale = new Vector3(scale.x*.57f,scale.y*.27f,scale.z*.57f);
        CenterAt(baseModel,Vector3.zero,CourtY-.035f);
        CenterAt(skull,Vector3.zero,Top-.08f);

        // The plinth's highest vertices are decorative spikes. Seat the skull
        // on the central stone surface under its visible footprint instead.
        var vertices=skull.GetComponent<MeshFilter>().sharedMesh.vertices.Select(skull.TransformPoint).ToArray();
        float bottom=vertices.Min(p=>p.y);
        var foot=vertices.Where(p=>p.y<bottom+.025f).Distinct().ToArray();
        var center=foot.Aggregate(Vector3.zero,(sum,p)=>sum+p)/foot.Length;
        Physics.SyncTransforms();
        var plinth=baseModel.GetComponent<MeshCollider>();
        if(!plinth.Raycast(new Ray(center+Vector3.up,Vector3.down),out var contact,5f))
            throw new InvalidOperationException("The skull has no plinth surface below its base.");
        skull.position+=Vector3.up*(contact.point.y-bottom-.018f);
    }

    static Transform CopyMesh(Transform source,Transform target,string name,bool collision)
    {
        var t = Child(target,name);
        t.localRotation = source.localRotation;
        t.localScale = source.localScale;
        var mesh = source.GetComponent<MeshFilter>().sharedMesh;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
        if (collision) t.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        return t;
    }

    static void CenterAt(Transform t,Vector3 target,float y)
    {
        var bounds = t.GetComponent<Renderer>().bounds;
        t.position += new Vector3(target.x-bounds.center.x,y-bounds.min.y,target.z-bounds.center.z);
    }

    static void Wall(Transform root,Vector2 from,Vector2 to,int count,Material sides,Material cap)
    {
        var distance = (to-from).magnitude;
        var delta = (to-from).normalized;
        float yaw = Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg;
        for (int i=0;i<count;i++)
        {
            var p = Vector2.Lerp(from,to,(i+.5f)/count);
            float height = i%3==0?1.24f:.92f;
            Box(root,"Broken_Sandstone_Block",new Vector3(p.x,CourtY+height*.5f,p.y),
                new Vector3(.88f,height,distance/count-.14f),sides,yaw);
            Box(root,"Weathered_Wall_Cap",new Vector3(p.x,CourtY+height+.075f,p.y),
                new Vector3(1.0f,.15f,distance/count-.19f),cap,yaw);
        }
    }

    static void Niche(Transform parent,float x,float z,float yaw,Material wall,Material trim)
    {
        var t = Child(parent,"Sandstone_Offering_Niche");
        t.localPosition = new Vector3(x,CourtY,z);
        t.localRotation = Quaternion.Euler(0,yaw,0);
        Box(t,"Base",new Vector3(0,.39f,0),new Vector3(2.32f,.78f,1.15f),wall);
        Box(t,"Shelf",new Vector3(0,.84f,.05f),new Vector3(2.25f,.12f,1.18f),trim);
        Box(t,"Back",new Vector3(0,1.30f,-.40f),new Vector3(2.08f,1.1f,.32f),wall);
        foreach(float side in new[]{-1f,1f})
            Box(t,"Side",new Vector3(side*.94f,1.4f,-.03f),new Vector3(.4f,1.05f,1.02f),wall);
        Box(t,"Lintel",new Vector3(0,2.03f,-.05f),new Vector3(2.30f,.28f,1.1f),trim);
    }

    static void Place(Transform parent,string path,string name,Vector3 point,float height,float yaw,Material material)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!source) throw new InvalidOperationException("Missing workshop prop "+path);
        var prop = (GameObject)PrefabUtility.InstantiatePrefab(source,parent);
        prop.name = name;
        prop.transform.SetPositionAndRotation(new Vector3(point.x,0,point.z),Quaternion.Euler(0,yaw,0));
        var renderers = prop.GetComponentsInChildren<MeshRenderer>();
        var b = renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1)) b.Encapsulate(renderer.bounds);
        prop.transform.localScale *= height/b.size.y;
        var vertices = prop.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh)
            .SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
        float low = vertices.Min(p=>p.y);
        var feet = vertices.Where(p=>p.y<low+.025f).ToArray();
        var center = feet.Aggregate(Vector3.zero,(sum,p)=>sum+p)/feet.Length;
        prop.transform.position += new Vector3(point.x-center.x,point.y-low-.012f,point.z-center.z);
        if(material)
            foreach(var renderer in renderers)
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_=>material).ToArray();
    }

    static void Tuft(Transform parent,Vector3 position,float size,Material material)
    {
        var t = Child(parent,"Dry_Grass");
        t.localPosition = position-Vector3.up*.014f;
        t.localScale = Vector3.one*size;
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Workshop/Pond1/Reed_Clump.asset");
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    static Vector3 Polar(float angle,float radius,float y)
    {
        float r = angle*Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r)*radius,y,-Mathf.Cos(r)*radius);
    }

    static Vector2[] Wedge(float a,float b,float inner,float outer) =>
        new[]{Polar(a,inner,0),Polar(a,outer,0),Polar(b,outer,0),Polar(b,inner,0)}
            .Select(p=>new Vector2(p.x,p.z)).ToArray();

    static void Marker(Transform parent,string name,Vector3 pos,float yaw=0)
    {
        var marker = Child(parent,name);
        marker.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
    }

    static Transform Child(Transform parent,string name)
    {
        var child = new GameObject(name).transform;
        SceneManager.MoveGameObjectToScene(child.gameObject,parent.gameObject.scene);
        child.SetParent(parent,false);
        return child;
    }

    static void Box(Transform parent,string name,Vector3 p,Vector3 scale,Material material,float yaw=0)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = p;
        go.transform.localRotation = Quaternion.Euler(0,yaw,0);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
    }

    static Material Paint(string name,string hex)
    {
        var path = Folder+name+".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Standard")) {name=name};
            AssetDatabase.CreateAsset(m,path);
        }
        ColorUtility.TryParseHtmlString(hex,out var color);
        m.color = color;
        m.SetFloat("_Glossiness",.03f);
        EditorUtility.SetDirty(m);
        return m;
    }

    sealed class SolidMesh
    {
        readonly List<Vector3> vertices = new();
        readonly List<int> triangles = new();

        void Face(IEnumerable<Vector3> positions)
        {
            var p = positions.ToArray();
            int first = vertices.Count;
            vertices.AddRange(p);
            for(int i=1;i<p.Length-1;i++)
                triangles.AddRange(new[]{first,first+i,first+i+1});
        }

        public void Surface(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float nearY,float farY)
        {
            var points=new[]{new Vector3(a.x,nearY,a.y),new Vector3(b.x,nearY,b.y),
                new Vector3(c.x,farY,c.y),new Vector3(d.x,farY,d.y)};
            if(Vector3.Cross(points[1]-points[0],points[2]-points[0]).y<0) Array.Reverse(points);
            Face(points);
        }

        public void CutFace(Vector2 a,Vector2 b,float low,float high)
        {
            Face(new[]{new Vector3(a.x,high,a.y),new Vector3(a.x,low,a.y),
                new Vector3(b.x,low,b.y),new Vector3(b.x,high,b.y)});
        }

        public void StairCheek(float x,float front,float back,float tread,float bankAtFront,bool facesRight)
        {
            var points=new[]{new Vector3(x,tread,front),new Vector3(x,bankAtFront,front),new Vector3(x,tread,back)};
            if(!facesRight) Array.Reverse(points);
            Face(points);
        }

        public void Prism(Vector2[] outline,float low,float high)
        {
            var top = outline.Select(p=>new Vector3(p.x,high,p.y)).ToArray();
            if(Vector3.Cross(top[1]-top[0],top[2]-top[0]).y<0) Array.Reverse(top);
            var bottom = top.Select(v=>new Vector3(v.x,low,v.z)).ToArray();
            Face(top);
            Face(bottom.Reverse());
            for(int i=0;i<top.Length;i++)
            {
                int j=(i+1)%top.Length;
                Face(new[]{top[j],top[i],bottom[i],bottom[j]});
            }
        }

        public void Make(Transform parent,string name,Material material,bool solid)
        {
            var mesh = new Mesh {name=name};
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var path = Folder+name+".asset";
            var current = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (current)
            {
                EditorUtility.CopySerialized(mesh,current);
                EditorUtility.SetDirty(current);
                Object.DestroyImmediate(mesh);
                mesh=current;
            }
            else AssetDatabase.CreateAsset(mesh,path);
            var go = Child(parent,name).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if (solid) go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
