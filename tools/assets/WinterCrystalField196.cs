using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WinterCrystalField196
{
    const string Winter = "Assets/Events/NewYear/";
    const string Folder = Winter + "Route196";
    const string Prefab = Winter + "Route181/Bank_07_Ice_Crystal_Field.prefab";
    const string Model = "Shattered_Ice_Clearing";
    const float ModelScale = 2f;
    static readonly Quaternion Facing = Quaternion.Euler(0f, 74f, 0f);
    enum Finish { Stone, Snow, Frost, Cyan, Blue, Deep }

    sealed class Draft
    {
        readonly List<Vector3> vertices = new();
        readonly List<int>[] triangles = Enum.GetValues(typeof(Finish)).Cast<Finish>().Select(_ => new List<int>()).ToArray();

        public void Face(Finish finish, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            var normal = Vector3.Cross(b-a,c-a);
            if (normal.sqrMagnitude<1e-10f) return;
            if (Vector3.Dot(normal,outward)<0f) (b,c)=(c,b);
            var indices=triangles[(int)finish];
            indices.Add(vertices.Count);
            indices.Add(vertices.Count+1);
            indices.Add(vertices.Count+2);
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
        }

        public void Quad(Finish finish,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
        {
            Face(finish,a,b,c,outward);
            Face(finish,a,c,d,outward);
        }

        public Bounds Bounds()
        {
            var bounds=new Bounds(Facing*vertices[0],Vector3.zero);
            foreach(var vertex in vertices) bounds.Encapsulate(Facing*vertex);
            return bounds;
        }

        public float Radius => vertices.Max(p=>new Vector2(p.x,p.z).magnitude);

        public Mesh Save()
        {
            string path=Folder+"/"+Model+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool existing=mesh;
            if (!existing) mesh=new Mesh {name=Model};
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.subMeshCount=triangles.Length;
            for(int i=0;i<triangles.Length;i++) mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if(existing)
            {
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }
            else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
    }

    static void Shard(Draft ice,float x,float z,float height,float width,float leanX,float leanZ,int seed)
    {
        const int sides=5;
        const float baseY=-.145f;
        var foot=new Vector3(x,baseY,z);
        var lean=new Vector3(leanX,0f,leanZ);
        var tip=foot+Vector3.up*height+lean;
        Vector3 Ring(int side,int band)
        {
            side %= sides;
            float angle=side*(2f*Mathf.PI/sides)+seed*.79f;
            float irregular=1f+.13f*Mathf.Cos(side*1.57f+seed*2.13f);
            float heightFraction=band==0 ? 0f : band==1 ? .29f+.054f*Mathf.Sin(side*1.8f+seed)
                : .72f+.078f*Mathf.Cos(side*1.3f+seed);
            float radius=width*irregular*(band==0?.73f:band==1?1f:.68f);
            return foot+lean*heightFraction+Vector3.up*(height*heightFraction)+
                new Vector3(radius*Mathf.Cos(angle),0f,radius*.81f*Mathf.Sin(angle));
        }

        for(int side=0;side<sides;side++)
        {
            var a=Ring(side,0); var b=Ring(side+1,0);
            var c=Ring(side+1,1); var d=Ring(side,1);
            var e=Ring(side+1,2); var f=Ring(side,2);
            var normal=(a+b)*.5f-foot;
            Finish face=((side+seed)%5) switch
            {
                0=>Finish.Deep, 1=>Finish.Blue, 2=>Finish.Cyan, 3=>Finish.Blue, _=>Finish.Deep
            };
            ice.Quad(face,a,b,c,d,normal);
            ice.Quad(face,d,c,e,f,normal);
            Finish crown=(side+seed)%3==0 ? Finish.Frost : face==Finish.Deep ? Finish.Blue : Finish.Cyan;
            ice.Face(crown,f,e,tip,normal+Vector3.up*.30f);
            ice.Face(face,foot,a,b,Vector3.down);
        }
    }

    static void Mound(Draft ground,float x,float z,float width,float depth,float height,int seed,bool stone)
    {
        var center=new Vector3(x,0f,z);
        const int sides=7;
        Vector3 Ring(int i,bool top)
        {
            i %= sides;
            float angle=i*2f*Mathf.PI/sides+seed*.63f;
            float spread=1f+.12f*Mathf.Sin(i*1.37f+seed);
            if(top) spread*=.49f+.065f*Mathf.Cos(i*1.81f+seed);
            return center+new Vector3(width*spread*Mathf.Cos(angle),top ? height*(.77f+.19f*Mathf.Cos(i*1.41f)) : -.075f,
                depth*spread*Mathf.Sin(angle));
        }
        var peak=center+new Vector3(.08f,height,-.11f);
        for(int i=0;i<sides;i++)
        {
            var a=Ring(i,false); var b=Ring(i+1,false);
            var c=Ring(i+1,true); var d=Ring(i,true);
            var normal=(a+b)*.5f-center+Vector3.up*.26f;
            ground.Quad(stone && i%3!=0 ? Finish.Stone : Finish.Snow,a,b,c,d,normal);
            ground.Face(stone && i%4==2 ? Finish.Frost : Finish.Snow,peak,d,c,Vector3.up);
        }
    }

    static void FrozenSeam(Draft ice)
    {
        // The ice follows a narrow break in the snow through the clearing.
        var seam=new[]
        {
            new Vector3(-.56f,.019f,-3.67f),new Vector3(-.37f,.023f,-2.87f),new Vector3(-.68f,.027f,-1.91f),
            new Vector3(-.28f,.034f,-1.11f),new Vector3(.24f,.025f,-.19f),new Vector3(.05f,.032f,.70f),
            new Vector3(.53f,.027f,1.64f),new Vector3(.30f,.023f,2.44f),new Vector3(.50f,.017f,3.32f)
        };
        var widths=new[]{.016f,.16f,.32f,.37f,.27f,.45f,.32f,.18f,.013f};
        for(int i=0;i<seam.Length-1;i++)
        {
            var a=seam[i]; var b=seam[i+1];
            var left=a-Vector3.right*widths[i];
            var nextLeft=b-Vector3.right*widths[i+1];
            var right=a+Vector3.right*widths[i]*.76f;
            var nextRight=b+Vector3.right*widths[i+1]*.76f;
            ice.Quad(Finish.Frost,left,nextLeft,b,a,Vector3.up);
            ice.Quad(i%3==0?Finish.Cyan:Finish.Frost,a,b,nextRight,right,Vector3.up);
        }
        Branch(seam[3],new Vector3(-1.62f,.016f,-.50f),.145f);
        Branch(seam[5],new Vector3(1.60f,.016f,.46f),.115f);
        Branch(seam[6],new Vector3(.98f,.016f,2.72f),.097f);

        void Branch(Vector3 from,Vector3 to,float width)
        {
            var cross=Vector3.Cross(Vector3.up,(to-from).normalized);
            var mid=Vector3.Lerp(from,to,.57f)+cross*.16f;
            ice.Face(Finish.Cyan,from-cross*width,from+cross*width,mid,Vector3.up);
            ice.Face(Finish.Frost,from-cross*width,mid,to,Vector3.up);
        }
    }

    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var field=new Draft();
        Mound(field,-2.04f,.29f,1.54f,1.59f,.35f,1,true);
        Mound(field,1.95f,.57f,1.29f,1.33f,.32f,3,true);
        Mound(field,-1.20f,2.53f,.92f,.75f,.20f,7,false);
        Mound(field,1.75f,-2.47f,.91f,.79f,.16f,4,false);
        FrozenSeam(field);

        Shard(field,-2.30f,.37f,4.42f,.69f,-.44f,.23f,1);
        Shard(field,-2.65f,-.18f,2.76f,.46f,-.74f,-.45f,3);
        Shard(field,-1.75f,.54f,2.32f,.46f,.50f,.24f,2);
        Shard(field,-2.86f,.88f,1.57f,.34f,-.54f,.31f,5);
        Shard(field,-1.77f,-.44f,1.16f,.29f,.18f,-.39f,8);

        Shard(field,1.99f,.69f,3.35f,.58f,.41f,.32f,4);
        Shard(field,2.40f,.17f,2.20f,.47f,.57f,-.47f,9);
        Shard(field,1.56f,.40f,1.67f,.35f,-.33f,-.43f,5);
        Shard(field,2.45f,1.34f,1.24f,.33f,.35f,.49f,6);

        Shard(field,-1.27f,2.54f,1.88f,.39f,-.27f,.43f,7);
        Shard(field,-1.55f,2.78f,.98f,.25f,-.46f,.24f,12);
        Shard(field,1.71f,-2.42f,1.08f,.31f,-.23f,-.25f,10);
        Shard(field,2.08f,-2.37f,.77f,.24f,.31f,.06f,2);
        Shard(field,-1.61f,-2.23f,.59f,.21f,-.11f,-.20f,11);

        var bounds=field.Bounds();
        if(field.Radius>5.5f || bounds.min.y>=-.025f || bounds.max.y<4f)
            throw new InvalidOperationException("Unexpected crystal clearing bounds "+bounds);

        Material WinterMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(Winter+name+".mat");
        var blue=WinterMaterial("Route181/Satin_Ice_Blue");
        var materials=new[]
        {
            WinterMaterial("Route181/Grotto_Stone_Dark"),WinterMaterial("Snow"),WinterMaterial("Route182/Blue_Packed_Snow"),
            WinterMaterial("Route181/Satin_Ice_Cyan"),blue
        };
        if(materials.Any(m=>!m)) throw new InvalidOperationException("Missing winter crystal materials");
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Winter.TrimEnd('/'),"Route196");
        string deepPath=Folder+"/Sapphire_Ice.mat";
        var deep=AssetDatabase.LoadAssetAtPath<Material>(deepPath);
        if(!deep)
        {
            deep=new Material(blue){name="Sapphire_Ice",color=new Color(.075f,.31f,.54f)};
            deep.SetFloat("_Glossiness",.23f);
            AssetDatabase.CreateAsset(deep,deepPath);
        }
        var prefab=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var model=prefab.transform.Find(Model);
            if(!model)
            {
                model=new GameObject(Model).transform;
                model.SetParent(prefab.transform,false);
                model.gameObject.AddComponent<MeshFilter>();
                model.gameObject.AddComponent<MeshRenderer>();
            }
            model.localRotation=Facing;
            model.localScale=Vector3.one*ModelScale;
            model.GetComponent<MeshFilter>().sharedMesh=field.Save();
            model.GetComponent<MeshRenderer>().sharedMaterials=materials.Append(deep).ToArray();
            foreach(Transform child in prefab.transform)
                if(child!=model) child.gameObject.SetActive(false);
            var trigger=prefab.GetComponent<BoxCollider>();
            trigger.center=bounds.center*ModelScale;
            trigger.size=bounds.size*ModelScale;
            if(!PrefabUtility.SaveAsPrefabAsset(prefab,Prefab)) throw new InvalidOperationException("Could not save ice clearing");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var report=$"Crystal clearing 07: 14 varied grounded shards, four staggered clusters, and a shallow ice seam. At scale {ModelScale:F0}: bounds {bounds.min*ModelScale}..{bounds.max*ModelScale}, radius {field.Radius*ModelScale:F2} m.";
        File.WriteAllText("Docs/locations/previews/event-winter196-assets.txt",report+Environment.NewLine);
        return report;
    }
}
