using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterSled185
{
    const string Winter = "Assets/Events/NewYear/";
    const string Folder = Winter + "Route185";
    const string Prefab = Winter + "Route181/Bank_05_Gift_Sled.prefab";
    const string MeshPath = Folder + "/Painted_Gift_Sleigh.asset";
    const float ModelScale = 2f;

    enum Paint { Red, Gold, Wood, DarkWood, Green, Blue, Violet, Snow }

    static readonly string[] Paints =
    {
        "Ruby", "Gold", "Route179/Log_Exposed_Wood", "Route179/Log_Bark_Ridges",
        "Fir", "WinterTown170/Gift_Blue", "WinterTown170/Gift_Violet", "Snow"
    };

    sealed class Geometry
    {
        readonly List<Vector3> points = new();
        readonly List<int>[] triangles = Paints.Select(_ => new List<int>()).ToArray();

        public void Triangle(Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b-a,c-a),outward) < 0f) (b,c) = (c,b);
            var indices = triangles[(int)paint];
            indices.Add(points.Count);
            indices.Add(points.Count+1);
            indices.Add(points.Count+2);
            points.Add(a); points.Add(b); points.Add(c);
        }

        public void Quad(Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Triangle(paint,a,b,c,outward);
            Triangle(paint,a,c,d,outward);
        }

        public void Box(Paint paint, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var half = size*.5f;
            foreach (var normal in new[] { Vector3.right, Vector3.up, Vector3.forward })
                foreach (float sign in new[] { -1f, 1f })
                {
                    var outward = normal*sign;
                    var a = normal.x != 0f ? Vector3.up : Vector3.right;
                    var b = Vector3.Cross(normal,a);
                    Vector3 Corner(float u, float v) => center + rotation*Vector3.Scale(half,outward+u*a+v*b);
                    Quad(paint,Corner(-1,-1),Corner(1,-1),Corner(1,1),Corner(-1,1),rotation*outward);
                }
        }

        public void Beam(Paint paint, Vector3 a, Vector3 b, Vector2 size)
        {
            var direction = b-a;
            Box(paint,(a+b)*.5f,new Vector3(size.x,size.y,direction.magnitude+.025f),
                Quaternion.LookRotation(direction, Mathf.Abs(direction.normalized.y)>.95f ? Vector3.forward : Vector3.up));
        }

        public void Drift(Mesh rock, Vector3 center, Vector3 size, float yaw)
        {
            var transform = Matrix4x4.TRS(center,Quaternion.Euler(0,yaw,0),size);
            var corners = rock.vertices.Select(transform.MultiplyPoint3x4).ToArray();
            var source = rock.triangles;
            for (int i=0;i<source.Length;i+=3)
            {
                var a = corners[source[i]];
                var b = corners[source[i+1]];
                var c = corners[source[i+2]];
                Triangle(Paint.Snow,a,b,c,(a+b+c)/3f-center);
            }
        }

        public Mesh Save()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            bool exists = mesh;
            if (!exists) mesh = new Mesh { name = "Painted_Gift_Sleigh" };
            mesh.Clear();
            mesh.SetVertices(points);
            mesh.subMeshCount = triangles.Length;
            for (int i=0;i<triangles.Length;i++) mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (!exists) AssetDatabase.CreateAsset(mesh,MeshPath);
            else
            {
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }
            return mesh;
        }

        public Bounds Bounds()
        {
            var bounds = new Bounds(points[0],Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            return bounds;
        }

        public float Radius() => points.Max(p=>new Vector2(p.x,p.z).magnitude);
    }

    static void Shell(Geometry mesh)
    {
        // Rear, bench, footwell and nose form one stepped, tapered side.
        var sections = new[]
        {
            new Vector4(-1.91f,.75f,.75f,1.53f), new Vector4(-1.64f,.97f,.59f,1.71f),
            new Vector4(-1.22f,1.06f,.51f,1.65f), new Vector4(-.62f,1.09f,.47f,1.37f),
            new Vector4(.04f,1.04f,.46f,1.06f), new Vector4(.72f,.96f,.47f,.95f),
            new Vector4(1.29f,.82f,.54f,1.10f), new Vector4(1.63f,.65f,.67f,1.27f),
            new Vector4(1.78f,.44f,.82f,1.32f)
        };
        foreach (float side in new[] { -1f,1f })
        {
            Vector3 Edge(int i,bool top,bool inside)
            {
                var s=sections[i];
                float width=s.y-(inside?.13f:0f)-(top?0f:.12f);
                return new Vector3(side*width,top?s.w:s.z,s.x);
            }
            for (int i=0;i<sections.Length-1;i++)
            {
                var lower=Edge(i,false,false);
                var top=Edge(i,true,false);
                var nextLower=Edge(i+1,false,false);
                var nextTop=Edge(i+1,true,false);
                var inside=Edge(i,true,true);
                var nextInside=Edge(i+1,true,true);
                var lowerInside=Edge(i,false,true);
                var nextLowerInside=Edge(i+1,false,true);
                mesh.Quad(Paint.Red,lower,nextLower,nextTop,top,Vector3.right*side);
                mesh.Quad(Paint.DarkWood,lowerInside,inside,nextInside,nextLowerInside,Vector3.left*side);
                mesh.Quad(Paint.Gold,top,nextTop,nextInside,inside,Vector3.up);
                mesh.Quad(Paint.Red,lower,lowerInside,nextLowerInside,nextLower,Vector3.down);
                mesh.Beam(Paint.Gold,Vector3.Lerp(lower,top,.23f)+Vector3.right*side*.015f,
                    Vector3.Lerp(nextLower,nextTop,.23f)+Vector3.right*side*.015f,new Vector2(.055f,.06f));
            }
            foreach (int end in new[] { 0, sections.Length-1 })
                mesh.Quad(Paint.Red,Edge(end,false,false),Edge(end,true,false),Edge(end,true,true),Edge(end,false,true),
                    end==0 ? Vector3.back : Vector3.forward);
        }
        for (int i=0;i<5;i++)
            mesh.Box(Paint.Wood,new Vector3(-.61f+i*.305f,.555f,-.24f),new Vector3(.295f,.13f,2.96f),Quaternion.identity);

        mesh.Quad(Paint.Wood,new Vector3(-.70f,.62f,1.22f),new Vector3(-.33f,.81f,1.77f),
            new Vector3(.33f,.81f,1.77f),new Vector3(.70f,.62f,1.22f),Vector3.up);
        CrossPanel(-1.90f,.64f,1.56f,.75f,.74f);
        CrossPanel(1.77f,.79f,1.32f,.32f,.45f);

        void CrossPanel(float z,float low,float high,float widthLow,float widthHigh)
        {
            var a=new Vector3(-widthLow,low,z);
            var b=new Vector3(widthLow,low,z);
            var c=new Vector3(widthHigh,high,z);
            var d=new Vector3(-widthHigh,high,z);
            var inset=Vector3.forward*(z>0f?-.11f:.11f);
            mesh.Quad(Paint.Red,a,b,c,d,-inset);
            mesh.Quad(Paint.DarkWood,a+inset,d+inset,c+inset,b+inset,inset);
            mesh.Quad(Paint.Gold,d,c,c+inset,d+inset,Vector3.up);
            mesh.Quad(Paint.Red,a,d,d+inset,a+inset,Vector3.left);
            mesh.Quad(Paint.Red,b,c,c+inset,b+inset,Vector3.right);
            mesh.Quad(Paint.Red,a,a+inset,b+inset,b,Vector3.down);
        }
    }

    static void Runners(Geometry mesh)
    {
        var track = new[]
        {
            new Vector2(-2.13f,.14f),new Vector2(-1.86f,.07f),new Vector2(-1.56f,.047f),
            new Vector2(1.32f,.047f),new Vector2(1.70f,.11f),new Vector2(2.05f,.25f),
            new Vector2(2.33f,.49f),new Vector2(2.48f,.75f),new Vector2(2.47f,.99f),
            new Vector2(2.33f,1.12f),new Vector2(2.14f,1.10f)
        };
        foreach (float side in new[] { -1f,1f })
        {
            float x=.81f*side;
            // Closed segments follow the curl's tangent. Extruding every top face vertically
            // folds the strip when the tip turns back, leaving visible gaps and dark wedges.
            for (int i=0;i<track.Length-1;i++)
            {
                mesh.Beam(Paint.Gold,new Vector3(x,track[i].y+.025f,track[i].x),
                    new Vector3(x,track[i+1].y+.025f,track[i+1].x),new Vector2(.186f,.15f));
            }
            foreach (float z in new[] { -1.21f,.98f })
                mesh.Beam(Paint.DarkWood,new Vector3(x,.11f,z),new Vector3(side*.68f,.55f,z-.12f),new Vector2(.20f,.19f));
        }
    }

    static void Gift(Geometry mesh, Vector3 foot, Vector3 size, float yaw, Paint paper, bool bow = true)
    {
        var orientation = Quaternion.Euler(0,yaw,0);
        Vector3 At(float x,float y,float z) => foot+orientation*new Vector3(x,y,z);
        void Box(Paint paint,float height,Vector3 dimensions) => mesh.Box(paint,At(0,height,0),dimensions,orientation);
        Box(paper,size.y*.5f,size);
        Box(paper,size.y*.91f,new Vector3(size.x*1.06f,size.y*.18f,size.z*1.06f));
        Box(Paint.Gold,size.y*.51f,new Vector3(size.x*.17f,size.y*1.03f,size.z*1.075f));
        Box(Paint.Gold,size.y*.51f,new Vector3(size.x*1.075f,size.y*1.034f,size.z*.15f));
        if (!bow) return;
        Box(Paint.Gold,size.y+.053f,new Vector3(size.x*.14f,.105f,size.z*.13f));
        foreach (float side in new[] { -1f,1f })
        {
            var width=new Vector2(.085f,.065f)*Mathf.Lerp(.85f,1.25f,size.x);
            var inner=At(0,size.y+.073f,0);
            var upper=At(side*size.x*.26f,size.y+size.x*.22f,-side*.04f);
            var outer=At(side*size.x*.34f,size.y+size.x*.115f,-side*.065f);
            mesh.Beam(Paint.Gold,inner,upper,width);
            mesh.Beam(Paint.Gold,upper,outer,width);
            mesh.Beam(Paint.Gold,outer,inner,width);
        }
    }

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var mesh=new Geometry();
        Shell(mesh);
        Runners(mesh);
        Gift(mesh,new Vector3(-.42f,.62f,-1.13f),new Vector3(.76f,.90f,.70f),-7,Paint.Green,bow:false);
        Gift(mesh,new Vector3(.45f,.62f,-.66f),new Vector3(.71f,1.13f,.80f),6,Paint.Blue);
        Gift(mesh,new Vector3(-.42f,1.522f,-1.13f),new Vector3(.60f,.60f,.60f),14,Paint.Red);
        Gift(mesh,new Vector3(-.43f,.62f,-.06f),new Vector3(.70f,.75f,.67f),-12,Paint.Violet);
        Gift(mesh,new Vector3(.38f,.62f,.39f),new Vector3(.60f,.63f,.66f),-13,Paint.Red);
        var boulder=AssetDatabase.LoadAssetAtPath<Mesh>(Winter+"Route181/Faceted_Boulder.asset");
        mesh.Drift(boulder,new Vector3(-.72f,.015f,-1.53f),new Vector3(1.64f,.26f,1.67f),17);
        mesh.Drift(boulder,new Vector3(1.19f,.035f,-1.16f),new Vector3(1.61f,.34f,1.32f),-9);
        mesh.Drift(boulder,new Vector3(-.93f,.0f,1.28f),new Vector3(1.06f,.16f,1.25f),15);
        Gift(mesh,new Vector3(-1.64f,.006f,1.26f),new Vector3(.67f,.65f,.71f),25,Paint.Blue);
        Gift(mesh,new Vector3(-2.16f,.006f,.64f),new Vector3(.49f,.43f,.48f),-17,Paint.Red);

        var shapeBounds=mesh.Bounds();
        if (mesh.Radius()>4.1f || shapeBounds.min.y<-.32f || shapeBounds.min.y>.02f || shapeBounds.max.y<2.1f)
            throw new InvalidOperationException("Invalid sleigh geometry " + shapeBounds);
        var materials=Paints.Select(p=>AssetDatabase.LoadAssetAtPath<Material>(Winter+p+".mat")).ToArray();
        if (materials.Any(m=>!m)) throw new InvalidOperationException("Missing winter materials");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Winter.TrimEnd('/'),"Route185");
        var prefab=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var shape=prefab.transform.Find("Painted_Gift_Sleigh");
            if (!shape)
            {
                shape=new GameObject("Painted_Gift_Sleigh").transform;
                shape.SetParent(prefab.transform,false);
                shape.gameObject.AddComponent<MeshFilter>();
                shape.gameObject.AddComponent<MeshRenderer>();
            }
            shape.localRotation=Quaternion.Euler(0,-128f,0);
            shape.localScale=Vector3.one*ModelScale;
            var asset=mesh.Save();
            shape.GetComponent<MeshFilter>().sharedMesh=asset;
            var renderer=shape.GetComponent<MeshRenderer>();
            renderer.sharedMaterials=materials;
            foreach (Transform child in prefab.transform)
                if (child != shape) child.gameObject.SetActive(false);
            var rotated=asset.vertices.Select(v=>shape.localRotation*(v*ModelScale)).ToArray();
            var bounds=new Bounds(rotated[0],Vector3.zero);
            foreach (var vertex in rotated) bounds.Encapsulate(vertex);
            var trigger=prefab.GetComponent<BoxCollider>();
            trigger.center=bounds.center;
            trigger.size=bounds.size;
            if (!PrefabUtility.SaveAsPrefabAsset(prefab,Prefab)) throw new InvalidOperationException("Could not save the sleigh");
            var report=$"Painted sleigh 05: 7 presents; scale {ModelScale:F0}; root tag and trigger retained. Bounds {bounds.min}..{bounds.max}; radius {mesh.Radius()*ModelScale:F2} m.";
            File.WriteAllText("Docs/locations/previews/event-winter185-assets.txt",report+Environment.NewLine);
            return report;
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
}
