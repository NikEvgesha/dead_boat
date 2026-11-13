using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterSignalPass209
{
    const string Event = "Assets/Events/NewYear/";
    const string Folder = Event+"ShorePOI/SignalPass209";
    const string Point = Event+"ShorePOI/POI_01_Snow_Tunnel.prefab";
    const float TrailHalfWidth = .95f;
    static readonly Vector3 Tower = new(1.25f,9.17f,3.20f);
    static readonly Vector3[] Route =
    {
        new(11.30f,.035f,-10.70f),new(10.85f,.43f,-8.25f),new(10.20f,3.36f,4.25f),
        new(9.55f,3.68f,5.50f),new(8.45f,3.91f,5.62f),new(7.78f,4.15f,4.53f),
        new(7.30f,6.48f,-5.85f),new(6.60f,6.72f,-6.95f),new(5.45f,6.96f,-6.80f),
        new(4.80f,7.23f,-5.66f),new(4.20f,8.91f,1.96f),new(3.35f,9.16f,3.20f),Tower
    };
    static readonly (int Segment, float Along, int Side)[] TrailPosts =
    {
        (0,.50f,1),(1,.20f,1),(1,.47f,1),(1,.73f,1),(2,.72f,1),(4,.65f,1),
        (5,.20f,-1),(5,.49f,-1),(5,.80f,-1),(7,.48f,-1),
        (9,.18f,1),(9,.52f,1),(9,.87f,1),(10,.92f,1)
    };
    static readonly int[][] Tetrahedra =
    {
        new[] {0,5,1,7},new[] {0,1,3,7},new[] {0,3,2,7},
        new[] {0,2,6,7},new[] {0,6,4,7},new[] {0,4,5,7}
    };

    static Vector3 V(float x,float y,float z) => new(x,y,z);
    static Vector2 XZ(Vector3 p) => new(p.x,p.z);
    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Event+name+".mat")
        ?? throw new InvalidOperationException("Missing material "+name);

    static float HillHeight(Vector3 p)
    {
        float x=(p.x+.65f)/11.3f, z=p.z/11.6f;
        float shape=1f-x*x-Mathf.Pow(Mathf.Abs(z),3.5f);
        float crest=1.75f*Mathf.Exp(-Mathf.Pow((p.x-1.3f)/4f,2)-Mathf.Pow((p.z-2.6f)/4.8f,2));
        float height=shape>0f? (8.20f+crest+.46f*Mathf.Sin(.52f*p.z+.64f*p.x))*Mathf.Pow(shape,.73f)-.19f : -.19f+shape*1.4f;
        float distance=12f, target=height;
        var sample=XZ(p);
        for(int i=0;i<Route.Length-1;i++)
        {
            var a=Route[i]; var b=Route[i+1];
            var span=XZ(b-a);
            float t=Mathf.Clamp01(Vector2.Dot(sample-XZ(a),span)/span.sqrMagnitude);
            float d=(sample-XZ(a)-span*t).sqrMagnitude;
            if(d>=distance) continue;
            distance=d;
            target=Mathf.Lerp(a.y,b.y,t)-.14f;
        }
        float influence=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(1.10f,2.55f,Mathf.Sqrt(distance)));
        float plateau=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(1.70f,3.00f,Vector2.Distance(sample,XZ(Tower))));
        return Mathf.Lerp(Mathf.Lerp(height,Tower.y-.13f,plateau),target,influence);
    }

    static float Opening(Vector3 p)
    {
        float mouth=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6f,10.5f,Mathf.Abs(p.z)));
        float across=(p.x+.65f-.20f*Mathf.Sin(p.z*.27f))/(3.06f+.43f*mouth);
        float up=Mathf.Max(p.y-.42f,0)/(3.96f+.38f*mouth);
        return (Mathf.Sqrt(across*across+up*up)-1f)*3f;
    }

    static Mesh Save(string name,List<Vector3> vertices,List<Vector3> normals,params List<int>[] triangles)
    {
        string path=Folder+"/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool created=!mesh;
        if(created) mesh=new Mesh {name=name};
        mesh.Clear();
        mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.subMeshCount=triangles.Length;
        for(int i=0;i<triangles.Length;i++) mesh.SetTriangles(triangles[i],i);
        if(normals!=null) mesh.SetNormals(normals);
        else mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if(created) AssetDatabase.CreateAsset(mesh,path);
        else {EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);}
        return mesh;
    }

    static Mesh Mountain()
    {
        const float cell=.45f;
        const int nx=61,ny=27,nz=57;
        Vector3 Position(int x,int y,int z) => V(-13.6f+x*cell,-.58f+y*cell,-12.6f+z*cell);
        var field=new float[nx,ny,nz];
        for(int x=0;x<nx;x++)
            for(int z=0;z<nz;z++)
            {
                float top=HillHeight(Position(x,0,z));
                for(int y=0;y<ny;y++)
                {
                    var p=Position(x,y,z);
                    field[x,y,z]=Mathf.Max(Mathf.Max(p.y-top,-.23f-p.y),-Opening(p));
                }
            }

        var vertices=new List<Vector3>();
        var normals=new List<Vector3>();
        var gradients=new Dictionary<Vector3,Vector3>();
        var indices=new[]{new List<int>(),new List<int>()};
        var corners=new Vector3[8]; var values=new float[8];
        var inside=new int[4]; var outside=new int[4];
        Vector3 CrossEdge(int a,int b) => Vector3.Lerp(corners[a],corners[b],values[a]/(values[a]-values[b]));
        float Field(Vector3 p) => Mathf.Max(Mathf.Max(p.y-HillHeight(p),-.23f-p.y),-Opening(p));
        Vector3 ShadingNormal(Vector3 point,Vector3 face)
        {
            if(!gradients.TryGetValue(point,out var gradient))
            {
                const float d=.085f;
                gradient=V(Field(point+Vector3.right*d)-Field(point-Vector3.right*d),
                    Field(point+Vector3.up*d)-Field(point-Vector3.up*d),
                    Field(point+Vector3.forward*d)-Field(point-Vector3.forward*d)).normalized;
                gradients.Add(point,gradient);
            }
            return Vector3.Lerp(face,gradient,.93f).normalized;
        }
        void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
        {
            var cross=Vector3.Cross(b-a,c-a);
            if(cross.sqrMagnitude<1e-9f) return;
            if(Vector3.Dot(cross,normal)<0) { (b,c)=(c,b); cross=-cross; }
            var midpoint=(a+b+c)/3f;
            var faces=indices[midpoint.y>-.15f && -Opening(midpoint)>=midpoint.y-HillHeight(midpoint)-.035f ? 1 : 0];
            int first=vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            var face=cross.normalized;
            normals.Add(ShadingNormal(a,face)); normals.Add(ShadingNormal(b,face)); normals.Add(ShadingNormal(c,face));
            faces.Add(first); faces.Add(first+1); faces.Add(first+2);
        }
        for(int x=0;x<nx-1;x++)
            for(int y=0;y<ny-1;y++)
                for(int z=0;z<nz-1;z++)
                {
                    int count=0;
                    for(int i=0;i<8;i++)
                    {
                        int ix=x+(i&1),iy=y+((i>>1)&1),iz=z+((i>>2)&1);
                        corners[i]=Position(ix,iy,iz); values[i]=field[ix,iy,iz];
                        if(values[i]<0) count++;
                    }
                    if(count==0 || count==8) continue;
                    foreach(var tetra in Tetrahedra)
                    {
                        int a=0,b=0;
                        var inn=Vector3.zero; var outer=Vector3.zero;
                        foreach(int i in tetra)
                        {
                            if(values[i]<0) {inside[a++]=i; inn+=corners[i];}
                            else {outside[b++]=i; outer+=corners[i];}
                        }
                        if(a==0 || b==0) continue;
                        var normal=outer/b-inn/a;
                        if(a==1) Face(CrossEdge(inside[0],outside[0]),CrossEdge(inside[0],outside[1]),CrossEdge(inside[0],outside[2]),normal);
                        else if(b==1) Face(CrossEdge(inside[0],outside[0]),CrossEdge(inside[1],outside[0]),CrossEdge(inside[2],outside[0]),normal);
                        else
                        {
                            var p=CrossEdge(inside[0],outside[0]); var q=CrossEdge(inside[0],outside[1]);
                            var r=CrossEdge(inside[1],outside[1]); var s=CrossEdge(inside[1],outside[0]);
                            Face(p,q,r,normal); Face(p,r,s,normal);
                        }
                    }
                }
        return Save("Signal_Pass_Mountain",vertices,normals,indices);
    }

    static Vector3 Across(int i)
    {
        var incoming=Vector3.Cross(Vector3.up,Route[i]-Route[Mathf.Max(0,i-1)]).normalized;
        var outgoing=Vector3.Cross(Vector3.up,Route[Mathf.Min(Route.Length-1,i+1)]-Route[i]).normalized;
        var bisector=(incoming+outgoing).normalized;
        return bisector*(TrailHalfWidth/Mathf.Max(.77f,Vector3.Dot(bisector,i==0?outgoing:incoming)));
    }

    static Mesh Trail()
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int i=0;i<Route.Length;i++)
        {
            vertices.Add(Route[i]-Across(i)); vertices.Add(Route[i]+Across(i));
            if(i==0) continue;
            int a=i*2-2;
            triangles.AddRange(new[]{a,a+2,a+3,a,a+3,a+1});
        }
        return Save("Signal_Pass_Trail",vertices,null,triangles);
    }

    static Mesh Ice()
    {
        var baseRing=new[]{V(-.48f,0,-.30f),V(.28f,0,-.43f),V(.56f,0,.06f),V(.25f,0,.47f),V(-.45f,0,.36f)};
        var vertices=new List<Vector3>(); var bright=new List<int>(); var dark=new List<int>();
        var tip=V(.29f,1f,-.12f);
        for(int i=0;i<baseRing.Length;i++)
        {
            var a=baseRing[i]; var b=baseRing[(i+1)%baseRing.Length];
            var shoulder=V(a.x*.64f+.11f,.70f,a.z*.60f-.07f);
            var next=V(b.x*.64f+.11f,.70f,b.z*.60f-.07f);
            void Face(List<int> set,Vector3 p,Vector3 q,Vector3 r)
            {
                int first=vertices.Count;
                vertices.Add(p); vertices.Add(q); vertices.Add(r);
                set.AddRange(new[]{first,first+1,first+2});
            }
            var mat=i%2==0?bright:dark;
            Face(mat,a,shoulder,b); Face(mat,shoulder,next,b); Face(mat,shoulder,tip,next);
            Face(dark,Vector3.zero,a,b);
        }
        return Save("Signal_Pass_Ice",vertices,null,bright,dark);
    }

    static Transform Group(Transform parent,string name,Vector3 position)
    {
        var group=new GameObject(name).transform;
        group.SetParent(parent,false); group.localPosition=position;
        return group;
    }

    static Transform MeshPart(Transform parent,string name,Vector3 position,Mesh mesh,bool solid,params Material[] mats)
    {
        var part=Group(parent,name,position);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        part.gameObject.AddComponent<MeshRenderer>().sharedMaterials=mats;
        if(solid) part.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
        return part;
    }

    static Transform Block(Transform parent,string name,Material mat,Vector3 position,Vector3 size,bool solid=false)
    {
        var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name=name;
        block.transform.SetParent(parent,false); block.transform.localPosition=position; block.transform.localScale=size;
        block.GetComponent<MeshRenderer>().sharedMaterial=mat;
        if(!solid) Object.DestroyImmediate(block.GetComponent<BoxCollider>());
        return block.transform;
    }

    static void BuildTower(Transform pass)
    {
        var tower=Group(pass,"Signal_Tower",Tower);
        var wood=Mat("Models/Wood_Brown"); var dark=Mat("Route179/Log_Bark"); var snow=Mat("Snow");
        Block(tower,"Walkable_Deck",wood,V(0,-.033f,0),V(3.5f,.17f,2.9f),true);
        for(int side=-1;side<=1;side+=2)
        {
            for(int end=-1;end<=1;end+=2)
                Block(tower,$"Post_{side}_{end}",dark,V(side*1.47f,1.37f,end*1.15f),V(.21f,2.78f,.21f),true);
            Block(tower,$"Rail_Z_{side}",wood,V(0,.79f,side*1.28f),V(3.0f,.19f,.15f),true);
            var roof=Block(tower,$"Gabled_Roof_{side}",dark,V(side*.86f,2.86f,0),V(2.0f,.22f,3.32f));
            roof.localRotation=Quaternion.Euler(0,0,-side*28f);
            var cap=Block(tower,$"Snow_Cap_{side}",snow,V(side*.86f,3.02f,0),V(1.99f,.13f,3.38f));
            cap.localRotation=roof.localRotation;
        }
        Block(tower,"Rear_Rail",wood,V(-1.54f,.79f,0),V(.15f,.19f,2.55f),true);
        Block(tower,"Lantern_Hanger",dark,V(0,2.42f,-.98f),V(.09f,.60f,.09f));
        Block(tower,"Signal_Lantern",Mat("WinterTown169/Warm_Light"),V(0,1.98f,-.98f),V(.42f,.49f,.42f));
        Block(tower,"Lantern_Rim",dark,V(0,1.70f,-.98f),V(.50f,.10f,.50f));
        Block(tower,"Flag_Pole",wood,V(.15f,3.81f,.43f),V(.10f,1.16f,.10f));
        var flag=Block(tower,"Red_Pennant",Mat("Ruby"),V(.58f,4.11f,.43f),V(.81f,.35f,.055f));
        flag.localRotation=Quaternion.Euler(0,-18f,0);
        FinishTowerAndTrail(pass,LanternMaterial());
    }

    static Material LanternMaterial()
    {
        string path=Folder+"/Signal_Lantern_Glow.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material) return material;
        material=new Material(Mat("WinterTown169/Warm_Light")) {name="Signal Lantern Glow"};
        material.color=new Color(1f,.77f,.35f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor",new Color(1.48f,.80f,.24f));
        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(material,path);
        return material;
    }

    static void FinishTowerAndTrail(Transform pass,Material glass)
    {
        var tower=pass.Find("Signal_Tower");
        var lantern=tower.Find("Signal_Lantern");
        lantern.GetComponent<MeshRenderer>().sharedMaterial=glass;
        var source=tower.Find("Lantern_Warm_Light");
        if(!source) source=Group(tower,"Lantern_Warm_Light",lantern.localPosition+Vector3.down*.34f);
        var light=source.GetComponent<Light>();
        if(!light) light=source.gameObject.AddComponent<Light>();
        light.type=LightType.Point;
        light.renderMode=LightRenderMode.ForcePixel;
        light.color=new Color(1f,.66f,.32f);
        light.intensity=2.4f;
        light.range=5f;

        var dark=Mat("Route179/Log_Bark");
        var snow=Mat("Snow");
        for(int i=0;i<TrailPosts.Length;i++)
        {
            var (segment,along,side)=TrailPosts[i];
            var a=Route[segment]+Across(segment)*side;
            var b=Route[segment+1]+Across(segment+1)*side;
            var edge=Vector3.Lerp(a,b,along);
            string postName=$"Trail_Marker_{i+1:00}",capName=$"Marker_Snow_{i+1:00}";
            var post=pass.Find(postName);
            var cap=pass.Find(capName);
            if(!post) post=Block(pass,postName,dark,Vector3.zero,V(.16f,.56f,.16f));
            if(!cap) cap=Block(pass,capName,snow,Vector3.zero,V(.19f,.085f,.19f));
            post.localPosition=edge+Vector3.up*.245f;
            cap.localPosition=edge+Vector3.up*.565f;
        }
    }

    public static string LightLanternAndAlignPosts()
    {
        var glass=LanternMaterial();
        var root=PrefabUtility.LoadPrefabContents(Point);
        try
        {
            FinishTowerAndTrail(root.transform.Find("Signal_Pass"),glass);
            PrefabUtility.SaveAsPrefabAsset(root,Point,out var saved);
            if(!saved) throw new InvalidOperationException("Failed to save signal tower and trail details");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        int updated=0;
        if(EditorApplication.isPlaying)
            foreach(var location in Object.FindObjectsByType<LocationContentSpawner>(FindObjectsSortMode.None))
            {
                var pass=location.transform.Find("Signal_Pass");
                if(!pass) continue;
                FinishTowerAndTrail(pass,glass);
                updated++;
            }
        return $"Lit the lantern and placed {TrailPosts.Length} edge posts; {updated} live signal passes updated";
    }

    static void BuildPortal(Transform pass,Mesh ice,float end)
    {
        var portal=Group(pass,end<0?"Ice_Mouth_River":"Ice_Mouth_Far",V(-.40f,0,end*10.0f));
        var cyan=Mat("Route181/Satin_Ice_Cyan"); var blue=Mat("Route181/Satin_Ice_Blue");
        for(int side=-1;side<=1;side+=2)
        {
            void Shard(string name,Vector3 p,Vector3 scale,float yaw)
            {
                var shard=MeshPart(portal,name+side,p,ice,false,cyan,blue);
                shard.localScale=scale; shard.localRotation=Quaternion.Euler(0,yaw,0);
            }
            Shard("Portal_Blade_",V(side*4.12f,-.14f,end*.50f),V(2.32f,side<0?6.32f:5.55f,2.24f),side<0?14f:164f);
            SetPortalBlade(portal.Find("Portal_Blade_"+side),side);
            Shard("Outer_Blade_",V(side*5.32f,-.11f,end*.79f),V(1.58f,3.50f,1.85f),side*33f);
            Shard("Side_Shard_",V(side*3.88f,-.14f,end*1.89f),V(1.19f,1.22f,1.33f),side*52f);
            Shard("Shard_Foot_",V(side*5.07f,-.09f,end*2.16f),V(1.15f,.79f,.95f),side*94f);
            Shard("Embedded_Rib_",V(side*3.58f,.80f,-end*1.46f),V(.77f,3.08f,.94f),side*105f);
        }
    }

    static void SetPortalBlade(Transform blade,int side)
    {
        blade.localRotation=Quaternion.AngleAxis(side*16f,Vector3.forward)*Quaternion.Euler(0,side<0?14f:164f,0);
        var collision=blade.GetComponent<MeshCollider>();
        if(!collision) collision=blade.gameObject.AddComponent<MeshCollider>();
        collision.sharedMesh=blade.GetComponent<MeshFilter>().sharedMesh;
    }

    public static string Refine()
    {
        var hill=Mountain();
        var root=PrefabUtility.LoadPrefabContents(Point);
        try
        {
            var pass=root.transform.Find("Signal_Pass");
            var shell=pass.Find("Snow_Mountain").GetComponent<MeshCollider>();
            shell.sharedMesh=null;
            shell.sharedMesh=hill;
            foreach(var mouth in new[]{"Ice_Mouth_River","Ice_Mouth_Far"})
                for(int side=-1;side<=1;side+=2) SetPortalBlade(pass.Find(mouth+"/Portal_Blade_"+side),side);
            FitRegion(root.transform,pass);
            PrefabUtility.SaveAsPrefabAsset(root,Point,out var saved);
            if(!saved) throw new InvalidOperationException("Failed to save portal refinement");
            return "Smoothed snow normals and solid inward-leaning portal ice";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Bounds FitRegion(Transform root,Transform pass)
    {
        var bounds=pass.GetComponentInChildren<Renderer>().bounds;
        foreach(var renderer in pass.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
        if(bounds.max.x>-5.7f) throw new InvalidOperationException("Landmark enters river: "+bounds);
        var region=root.GetComponent<BoxCollider>();
        region.center=bounds.center;
        region.size=bounds.size+V(1.4f,1.1f,2.2f);
        var cleanup=root.Find("Keep_Background_Decor_Clear");
        cleanup.localPosition=region.center;
        cleanup.GetComponent<BoxCollider>().size=region.size;
        return bounds;
    }

    public static string Build()
    {
        var open=AssetDatabase.LoadAssetAtPath<GameObject>(Point);
        if(open.transform.Find("Signal_Pass")) throw new InvalidOperationException("Signal pass already authored");
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Event+"ShorePOI","SignalPass209");
        var hill=Mountain(); var trail=Trail(); var ice=Ice();
        var root=PrefabUtility.LoadPrefabContents(Point);
        try
        {
            var previous=root.transform.GetChild(0);
            previous.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(previous.gameObject);
            var pass=Group(root.transform,"Signal_Pass",V(-24f,0,0));
            pass.localRotation=Quaternion.Euler(0,-43f,0);
            var shell=MeshPart(pass,"Snow_Mountain",Vector3.zero,hill,true,Mat("Snow"),Mat("Route182/Blue_Packed_Snow"));
            shell.GetComponent<MeshRenderer>().receiveShadows=false;
            MeshPart(pass,"Switchback_Trail",Vector3.zero,trail,true,Mat("Route179/Snow_Drift_Blue"));
            BuildTower(pass);
            BuildPortal(pass,ice,-1f); BuildPortal(pass,ice,1f);

            var markers=Group(pass,"Trail_Checkpoints",Vector3.zero);
            for(int i=0;i<Route.Length;i++) Group(markers,$"Trail_{i:00}",Route[i]);
            var content=root.GetComponent<LocationContentSpawner>();
            content.itemSpawnPoints[0].spawnTransform.position=pass.TransformPoint(V(1.35f,.22f,-6.35f));
            content.itemSpawnPoints[1].spawnTransform.position=pass.TransformPoint(Tower+V(-.42f,.26f,.53f));
            content.enemySpawnPoints[0].position=pass.TransformPoint(V(-1.65f,.03f,11.20f));
            var bounds=FitRegion(root.transform,pass);
            PrefabUtility.SaveAsPrefabAsset(root,Point,out var saved);
            if(!saved) throw new InvalidOperationException("Failed to save Signal Pass prefab");
            return $"Signal pass {bounds.min:F2} to {bounds.max:F2}; {hill.vertexCount} mountain vertices; {Route.Length} trail points";
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
