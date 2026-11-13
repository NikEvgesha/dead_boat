// Run with Unity MCP script-execute: className BuildPond1Workshop, methodName Build.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BuildPond1Workshop
{
    const string Dir = "Assets/Prefabs/Workshop/Pond1";
    const string PrefabPath = Dir + "/Pond1_ExpandedOasis.prefab";
    const string ScenePath = "Assets/Scenes/Pond1Workshop.unity";
    const string Props = "Assets/Prefabs/Locations/Decoration/";
    const float WaterY = -.22f;
    static readonly Vector2[] Shore =
    {
        new(-21, 2), new(-19,-5), new(-13,-10), new(-5,-12), new(5,-13),
        new(14,-11.8f), new(20,-8), new(22,-3), new(22,1), new(17,5),
        new(10,7), new(3,8.5f), new(-4,13.5f), new(-12,16), new(-18,13), new(-21,8)
    };

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Pond 1 workshop requires Edit Mode.");
        if (SceneManager.GetSceneByPath(ScenePath).isDirty)
            throw new InvalidOperationException("Save the existing Pond 1 workshop before rebuilding.");

        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets/Prefabs/Workshop", "Pond1");

        var preview = EditorSceneManager.NewPreviewScene();
        var report = new List<string>();
        try
        {
            var root = new GameObject("Pond1_ExpandedOasis");
            SceneManager.MoveGameObjectToScene(root, preview);
            var sand = Paint("Oasis_Sand", "#D6A66E");
            var bank = Paint("Oasis_Bank", "#EAC38F");
            var edge = Paint("Oasis_Wet_Sand", "#CDAD7A");
            var stone = Paint("Oasis_Sandstone", "#D8C29D");
            var green = Paint("Oasis_Reeds", "#648954");
            var aqua = Paint("Oasis_Shallow", "#65C6C4", true);
            var blue = Paint("Oasis_Water", "#398EB6", true);
            var teal = Paint("Oasis_Water_Transition", "#4FADBD", true);

            // Every ring has the same vertex correspondence: the terrain meets the water at Shore.
            Vector3[] waterline = Ring(WaterY, _ => 0);
            Vector3[] dry = Ring(.20f, p => p.y < -4 ? 3.4f : 1.5f);
            Vector3[] apron = Ring(.28f, p => p.y < -4 ? 5.5f : 3.4f);
            Vector3[] outside = Shore.Select(p =>
            {
                var scale = Mathf.Min(37 / Mathf.Abs(p.x), 30 / Mathf.Abs(p.y));
                return new Vector3(p.x * scale, .22f, p.y * scale);
            }).ToArray();
            Vector3[] shelf = Ring(-.43f, p => p.y < -4 ? -2.2f : -.95f);
            Vector3[] depth = Ring(-1.25f, _ => -6f);
            var terrain = Surface(root.transform, "Pond_Basin_And_Shore",
                new[] { outside, apron, dry, waterline, shelf, depth },
                new[] { sand, bank, bank, edge, edge, edge }, true, -1.25f);
            Surface(root.transform, "Pond_Water",
                new[] { waterline, Ring(WaterY, p => p.y < -4 ? -1.85f : -.55f),
                    Ring(WaterY, p => p.y < -4 ? -2.65f : -.9f) },
                new[] { aqua, teal, blue }, false, WaterY);
            var ground = terrain.GetComponent<MeshCollider>();
            Physics.SyncTransforms();

            var decor = Child(root.transform, "Oasis_Decoration");
            GameObject Palm(int id, float x, float z, float h, float yaw) =>
                Prop(decor, ground, Props + $"Beach_Prop_Tree_Palm_{id}.prefab",
                    $"Palm_{id}", x, z, h, yaw, null, report);
            void Rock(int model, float x, float z, float h, float yaw, string name) =>
                Prop(decor, ground, Props + $"Shared_Prop_Boulder_{model}.prefab",
                    name, x, z, h, yaw, stone, null);
            void Reed(string name, float x, float z, float yaw, float size)
            {
                var bed = Child(decor, name);
                bed.SetPositionAndRotation(new Vector3(x, Ground(ground,x,z)-.009f, z), Quaternion.Euler(0,yaw,0));
                bed.localScale = Vector3.one * size;
                bed.gameObject.AddComponent<MeshFilter>().sharedMesh = Reeds();
                bed.gameObject.AddComponent<MeshRenderer>().sharedMaterial = green;
                report.Add($"{name} root clearance {bed.position.y - Ground(ground,x,z):F3} m");
            }

            Palm(1,-12.5f,-15.4f,7.1f,30);
            Palm(2,5.8f,14.2f,8.2f,92);
            Palm(3,14.2f,10.7f,6.6f,215);
            Rock(2,-18.5f,13.9f,1.52f,15,"Far_Bank_Boulder");
            Rock(4,-19.6f,13.9f,.52f,73,"Far_Bank_Stone");
            Rock(1,-6.6f,14.25f,.80f,58,"North_Bank_Stone");
            Rock(4,20.8f,7.9f,.93f,117,"East_Bank_Boulder");
            Rock(6,22f,8f,.40f,207,"East_Bank_Stone");
            Rock(3,22.3f,-9.5f,1.07f,250,"South_Bank_Boulder");
            Rock(6,23.4f,-9.6f,.45f,180,"South_Bank_Stone");
            Rock(2,-22f,-16.4f,1.25f,80,"Arrival_Boulder");
            Rock(3,-23.2f,-16.1f,.49f,56,"Arrival_Stone");
            Rock(6,-17.2f,-19.6f,.33f,38,"Loose_Sandstone");
            Reed("Reeds_West", -3.8f,14.5f,30,1.25f);
            Reed("Reeds_North", 10.9f,8.15f,100,1.4f);
            Reed("Reeds_East", 16.9f,6.35f,80,1.1f);

            Prop(decor,ground,"Assets/Import/LowPolyMedievalPropsLite/Prefabs/Jug_04.prefab",
                "Clay_Jug",-20.4f,-15.7f,.88f,20,null,null);
            for (int i = 0; i < 4; i++)
            {
                var x = -16.8f + i*.50f;
                var z = -19.2f + i*1.25f;
                var slab = Prop(decor,ground,Props + $"Shared_Prop_Boulder_{i+1}.prefab",
                    $"Arrival_Flagstone_{i+1}",x,z,.9f,24+i*33,stone,null,.14f);
                foreach (var col in slab.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }

            var anchors = Child(root.transform, "Workshop_Walk_Anchors");
            void Anchor(string name, float x, float z, float yaw = 0)
            {
                var marker = Child(anchors, name);
                marker.SetPositionAndRotation(new Vector3(x, Ground(ground,x,z)+.008f,z),Quaternion.Euler(0,yaw,0));
            }
            Anchor("Start",-10,-17.8f,19);
            Anchor("Item01",-15.0f,-16.6f);
            Anchor("Item02",-24.2f,3.2f);
            Anchor("Item03",-10.0f,19.2f);
            Anchor("Item04",24.9f,2.1f);
            Anchor("Item05",5.0f,-12.65f);

            float area = Mathf.Abs(Shore.Select((p,i) =>
            {
                var q=Shore[(i+1)%Shore.Length];
                return p.x*q.y-p.y*q.x;
            }).Sum())*.5f;
            report.Add($"Water surface: 43 x 29 m, {area:F0} m²; near bank 0.42 m fall across 3.4 m");
            if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath))
                throw new InvalidOperationException("Could not save the Pond 1 prefab.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }

        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        if (!scene.GetRootGameObjects().Any(g => g.name == "Pond1_ExpandedOasis"))
        {
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            var sun = new GameObject("Workshop_Sun");
            SceneManager.MoveGameObjectToScene(sun, scene);
            sun.transform.rotation = Quaternion.Euler(48,-32,0);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save the Pond 1 scene.");
        AssetDatabase.SaveAssets();
        return string.Join("\n",report) + "\nSaved: " + ScenePath;
    }

    static Vector3[] Ring(float y, Func<Vector2,float> offset) => Shore.Select(p =>
    {
        var q = p + p.normalized * offset(p);
        return new Vector3(q.x,y,q.y);
    }).ToArray();

    static GameObject Surface(Transform root, string name, Vector3[][] rings, Material[] mats, bool solid, float bottom)
    {
        var go = Child(root,name).gameObject;
        var mesh = new Mesh {name=name};
        int n = rings[0].Length, center = n*rings.Length;
        mesh.vertices = rings.SelectMany(r => r).Append(new Vector3(0,bottom,0)).ToArray();
        mesh.subMeshCount = mats.Length;
        for(int r=0;r<rings.Length;r++)
        {
            var tris = new List<int>();
            for(int i=0;i<n;i++)
            {
                int a=r*n+i, b=r*n+(i+1)%n;
                if(r+1<rings.Length) tris.AddRange(new[]{a,a+n,b,b,a+n,b+n});
                else tris.AddRange(new[]{a,center,b});
            }
            mesh.SetTriangles(tris,r);
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var saved = SaveMesh(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = saved;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        if(solid) go.AddComponent<MeshCollider>().sharedMesh = saved;
        return go;
    }

    static Transform Child(Transform parent,string name)
    {
        var child = new GameObject(name).transform;
        SceneManager.MoveGameObjectToScene(child.gameObject, parent.gameObject.scene);
        child.SetParent(parent, false);
        return child;
    }

    static float Ground(Collider col, float x, float z)
    {
        if (col.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,40)) return hit.point.y;
        throw new InvalidOperationException($"No terrain below ({x}, {z})");
    }

    static GameObject Prop(Transform parent, Collider ground, string path, string name, float x,
        float z, float height, float yaw, Material paint, List<string> report, float flatten = 1)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!source) throw new InvalidOperationException("Missing " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source,parent);
        go.name=name;
        go.transform.SetPositionAndRotation(new Vector3(x,0,z),Quaternion.Euler(0,yaw,0));
        var renderers = go.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
        go.transform.localScale *= height / bounds.size.y;
        var scale=go.transform.localScale;
        scale.y*=flatten;
        go.transform.localScale=scale;

        // Use the rendered vertices, not the imported model's pivot or box bottom.
        var vertices = go.GetComponentsInChildren<MeshFilter>()
            .Where(m => m.sharedMesh != null)
            .SelectMany(m => m.sharedMesh.vertices.Select(p => m.transform.TransformPoint(p))).ToArray();
        float low = vertices.Min(p=>p.y);
        var baseVerts = vertices.Where(p=>p.y <= low + .025f).ToArray();
        var baseCenter = baseVerts.Aggregate(Vector3.zero,(sum,p)=>sum+p)/baseVerts.Length;
        go.transform.position += new Vector3(x-baseCenter.x,0,z-baseCenter.z);
        go.transform.position += Vector3.up*(Ground(ground,x,z)-low-.017f);
        if(paint)
            foreach(var r in renderers) r.sharedMaterials=r.sharedMaterials.Select(_=>paint).ToArray();
        if(report!=null)
        {
            var offsets=baseVerts.Select(p=>p.y+go.transform.position.y
                -Ground(ground,p.x+x-baseCenter.x,p.z+z-baseCenter.z)).ToArray();
            report.Add($"{name} visible root clearance {offsets.Min():F3} to {offsets.Max():F3} m");
        }
        return go;
    }

    static Mesh Reeds()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "/Reed_Clump.asset");
        if(mesh) return mesh;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for(int i=0;i<7;i++)
        {
            float angle=i*2.4f;
            var d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var cross=new Vector3(-d.z,0,d.x);
            float h=.51f+.083f*(i%4), spread=.28f+.047f*(i%3);
            int n=vertices.Count;
            vertices.AddRange(new[]{d*.03f-cross*.03f,d*.03f+cross*.03f,
                d*.16f+Vector3.up*(h*.58f)+cross*.043f,d*.16f+Vector3.up*(h*.58f)-cross*.043f,
                d*spread+Vector3.up*h});
            triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+3,n+2,n+4,n+2,n+1,n,n+3,n+2,n,n+4,n+2,n+3});
        }
        mesh=new Mesh {name="Reed_Clump", vertices=vertices.ToArray(), triangles=triangles.ToArray()};
        mesh.RecalculateNormals();
        return SaveMesh(mesh);
    }

    static Mesh SaveMesh(Mesh mesh)
    {
        var path=Dir+"/"+mesh.name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!old){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,old);
        EditorUtility.SetDirty(old);
        Object.DestroyImmediate(mesh);
        return old;
    }

    static Material Paint(string name,string hex,bool unlit=false)
    {
        var path=Dir+"/"+name+".mat";
        var shader=Shader.Find(unlit ? "Unlit/Color" : "Standard");
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat)
        {
            mat=new Material(shader){name=name};
            AssetDatabase.CreateAsset(mat,path);
        }
        ColorUtility.TryParseHtmlString(hex,out var color);
        mat.color=color;
        if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.04f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
