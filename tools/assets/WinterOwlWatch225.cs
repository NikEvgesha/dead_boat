using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterOwlWatch225
{
    const string Winter = "Assets/Events/NewYear/";
    const string Point = Winter + "ShorePOI/POI_02_Rooted_Log.prefab";
    const string Folder = Winter + "ShorePOI/OwlWatch225";
    const string Group = "Owl_Watch";
    enum Paint { Bark, Wood, Grain, Honey, Dark, Snow, Rock, Fir, Needle, Amber, Orange, FireGold, Flame }

    static Vector3 V(float x, float y, float z) => new Vector3(x,y,z);

    sealed class Draft
    {
        readonly List<Vector3> points = new();
        readonly List<int>[] surfaces = Enum.GetValues(typeof(Paint)).Cast<Paint>().Select(_ => new List<int>()).ToArray();

        public void Triangle(Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 facing)
        {
            if (Vector3.Dot(Vector3.Cross(b-a,c-a),facing) < 0f) (b,c)=(c,b);
            var indices=surfaces[(int)paint];
            indices.Add(points.Count); indices.Add(points.Count+1); indices.Add(points.Count+2);
            points.Add(a); points.Add(b); points.Add(c);
        }

        public void Quad(Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing)
        {
            Triangle(paint,a,b,c,facing);
            Triangle(paint,a,c,d,facing);
        }

        public void Cylinder(Paint side, Paint end, Vector3 a, Vector3 b, float lower, float upper,
            int sides, Paint? alternate = null)
        {
            var up=(b-a).normalized;
            var axis=Vector3.Cross(up,Mathf.Abs(up.y)>.95f ? Vector3.forward : Vector3.up).normalized;
            var tangent=Vector3.Cross(up,axis);
            Vector3 Radial(int i)
            {
                float angle=(i+.5f)*Mathf.PI*2f/sides;
                return axis*Mathf.Cos(angle)+tangent*Mathf.Sin(angle);
            }
            for(int i=0;i<sides;i++)
            {
                var p=Radial(i); var q=Radial(i+1);
                Quad(alternate.HasValue && i%3==1 ? alternate.Value : side,
                    a+p*lower,a+q*lower,b+q*upper,b+p*upper,p+q);
                if(lower>0f) Triangle(end,a,a+p*lower,a+q*lower,-up);
                if(upper>0f) Triangle(end,b,b+p*upper,b+q*upper,up);
            }
        }

        public void Gem(Paint paint, Vector3 center, Vector3 size, int sides=8, Paint? alternate=null)
        {
            Vector3 Point(float lat,int i)
            {
                float angle=(i+.25f)*Mathf.PI*2f/sides;
                return center+Vector3.Scale(size,V(Mathf.Sin(angle)*Mathf.Cos(lat),
                    Mathf.Sin(lat),Mathf.Cos(angle)*Mathf.Cos(lat)));
            }
            for(int i=0;i<sides;i++)
            {
                var a=Point(-.58f,i); var b=Point(-.58f,i+1);
                var c=Point(.54f,i+1); var d=Point(.54f,i);
                var color=alternate.HasValue && i%3==0 ? alternate.Value : paint;
                Triangle(color,center-Vector3.up*size.y,b,a,(a+b)*.5f-center+Vector3.down);
                Quad(color,a,b,c,d,(a+b+c+d)*.25f-center);
                Triangle(paint,d,c,center+Vector3.up*size.y,(c+d)*.5f-center+Vector3.up);
            }
        }

        public void Relief(Paint paint, Vector3 offset, float scale, Vector3 center, params Vector3[] outline)
        {
            for(int i=0;i<outline.Length;i++)
            {
                var a=outline[i]; var b=outline[(i+1)%outline.Length];
                Triangle(paint,offset+center*scale,offset+a*scale,offset+b*scale,Vector3.back);
            }
        }

        public void Flame(Vector3 foot, Vector3 bend, float height, float width, Paint edge, Paint core)
        {
            const int sides=7;
            var middle=foot+Vector3.up*height*.34f+bend*.28f;
            var neck=foot+Vector3.up*height*.68f-bend*.13f;
            var tip=foot+Vector3.up*height+bend;
            Vector3 Rim(int index)
            {
                float angle=index*Mathf.PI*2f/sides;
                return V(Mathf.Cos(angle),0f,Mathf.Sin(angle));
            }
            for(int i=0;i<sides;i++)
            {
                var a=Rim(i);
                var b=Rim(i+1);
                var finish=i%3==0 ? edge : core;
                var normal=a+b;
                Quad(edge,foot+a*width*.72f,foot+b*width*.72f,middle+b*width,middle+a*width,normal);
                Quad(finish,middle+a*width,middle+b*width,neck+b*width*.43f,neck+a*width*.43f,normal);
                Triangle(finish,neck+a*width*.43f,neck+b*width*.43f,tip,normal);
            }
        }

        public Mesh Save(string name)
        {
            var path=Folder+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool existing=mesh;
            if(!existing) mesh=new Mesh { name=name };
            mesh.Clear();
            mesh.SetVertices(points);
            mesh.subMeshCount=surfaces.Length;
            for(int i=0;i<surfaces.Length;i++) mesh.SetTriangles(surfaces[i],i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if(existing) { EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh); }
            else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
    }

    static Material Existing(string path) => AssetDatabase.LoadAssetAtPath<Material>(Winter+path+".mat")
        ?? throw new InvalidOperationException("Missing material "+path);

    static Material Tint(string name, Color color, float glow=0f, bool unlit=false)
    {
        var path=Folder+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool existing=material;
        if(!existing) material=new Material(Existing("Route199/End_Grain")) { name=name };
        if(unlit) material.shader=Shader.Find("Unlit/Color") ?? throw new InvalidOperationException("Unlit color shader unavailable");
        material.color=color;
        if(glow>0f)
        {
            material.SetColor("_EmissionColor",color*glow);
            material.EnableKeyword("_EMISSION");
        }
        if(existing) { EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); }
        else AssetDatabase.CreateAsset(material,path);
        return material;
    }

    static Material[] Palette() => new[]{
        Tint("Owl_Bark",new Color(.27f,.125f,.061f)),
        Tint("Owl_Carved_Wood",new Color(.58f,.31f,.125f)),
        Tint("Owl_Grain",new Color(.39f,.19f,.078f)),
        Tint("Owl_Heartwood",new Color(.76f,.46f,.21f)),
        Tint("Owl_Charcoal",new Color(.14f,.083f,.055f)),
        Existing("Snow"),
        Tint("Owl_Slate",new Color(.26f,.33f,.40f)),
        Tint("Owl_Fir",new Color(.13f,.28f,.20f)),
        Tint("Owl_Fir_Light",new Color(.19f,.38f,.25f)),
        Tint("Owl_Lantern",new Color(1f,.69f,.22f),1.15f),
        Tint("Owl_Fire_Orange",new Color(1f,.31f,.055f),unlit:true),
        Tint("Owl_Fire_Gold",new Color(1f,.64f,.11f),unlit:true),
        Tint("Owl_Fire_Core",new Color(1f,.88f,.38f),unlit:true)
    };

    static void Owl(Draft d, Vector3 at, float scale, bool lantern)
    {
        Vector3 P(float x,float y,float z) => at+V(x,y,z)*scale;
        void Gem(Paint paint, float x,float y,float z, Vector3 size, Paint? alt=null)
            => d.Gem(paint,P(x,y,z),size*scale,8,alt);
        void Relief(Paint p,Vector3 center,params Vector3[] shape) => d.Relief(p,at,scale,center,shape);
        Gem(Paint.Grain,0f,.83f,.04f,V(.72f,.89f,.43f),Paint.Bark);
        Gem(Paint.Wood,0f,.78f,-.26f,V(.57f,.66f,.23f),Paint.Honey);
        Gem(Paint.Wood,0f,1.64f,-.03f,V(.85f,.66f,.48f),Paint.Grain);
        for(int side=-1;side<=1;side+=2)
        {
            Gem(Paint.Bark,side*.59f,.88f,.025f,V(.265f,.62f,.37f),Paint.Grain);
            Gem(Paint.Wood,side*.34f,.08f,-.09f,V(.22f,.14f,.27f),Paint.Honey);
            d.Cylinder(Paint.Grain,Paint.Wood,P(side*.45f,1.79f,.015f),P(side*1.04f,2.34f,.015f),.34f*scale,.10f*scale,5,Paint.Wood);
            d.Cylinder(Paint.Snow,Paint.Snow,P(side*.52f,2.064f,.065f),P(side*1.055f,2.395f,.033f),.145f*scale,.082f*scale,5);

            var eye=P(side*.373f,1.65f,-.45f);
            d.Cylinder(Paint.Grain,Paint.Honey,eye,eye+Vector3.back*.115f*scale,.332f*scale,.291f*scale,8,Paint.Wood);
            d.Cylinder(Paint.Dark,Paint.Dark,eye+Vector3.back*.107f*scale,eye+Vector3.back*.193f*scale,.239f*scale,.229f*scale,8);
            if(lantern)
                Gem(Paint.Amber,side*.373f,1.65f,-.641f,V(.164f,.181f,.062f));
            else
                Relief(Paint.Honey,V(side*.373f,1.59f,-.689f),
                    V(side*.525f,1.69f,-.645f),V(side*.373f,1.607f,-.65f),
                    V(side*.21f,1.69f,-.645f),V(side*.373f,1.54f,-.66f));
            Relief(Paint.Bark,V(side*.51f,1.95f,-.656f),
                V(side*.15f,1.85f,-.51f),V(side*.26f,1.985f,-.47f),
                V(side*.82f,2.11f,-.33f),V(side*.69f,1.87f,-.51f));
        }
        Relief(Paint.Honey,V(0,1.58f,-.81f),V(0,1.84f,-.61f),
            V(-.135f,1.55f,-.51f),V(0,1.295f,-.55f),V(.135f,1.55f,-.51f));
        for(int row=0;row<3;row++)
        for(int i=0;i<3;i++)
        {
            float x=(i-1)*(.29f-row*.045f);
            float y=1.24f-row*.235f-Mathf.Abs(x)*.16f;
            float z=-.50f+row*.028f+Mathf.Abs(x)*.21f;
            Relief((row+i)%3==0 ? Paint.Honey : Paint.Wood,V(x,y-.065f,z-.106f),
                V(x-.163f,y+.065f,z),V(x+.163f,y+.065f,z),V(x+.12f,y-.108f,z),V(x,y-.232f,z),V(x-.12f,y-.108f,z));
        }
    }

    static void Rock(Draft d, Vector3 center, Vector3 radius)
    {
        d.Gem(Paint.Rock,center,radius,7,Paint.Grain);
        d.Gem(Paint.Snow,center+Vector3.up*radius.y*.54f,Vector3.Scale(radius,V(.88f,.59f,.88f)),7);
    }

    static void Fir(Draft d, Vector3 foot, float h)
    {
        d.Cylinder(Paint.Bark,Paint.Wood,foot-Vector3.up*.04f,foot+Vector3.up*h*.68f,h*.071f,h*.047f,7);
        for(int i=0;i<3;i++)
        {
            float bottom=h*(.20f+i*.205f), top=h*(.72f+i*.14f), width=h*(.305f-i*.065f);
            d.Cylinder(Paint.Fir,Paint.Fir,foot+Vector3.up*bottom,foot+Vector3.up*top,width,0,7,Paint.Needle);
            for(int j=0;j<3;j++)
            {
                float angle=(j*2.094f+i*.72f);
                var lateral=V(Mathf.Cos(angle),0,Mathf.Sin(angle));
                d.Gem(Paint.Snow,foot+Vector3.up*(bottom+h*.072f)+lateral*width*.63f,
                    V(width*.28f,h*.059f,width*.31f),5);
            }
        }
    }

    static void Seats(Draft d)
    {
        void Log(Vector3 a,Vector3 b,float radius)
        {
            var axis=(b-a).normalized;
            d.Cylinder(Paint.Bark,Paint.Wood,a,b,radius,radius*.94f,10,Paint.Grain);
            d.Cylinder(Paint.Grain,Paint.Honey,a-axis*.012f,a,radius*.69f,radius*.69f,10);
            d.Cylinder(Paint.Grain,Paint.Honey,b,b+axis*.012f,radius*.67f,radius*.67f,10);
            d.Cylinder(Paint.Honey,Paint.Wood,a-axis*.019f,a,radius*.34f,radius*.34f,9);
            d.Cylinder(Paint.Honey,Paint.Wood,b,b+axis*.019f,radius*.32f,radius*.32f,9);
        }
        Log(V(3.52f,.151f,-2.97f),V(3.67f,.153f,-1.39f),.178f);
        Log(V(5.69f,.149f,-2.93f),V(5.96f,.151f,-1.31f),.172f);
        var stump=V(4.75f,-.026f,-.53f);
        d.Cylinder(Paint.Bark,Paint.Honey,stump,stump+Vector3.up*.35f,.33f,.26f,9,Paint.Grain);
        d.Cylinder(Paint.Grain,Paint.Wood,stump+Vector3.up*.349f,stump+Vector3.up*.357f,.177f,.171f,9);
        d.Cylinder(Paint.Wood,Paint.Honey,stump+Vector3.up*.357f,stump+Vector3.up*.363f,.099f,.096f,8);
    }

    static Transform Child(Transform parent,string name,Vector3 local)
    {
        var child=new GameObject(name).transform;
        child.SetParent(parent,false);
        child.localPosition=local;
        return child;
    }

    static void Model(Transform parent,string name, Mesh mesh,Material[] materials,bool solid=false)
    {
        var child=Child(parent,name,Vector3.zero).gameObject;
        child.AddComponent<MeshFilter>().sharedMesh=mesh;
        child.AddComponent<MeshRenderer>().sharedMaterials=materials;
        if(solid) child.AddComponent<MeshCollider>().sharedMesh=mesh;
    }

    static void PointLight(Transform root,string name,Vector3 position,Color color,float range,float intensity)
    {
        var light=Child(root,name,position).gameObject.AddComponent<Light>();
        light.type=LightType.Point;
        light.color=color;
        light.range=range;
        light.intensity=intensity;
        light.shadows=LightShadows.None;
    }

    static void Embers(Transform root,Vector3 position,Material material)
    {
        var child=Child(root,"Rising_Campfire_Embers",position);
        child.localRotation=Quaternion.Euler(-90f,0,0);
        var particles=child.gameObject.AddComponent<ParticleSystem>();
        var main=particles.main;
        main.loop=true;
        main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.15f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(.42f,.87f);
        main.startSize=new ParticleSystem.MinMaxCurve(.018f,.041f);
        main.startColor=Color.white;
        main.maxParticles=16;
        var emission=particles.emission;
        emission.rateOverTime=8f;
        var shape=particles.shape;
        shape.shapeType=ParticleSystemShapeType.Cone;
        shape.radius=.16f;
        shape.angle=16f;
        var renderer=particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode=ParticleSystemRenderMode.Mesh;
        renderer.mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Winter+"Route181/Faceted_Boulder.asset");
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false;
    }

    public static string Build()
    {
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Winter.TrimEnd('/')+"/ShorePOI","OwlWatch225");
        var materials=Palette();
        var root=PrefabUtility.LoadPrefabContents(Point);
        try
        {
            var axis=root.transform.Find("Bank_02_Rooted_Log/Snowbound_Hollow_Log");
            if(!axis || !axis.GetComponent<MeshCollider>()) throw new InvalidOperationException("Hollow log shell missing");
            var group=root.transform.Find(Group);
            if(group) Object.DestroyImmediate(group.gameObject);
            group=Child(root.transform,Group,root.transform.InverseTransformPoint(axis.position));
            group.localRotation=Quaternion.Inverse(root.transform.rotation)*axis.rotation;
            group.localScale=axis.lossyScale;
            var landmarks=new Draft();
            var main=V(-.12f,3.98f,-2.77f);
            Owl(landmarks,main,1f,true);
            var left=V(-2.97f,.0f,-3.50f);
            var right=V(2.71f,.0f,-3.88f);
            Rock(landmarks,left+V(0,.20f,0),V(.71f,.22f,.62f));
            Rock(landmarks,right+V(0,.20f,0),V(.59f,.22f,.55f));
            Owl(landmarks,left+V(.04f,.40f,.025f),.41f,false);
            Owl(landmarks,right+V(-.025f,.40f,.02f),.36f,false);
            Rock(landmarks,left+V(-.57f,.045f,-.58f),V(.52f,.13f,.49f));
            Rock(landmarks,right+V(.53f,.063f,-.43f),V(.44f,.15f,.42f));
            Rock(landmarks,V(-4.5f,.06f,-2.85f),V(.87f,.19f,.69f));
            Rock(landmarks,V(-4.95f,.06f,.88f),V(.72f,.18f,.74f));
            Fir(landmarks,V(-4.40f,0f,-1.36f),3.15f);
            Fir(landmarks,V(-5.06f,0f,.17f),2.29f);
            Fir(landmarks,V(-3.44f,0f,.80f),1.56f);
            Model(group,"Carved_Owls_And_Snowy_Firs",landmarks.Save("Carved_Owls_And_Snowy_Firs"),materials);

            var furniture=new Draft();
            Seats(furniture);
            Model(group,"Solid_Log_Seats_And_Stump",furniture.Save("Log_Seats_And_Stump"),materials,true);

            var camp=new Draft();
            var fire=V(4.63f,0f,-1.90f);
            for(int i=0;i<9;i++)
            {
                float angle=i*Mathf.PI*2f/9f;
                var p=fire+V(Mathf.Cos(angle)*.65f,.104f+Mathf.Sin(i*2.1f)*.03f,Mathf.Sin(angle)*.62f);
                camp.Gem(Paint.Rock,p,V(.245f,.16f,.218f),6,Paint.Dark);
            }
            camp.Gem(Paint.Dark,fire+V(0,.014f,0),V(.59f,.078f,.56f),8);
            for(int i=0;i<3;i++)
            {
                var offset=Quaternion.Euler(0,60f*i,0)*V(.47f,0,0);
                camp.Cylinder(Paint.Bark,Paint.Grain,fire+V(0,.105f,0)-offset,fire+V(0,.14f+i*.047f,0)+offset,.108f,.105f,7,Paint.Dark);
            }
            camp.Flame(fire+V(-.19f,.18f,.05f),V(-.17f,0,.06f),.63f,.21f,Paint.Orange,Paint.FireGold);
            camp.Flame(fire+V(.18f,.20f,.09f),V(.12f,0,.11f),.76f,.219f,Paint.Orange,Paint.FireGold);
            camp.Flame(fire+V(.01f,.22f,-.037f),V(-.12f,0,.09f),.92f,.252f,Paint.FireGold,Paint.Flame);
            camp.Flame(fire+V(.04f,.20f,-.23f),V(.02f,0,.08f),.50f,.133f,Paint.Flame,Paint.FireGold);
            Model(group,"Campfire",camp.Save("Campfire"),materials);
            Embers(group,fire+Vector3.up*.66f,materials[(int)Paint.FireGold]);
            PointLight(group,"Amber_Campfire_Light",fire+Vector3.up*.63f,new Color(1f,.53f,.22f),6.5f,3.2f);
            PointLight(group,"Amber_Owl_Eye_Light",main+V(0,1.59f,-.87f),new Color(1f,.68f,.32f),4.5f,2.1f);

            var props=new Bounds(root.transform.InverseTransformPoint(group.position),Vector3.zero);
            foreach(var renderer in group.GetComponentsInChildren<MeshRenderer>())
            {
                var bounds=renderer.bounds;
                foreach(float x in new[]{bounds.min.x,bounds.max.x})
                foreach(float z in new[]{bounds.min.z,bounds.max.z})
                    props.Encapsulate(root.transform.InverseTransformPoint(V(x,root.transform.position.y,z)));
            }
            foreach(var box in new[]{root.GetComponent<BoxCollider>(),root.transform.Find("Keep_Background_Decor_Clear").GetComponent<BoxCollider>()})
            {
                var center=root.transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
                var footprint=new Bounds(center,box.size);
                var low=props.min-Vector3.one*.85f; low.y=center.y;
                var high=props.max+Vector3.one*.85f; high.y=center.y;
                footprint.Encapsulate(low); footprint.Encapsulate(high);
                box.center=box.transform.InverseTransformPoint(root.transform.TransformPoint(footprint.center));
                box.size=footprint.size;
            }
            PrefabUtility.SaveAsPrefabAsset(root,Point,out bool saved);
            if(!saved) throw new InvalidOperationException("Could not save POI 02");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var savedPoint=AssetDatabase.LoadAssetAtPath<GameObject>(Point);
        if(!savedPoint.transform.Find(Group)) throw new InvalidOperationException("Owl Watch prefab did not persist");
        return "Owl Watch saved in POI 02";
    }
}
