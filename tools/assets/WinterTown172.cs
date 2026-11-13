using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class WinterTown172
{
    const string D = "Assets/Events/NewYear/WinterTown172";
    static Transform root;
    static Material snow, red, dark, orange, metal;
    static readonly List<Vector3[]> ground = new List<Vector3[]>();
    static readonly List<string> audit = new List<string>();
    static int patches;
 static Transform Node(string n,Transform p,Vector3 pos){var g=new GameObject(n);g.transform.SetParent(p,false);g.transform.localPosition=pos;return g.transform;}
 static Transform Shape(string n,Transform p,PrimitiveType type,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
 static Bounds LocalBounds(Transform o){bool first=true;var b=new Bounds();foreach(var mf in o.GetComponentsInChildren<MeshFilter>()){var rr=mf.GetComponent<Renderer>();if(!rr||!rr.enabled||!rr.gameObject.activeInHierarchy)continue;foreach(var v in mf.sharedMesh.vertices){var p=root.InverseTransformPoint(mf.transform.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
 static string Fingerprint(GameObject o){return string.Join("\n",o.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c).Select(c=>c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)))+"\nCOLLIDERS="+string.Join(";",o.GetComponentsInChildren<Collider>(true).Select(c=>EditorJsonUtility.ToJson(c)));}
 static void Batch(Transform category){var filters=category.GetComponentsInChildren<MeshFilter>().Where(m=>m.GetComponent<MeshRenderer>()!=null&&m.gameObject.activeInHierarchy).ToArray();var batches=new Dictionary<Material,List<CombineInstance>>();foreach(var mf in filters){var r=mf.GetComponent<MeshRenderer>();for(int i=0;i<mf.sharedMesh.subMeshCount;i++){var mat=r.sharedMaterials[Math.Min(i,r.sharedMaterials.Length-1)];if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();batches[mat].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=i,transform=category.worldToLocalMatrix*mf.transform.localToWorldMatrix});}}var children=category.Cast<Transform>().ToArray();foreach(var kv in batches){var m=new Mesh();m.name=category.name+"_"+kv.Key.name;m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.CombineMeshes(kv.Value.ToArray(),true,true);AssetDatabase.CreateAsset(m,D+"/"+m.name+".asset");var node=Node(m.name,category,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=m;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=kv.Key;}foreach(var t in children)UnityEngine.Object.DestroyImmediate(t.gameObject);}
 static float Ground(float x,float z){float best=float.NegativeInfinity;foreach(var t in ground){var a=t[0];var b=t[1];var c=t[2];float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<.00001f)continue;float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den;float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)best=Mathf.Max(best,u*a.y+v*b.y+(1-u-v)*c.y);}if(float.IsNegativeInfinity(best))throw new Exception("No ground at "+x+","+z);return best;}
 static void Drift(Transform p,Vector3 pos,Vector3 size){float y=Ground(pos.x,pos.z);pos.y=y+size.y*.5f-.055f;Shape("Grounded_Snowdrift",p,PrimitiveType.Sphere,pos,size,snow);audit.Add("snow x="+pos.x+" z="+pos.z+" ground="+y+" bottom="+(pos.y-size.y*.5f));patches++;}
 static void Cone(Transform p,Vector3 pos,float radius,float length){int n=10;var v=new List<Vector3>();for(int i=0;i<n;i++)v.Add(new Vector3(Mathf.Cos(i*2*Mathf.PI/n)*radius,Mathf.Sin(i*2*Mathf.PI/n)*radius,0));v.Add(Vector3.forward*length);v.Add(Vector3.zero);var tr=new List<int>();for(int i=0;i<n;i++){int j=(i+1)%n;tr.AddRange(new[]{i,j,n,n+1,j,i});}var m=new Mesh();m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();var g=Node("Pointed_Carrot",p,pos);g.gameObject.AddComponent<MeshFilter>().sharedMesh=m;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=orange;}

    static void ReadGround(Transform land, bool castleCoordinates)
    {
        foreach (var filter in land.GetComponentsInChildren<MeshFilter>())
        {
            if (!filter.sharedMesh || (!castleCoordinates && filter.name != "Land")) continue;
            var vertices = filter.sharedMesh.vertices;
            var indices = filter.sharedMesh.triangles;
            var matrix = filter.transform.localToWorldMatrix;
            if (castleCoordinates) matrix = root.worldToLocalMatrix * matrix;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = matrix.MultiplyPoint3x4(vertices[indices[i]]);
                var b = matrix.MultiplyPoint3x4(vertices[indices[i + 1]]);
                var c = matrix.MultiplyPoint3x4(vertices[indices[i + 2]]);
                if (Mathf.Max(a.y,b.y,c.y) > .8f || Mathf.Min(a.y,b.y,c.y) < -.5f) continue;
                if (Mathf.Abs(Vector3.Cross(b-a,c-a).y) > .01f) ground.Add(new[]{ a,b,c });
            }
        }
    }

    static void KeepOtherGroundSnow(Transform previous, Transform destination)
    {
        var filter = previous.GetComponentsInChildren<MeshFilter>().Single();
        var source = filter.sharedMesh;
        // DB-ON-171 emitted three spheres per fir before all other ground snow.
        const int spheres = 82;
        if (source.vertexCount % spheres != 0) throw new Exception("Unexpected ground snow mesh layout");
        int firstKeptVertex = 30 * (source.vertexCount / spheres);
        var triangles = source.triangles;
        var kept = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
            if (triangles[i] >= firstKeptVertex && triangles[i+1] >= firstKeptVertex && triangles[i+2] >= firstKeptVertex)
                kept.AddRange(new[]{ triangles[i], triangles[i+1], triangles[i+2] });
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = "Ground_Snow_Between_Houses";
        mesh.triangles = kept.ToArray();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, D + "/GroundSnowBetweenHouses.asset");
        var node = Node(mesh.name, destination, Vector3.zero);
        node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        node.gameObject.AddComponent<MeshRenderer>().sharedMaterial = snow;
    }

    static int BuildFir(Transform firs, Transform drifts, Vector3 position, float height, float yaw, int index)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/NY_FestiveFir.prefab");
        var fir = (GameObject)PrefabUtility.InstantiatePrefab(asset, firs);
        fir.transform.localPosition = Vector3.zero;
        fir.transform.localRotation = Quaternion.Euler(0, yaw, 0) * fir.transform.localRotation;
        fir.transform.localScale *= height / LocalBounds(fir.transform).size.y;
        var bounds = LocalBounds(fir.transform);
        float surface = Ground(position.x, position.z);
        fir.transform.localPosition += new Vector3(position.x, surface, position.z) -
            new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in fir.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        var ornaments = fir.transform.Find("NewYear_Decoration");
        var candidates = new List<(Vector3 position, Vector3 normal, float area)>();
        foreach (var filter in fir.GetComponentsInChildren<MeshFilter>())
        {
            if (ornaments && filter.transform.IsChildOf(ornaments)) continue;
            var vertices = filter.sharedMesh.vertices;
            var indices = filter.sharedMesh.triangles;
            var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var points = new[]{ matrix.MultiplyPoint3x4(vertices[indices[i]]),
                    matrix.MultiplyPoint3x4(vertices[indices[i+1]]), matrix.MultiplyPoint3x4(vertices[indices[i+2]]) };
                var cross = Vector3.Cross(points[1]-points[0], points[2]-points[0]);
                float area = cross.magnitude * .5f;
                var normals = filter.sharedMesh.normals;
                var normal = matrix.MultiplyVector(normals[indices[i]] + normals[indices[i+1]] + normals[indices[i+2]]).normalized;
                if (normal.y < .22f || area < .28f) continue;
                Array.Sort(points, (a,b) => a.y.CompareTo(b.y));
                var center = points[0]*.42f + points[1]*.39f + points[2]*.19f;
                if (center.y < surface + height*.27f || center.y > surface + height*.86f) continue;
                candidates.Add((center, normal, area));
            }
        }
        var random = new System.Random(172 + index);
        var placed = new List<Vector3>();
        foreach (var candidate in candidates.OrderBy(_ => random.Next()).ToArray())
        {
            if (placed.Any(other => Vector3.Distance(other, candidate.position) < height*.19f)) continue;
            float width = .49f + (float)random.NextDouble()*.21f;
            var cap = Shape("Small_Branch_Snowcap", firs, PrimitiveType.Sphere,
                candidate.position - candidate.normal*.018f, new Vector3(width,.14f,width*.72f), snow);
            cap.localRotation = Quaternion.FromToRotation(Vector3.up, candidate.normal);
            placed.Add(candidate.position);
            if (placed.Count == 3 + index%2) break;
        }
        if (placed.Count < 2) throw new Exception("Too few valid branch surfaces at " + position);
        Drift(drifts, position + new Vector3(-.85f,0,.34f), new Vector3(4.8f,.68f,4.5f));
        Drift(drifts, position + new Vector3(1.07f,0,-.66f), new Vector3(3.3f,.46f,3.8f));
        return placed.Count;
    }

    static void BuildSnowman(Transform parent, Vector3 position, float size, float yaw)
    {
        position.y = Ground(position.x, position.z);
        var g = Node("Snowman", parent, position);
        g.localScale = Vector3.one * size;
        g.localRotation = Quaternion.Euler(0,yaw,0);
        Shape("Body",g,PrimitiveType.Sphere,new Vector3(0,.74f,0),new Vector3(1.8f,1.6f,1.65f),snow);
        Shape("Chest",g,PrimitiveType.Sphere,new Vector3(0,1.65f,0),new Vector3(1.35f,1.25f,1.3f),snow);
        Shape("Head",g,PrimitiveType.Sphere,new Vector3(0,2.45f,0),Vector3.one*1.05f,snow);
        Shape("Scarf",g,PrimitiveType.Cylinder,new Vector3(0,2.05f,0),new Vector3(1.2f,.12f,1.2f),red);
        Shape("Scarf_Tail",g,PrimitiveType.Cube,new Vector3(.3f,1.65f,.62f),new Vector3(.24f,.7f,.1f),red);
        foreach (float x in new[]{-.18f,.18f})
            Shape("Eye",g,PrimitiveType.Sphere,new Vector3(x,2.55f,.48f),Vector3.one*.12f,dark);
        for (int i=0;i<3;i++)
            Shape("Button",g,PrimitiveType.Sphere,new Vector3(0,1.65f-i*.3f,.68f),Vector3.one*.13f,dark);
        Cone(g,new Vector3(0,2.4f,.49f),.13f,.65f);

        const int sides = 12;
        var vertices = new List<Vector3>();
        for (int ring=0;ring<2;ring++)
            for (int i=0;i<sides;i++)
            {
                float angle = i*2*Mathf.PI/sides;
                float radius = ring == 0 ? .59f : .43f;
                vertices.Add(new Vector3(Mathf.Cos(angle)*radius,2.76f+ring*.58f,Mathf.Sin(angle)*radius));
            }
        vertices.Add(new Vector3(0,3.34f,0));
        var triangles = new List<int>();
        for (int i=0;i<sides;i++)
        {
            int next = (i+1)%sides;
            triangles.AddRange(new[]{i,sides+next,next,i,sides+i,sides+next,sides+i,2*sides,sides+next});
        }
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();
        var bucket = Node("Lowered_Bucket",g,Vector3.zero);
        bucket.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        bucket.gameObject.AddComponent<MeshRenderer>().sharedMaterial = metal;
        Shape("Bucket_Rim",g,PrimitiveType.Cylinder,new Vector3(0,2.78f,0),new Vector3(1.21f,.04f,1.21f),metal);
    }

    public static void Main()
    {
        const string path = "Assets/Events/NewYear/NY_Castle.prefab";
        if (AssetDatabase.IsValidFolder(D)) throw new Exception("WinterTown172 resources already exist");
        Directory.CreateDirectory(D);
        AssetDatabase.Refresh();
        snow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        red = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Ruby.mat");
        dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Charcoal.mat");
        orange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Carrot.mat");
        metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown171/Bucket_Metal.mat");
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root = prefab.transform;
            var fingerprint = Fingerprint(prefab);
            ReadGround(root.Find("Visual"), true);
            var land = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Locations/Land.prefab");
            try { ReadGround(land.transform, false); }
            finally { PrefabUtility.UnloadPrefabContents(land); }

            var previous = root.Find("WinterTown_171");
            var updated = Node("WinterTown_172",root,Vector3.zero);
            KeepOtherGroundSnow(previous.Find("Grounded_Snow"),updated);
            foreach (var name in new[]{"Grounded_Snow","Scattered_Snowy_Firs","Bucket_Snowmen"})
                previous.Find(name).gameObject.SetActive(false);
            var firs = Node("Sparse_Snowy_Firs",updated,Vector3.zero);
            var drifts = Node("Snow_At_New_Firs",updated,Vector3.zero);
            var snowmen = Node("Scattered_Bucket_Snowmen",updated,Vector3.zero);
            var trees = new[]{
                new Vector4(-17,-39,8.5f,11), new Vector4(-19,-28,6.2f,76),
                new Vector4(-32,-8,7.4f,30), new Vector4(-15,37,9.1f,123),
                new Vector4(18,-19,7.8f,50), new Vector4(15,9,9.5f,150),
                new Vector4(39,18,6.8f,96)
            };
            int caps=0;
            for (int i=0;i<trees.Length;i++)
                caps += BuildFir(firs,drifts,new Vector3(trees[i].x,0,trees[i].y),trees[i].z,trees[i].w,i);
            BuildSnowman(snowmen,new Vector3(-21,0,-2),.89f,75);
            BuildSnowman(snowmen,new Vector3(18,0,27),.78f,-65);
            BuildSnowman(snowmen,new Vector3(42,0,-41),.95f,-112);
            foreach (var category in new[]{firs,drifts,snowmen}) Batch(category);
            if (Fingerprint(prefab) != fingerprint) throw new Exception("Gameplay content changed");
            if (!previous.Find("House_Roof_Lights").gameObject.activeSelf) throw new Exception("Roof lights lost");
            PrefabUtility.SaveAsPrefabAsset(prefab,path);
            AssetDatabase.SaveAssets();
            audit.Add($"Trees={trees.Length}; small branch caps={caps}; snowmen=3; new grounded drifts={patches}; roof lights active; gameplay fingerprint unchanged.");
            File.WriteAllLines("Docs/locations/previews/winter-town172-verification.txt",audit);
            Debug.Log(audit.Last());
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
}