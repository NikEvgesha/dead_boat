using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterTrees183
{
    const string Folder = "Assets/Events/NewYear/Route183";
    const string Castle = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string Report = "Docs/locations/previews/event-winter183-assets.txt";
    static readonly List<string> audit = new List<string>();
    static Material snow;
    static Transform Node(string name, Transform parent, Vector3 position)
    {
        var node = new GameObject(name).transform;
        if (parent) node.SetParent(parent, false);
        node.localPosition = position;
        return node;
    }

    static Transform Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }

    static MeshFilter[] Meshes(Transform parent) =>
        parent.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh && f.GetComponent<MeshRenderer>()).ToArray();

    static Bounds BoundsOf(Transform parent)
    {
        var bounds = parent.GetComponentInChildren<Renderer>().bounds;
        foreach (var renderer in parent.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static bool RayTriangle(Vector3 origin, Vector3 direction, Vector3[] triangle, out float distance)
    {
        var edgeA = triangle[1]-triangle[0];
        var edgeB = triangle[2]-triangle[0];
        var p = Vector3.Cross(direction,edgeB);
        float det = Vector3.Dot(edgeA,p);
        distance=0;
        if (Mathf.Abs(det)<.00001f) return false;
        var t = origin-triangle[0];
        float u = Vector3.Dot(t,p)/det;
        if (u<0 || u>1) return false;
        var q = Vector3.Cross(t,edgeA);
        float v = Vector3.Dot(direction,q)/det;
        if (v<0 || u+v>1) return false;
        distance=Vector3.Dot(edgeB,q)/det;
        return distance>=0;
    }


    static Transform CopyVisual(Transform source, Transform parent)
    {
        var clone = Node(source.name, parent, source.localPosition);
        clone.localRotation = source.localRotation;
        clone.localScale = source.localScale;
        var filter = source.GetComponent<MeshFilter>();
        var renderer = source.GetComponent<MeshRenderer>();
        if (filter && renderer && renderer.enabled && filter.sharedMesh)
        {
            clone.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            clone.gameObject.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        foreach (Transform child in source)
            if (child.gameObject.activeSelf) CopyVisual(child, clone);
        return clone;
    }

    static void AddSnowOnBranches(Transform tree, Bounds bounds, float yaw)
    {
        var meshes = Meshes(tree).Select(f => new {
            points = f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v)).ToArray(),
            indices = f.sharedMesh.triangles
        }).ToArray();
        int count = 0;
        for (int i = 0; i < 5; i++)
        {
            float angle = (yaw + i*137f)*Mathf.Deg2Rad;
            float scale = .47f + (i%3)*.12f;
            var p = new Vector3(bounds.center.x+Mathf.Cos(angle)*bounds.extents.x*scale,
                bounds.max.y+.30f,bounds.center.z+Mathf.Sin(angle)*bounds.extents.z*scale);
            float nearest = float.PositiveInfinity;
            foreach (var mesh in meshes)
                for (int j=0;j<mesh.indices.Length;j+=3)
                    if (RayTriangle(p,Vector3.down,new[]{mesh.points[mesh.indices[j]],mesh.points[mesh.indices[j+1]],
                        mesh.points[mesh.indices[j+2]]},out var distance))
                        nearest = Mathf.Min(nearest,distance);
            if (float.IsInfinity(nearest)) continue;
            p.y-=nearest+.002f;
            float height = bounds.size.y;
            var cap=Shape("Snow_On_Branch",tree,PrimitiveType.Sphere,tree.InverseTransformPoint(p),
                new Vector3(height*.081f,height*.025f,height*.054f)/tree.lossyScale.x,snow);
            cap.localEulerAngles = new Vector3(0,angle*Mathf.Rad2Deg-yaw,12f);
            count++;
        }
        if (count<3) throw new InvalidOperationException("Too few grounded branch caps");
    }

    static Bounds Tree(Transform parent, string assetPath, Vector3 position, float height, float yaw, bool addSnow)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        var tree=CopyVisual(prefab.transform,parent);
        tree.name=addSnow?"Snowy_Fir":"Decorated_Fir";
        tree.localPosition=Vector3.zero;
        tree.localRotation=Quaternion.Euler(0,yaw,0)*tree.localRotation;
        var bounds=BoundsOf(tree);
        tree.localScale*=height/bounds.size.y;
        bounds=BoundsOf(tree);
        tree.position+=parent.TransformPoint(position)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        bounds=BoundsOf(tree);
        if(addSnow) AddSnowOnBranches(tree,bounds,yaw);
        return BoundsOf(tree);
    }

    const string Decorated = "Assets/Events/NewYear/Route178/Bank_Decorated_Fir.prefab";
    const string Trees = "Assets/Prefabs/Locations/Decoration/Hilly_Prop_Tree_Pine_";

    static void Understory(Transform parent, float x,float z,float width)
    {
        var center = new Vector3(x,.035f,z);
        Shape("Settled_Snow",parent,PrimitiveType.Sphere,center,new Vector3(width,.19f,width*.68f),snow);
    }

    static GameObject Grove(string name, Action<Transform,List<Bounds>> build)
    {
        var root=Node(name,null,Vector3.zero);
        try
        {
            var trees=new List<Bounds>();
            build(root,trees);
            var bounds=BoundsOf(root);
            var vertices=Meshes(root).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v)));
            float radial=vertices.Max(v=>new Vector2(v.x,v.z).magnitude);
            if(radial>6f || Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x))>5.1f ||
                Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z))>5.0f || bounds.min.y<-.25f)
                throw new InvalidOperationException("Grove exceeds safe bank footprint "+name+" "+bounds+" radius="+radial);
            if(trees.Any(t=>Mathf.Abs(t.min.y)>.04f))
                throw new InvalidOperationException("A grove tree is not grounded");
            float crownGap=float.PositiveInfinity;
            for(int i=0;i<trees.Count;i++)
                for(int j=i+1;j<trees.Count;j++)
                    crownGap=Mathf.Min(crownGap,Gap(trees[i],trees[j]));
            if(crownGap<.48f)
                throw new InvalidOperationException("Crowns too close in "+name+": "+crownGap.ToString("F2")+" m");

            root.tag="Decoration";
            root.gameObject.layer=9;
            var trigger=root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger=true;
            trigger.center=bounds.center;
            trigger.size=bounds.size;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/"+name+".prefab");
            if(!prefab) throw new InvalidOperationException("Could not save "+name);
            audit.Add($"{name}: {trees.Count} separated trees, {string.Join(", ", trees.Select(b=>b.size.y.ToString("F2")))} m; grounded; crown gap {crownGap:F2} m; radius {radial:F2} m.");
            return prefab;
        }
        finally {Object.DestroyImmediate(root.gameObject);}
    }

    static GameObject[] BuildGroves() => new[]
    {
        Grove("Bank_Tall_And_Small_Firs", (root,trees)=>
        {
            trees.Add(Tree(root,Trees+"3.prefab",new Vector3(-.65f,0,1.82f),4.90f,34,true));
            trees.Add(Tree(root,Decorated,new Vector3(2.85f,0,-2.52f),3.25f,115,false));
            trees.Add(Tree(root,Trees+"2.prefab",new Vector3(-3.25f,0,-2.85f),2.15f,202,true));
            Understory(root,-.65f,1.82f,3.15f);
            Understory(root,2.85f,-2.52f,1.80f);
        }),
        Grove("Bank_Young_Fir_Grove", (root,trees)=>
        {
            trees.Add(Tree(root,Trees+"2.prefab",new Vector3(-3.22f,0,.88f),2.25f,52,true));
            trees.Add(Tree(root,Trees+"3.prefab",new Vector3(-1.05f,0,-2.92f),3.40f,140,true));
            trees.Add(Tree(root,Decorated,new Vector3(1.58f,0,2.36f),4.60f,77,false));
            trees.Add(Tree(root,Trees+"2.prefab",new Vector3(3.23f,0,-1.68f),1.75f,172,true));
            Understory(root,1.58f,2.36f,2.38f);
            Understory(root,-1.05f,-2.92f,1.82f);
        })
    };

    static float Gap(Bounds a,Bounds b)
    {
        float x=Mathf.Max(0f,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x));
        float z=Mathf.Max(0f,Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z));
        return new Vector2(x,z).magnitude;
    }

    static float Nearest(Bounds a,IEnumerable<Bounds> b) =>
        b.Where(o=>o.min.y<a.max.y && o.max.y>a.min.y).Select(o=>Gap(a,o)).DefaultIfEmpty(float.PositiveInfinity).Min();

    static void CheckCastle()
    {
        var castle=PrefabUtility.LoadPrefabContents(Castle);
        try
        {
            var root=castle.transform;
            var decorated=root.Find("Winter_176/Festive_Firs");
            var lone=root.Find("08_Distributed_Greenery/Green_Cluster_3 (1)/Shade_Tree (1)");
            var houses=root.Find("03_Service_Houses").Cast<Transform>().Select(BoundsOf).ToArray();
            var chateau=root.Find("01_Original_Castle_Perimeter/Chateau");
            var towers=chateau.Cast<Transform>().Where(t=>t.name.StartsWith("Tower") && t.GetComponentInChildren<MeshRenderer>()).Select(BoundsOf).ToArray();
            var walls=chateau.Cast<Transform>().Where(t=>t.name.StartsWith("Wall2") && t.GetComponentInChildren<MeshRenderer>()).Select(BoundsOf).ToArray();
            var bankY=root.Find("02_Courtyard/West_Bank").GetComponent<Renderer>().bounds.max.y;
            float eastY=root.Find("02_Courtyard/East_Bank").GetComponent<Renderer>().bounds.max.y;
            if(Mathf.Abs(bankY-eastY)>.001f) throw new InvalidOperationException("The banks have different elevation");
            var start = new[]
            {
                new Vector3(-18.8f,.20f,-19f),new Vector3(-7.6f,.20f,-5.2f),new Vector3(-17.8f,.20f,9f),
                new Vector3(7.3f,.20f,-6.9f),new Vector3(17.5f,.20f,5.2f),new Vector3(14.5f,.20f,19.2f),
                new Vector3(-7.54f,.24f,8.33f)
            };
            var end=(Vector3[])start.Clone();
            end[0]=new Vector3(-18.12f,bankY,-18.45f);
            end[2]=new Vector3(-18.24f,bankY,9f);
            end[5]=new Vector3(17.35f,bankY,19.05f);
            end[6].y=bankY;
            var old=new Bounds[start.Length];
            var current=new Bounds[start.Length];
            var counts=new int[start.Length];
            void Include(int i,Vector3 p)
            {
                var q=p+end[i]-start[i];
                if(counts[i]++==0) {old[i]=new Bounds(p,Vector3.zero);current[i]=new Bounds(q,Vector3.zero);}
                else {old[i].Encapsulate(p);current[i].Encapsulate(q);}
            }
            var replacements=new Dictionary<Mesh,Vector3[]>();
            foreach(var filter in Meshes(decorated))
            {
                var mesh=filter.sharedMesh;
                if(!AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/Events/NewYear/Castle176/"))
                    throw new InvalidOperationException("Unexpected shared tree mesh");
                var vertices=mesh.vertices;
                for(int j=0;j<vertices.Length;j++)
                {
                    var p=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[j]));
                    int index=Array.FindIndex(start,c=>new Vector2(p.x-c.x,p.z-c.z).sqrMagnitude<2.0f*2.0f);
                    if(index<0) throw new InvalidOperationException("Found decoration away from all seven trees: "+p);
                    Include(index,p);
                    vertices[j]=filter.transform.InverseTransformPoint(root.TransformPoint(p+end[index]-start[index]));
                }
                replacements.Add(mesh,vertices);
            }
            foreach(var filter in Meshes(lone))
                foreach(var vertex in filter.sharedMesh.vertices)
                    Include(6,root.InverseTransformPoint(filter.transform.TransformPoint(vertex)));
            for(int i=0;i<start.Length;i++)
            {
                float house=Nearest(current[i],houses);
                float tower=Nearest(current[i],towers);
                float wall=Nearest(current[i],walls);
                if(counts[i]==0 || Mathf.Abs(current[i].min.y-bankY)>.022f || house<.25f || tower<.25f || wall<.25f ||
                    current[i].min.x < -22.79f || current[i].max.x > 22.79f ||
                    current[i].min.z < -23.11f || current[i].max.z > 23.04f ||
                    current[i].min.x<3.5f && current[i].max.x>-3.5f)
                    throw new InvalidOperationException($"Tree {i+1} clearance failed at {end[i]}: {current[i]}, house {house:F2}, tower {tower:F2}, wall {wall:F2}");
                audit.Add($"Castle fir {i+1}: {start[i]} -> {end[i]}, trunk base {current[i].min.y:F2} m, house gap {house:F2} m, tower/wall gap {Mathf.Min(tower,wall):F2} m. Prior tower gap {Nearest(old[i],towers):F2} m.");
            }
            lone.position+=root.TransformVector(end[6]-start[6]);
            if(!PrefabUtility.SaveAsPrefabAsset(castle,Castle)) throw new InvalidOperationException("Castle save failed");
            foreach(var pair in replacements)
            {
                pair.Key.vertices=pair.Value;
                pair.Key.RecalculateBounds();
                EditorUtility.SetDirty(pair.Key);
                AssetDatabase.SaveAssetIfDirty(pair.Key);
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(castle);}
    }

    public static string RebuildGroves()
    {
        audit.Clear();
        snow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        BuildGroves();
        return string.Join(" | ",audit);
    }

    public static string Build()
    {
        if(!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Events/NewYear","Route183");
        audit.Clear();
        snow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        var collection=AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>("Assets/Events/NewYear/NY_Decor.asset");
        if(collection.spawnItems.Count!=11) throw new InvalidOperationException("Unexpected route decor pool");
        var groves=BuildGroves();
        CheckCastle();
        collection.spawnItems.Single(i=>i.prefab.name=="Bank_Snowdrifts").chance=29f;
        collection.spawnItems.Add(new DecorationSpawnItem{prefab=groves[0],chance=29f});
        collection.spawnItems.Add(new DecorationSpawnItem{prefab=groves[1],chance=21f});
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssetIfDirty(collection);
        audit.Add("NY_Decor: 13 variants; more tree clusters while all six selected decorations stay in rotation.");
        File.WriteAllLines(Report,audit);
        return string.Join(" | ",audit);
    }
}
