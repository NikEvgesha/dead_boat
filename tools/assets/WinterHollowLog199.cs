using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WinterHollowLog199
{
    const string Winter = "Assets/Events/NewYear/";
    const string Folder = Winter + "Route199";
    const string Prefab = Winter + "Route181/Bank_02_Rooted_Log.prefab";
    const string Model = "Snowbound_Hollow_Log";
    static readonly Quaternion Facing = Quaternion.Euler(0f, 66f, 0f);

    enum Finish { Bark, Ridge, Wood, Grain, Interior, Snow, Red, Gold }

    sealed class Draft
    {
        readonly List<Vector3> vertices = new();
        readonly List<int>[] indices = Enum.GetValues(typeof(Finish)).Cast<Finish>().Select(_=>new List<int>()).ToArray();

        public void Face(Finish finish,Vector3 a,Vector3 b,Vector3 c,Vector3 outward)
        {
            var n=Vector3.Cross(b-a,c-a);
            if(n.sqrMagnitude<1e-10f) return;
            if(Vector3.Dot(n,outward)<0f) (b,c)=(c,b);
            var triangles=indices[(int)finish];
            triangles.Add(vertices.Count);
            triangles.Add(vertices.Count+1);
            triangles.Add(vertices.Count+2);
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
        }

        public void Quad(Finish finish,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
        {
            Face(finish,a,b,c,outward);
            Face(finish,a,c,d,outward);
        }

        public void Box(Finish finish,Vector3 center,Vector3 size,Quaternion rotation)
        {
            foreach(var axis in new[]{Vector3.right,Vector3.up,Vector3.forward})
                foreach(float sign in new[]{-1f,1f})
                {
                    var outward=axis*sign;
                    var right=axis.x!=0f?Vector3.up:Vector3.right;
                    var up=Vector3.Cross(axis,right);
                    Vector3 Point(float x,float y)=>center+rotation*Vector3.Scale(size*.5f,outward+right*x+up*y);
                    Quad(finish,Point(-1,-1),Point(1,-1),Point(1,1),Point(-1,1),rotation*outward);
                }
        }

        public Bounds Bounds()
        {
            var bounds=new Bounds(Facing*vertices[0],Vector3.zero);
            foreach(var vertex in vertices) bounds.Encapsulate(Facing*vertex);
            return bounds;
        }

        public float Radius=>vertices.Max(v=>new Vector2(v.x,v.z).magnitude);

        public Mesh Save()
        {
            string path=Folder+"/"+Model+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool existing=mesh;
            if(!existing) mesh=new Mesh {name=Model};
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.subMeshCount=indices.Length;
            for(int i=0;i<indices.Length;i++) mesh.SetTriangles(indices[i],i);
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

    const int Sides=12;
    static readonly float[] Depth={-3.73f,-2.32f,-.71f,1.01f,2.48f,3.78f};
    static readonly float[] Bend={-.19f,-.08f,.10f,.22f,.14f,-.04f};
    static readonly float[] Radius={2.26f,2.24f,2.16f,2.12f,2.18f,2.09f};
    static readonly float[] Height={4.12f,4.25f,4.06f,3.94f,4.00f,3.85f};

    static Vector3 Section(int row,int side,bool inside)
    {
        float angle=side*(Mathf.PI/Sides);
        float rise=Mathf.Sin(angle);
        float lobe=Mathf.Sin(side*2.34f+row*.63f);
        float radius=inside?1.58f+.082f*lobe:Radius[row]+.14f*lobe;
        float x=Bend[row]-Mathf.Cos(angle)*radius+.078f*rise*Mathf.Sin(row*.91f+side*.55f);
        float height=inside?3.24f+.08f*Mathf.Cos(side*1.42f+row*.53f):Height[row]+.17f*lobe;
        float y=-.185f+height*Mathf.Pow(Mathf.Max(rise,0f),inside?.79f:.90f);
        bool end=row==0||row==Depth.Length-1;
        float ragged=inside?.105f:.205f;
        float z=Depth[row]+(end ? ragged*Mathf.Sin(side*1.57f+row*.83f) : .069f*Mathf.Cos(side*.81f+row));
        return new Vector3(x,y,z);
    }

    static void Trunk(Draft mesh)
    {
        int last=Depth.Length-1;
        for(int row=0;row<last;row++)
        {
            for(int side=0;side<Sides;side++)
            {
                var a=Section(row,side,false);
                var b=Section(row,side+1,false);
                var c=Section(row+1,side+1,false);
                var d=Section(row+1,side,false);
                var outward=(a+b)*.5f-new Vector3(Bend[row],.08f,Depth[row]);
                outward.z=0f;
                var bark=(side==2 || side==9 || (side+row)%7==0)?Finish.Ridge:Finish.Bark;
                if(side>=4 && side<=7) mesh.Quad(Finish.Snow,a,b,c,d,outward);
                else if(side==3 || side==8)
                {
                    // Snow and bark share their mesh edge, so the lip follows the surface.
                    var e=Vector3.Lerp(a,b,.46f+.21f*Mathf.Sin(row*1.91f+side));
                    var f=Vector3.Lerp(d,c,.46f+.21f*Mathf.Sin((row+1)*1.91f+side));
                    mesh.Quad(side==3?bark:Finish.Snow,a,e,f,d,outward);
                    mesh.Quad(side==3?Finish.Snow:bark,e,b,c,f,outward);
                }
                else mesh.Quad(bark,a,b,c,d,outward);

                a=Section(row,side,true);
                b=Section(row,side+1,true);
                c=Section(row+1,side+1,true);
                d=Section(row+1,side,true);
                var inward=new Vector3(Bend[row]-(a.x+b.x)*.5f,-(a.y+b.y)*.5f-.24f,0f);
                mesh.Quad(side==1 || side==10?Finish.Grain:Finish.Interior,a,b,c,d,inward);
            }
            foreach(int edge in new[]{0,Sides})
                mesh.Quad(Finish.Bark,Section(row,edge,false),Section(row+1,edge,false),
                    Section(row+1,edge,true),Section(row,edge,true),Vector3.down);
        }

        foreach(int row in new[]{0,last})
        {
            var outward=row==0?Vector3.back:Vector3.forward;
            var fractions=new[]{0f,.17f,.232f,.49f,.553f,.76f,.822f,1f};
            Vector3 Ring(int side,int band)
            {
                float t=fractions[band];
                float distortion=Mathf.Sin(side*.94f+band*.67f+row)*.073f*Mathf.Sin(t*Mathf.PI);
                var point=Vector3.Lerp(Section(row,side,true),Section(row,side,false),t+distortion);
                return point+outward*(.09f*Mathf.Sin(t*Mathf.PI)*Mathf.Cos(side*.54f+row));
            }
            for(int side=0;side<Sides;side++)
                for(int band=0;band<fractions.Length-1;band++)
                {
                    bool snowyLip=side>=4 && side<=(row==0?7:8);
                    var finish=band==fractions.Length-2 ? snowyLip?Finish.Snow:Finish.Ridge : band%2==0?Finish.Wood:Finish.Grain;
                    mesh.Quad(finish,Ring(side,band),Ring(side+1,band),Ring(side+1,band+1),Ring(side,band+1),outward);
                }
        }
    }

    static void Root(Draft mesh,Vector3[] path,float[] widths,bool broken=false)
    {
        const int sides=5;
        Vector3 Rim(int row,int i)
        {
            var tangent=(path[Mathf.Min(row+1,path.Length-1)]-path[Mathf.Max(row-1,0)]).normalized;
            var right=Vector3.Cross(tangent,Vector3.up).normalized;
            var up=Vector3.Cross(right,tangent);
            float angle=(i%sides)*2f*Mathf.PI/sides+.16f;
            return path[row]+widths[row]*(right*Mathf.Cos(angle)+up*Mathf.Sin(angle)*.76f);
        }
        int last=path.Length-1;
        for(int i=0;i<sides;i++)
        {
            for(int row=0;row<last;row++)
            {
                var a=Rim(row,i); var b=Rim(row,i+1);
                mesh.Quad(i==2?Finish.Ridge:Finish.Bark,a,b,Rim(row+1,i+1),Rim(row+1,i),(a+b)*.5f-path[row]);
            }
            mesh.Face(broken?Finish.Wood:Finish.Bark,path[last],Rim(last,i),Rim(last,i+1),path[last]-path[last-1]);
        }
    }

    static void Drift(Draft mesh,Vector3 center,Vector2 radius,float height,int seed)
    {
        const int sides=7;
        Vector3 Rim(int i,bool upper)
        {
            i%=sides;
            float a=i*(2f*Mathf.PI/sides)+seed*.63f;
            float rough=1f+.13f*Mathf.Sin(i*1.81f+seed);
            if(upper) rough*=.42f+.11f*Mathf.Cos(i*1.24f);
            return center+new Vector3(radius.x*rough*Mathf.Cos(a),upper ? height*(.84f+.10f*Mathf.Cos(i)) : -.095f,
                radius.y*rough*Mathf.Sin(a));
        }
        for(int i=0;i<sides;i++)
        {
            var a=Rim(i,false); var b=Rim(i+1,false);
            var c=Rim(i+1,true); var d=Rim(i,true);
            mesh.Quad(Finish.Snow,a,b,c,d,(a+b)*.5f-center+Vector3.up*.34f);
            mesh.Face(Finish.Snow,center+new Vector3(.04f,height,-.08f),d,c,Vector3.up);
        }
    }

    static void Accessories(Draft mesh)
    {
        Root(mesh,new[]{new Vector3(-2.02f,.74f,-2.88f),new Vector3(-2.67f,.37f,-3.07f),
            new Vector3(-3.28f,.12f,-3.61f),new Vector3(-3.91f,-.045f,-3.82f)},new[]{.46f,.35f,.17f,.052f});
        Root(mesh,new[]{new Vector3(-2.53f,.32f,-3.10f),new Vector3(-3.14f,.07f,-2.60f),
            new Vector3(-3.61f,-.042f,-2.56f)},new[]{.24f,.12f,.052f});
        Root(mesh,new[]{new Vector3(1.91f,.87f,-2.60f),new Vector3(2.51f,.36f,-2.90f),
            new Vector3(3.10f,.12f,-2.96f),new Vector3(3.85f,-.051f,-3.41f)},new[]{.47f,.32f,.20f,.061f});
        Root(mesh,new[]{new Vector3(1.76f,.67f,2.71f),new Vector3(2.53f,.24f,3.05f),
            new Vector3(3.16f,-.068f,3.61f)},new[]{.37f,.23f,.052f});
        Root(mesh,new[]{new Vector3(-1.78f,.59f,2.69f),new Vector3(-2.49f,.16f,3.18f),
            new Vector3(-3.07f,-.062f,3.51f)},new[]{.35f,.18f,.05f});
        Root(mesh,new[]{new Vector3(-1.83f,2.01f,-.64f),new Vector3(-2.43f,2.24f,-.47f),
            new Vector3(-2.85f,2.59f,-.66f)},new[]{.40f,.285f,.192f},true);
        Root(mesh,new[]{new Vector3(1.70f,2.44f,1.82f),new Vector3(2.34f,2.80f,2.12f)},
            new[]{.30f,.149f},true);
        Root(mesh,new[]{new Vector3(-1.90f,2.01f,-3.58f),new Vector3(-2.18f,2.08f,-3.94f),
            new Vector3(-2.20f,2.15f,-4.29f)},new[]{.24f,.15f,.023f},true);
        Root(mesh,new[]{new Vector3(.57f,3.46f,-3.64f),new Vector3(.66f,3.51f,-4.02f),
            new Vector3(.71f,3.61f,-4.27f)},new[]{.18f,.108f,.024f},true);

        Drift(mesh,new Vector3(-2.83f,0f,-.55f),new Vector2(.91f,1.59f),.28f,2);
        Drift(mesh,new Vector3(2.42f,0f,.08f),new Vector2(.91f,1.75f),.32f,6);
        Drift(mesh,new Vector3(-2.72f,0f,2.05f),new Vector2(.69f,1.07f),.26f,4);
        Drift(mesh,new Vector3(2.90f,0f,2.66f),new Vector2(.66f,.73f),.17f,3);

        var gift=new Vector3(2.81f,.0f,-3.52f);
        var yaw=Quaternion.Euler(0f,24f,0f);
        mesh.Box(Finish.Red,gift+Vector3.up*.255f,new Vector3(.55f,.51f,.50f),yaw);
        mesh.Box(Finish.Gold,gift+Vector3.up*.26f,new Vector3(.103f,.52f,.514f),yaw);
        mesh.Box(Finish.Gold,gift+Vector3.up*.262f,new Vector3(.565f,.525f,.098f),yaw);
        for(int side=-1;side<=1;side+=2)
            mesh.Box(Finish.Gold,gift+Vector3.up*.562f+yaw*Vector3.right*(side*.065f),
                new Vector3(.16f,.057f,.076f),yaw*Quaternion.Euler(0f,0f,side*26f));
    }

    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var draft=new Draft();
        Trunk(draft);
        Accessories(draft);
        var bounds=draft.Bounds();
        if(draft.Radius>6f || bounds.min.x< -5.3f || bounds.max.x>5.3f ||
            bounds.min.z< -5.3f || bounds.max.z>5.3f || bounds.max.y<3.9f)
            throw new InvalidOperationException("Unexpected log bounds "+bounds+", radius "+draft.Radius);

        Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Winter+name+".mat");
        var bark=Mat("Route179/Log_Bark");
        var ridge=Mat("Route179/Log_Bark_Ridges");
        var wood=Mat("Route179/Log_Exposed_Wood");
        if(!bark || !ridge || !wood) throw new InvalidOperationException("Missing winter bark materials");
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Winter.TrimEnd('/'),"Route199");
        Material Tone(string name,Color color,Color emission)
        {
            string path=Folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material) return material;
            material=new Material(wood){name=name,color=color};
            material.SetFloat("_Glossiness",.13f);
            if(emission.maxColorComponent>0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",emission);
            }
            AssetDatabase.CreateAsset(material,path);
            return material;
        }
        var materials=new[]{bark,ridge,wood,Tone("End_Grain",new Color(.36f,.209f,.11f),Color.clear),
            Tone("Hollow_Heartwood",new Color(.36f,.226f,.148f),new Color(.039f,.023f,.013f)),Mat("Snow"),Mat("Ruby"),Mat("Gold")};
        if(materials.Any(m=>!m)) throw new InvalidOperationException("Missing winter log materials");

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
                model.gameObject.AddComponent<MeshCollider>();
            }
            model.localRotation=Facing;
            var mesh=draft.Save();
            model.GetComponent<MeshFilter>().sharedMesh=mesh;
            model.GetComponent<MeshRenderer>().sharedMaterials=materials;
            var collision=model.GetComponent<MeshCollider>();
            collision.sharedMesh=null;
            collision.sharedMesh=mesh;
            foreach(Transform child in prefab.transform)
                if(child!=model) child.gameObject.SetActive(false);
            var trigger=prefab.GetComponent<BoxCollider>();
            trigger.center=bounds.center;
            trigger.size=bounds.size;
            if(!PrefabUtility.SaveAsPrefabAsset(prefab,Prefab)) throw new InvalidOperationException("Could not save hollow log");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var report=$"Hollow log 02: cut end grain, branched roots, snow lip and two open ends. Bounds {bounds.min}..{bounds.max}; footprint radius {draft.Radius:F2} m.";
        File.WriteAllText("Docs/locations/previews/event-winter199-assets.txt",report+Environment.NewLine);
        return report;
    }
}
