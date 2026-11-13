using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WinterCastle175
{
    public const string Destination = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string D = "Assets/Events/NewYear/Castle175";
    static Transform root;
    static Material snow, red, gold, blue;
    static readonly List<Vector3[]> ground = new List<Vector3[]>();
    static readonly List<string> audit = new List<string>();
    static int patches;
 static Transform Node(string n,Transform p,Vector3 pos){var g=new GameObject(n);g.transform.SetParent(p,false);g.transform.localPosition=pos;return g.transform;}
 static Transform Shape(string n,Transform p,PrimitiveType type,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
 static Bounds LocalBounds(Transform o){bool first=true;var b=new Bounds();foreach(var mf in o.GetComponentsInChildren<MeshFilter>()){var rr=mf.GetComponent<Renderer>();if(!rr||!rr.enabled||!rr.gameObject.activeInHierarchy)continue;foreach(var v in mf.sharedMesh.vertices){var p=root.InverseTransformPoint(mf.transform.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
 static string Fingerprint(GameObject o){return string.Join("\n",o.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c).Select(c=>c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)))+"\nCOLLIDERS="+string.Join(";",o.GetComponentsInChildren<Collider>(true).Select(c=>EditorJsonUtility.ToJson(c)));}
 static void Batch(Transform category){var filters=category.GetComponentsInChildren<MeshFilter>().Where(m=>m.GetComponent<MeshRenderer>()!=null&&m.gameObject.activeInHierarchy).ToArray();var batches=new Dictionary<Material,List<CombineInstance>>();foreach(var mf in filters){var r=mf.GetComponent<MeshRenderer>();for(int i=0;i<mf.sharedMesh.subMeshCount;i++){var mat=r.sharedMaterials[Math.Min(i,r.sharedMaterials.Length-1)];if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();batches[mat].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=i,transform=category.worldToLocalMatrix*mf.transform.localToWorldMatrix});}}var children=category.Cast<Transform>().ToArray();foreach(var kv in batches){var name=category.name+"_"+kv.Key.name;var path=D+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m){m.Clear();}else{m=new Mesh{name=name};AssetDatabase.CreateAsset(m,path);}m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.CombineMeshes(kv.Value.ToArray(),true,true);EditorUtility.SetDirty(m);var node=Node(m.name,category,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=m;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=kv.Key;}foreach(var t in children)UnityEngine.Object.DestroyImmediate(t.gameObject);}
 static float Ground(float x,float z){float best=float.NegativeInfinity;foreach(var t in ground){var a=t[0];var b=t[1];var c=t[2];float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<.00001f)continue;float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den;float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)best=Mathf.Max(best,u*a.y+v*b.y+(1-u-v)*c.y);}if(float.IsNegativeInfinity(best))throw new Exception("No ground at "+x+","+z);return best;}
 static void Drift(Transform p,Vector3 pos,Vector3 size){float y=Ground(pos.x,pos.z);pos.y=y+size.y*.5f-.055f;Shape("Grounded_Snowdrift",p,PrimitiveType.Sphere,pos,size,snow);audit.Add("snow x="+pos.x+" z="+pos.z+" ground="+y+" bottom="+(pos.y-size.y*.5f));patches++;}
    static MeshFilter[] Meshes(Transform group) => group.GetComponentsInChildren<MeshFilter>()
        .Where(m => m.sharedMesh && m.TryGetComponent<MeshRenderer>(out var r) && r.enabled).ToArray();

    static IEnumerable<Vector3[]> Triangles(MeshFilter filter)
    {
        var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        var vertices = filter.sharedMesh.vertices;
        var indices = filter.sharedMesh.triangles;
        for (int i=0;i<indices.Length;i+=3)
            yield return new[]{matrix.MultiplyPoint3x4(vertices[indices[i]]),
                matrix.MultiplyPoint3x4(vertices[indices[i+1]]),matrix.MultiplyPoint3x4(vertices[indices[i+2]])};
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

    static void Cord(Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
    {
        var delta=b-a;
        if (delta.sqrMagnitude<.000001f) return;
        var cord=Shape("Cord",parent,PrimitiveType.Cube,(a+b)*.5f,
            new Vector3(thickness,thickness,delta.magnitude),mat);
        cord.localRotation=Quaternion.LookRotation(delta);
    }

    static int Fir(Transform trees, Transform drifts, Vector3 pos, float height, float yaw)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/NY_FestiveFir.prefab");
        var fir=(GameObject)PrefabUtility.InstantiatePrefab(source,trees);
        var old=fir.transform.Find("NewYear_Decoration");
        if(old) old.gameObject.SetActive(false);
        fir.transform.localPosition=Vector3.zero;
        fir.transform.localRotation=Quaternion.Euler(0,yaw,0)*fir.transform.localRotation;
        fir.transform.localScale*=height/LocalBounds(fir.transform).size.y;
        var bounds=LocalBounds(fir.transform);
        float y=Ground(pos.x,pos.z);
        fir.transform.localPosition+=new Vector3(pos.x,y,pos.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        foreach(var collider in fir.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        var foliage=Meshes(fir.transform).SelectMany(Triangles).ToArray();
        int attached=0;
        for(int tier=0;tier<4;tier++)
            for(int turn=0;turn<4;turn++)
            {
                float angle=(turn*90+tier*37+yaw)*Mathf.Deg2Rad;
                var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                var origin=new Vector3(pos.x,y+height*(.33f+tier*.13f),pos.z)+radial*3;
                float nearest=float.PositiveInfinity;
                foreach(var tri in foliage)
                    if(RayTriangle(origin,-radial,tri,out float distance)) nearest=Mathf.Min(nearest,distance);
                if(float.IsPositiveInfinity(nearest)) continue;
                var contact=origin-radial*nearest;
                var center=contact+radial*.055f-Vector3.up*.075f;
                Cord(trees,contact,center,.018f,gold);
                Shape("Attached_Bauble",trees,PrimitiveType.Sphere,center,Vector3.one*.19f,
                    (tier+turn)%3==0?blue:(tier+turn)%2==0?gold:red);
                attached++;
                if(turn==tier)
                    Shape("Branch_Snow",trees,PrimitiveType.Sphere,contact-radial*.025f+Vector3.up*.025f,
                        new Vector3(.29f,.10f,.24f),snow);
            }
        Shape("Tree_Topper",trees,PrimitiveType.Sphere,new Vector3(pos.x,y+height-.025f,pos.z),Vector3.one*.19f,gold);
        Drift(drifts,pos,new Vector3(2.3f,.30f,1.9f));
        if(attached<10)throw new InvalidOperationException("Too few branch attachments at "+pos);
        return attached;
    }

    static Material RoofMaterial(Material template, string name, Color color)
    {
        string path = D + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(template) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    public static void Build()
    {
        var castle = PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            root = castle.transform;
            var previousArt = root.Find("Winter_175");
            if (previousArt) UnityEngine.Object.DestroyImmediate(previousArt.gameObject);
            var original = Fingerprint(castle);
            Directory.CreateDirectory(D);
            AssetDatabase.Refresh();
            snow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
            red = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Ruby.mat");
            gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Gold.mat");
            blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown170/Gift_Blue.mat");
            ground.Clear();
            audit.Clear();
            patches = 0;

            var court = root.Find("02_Courtyard");
            foreach (var triangle in Meshes(court).SelectMany(Triangles))
                if (triangle.All(p => p.y < .9f && p.y > -.6f) &&
                    Mathf.Abs(Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]).y) > .0001f)
                    ground.Add(triangle);

            foreach (var name in new[] { "East_Bank", "West_Bank" })
                court.Find(name).GetComponent<MeshRenderer>().sharedMaterial = snow;

            var ordinaryPlants = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Tree1" || t.name == "Shade_Tree" || t.name == "Grass_Tuft").ToArray();
            foreach (var plant in ordinaryPlants) plant.gameObject.SetActive(false);

            var perimeter = root.Find("01_Original_Castle_Perimeter");
            var caps = perimeter.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.sharedMaterials.Any(m => m && (m.name == "Color_B08" || m.name == "Tower_Pine_Green" || m.name == "Tower_Cranberry_Red")) &&
                    r.bounds.center.y > 4f)
                .OrderBy(r => r.bounds.center.z).ThenBy(r => r.bounds.center.x).ToArray();
            if (caps.Length < 4) throw new InvalidOperationException("Expected tower roofs, found " + caps.Length);
            var originalRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/Import/3D Cartoon Village/Materials/Color_B08.mat");
            var greenRoof = RoofMaterial(originalRed, "Tower_Pine_Green", new Color(.085f, .36f, .20f));
            var redRoof = RoofMaterial(originalRed, "Tower_Cranberry_Red", new Color(.64f, .105f, .135f));
            for (int i = 0; i < caps.Length; i++)
            {
                var color = i == 0 || i == caps.Length - 1 ? greenRoof : redRoof;
                caps[i].sharedMaterials = caps[i].sharedMaterials.Select(m =>
                    m == originalRed || m == greenRoof || m == redRoof ? color : m).ToArray();
            }
            foreach (var roof in caps) audit.Add("roof " + roof.bounds.center + " " + string.Join(",", roof.sharedMaterials.Select(m => m.name)));

            root.Find("Winter_174/Festive_Firs").gameObject.SetActive(false);
            root.Find("Winter_174/Courtyard_Snow").gameObject.SetActive(false);
            var winter = Node("Winter_175", root, Vector3.zero);
            var trees = Node("Festive_Firs", winter, Vector3.zero);
            var drifts = Node("Courtyard_Snow", winter, Vector3.zero);
            // Stable irregular spacing so the authored scene and streamed prefab look alike.
            var placements = new[]
            {
                new Vector4(-18.8f,-19.0f,3.7f,12), new Vector4(-7.6f,-5.2f,3.2f,69),
                new Vector4(-17.8f,9.0f,4.1f,110), new Vector4(7.3f,-6.9f,3.15f,44),
                new Vector4(17.5f,5.2f,3.55f,133), new Vector4(14.5f,21.1f,3.0f,172)
            };
            int ornaments = placements.Sum(t => Fir(trees, drifts, new Vector3(t.x, 0, t.y), t.z, t.w));
            foreach (var p in new[]
            {
                new Vector4(-20,-9,3.3f,1.9f), new Vector4(-19,1,3.0f,2.4f),
                new Vector4(-22,13,4.0f,2.7f), new Vector4(-13,22,4.6f,2.2f),
                new Vector4(18,-20,4.5f,2.8f), new Vector4(20,-8,4.0f,2.0f),
                new Vector4(20,13,3.4f,2.2f), new Vector4(14,22,3.3f,2.1f)
            })
                Drift(drifts, new Vector3(p.x, 0, p.y), new Vector3(p.z, p.z * .11f, p.w));
            Batch(trees);
            Batch(drifts);
            if (Fingerprint(castle) != original) throw new InvalidOperationException("Original gameplay components changed");
            if (!PrefabUtility.SaveAsPrefabAsset(castle, Destination)) throw new InvalidOperationException("Prefab save failed");
            AssetDatabase.SaveAssets();
            audit.Add($"Snow banks=2, drifts={patches}, firs={placements.Length}, attached ornaments={ornaments}, hidden ordinary trees={ordinaryPlants.Count(p => p.name != "Grass_Tuft")}");
            File.WriteAllLines("Docs/locations/previews/event-castle175-art.txt", audit);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(castle);
        }
    }

    public static void Main()
    {
        Build();
        CastleView175.Main();
    }
}

