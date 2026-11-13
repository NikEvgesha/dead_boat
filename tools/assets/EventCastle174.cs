using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EventCastle174
{
    public const string Destination = "Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab";
    const string D = "Assets/Events/NewYear/Castle174";
    const string Previous = "Assets/Events/NewYear/NY_Castle.prefab";
    const string ScenePath = "Assets/Scenes/LevelEvent.unity";
    static Transform root;
    static Material snow, red, dark, orange, metal, gold, blue, green, light;
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

    static void Garland(Transform parent, Vector3 a, Vector3 b)
    {
        Cord(parent,a,b,.022f,dark);
        int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.29f));
        for (int i=0;i<count;i++)
            Shape("Roof_Light",parent,PrimitiveType.Sphere,Vector3.Lerp(a,b,i/(float)count),
                Vector3.one*.115f,i%4==0?red:light);
    }

    static void Roof(Transform house, Transform snowRoot, Transform garlands)
    {
        var roof=house.Find("04_Roof");
        var filter=Meshes(roof).Single(m=>m.name=="Continuous_Pitched_Roof");
        var bounds=filter.sharedMesh.bounds;
        var matrix=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
        float xMin=bounds.min.x, xMax=bounds.max.x, xMid=bounds.center.x;
        float eave=bounds.min.y+.14f, ridge=bounds.max.y;
        float zMin=bounds.min.z, zMax=bounds.max.z;
        Vector3 Edge(float x,float y,float z)=>matrix.MultiplyPoint3x4(new Vector3(x,y+.08f,z));
        var a=Edge(xMin-.025f,eave,zMin-.035f); var b=Edge(xMax+.025f,eave,zMin-.035f);
        var c=Edge(xMin-.025f,eave,zMax+.035f); var d=Edge(xMax+.025f,eave,zMax+.035f);
        var r1=Edge(xMid,ridge,zMin-.035f); var r2=Edge(xMid,ridge,zMax+.035f);
        Garland(garlands,a,c); Garland(garlands,b,d);
        Garland(garlands,a,r1); Garland(garlands,r1,b);
        Garland(garlands,c,r2); Garland(garlands,r2,d);

        var tiles=Meshes(roof).Where(m=>m.name.EndsWith("_Tiles") || m.sharedMesh.name=="CottageRoofTiles").ToArray();
        if (tiles.Length==0) tiles=new[]{filter};
        var vertices=new List<Vector3>();
        foreach (var tile in tiles)
            foreach (var face in Triangles(tile))
            {
                var normal=Vector3.Cross(face[1]-face[0],face[2]-face[0]).normalized;
                if (normal.y>=.35f) vertices.AddRange(face.Select(v=>v+Vector3.up*.028f));
            }
        if (vertices.Count==0) throw new InvalidOperationException("No upper roof surface on "+house.name);
        var cover=new Mesh();
        cover.SetVertices(vertices);
        cover.SetTriangles(Enumerable.Range(0,vertices.Count).ToArray(),0);
        cover.RecalculateNormals();
        var node=Node(house.name+"_Roof_Snow",snowRoot,Vector3.zero);
        node.gameObject.AddComponent<MeshFilter>().sharedMesh=cover;
        node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=snow;
        audit.Add($"{house.name}: 6 roof edge segments, {vertices.Count/3} snow triangles");
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

    static void Present(Transform group, float x, float z, float size, Material wrap, float yaw)
    {
        var node=Node("Gift",group,new Vector3(x,Ground(x,z),z));
        node.localRotation=Quaternion.Euler(0,yaw,0);
        var center=Vector3.up*size*.4f;
        Shape("Box",node,PrimitiveType.Cube,center,new Vector3(size,size*.8f,size),wrap);
        Shape("Ribbon",node,PrimitiveType.Cube,center,new Vector3(size*1.02f,size*.82f,size*.13f),gold);
        Shape("Ribbon",node,PrimitiveType.Cube,center,new Vector3(size*.13f,size*.82f,size*1.02f),gold);
    }

    static void Build()
    {
        Directory.CreateDirectory(D);
        AssetDatabase.Refresh();
        if(!AssetDatabase.CopyAsset("Assets/Events/CastleBase/EventCastle_Base.prefab",Destination))
            throw new InvalidOperationException("Castle copy failed");
        snow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
        red=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Ruby.mat");
        gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Gold.mat");
        green=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Fir.mat");
        blue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown170/Gift_Blue.mat");
        dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Charcoal.mat");
        orange=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Carrot.mat");
        metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown171/Bucket_Metal.mat");
        light=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/WinterTown169/Warm_Light.mat");
        ground.Clear(); audit.Clear(); patches=0;
        var castle=PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            root=castle.transform;
            castle.name="EventCastle_Winter";
            var original=Fingerprint(castle);
            var court=root.Find("02_Courtyard");
            foreach(var tri in Meshes(court).SelectMany(Triangles))
                if(tri.All(p=>p.y<.9f&&p.y>-.6f)&&Mathf.Abs(Vector3.Cross(tri[1]-tri[0],tri[2]-tri[0]).y)>.0001f)ground.Add(tri);
            if(ground.Count==0)throw new InvalidOperationException("Courtyard ground missing");
            var art=Node("Winter_174",root,Vector3.zero);
            var roofSnow=Node("Roof_Snow",art,Vector3.zero);var lights=Node("Roof_Garlands",art,Vector3.zero);
            var trees=Node("Festive_Firs",art,Vector3.zero);var drifts=Node("Courtyard_Snow",art,Vector3.zero);
            var gifts=Node("Presents",art,Vector3.zero);var men=Node("Snowman",art,Vector3.zero);
            var houses=root.Find("03_Service_Houses");
            if(houses.childCount!=6)throw new InvalidOperationException("Expected six houses");
            foreach(Transform house in houses)Roof(house,roofSnow,lights);
            var placements=new[]{new Vector4(-7,-21,3.7f,12),new Vector4(-7.7f,-6,3.25f,69),
                new Vector4(-16,7,3.2f,110),new Vector4(6.5f,-19,3.5f,44),
                new Vector4(7.4f,6.8f,4.1f,133),new Vector4(7.8f,21,3.0f,172)};
            int ornaments=0;
            var wraps=new[]{red,green,blue,gold};
            for(int i=0;i<placements.Length;i++)
            {
                var t=placements[i];ornaments+=Fir(trees,drifts,new Vector3(t.x,0,t.y),t.z,t.w);
                Present(gifts,t.x+.67f,t.y+.8f,.38f,wraps[i%4],17+i*21);
                Present(gifts,t.x-.64f,t.y-.85f,.53f,wraps[(i+2)%4],68-i*11);
                if(i%2==0) Present(gifts,t.x+.96f,t.y-.62f,.29f,wraps[(i+1)%4],27+i*12);
            }
            Present(gifts,-5.2f,-11,.45f,blue,34);Present(gifts,5.6f,12,.4f,red,59);
            Present(gifts,-6.7f,21.5f,.53f,green,18);
            BuildSnowman(men,new Vector3(-6.6f,0,18.4f),.59f,75);
            foreach(var group in new[]{roofSnow,lights,trees,drifts,gifts,men})Batch(group);
            if(Fingerprint(castle)!=original)throw new InvalidOperationException("Original gameplay components changed");
            var previous=AssetDatabase.LoadAssetAtPath<GameObject>(Previous);
            var entry=UnityEngine.Object.Instantiate(previous.transform.Find("EntryTrigger").gameObject,root);
            entry.name="EntryTrigger";
            entry.transform.localPosition=new Vector3(0,0,LocalBounds(court).min.z+.8f);
            var trigger=entry.GetComponent<BoxCollider>();
            trigger.center=new Vector3(0,trigger.center.y,3.5f);
            trigger.size=new Vector3(8.5f,trigger.size.y,8);
            if(houses.GetComponentsInChildren<StorePoint>(true).Length!=9||houses.GetComponentsInChildren<SellPoint>(true).Length!=2)
                throw new InvalidOperationException("Shop components changed");
            if(!PrefabUtility.SaveAsPrefabAsset(castle,Destination))throw new InvalidOperationException("Cannot save winter castle");
            AssetDatabase.SaveAssets();
            audit.Add($"Houses=6; firs=6; branch-contact ornaments={ornaments}; gifts=18; snowmen=1; entry={entry.transform.localPosition}");
            File.WriteAllLines("Docs/locations/previews/event-castle174-verification.txt",audit);
        }
        finally { PrefabUtility.UnloadPrefabContents(castle); }
    }

    static readonly Dictionary<string,string> HouseNames = new Dictionary<string,string>
    {
        {"General Store","01_Supplies"}, {"Fisherman Shark","02_Armory"},
        {"Trading hut","03_LootTrader"}, {"Guns Store","04_Professions"},
        {"Sheriffs","05_Nursery"}, {"Hospital","06_AnimalMerge"}
    };

    static Transform ObjectTransform(UnityEngine.Object obj)
    {
        if (obj is GameObject go) return go.transform;
        if (obj is Component component) return component.transform;
        return null;
    }

    static UnityEngine.Object ObjectAt(Transform transform, UnityEngine.Object example)
    {
        if (!transform) return null;
        return example is GameObject ? (UnityEngine.Object)transform.gameObject : transform.GetComponent(example.GetType());
    }

    static UnityEngine.Object MapReference(GameObject old, GameObject start, UnityEngine.Object target)
    {
        var transform=ObjectTransform(target);
        if (transform==old.transform) return ObjectAt(start.transform,target);
        var house=transform;
        while(house.parent!=old.transform) house=house.parent;
        Transform scope=start.transform;
        if(HouseNames.TryGetValue(house.name,out string newHouse))
        {
            var wrapper=start.transform.Find("03_Service_Houses/"+newHouse);
            scope=wrapper.Find("15_Gameplay_Source");
            if(!scope) scope=wrapper;
            string relative=AnimationUtility.CalculateTransformPath(transform,house);
            var direct=ObjectAt(relative.Length==0?scope:scope.Find(relative),target);
            if(direct) return direct;
        }
        var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(target);
        if(original)
        {
            var candidates=scope.GetComponentsInChildren<Transform>(true).Select(t=>ObjectAt(t,target))
                .Where(c=>c&&PrefabUtility.GetCorrespondingObjectFromOriginalSource(c)==original).ToArray();
            if(candidates.Length==1) return candidates[0];
        }
        string path=AnimationUtility.CalculateTransformPath(transform,old.transform);
        return ObjectAt(start.transform.Find(path),target);
    }

    static void Connect()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(scene.isDirty)throw new InvalidOperationException("Save your existing LevelEvent changes before connecting the castle");
        var roots=scene.GetRootGameObjects();
        var city=roots.SelectMany(o=>o.GetComponentsInChildren<CitySpawner>(true)).Single();
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Destination);
        if(roots.Any(o=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o)==Destination)&&city.cityPrefab==asset)return;
        var old=roots.Single(o=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o)==Previous);
        var start=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
        var position=old.transform.position;
        position.x=0; // The new castle's seven-meter river gap is centered on the level's channel.
        start.transform.SetPositionAndRotation(position,old.transform.rotation);
        start.transform.localScale=old.transform.localScale;
        var links=new List<(Component owner,string path,UnityEngine.Object replacement)>();
        try
        {
            foreach(var other in roots.Where(o=>o!=old))
                foreach(var component in other.GetComponentsInChildren<Component>(true).Where(c=>c&&!(c is Transform)))
                {
                    var iterator=new SerializedObject(component).GetIterator();
                    while(iterator.Next(true))
                    {
                        if(iterator.propertyType!=SerializedPropertyType.ObjectReference)continue;
                        var target=iterator.objectReferenceValue;
                        var t=ObjectTransform(target);
                        if(!t||!t.IsChildOf(old.transform))continue;
                        var replacement=MapReference(old,start,target);
                        if(!replacement)throw new InvalidOperationException($"Cannot map {component.name}/{iterator.propertyPath} to the new castle. Old target: {AnimationUtility.CalculateTransformPath(t,old.transform)} ({target.GetType().Name})");
                        links.Add((component,iterator.propertyPath,replacement));
                    }
                }
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(start);
            throw;
        }
        foreach(var link in links)
        {
            var serialized=new SerializedObject(link.owner);
            serialized.FindProperty(link.path).objectReferenceValue=link.replacement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(link.owner);
        }
        city.cityPrefab=asset;
        PrefabUtility.RecordPrefabInstancePropertyModifications(city);
        UnityEngine.Object.DestroyImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save LevelEvent");
        File.AppendAllText("Docs/locations/previews/event-castle174-verification.txt",
            $"LevelEvent start: {start.transform.position}, scale {start.transform.localScale}. CitySpawner: {AssetDatabase.GetAssetPath(city.cityPrefab)}\nRebound {links.Count} external scene references.\n");
    }
    [MenuItem("Tools/Asset Workshop/Build Winter Event Castle")]
    public static void Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building the castle");
        if(!File.Exists(Destination))
        {
            if(Directory.Exists(D))throw new InvalidOperationException("Existing castle output directory must be reviewed before retrying");
            try { Build(); }
            catch
            {
                // This invocation owns the newly created output folder.
                AssetDatabase.DeleteAsset(D);
                throw;
            }
        }
        Connect();
    }
}