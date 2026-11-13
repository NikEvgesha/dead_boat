using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WinterFirArch192
{
    const string Winter = "Assets/Events/NewYear/";
    const string Folder = Winter + "Route192";
    const string Prefab = Winter + "Route181/Bank_06_Fir_Arch.prefab";
    const string Model = "Snowy_Fir_Arch";
    static readonly Quaternion Facing = Quaternion.Euler(0f, 76f, 0f);
    enum Finish { Bark, Needles, Garland, Snow, Red, Warm, Gold, Blue }

    sealed class Draft
    {
        readonly List<Vector3> points = new();
        readonly List<int>[] triangles = Enum.GetValues(typeof(Finish)).Cast<Finish>().Select(_ => new List<int>()).ToArray();

        public void Face(Finish finish, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-10f) return;
            if (Vector3.Dot(normal, outward) < 0f) (b, c) = (c, b);
            var indices = triangles[(int)finish];
            indices.Add(points.Count);
            indices.Add(points.Count + 1);
            indices.Add(points.Count + 2);
            points.Add(a); points.Add(b); points.Add(c);
        }

        public void Quad(Finish finish, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Face(finish, a, b, c, outward);
            Face(finish, a, c, d, outward);
        }

        public void Tube(Finish finish, Vector3 a, Vector3 b, float start, float end, int sides = 6)
        {
            var axis = (b-a).normalized;
            var right = Vector3.Cross(axis, Mathf.Abs(axis.z) > .85f ? Vector3.right : Vector3.forward).normalized;
            var up = Vector3.Cross(axis, right);
            Vector3 Radial(int i)
            {
                float angle = i * (2f * Mathf.PI / sides);
                return right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
            }
            for (int i = 0; i < sides; i++)
            {
                var first = Radial(i);
                var next = Radial(i+1);
                Quad(finish, a+first*start, a+next*start, b+next*end, b+first*end, first+next);
                Face(finish, a, a+first*start, a+next*start, -axis);
                Face(finish, b, b+next*end, b+first*end, axis);
            }
        }

        public void Oval(Finish finish, Vector3 center, Vector3 radii, float twist = 0f)
        {
            const int sides = 7;
            Vector3 Point(int i, float y)
            {
                float angle = i * (2f * Mathf.PI / sides) + twist;
                float inset = Mathf.Sqrt(1f-y*y);
                return center + Vector3.Scale(radii, new Vector3(inset*Mathf.Cos(angle), y, inset*Mathf.Sin(angle)));
            }
            for (int i = 0; i < sides; i++)
            {
                var a=Point(i,-.43f); var b=Point(i+1,-.43f);
                var c=Point(i+1,.48f); var d=Point(i,.48f);
                Quad(finish,a,b,c,d,(a+b+c+d)*.25f-center);
                Face(finish,center-Vector3.up*radii.y,a,b,Vector3.down+(a+b)*.5f-center);
                Face(finish,center+Vector3.up*radii.y,d,c,Vector3.up+(d+c)*.5f-center);
            }
        }

        public float Radius => points.Max(p => new Vector2(p.x,p.z).magnitude);
        public Bounds RotatedBounds()
        {
            var bounds = new Bounds(Facing*points[0],Vector3.zero);
            foreach(var point in points) bounds.Encapsulate(Facing*point);
            return bounds;
        }

        public Mesh Save()
        {
            string path = Folder + "/" + Model + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool existing = mesh;
            if (!existing) mesh = new Mesh { name = Model };
            mesh.Clear();
            mesh.SetVertices(points);
            mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            if (existing)
            {
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }
            else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
    }

    static Vector3 TreeAxis(int side,float y)
    {
        float baseX = side < 0 ? -2.24f : 2.17f;
        float bend = side < 0 ? .136f : -.142f;
        return new Vector3(baseX + bend*y, y, side < 0 ? -.16f+.043f*y : .14f-.030f*y);
    }

    static void Fir(Draft mesh, int side, float scale)
    {
        var foot = TreeAxis(side,0f);
        foot.y = -.085f;
        mesh.Tube(Finish.Bark,foot,TreeAxis(side,1.03f),.252f,.189f,7);
        mesh.Tube(Finish.Bark,TreeAxis(side,.98f),TreeAxis(side,3.97f*scale),.189f,.072f,7);

        var layers = new[]
        {
            new Vector4(.67f,2.43f,1.32f,.32f),
            new Vector4(1.50f,3.22f,1.09f,.25f),
            new Vector4(2.37f,4.01f,.82f,.17f),
            new Vector4(3.23f,4.91f,.57f,.022f)
        };
        const int sides = 7;
        for (int layer = 0; layer < layers.Length; layer++)
        {
            var tier = layers[layer];
            var origin = TreeAxis(side,tier.x*scale);
            var tip = TreeAxis(side,tier.y*scale);
            Vector3 Rim(int i, bool upper)
            {
                float angle = i*2f*Mathf.PI/sides + layer*.29f + side*.27f;
                float noise = 1f + .10f*Mathf.Sin(i*2.6f+layer*1.17f+side);
                float radius = (upper ? tier.w : tier.z)*noise;
                var center = upper ? tip : origin;
                return center + new Vector3(radius*Mathf.Cos(angle), upper ? 0f : .075f*Mathf.Cos(i*2.3f+layer),radius*.91f*Mathf.Sin(angle));
            }
            for (int i = 0; i < sides; i++)
            {
                var a=Rim(i,false); var b=Rim(i+1,false);
                var c=Rim(i+1,true); var d=Rim(i,true);
                var outward=(a+b)*.5f-origin;
                outward.y=.3f;
                mesh.Face(Finish.Needles,origin-Vector3.up*.09f,a,b,Vector3.down);
                mesh.Face(Finish.Needles,tip+Vector3.up*.024f,d,c,Vector3.up);
                if ((i+layer+side+7)%3==0 || i==((layer+side+7)%2==0 ? 1 : 5))
                {
                    // White patches share the same edge as the needles, including the irregular snow line.
                    float snowStart=layer==3 ? .64f : .18f;
                    float snowEnd=layer==3 ? .98f : .45f;
                    var e=Vector3.Lerp(a,d,snowStart+.064f*Mathf.Sin(i+layer));
                    var f=Vector3.Lerp(b,c,snowStart+.069f*Mathf.Cos(i+layer));
                    var g=Vector3.Lerp(b,c,snowEnd-.054f*Mathf.Sin(i+layer));
                    var h=Vector3.Lerp(a,d,snowEnd-.079f*Mathf.Cos(i+layer));
                    mesh.Quad(Finish.Needles,a,b,f,e,outward);
                    mesh.Quad(Finish.Snow,e,f,g,h,outward);
                    mesh.Quad(Finish.Needles,h,g,c,d,outward);
                }
                else mesh.Quad(Finish.Needles,a,b,c,d,outward);

                if (layer>0 && layer<3 && Mathf.Abs(outward.z)>.45f && (i+layer+side+7)%2==0)
                {
                    var midpoint=Vector3.Lerp((a+b)*.5f,(c+d)*.5f,.18f);
                    var radial=new Vector3(outward.x,0f,outward.z).normalized;
                    float radius=layer==1?.145f:.115f;
                    var ball=midpoint+radial*radius*.61f-Vector3.up*.065f;
                    mesh.Tube(Finish.Gold,midpoint+Vector3.up*.075f,ball+Vector3.up*.075f,.034f,.038f);
                    mesh.Oval((i+side)%3==0?Finish.Blue:Finish.Red,ball,Vector3.one*radius,i*.4f);
                }
            }
        }
        mesh.Oval(Finish.Snow,foot+new Vector3(side*.59f,.019f,.34f),new Vector3(.53f,.145f,.43f),side*.62f);
        mesh.Oval(Finish.Snow,foot+new Vector3(side*.37f,.039f,-.43f),new Vector3(.41f,.11f,.33f),side*.93f);
    }

    static void Bow(Draft mesh,Vector3 center)
    {
        foreach(float face in new[]{-1f,1f})
        {
            var front=Vector3.forward*face;
            var knot=center+front*.122f;
            foreach(float side in new[]{-1f,1f})
            {
                var top=knot+new Vector3(side*.34f,.14f,face*.019f);
                var outer=knot+new Vector3(side*.42f,-.066f,face*.011f);
                var fold=knot+new Vector3(side*.31f,.019f,face*.10f);
                var bottom=knot+new Vector3(side*.29f,-.105f,face*.023f);
                mesh.Face(Finish.Red,knot,top,fold,front);
                mesh.Face(Finish.Red,top,outer,fold,front);
                mesh.Face(Finish.Red,outer,bottom,fold,front);
                mesh.Face(Finish.Red,bottom,knot,fold,front);
                var inner=knot+new Vector3(side*.076f,-.039f,0f);
                var end=knot+new Vector3(side*.23f,-.425f,face*.016f);
                var width=new Vector3(side*.12f,.053f,0f);
                var notch=end+width*.48f+Vector3.up*.07f;
                mesh.Quad(Finish.Red,inner-width*.24f,inner+width,end+width,notch,front);
                mesh.Face(Finish.Red,inner-width*.24f,notch,end,front);
            }
            mesh.Oval(Finish.Gold,knot,new Vector3(.091f,.102f,.07f));
        }
    }

    static void Garland(Draft mesh)
    {
        var left=TreeAxis(-1,3.95f);
        var right=TreeAxis(1,3.72f);
        Vector3 Cable(float t)
        {
            float sag=Mathf.Sin(t*Mathf.PI);
            return Vector3.Lerp(left,right,t) - new Vector3(0f,.49f*sag,.04f*sag);
        }
        const int segments=18;
        for(int i=0;i<segments;i++)
        {
            var a=Cable((float)i/segments);
            var b=Cable((float)(i+1)/segments);
            mesh.Tube(Finish.Garland,a,b,.095f,.095f);
            var tip=a+(b-a)*.64f+new Vector3(0f,.068f*Mathf.Cos(i*1.7f),.105f*Mathf.Sin(i*1.4f));
            mesh.Tube(Finish.Needles,a,tip,.091f,.017f,5);
            if(i%2==0 || i==9) continue;
            var bulb=a-Vector3.up*.179f;
            mesh.Tube(Finish.Gold,a,bulb+Vector3.up*.058f,.045f,.049f);
            mesh.Oval(i%4==1?Finish.Warm:Finish.Red,bulb,new Vector3(.093f,.12f,.093f),i*.23f);
        }
        Bow(mesh,Cable(.5f)-Vector3.up*.11f);
    }

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var geometry=new Draft();
        Fir(geometry,-1,1f);
        Fir(geometry,1,.923f);
        Garland(geometry);
        var bounds=geometry.RotatedBounds();
        if (geometry.Radius>5.5f || bounds.min.y>-.025f || bounds.max.y<4.6f)
            throw new InvalidOperationException("Unexpected arch bounds "+bounds);

        var fir=AssetDatabase.LoadAssetAtPath<GameObject>(Winter+"Route178/Bank_Decorated_Fir.prefab");
        Material FirMaterial(string name) => fir.GetComponentsInChildren<MeshRenderer>()
            .SelectMany(r=>r.sharedMaterials).First(m=>m.name==name);
        Material WinterMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(Winter+name+".mat");
        var materials = new[]
        {
            FirMaterial("Wood_Dark"),FirMaterial("Pine_Leaves"),WinterMaterial("Fir"),WinterMaterial("Snow"),
            WinterMaterial("Ruby"),WinterMaterial("WinterTown169/Warm_Light"),WinterMaterial("Gold"),WinterMaterial("WinterTown170/Gift_Blue")
        };
        if (materials.Any(m=>!m)) throw new InvalidOperationException("Missing arch materials");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Winter.TrimEnd('/'),"Route192");
        var prefab=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var model=prefab.transform.Find(Model);
            if (!model)
            {
                model=new GameObject(Model).transform;
                model.SetParent(prefab.transform,false);
                model.gameObject.AddComponent<MeshFilter>();
                model.gameObject.AddComponent<MeshRenderer>();
            }
            model.localRotation=Facing;
            model.GetComponent<MeshFilter>().sharedMesh=geometry.Save();
            model.GetComponent<MeshRenderer>().sharedMaterials=materials;
            foreach (Transform old in prefab.transform)
                if (old!=model) old.gameObject.SetActive(false);
            var trigger=prefab.GetComponent<BoxCollider>();
            trigger.center=bounds.center;
            trigger.size=bounds.size;
            if (!PrefabUtility.SaveAsPrefabAsset(prefab,Prefab)) throw new InvalidOperationException("Could not save fir arch");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var report=$"Fir arch 06: two grounded firs, snow surfaces, double-sided bow and sagging garland. Bounds {bounds.min}..{bounds.max}, footprint radius {geometry.Radius:F2} m.";
        File.WriteAllText("Docs/locations/previews/event-winter192-assets.txt",report+Environment.NewLine);
        return report;
    }
}
