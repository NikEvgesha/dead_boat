// Unity MCP script-execute: BuildTower1Workshop.Build.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BuildTower1Workshop
{
    const string Folder="Assets/Prefabs/Workshop/Tower1/";
    const string Prefab=Folder+"Tower1_OvergrownCourt.prefab";
    const string ScenePath="Assets/Scenes/Tower1Workshop.unity";
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
    static Vector3 Polar(float a,float r,float y=0)
    {a*=Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*r,y,-Mathf.Cos(a)*r);}
    static Vector2 P(float a,float r){var v=Polar(a,r);return new Vector2(v.x,v.z);}
    static void Sector(Transform parent,string name,float a,float b,float inner,float outer,float low,float high,Material m)
    {
        Polygon(parent,name,new[]{P(a,inner),P(a,outer),P((a+b)*.5f,outer),P(b,outer),P(b,inner),P((a+b)*.5f,inner)},low,high,m);
    }
    static void Solid(Transform parent,string name,Vector3[] corners,int[][] faces,Material mat)
    {
        var vertices=new List<Vector3>();var indices=new List<int>();
        foreach(var face in faces)
        {
            var points=face.Select(i=>corners[i]).ToArray();
            var center=corners.Aggregate(Vector3.zero,(s,p)=>s+p)/corners.Length;
            var faceCenter=points.Aggregate(Vector3.zero,(s,p)=>s+p)/points.Length;
            if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),faceCenter-center)<0)Array.Reverse(points);
            int first=vertices.Count;vertices.AddRange(points);
            for(int i=1;i<face.Length-1;i++)indices.AddRange(new[]{first,first+i,first+i+1});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject(parent,name,mesh,new[]{mat},true);
    }
    static void OuterWall(Transform parent,string name,float a,float b,float h0,float h1,Material m)
    {
        var v=new[]{Polar(a,5.61f,-.02f),Polar(b,5.61f,-.02f),Polar(b,5.91f,-.02f),Polar(a,5.91f,-.02f),
            Polar(a,5.61f,h0),Polar(b,5.61f,h1),Polar(b,5.91f,h1),Polar(a,5.91f,h0)};
        Solid(parent,name,v,new[]{new[]{0,1,5,4},new[]{3,7,6,2},new[]{4,5,6,7},
            new[]{0,3,2,1},new[]{0,4,7,3},new[]{1,2,6,5}},m);
    }
    // Treads share one boundary mesh, with no coincident internal faces or landing tops.
    sealed class StoneMesh
    {
        readonly List<Vector3> vertices=new();readonly List<int> indices=new();
        public void Face(Vector3 normal,params Vector3[] points)
        {
            if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),normal)<0)Array.Reverse(points);
            int first=vertices.Count;vertices.AddRange(points);
            for(int i=1;i<points.Length-1;i++)indices.AddRange(new[]{first,first+i,first+i+1});
        }
        public Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
    static float RoofRadius(float angle)
    {
        float deviation=Mathf.DeltaAngle(Mathf.Round(angle/45)*45,angle);
        return 3.5f*Mathf.Cos(22.5f*Mathf.Deg2Rad)/Mathf.Cos(deviation*Mathf.Deg2Rad);
    }
    static void BuildJoinedStair(Transform stairs,Material stone,Material cap)
    {
        const float outside=5.6f,inside=2.52f,lastAngle=-318.5f;
        var tread=new StoneMesh();var wall=new StoneMesh();
        // Each piece is split at roof vertices, preserving exact contact with the octagon.
        var spans=Enumerable.Range(0,40).Select(i=>(a:-35-i*6.75f,b:-35-(i+1)*6.75f,h:(i+1)*.16f,prev:i*.16f,join:i==39))
            .Concat(new[]{(a:-305f,b:lastAngle,h:Top,prev:Top,join:true)});
        foreach(var span in spans)
        {
            var angles=new List<float>{span.a,span.b,(span.a+span.b)*.5f};
            for(int k=-12;k<4;k++){float v=22.5f+k*45;if(v<span.a&&v>span.b)angles.Add(v);}
            angles=angles.Distinct().OrderByDescending(a=>a).ToList();
            float Inner(float a)=>span.join?RoofRadius(a):inside;
            float WallTop(float a)=>span.prev>=Top-.001f?Top+.78f:span.prev+.78f+.16f*(span.a-a)/(span.a-span.b);
            for(int j=0;j<angles.Count-1;j++)
            {
                float a=angles[j],b=angles[j+1],mid=(a+b)*.5f;
                var ia=Polar(a,Inner(a),span.h);var ib=Polar(b,Inner(b),span.h);
                var oa=Polar(a,outside,span.h);var ob=Polar(b,outside,span.h);
                tread.Face(Vector3.up,ia,oa,ob,ib);
                tread.Face(Polar(mid,1),oa,Polar(a,outside,-.035f),Polar(b,outside,-.035f),ob);
                tread.Face(-Polar(mid,1),ia,ib,Polar(b,Inner(b),-.035f),Polar(a,Inner(a),-.035f));
                tread.Face(Vector3.down,Polar(a,Inner(a),-.035f),Polar(b,Inner(b),-.035f),Polar(b,outside,-.035f),Polar(a,outside,-.035f));
                float h0=WallTop(a),h1=WallTop(b);
                var wi0=Polar(a,5.61f,h0);var wi1=Polar(b,5.61f,h1);
                var wo0=Polar(a,5.91f,h0);var wo1=Polar(b,5.91f,h1);
                wall.Face(Vector3.up,wi0,wi1,wo1,wo0);
                wall.Face(-Polar(mid,1),wi0,Polar(a,5.61f,-.035f),Polar(b,5.61f,-.035f),wi1);
                wall.Face(Polar(mid,1),wo0,wo1,Polar(b,5.91f,-.035f),Polar(a,5.91f,-.035f));
                wall.Face(Vector3.down,Polar(a,5.61f,-.035f),Polar(a,5.91f,-.035f),Polar(b,5.91f,-.035f),Polar(b,5.61f,-.035f));
            }
            if(span.h-span.prev>.001f)
            {
                float a=span.a;var forward=Polar(a+90,1);
                tread.Face(forward,Polar(a,inside,span.prev),Polar(a,outside,span.prev),Polar(a,outside,span.h),Polar(a,inside,span.h));
            }
        }
        var endNormal=Polar(lastAngle-90,1);
        tread.Face(endNormal,Polar(lastAngle,RoofRadius(lastAngle),-.035f),Polar(lastAngle,RoofRadius(lastAngle),Top),
            Polar(lastAngle,outside,Top),Polar(lastAngle,outside,-.035f));
        foreach(float a in new[]{-35f,lastAngle})
        {
            float h=a==-35?.78f:Top+.78f;
            wall.Face(Polar(a+(a==-35?90:-90),1),Polar(a,5.61f,-.035f),Polar(a,5.91f,-.035f),Polar(a,5.91f,h),Polar(a,5.61f,h));
        }
        MeshObject(stairs,"Continuous_Stair",tread.Finish("Continuous_Stair"),new[]{stone},true);
        MeshObject(stairs,"Continuous_Outer_Parapet",wall.Finish("Continuous_Outer_Parapet"),new[]{cap},true);
        // Close the exposed far edge of the new top step, keeping the radial roof entrance open.
        BuildLandingWall(stairs,cap);
    }
    static void BuildLandingWall(Transform stairs,Material cap)
    {
        const float a=41.5f;
        float r0=RoofRadius(a)*3.12f/3.5f-.04f,r1=5.95f;
        var tangent=Polar(a+90,.12f);var mesh=new StoneMesh();
        const float bottom=Top-.04f;
        Vector3 P(float r,float y,float side)=>Polar(a,r,y)+tangent*side;
        mesh.Face(Vector3.up,P(r0,Top+.78f,-1),P(r1,Top+.78f,-1),P(r1,Top+.78f,1),P(r0,Top+.78f,1));
        foreach(int side in new[]{-1,1})mesh.Face(tangent*side,P(r0,bottom,side),P(r1,bottom,side),P(r1,Top+.78f,side),P(r0,Top+.78f,side));
        foreach(var edge in new[]{(r:r0,n:-1),(r:r1,n:1)})mesh.Face(Polar(a,edge.n),P(edge.r,bottom,-1),P(edge.r,bottom,1),P(edge.r,Top+.78f,1),P(edge.r,Top+.78f,-1));
        // Seal the exposed underside below the floor plane, preventing both see-through and coplanar flicker.
        mesh.Face(Vector3.down,P(r0,bottom,-1),P(r0,bottom,1),P(r1,bottom,1),P(r1,bottom,-1));
        MeshObject(stairs,"Landing_End_Parapet",mesh.Finish("Continuous_Landing_Wall"),new[]{cap},true);
    }
    static void BuildCornerJoin(Transform tower,Material stone)
    {
        // A full-height masonry pier fills the radial recess between the core and the stair end.
        const float angle=41.5f,inner=2.48f,outer=3.40f,bottom=-.04f,high=Top-.02f;
        Box(tower,"Stair_Corner_Masonry",Polar(angle,(inner+outer)*.5f,(bottom+high)*.5f),
            new Vector3(.30f,high-bottom,outer-inner),stone,-angle);
    }
    static void BuildWallIvy(Transform tower)
    {
        var ivy=Child(tower,"Wall_Ivy");
        var green=Color("Ivy_Green","#639747");var stem=Color("Ivy_Stem","#4E6B35");
        void Vine(string name,float angle,float first,float spacing,int count,bool frontRight=false)
        {
            var group=Child(ivy,name);float face=Mathf.Round(angle/45)*45;
            float radius=2.8f*Mathf.Cos(22.5f*Mathf.Deg2Rad)/Mathf.Cos((angle-face)*Mathf.Deg2Rad);
            var normal=Polar(face,1);var anchor=Polar(angle,radius);
            var tangent=new Vector3(normal.z,0,-normal.x);
            float low=first-.20f,high=first+(count-1)*spacing+.15f;
            Box(group,"Attached_Stem",anchor+normal*.025f+Vector3.up*((low+high)*.5f),new Vector3(.045f,high-low,.045f),stem);
            for(int i=0;i<count;i++)
            {
                float zigzag=frontRight?-.14f-(i%2)*.06f:(i%2==0?.14f:-.14f);
                var p=anchor+normal*.055f+tangent*zigzag+Vector3.up*(first+i*spacing);
                Box(group,"Attached_Leaf_"+i,p,new Vector3(.33f,.37f,.075f),green,-face);
                group.Find("Attached_Leaf_"+i).Rotate(0,0,i%2==0?25:-25,Space.Self);
            }
        }
        Vine("Front_Left",-45,1.65f,.43f,10);
        Vine("Front_Right",27,1.65f,.43f,10,true);
        // Rear vines start above the stair surface, keeping foliage out of the climb route.
        Vine("Rear_Center",180,4.10f,.33f,6);
        Vine("Rear_Left",225,3.00f,.34f,9);
    }
    public static string JoinCornerAndAddRearIvy()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var tower=root.transform.Find("Tower");
            foreach(string name in new[]{"Stair_Corner_Masonry","Wall_Ivy"})
            {var old=tower.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
            BuildCornerJoin(tower,AssetDatabase.LoadAssetAtPath<Material>(Folder+"Tower_Stone.mat"));
            BuildWallIvy(tower);Physics.SyncTransforms();
            var stairs=root.transform.Find("Low_Wraparound_Stair").GetComponentsInChildren<Collider>()
                .Append(tower.Find("Stair_Corner_Masonry").GetComponent<Collider>()).ToArray();
            int tested=0;
            foreach(var leaf in tower.Find("Wall_Ivy").GetComponentsInChildren<BoxCollider>())
            foreach(var stair in stairs)
            {
                if(Physics.ComputePenetration(leaf,leaf.transform.position,leaf.transform.rotation,stair,stair.transform.position,
                    stair.transform.rotation,out _,out float depth)&&depth>.002f)
                    throw new InvalidOperationException("Ivy intersects staircase: "+leaf.transform.parent.name+"/"+leaf.name);
                tested++;
            }
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Cannot save corner and ivy");
            AssetDatabase.SaveAssets();return $"Full-height stone corner joined; two front and two rear ivy strands saved; {tested} ivy/stair clearance pairs passed.";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static string SealLandingUnderside()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var stairs=root.transform.Find("Low_Wraparound_Stair");
            var old=stairs.Find("Landing_End_Parapet");if(old)Object.DestroyImmediate(old.gameObject);
            BuildLandingWall(stairs,AssetDatabase.LoadAssetAtPath<Material>(Folder+"Pale_Coping.mat"));
            Physics.SyncTransforms();var wall=stairs.Find("Landing_End_Parapet").GetComponent<MeshCollider>();int count=0;
            for(int i=0;i<12;i++)foreach(float side in new[]{-.07f,0,.07f})
            {
                var p=Polar(41.5f,3.05f+i*.24f,Top-.40f)+Polar(131.5f,side,0);
                if(!wall.Raycast(new Ray(p,Vector3.up),out var hit,.5f)||hit.normal.y>-.99f||Mathf.Abs(hit.point.y-(Top-.04f))>.001f)
                    throw new InvalidOperationException("Open landing underside at "+p);
                count++;
            }
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Cannot save sealed underside");
            AssetDatabase.SaveAssets();return $"PASS {count} upward rays: solid underside at {Top-.04f:F2} m, 4 cm below the walking surface.";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BuildRoof(Transform tower,Material stone,Material cap)
    {
        var roof=new StoneMesh();
        float Radius(float a,float r)=>RoofRadius(a)*r/3.5f;
        Vector3 Inner(float a,float y)=>Polar(a,Radius(a,3.12f),y);
        Vector3 Outer(float a,float y)=>Polar(a,RoofRadius(a),y);
        var spans=new List<(float a,float b,float h)>();
        for(int i=0;i<8;i++)
        {
            float a=22.5f+i*45,b=a+45;
            if(i==0){spans.Add((a,41.5f,.78f));spans.Add((41.5f,b,0));}
            else spans.Add((a,b,.62f));
        }
        for(int i=0;i<spans.Count;i++)
        {
            var s=spans[i];var next=spans[(i+1)%spans.Count];
            float a=s.a,b=s.b,mid=(a+b)*.5f;
            roof.Face(Vector3.up,new Vector3(0,Top,0),Inner(a,Top),Inner(b,Top));
            roof.Face(Vector3.down,new Vector3(0,Top-.16f,0),Outer(a,Top-.16f),Outer(b,Top-.16f));
            roof.Face(Vector3.up,Inner(a,Top+s.h),Outer(a,Top+s.h),Outer(b,Top+s.h),Inner(b,Top+s.h));
            roof.Face(Polar(mid,1),Outer(a,Top-.16f),Outer(b,Top-.16f),Outer(b,Top+s.h),Outer(a,Top+s.h));
            if(s.h>0)roof.Face(-Polar(mid,1),Inner(a,Top),Inner(b,Top),Inner(b,Top+s.h),Inner(a,Top+s.h));
            if(Mathf.Abs(s.h-next.h)>.001f)
            {
                float low=Top+Mathf.Min(s.h,next.h),high=Top+Mathf.Max(s.h,next.h);
                roof.Face(Polar(b+(s.h>next.h?90:-90),1),Inner(b,low),Outer(b,low),Outer(b,high),Inner(b,high));
            }
        }
        MeshObject(tower,"Rooftop_Floor",roof.Finish("Continuous_Rooftop"),new[]{cap},true);
        var parapet=Child(tower,"Roof_Parapet");
        for(int i=1;i<8;i++)
        {
            float angle=45+i*45;
            Box(parapet,"Battlement_"+i,Polar(angle,3.06f,Top+.90f),new Vector3(.70f,.60f,.49f),stone,-angle);
        }
    }
    static void BuildDoor(Transform tower)
    {
        var door=Child(tower,"Detailed_Arched_Door");
        var dark=Color("Door_Shadow_Wood","#4F3524");var iron=Color("Door_Iron","#353D43");var brass=Color("Door_Brass","#AB843B");
        var wood=new[]{Color("Door_Oak_1","#987044"),Color("Door_Oak_2","#AB8050"),Color("Door_Oak_3","#A27748")};
        var outline=new List<Vector2>{new(-.73f,.015f),new(.73f,.015f)};
        for(int i=0;i<=12;i++){float a=i*Mathf.PI/12;outline.Add(new Vector2(.73f*Mathf.Cos(a),1.95f+.73f*Mathf.Sin(a)));}
        var backing=Child(door,"Arched_Backing");Polygon(backing,"Door_Arched_Backing",outline.ToArray(),-.035f,.035f,dark);
        backing.localRotation=Quaternion.Euler(-90,0,0);backing.localPosition=new Vector3(0,0,-2.76f);
        for(int i=0;i<7;i++)
        {
            float left=-.70f+i*.2f+.006f,right=left+.188f,mid=(left+right)*.5f;
            float Height(float x)=>1.95f+Mathf.Sqrt(Mathf.Max(0,.70f*.70f-x*x));
            var plank=Child(door,"Plank_"+i);
            Polygon(plank,"Door_Plank_"+i,new[]{new Vector2(left,.04f),new Vector2(right,.04f),new Vector2(right,Height(right)),new Vector2(mid,Height(mid)),new Vector2(left,Height(left))},-.022f,.022f,wood[i%3]);
            plank.localRotation=Quaternion.Euler(-90,0,0);plank.localPosition=new Vector3(0,0,-2.815f);
        }
        foreach(float y in new[]{.61f,1.65f})
        {
            Box(door,"Iron_Strap_"+y,new Vector3(0,y,-2.865f),new Vector3(1.28f,.115f,.04f),iron);
            Box(door,"Hinge_"+y,new Vector3(-.67f,y,-2.88f),new Vector3(.10f,.19f,.065f),iron);
            foreach(float x in new[]{-.51f,.51f})Box(door,$"Rivet_{x}_{y}",new Vector3(x,y,-2.893f),new Vector3(.035f,.035f,.025f),brass);
        }
        Box(door,"Handle_Plate",new Vector3(.39f,1.12f,-2.865f),new Vector3(.13f,.21f,.04f),iron);
        Box(door,"Handle_Pin",new Vector3(.39f,1.17f,-2.93f),new Vector3(.045f,.045f,.10f),brass);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
            var p=new Vector3(.39f+.085f*Mathf.Cos(a),1.085f+.085f*Mathf.Sin(a),-2.98f);
            var q=new Vector3(.39f+.085f*Mathf.Cos(b),1.085f+.085f*Mathf.Sin(b),-2.98f);
            Box(door,"Handle_Ring_"+i,(p+q)*.5f,new Vector3(Vector3.Distance(p,q)+.01f,.025f,.025f),brass);
            door.Find("Handle_Ring_"+i).localRotation=Quaternion.Euler(0,0,Mathf.Atan2(q.y-p.y,q.x-p.x)*Mathf.Rad2Deg);
        }
    }
    public static string RefineRoofAndDoor()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var tower=root.transform.Find("Tower");var stairs=root.transform.Find("Low_Wraparound_Stair");
            foreach(string n in new[]{"Rooftop_Floor","Roof_Parapet","Wooden_Door","Detailed_Arched_Door"})
            {var old=tower.Find(n);if(old)Object.DestroyImmediate(old.gameObject);}
            var end=stairs.Find("Landing_End_Parapet");if(end)Object.DestroyImmediate(end.gameObject);
            var stone=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Tower_Stone.mat");var cap=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Pale_Coping.mat");
            BuildRoof(tower,stone,cap);BuildLandingWall(stairs,cap);BuildDoor(tower);
            var core=tower.Find("Octagonal_Core").GetComponent<MeshFilter>().sharedMesh;
            float high=core.vertices.Max(v=>v.y);
            core.vertices=core.vertices.Select(v=>v.y>high-.001f?new Vector3(v.x,Top-.10f,v.z):v).ToArray();
            core.RecalculateNormals();core.RecalculateBounds();EditorUtility.SetDirty(core);
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Could not save roof and door");
            AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Roof and parapet integrated without coplanar bottom caps; landing wall connected to roof wall; seven-plank arched oak door saved.";
    }
    public static string RepairStair()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var stairs=root.transform.Find("Low_Wraparound_Stair");
            foreach(var child in stairs.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            BuildJoinedStair(stairs,AssetDatabase.LoadAssetAtPath<Material>(Folder+"Tower_Stone.mat"),AssetDatabase.LoadAssetAtPath<Material>(Folder+"Pale_Coping.mat"));
            var core=root.transform.Find("Tower/Octagonal_Core").GetComponent<MeshFilter>().sharedMesh;
            float coreTop=core.vertices.Max(v=>v.y);
            core.vertices=core.vertices.Select(v=>v.y>coreTop-.001f?new Vector3(v.x,Top-.10f,v.z):v).ToArray();
            core.RecalculateNormals();core.RecalculateBounds();EditorUtility.SetDirty(core);
            int repaired=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.name.StartsWith("Continuous_"))continue;
                var mesh=filter.sharedMesh;
                if(!mesh||!AssetDatabase.GetAssetPath(mesh).StartsWith(Folder))continue;
                if(mesh.name.StartsWith("Continuous_"))continue;
                var v=mesh.vertices;int n=v.Length/6;
                if(v.Length%6!=0||n<3)throw new InvalidOperationException("Unexpected polygon mesh "+mesh.name);
                var replacement=PolygonMesh(mesh.name,v.Take(n).Select(p=>new Vector2(p.x,p.z)).ToArray(),v.Min(p=>p.y),v.Max(p=>p.y));
                EditorUtility.CopySerialized(replacement,mesh);Object.DestroyImmediate(replacement);RefreshMesh(mesh);
                var collider=filter.GetComponent<MeshCollider>();if(collider){collider.sharedMesh=null;collider.sharedMesh=mesh;}
                repaired++;
            }
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Could not save repaired stairs");
            AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Joined stair mesh saved; top step extended to 41.5 degrees; duplicate landing removed; core top recessed below roof.";
    }
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode");
        var loaded=SceneManager.GetSceneByPath(ScenePath);
        if(loaded.IsValid()&&loaded.isDirty)throw new InvalidOperationException("Save manual workshop edits first");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab))throw new InvalidOperationException("Workshop exists; inspect before rebuilding");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","Tower1");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Tower1_OvergrownCourt");SceneManager.MoveGameObjectToScene(root,scene);
            var stone=Color("Tower_Stone","#B6C0CA");var cap=Color("Pale_Coping","#CAD1D6");
            var wood=Color("Warm_Wood","#A2784B");var sand=Color("Court_Sand","#D2B582");
            var purple=Color("Purple_Flowers","#8B5CA6");var green=Color("Ivy_Green","#639747");
            var stem=Color("Ivy_Stem","#4E6B35");
            Box(root.transform,"Workshop_Ground",new Vector3(0,-.5f,0),new Vector3(65,1,65),sand);
            var tower=Child(root.transform,"Tower");
            Vector2[] Oct(float radius)=>Enumerable.Range(0,8).Select(i=>P(22.5f+i*45,radius)).ToArray();
            Polygon(tower,"Octagonal_Core",Oct(2.8f),-.04f,Top-.10f,stone);
            Polygon(tower,"Middle_Brown_Band",Oct(2.86f),3.02f,3.18f,wood);
            Polygon(tower,"Upper_Brown_Band",Oct(2.89f),Top-.32f,Top-.16f,wood);
            BuildRoof(tower,stone,cap);
            var stairs=Child(root.transform,"Low_Wraparound_Stair");
            BuildJoinedStair(stairs,stone,cap);
            // The door is decorative, as in the approved reference; rooftop access is outside.
            BuildDoor(tower);
            Box(tower,"Door_Frame_Left",new Vector3(-.86f,1.02f,-2.72f),new Vector3(.26f,2.04f,.30f),cap);
            Box(tower,"Door_Frame_Right",new Vector3(.86f,1.02f,-2.72f),new Vector3(.26f,2.04f,.30f),cap);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
                var g=Child(tower,"Door_Arch_"+i);
                var outline=new[]{new Vector2(.73f*Mathf.Cos(a),1.95f+.73f*Mathf.Sin(a)),
                    new Vector2(1.0f*Mathf.Cos(a),1.95f+Mathf.Sin(a)),
                    new Vector2(1.0f*Mathf.Cos(b),1.95f+Mathf.Sin(b)),
                    new Vector2(.73f*Mathf.Cos(b),1.95f+.73f*Mathf.Sin(b))};
                Polygon(g,"Arch_Stone_"+i,outline,-.15f,.15f,cap);
                g.localRotation=Quaternion.Euler(-90,0,0);g.localPosition=new Vector3(0,0,-2.72f);
            }
            // A pitched wooden canopy touches the facade, outside the stair route.
            Box(tower,"Door_Canopy",new Vector3(0,3.00f,-2.90f),new Vector3(2.30f,.14f,1.0f),wood);
            tower.Find("Door_Canopy").localRotation=Quaternion.Euler(-12,0,0);
            BuildCornerJoin(tower,stone);
            BuildWallIvy(tower);
            var walls=Child(root.transform,"Broken_Court_Walls");
            for(int i=0;i<10;i++)
            {
                float a=35+i*29;
                if(i==4||i==8)continue;
                float h=i%3==0?1.25f:.86f;
                Sector(walls,"Court_Wall_"+i,a,a+25,9.5f,9.95f,-.025f,h,stone);
                Box(walls,"Chipped_Cap_"+i,Polar(a+12,9.72f,h+.15f),new Vector3(.62f,.30f,.48f),cap,-a-12);
            }
            var props=Child(root.transform,"Grounded_Plants_And_Rocks");
            const string decor="Assets/Prefabs/Locations/Decoration/";
            Prop(props,decor+"Hilly_Prop_Tree_Oak_4.prefab","Courtyard_Oak",new Vector3(11.8f,0,4.3f),6.8f,30);
            Prop(props,decor+"Hilly_Prop_Bush_2.prefab","West_Bush",new Vector3(-7.4f,0,-2.0f),1.45f,25);
            Prop(props,decor+"Hilly_Prop_Bush_3.prefab","East_Bush",new Vector3(7.2f,0,1.5f),1.35f,140);
            Prop(props,decor+"Shared_Prop_Boulder_5.prefab","West_Rock",new Vector3(-6.7f,0,3.0f),.64f,96);
            Prop(props,decor+"Shared_Prop_Boulder_3.prefab","East_Rock",new Vector3(6.7f,0,4.4f),.50f,40);
            var flowerPoints=new[]{new Vector3(-3.8f,0,-7.2f),new Vector3(-5.8f,0,-8.7f),
                new Vector3(3.8f,0,-7.2f),new Vector3(10.8f,0,-4.6f),new Vector3(3.8f,0,6.9f)};
            for(int i=0;i<flowerPoints.Length;i++)Prop(props,"Assets/Import/3D Cartoon Village/Prefabs/Flower.prefab",
                "Purple_Flowers_"+i,flowerPoints[i],.70f,35*i,purple);
            for(int i=0;i<5;i++)
            {
                float x=i%2==0?-.18f:.20f,z=-8.7f+i*1.12f;
                Polygon(root.transform,"Path_Stone_"+i,new[]{new Vector2(x-.65f,z-.32f),new Vector2(x+.65f,z-.34f),
                    new Vector2(x+.75f,z+.25f),new Vector2(x-.45f,z+.34f)},-.025f,.06f,cap);
            }
            Box(root.transform,"Ground_Cup_Plinth",new Vector3(6.5f,.45f,-2.3f),new Vector3(1.25f,.9f,1.05f),stone);
            var anchors=Child(root.transform,"Workshop_Walk_Anchors");
            void Anchor(string n,Vector3 p){Child(anchors,n).localPosition=p;}
            Anchor("Start",new Vector3(0,.05f,-16));
            Anchor("Item01",new Vector3(1.4f,.008f,-6.8f));
            Anchor("Item02",new Vector3(-5.4f,.008f,-6.1f));
            Anchor("Item03",new Vector3(6.5f,.908f,-2.3f));
            Anchor("Item04",new Vector3(-1.55f,Top+.008f,-.5f));
            Anchor("Item05",new Vector3(1.55f,Top+.008f,-1.1f));
            foreach(var c in root.GetComponentsInChildren<Collider>()){c.gameObject.layer=6;c.gameObject.tag="Untagged";}
            if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab))throw new InvalidOperationException("Cannot save Tower workshop prefab");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var workshop=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(workshop);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),workshop);
        var sun=new GameObject("Workshop_Sun");SceneManager.MoveGameObjectToScene(sun,workshop);
        sun.transform.rotation=Quaternion.Euler(45,-30,0);var light=sun.AddComponent<Light>();
        light.type=LightType.Directional;light.intensity=.9f;light.shadows=LightShadows.Soft;
        if(!EditorSceneManager.SaveScene(workshop,ScenePath))throw new InvalidOperationException("Cannot save Tower scene");
        AssetDatabase.SaveAssets();return "Tower1Workshop saved: 40 steps x .16 m, rooftop 6.4 m; five collectible anchors.";
    }
}
