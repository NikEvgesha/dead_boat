using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterRoute182
{
    const string Folder = "Assets/Events/NewYear/Route182";
    const string ScenePath = "Assets/Scenes/LevelEvent.unity";
    const string TunnelPath = "Assets/Events/NewYear/Route181/Bank_01_Snow_Tunnel.prefab";
    const string Report = "Docs/locations/previews/event-winter182-assets.txt";
    static readonly List<string> audit = new List<string>();
    static readonly int[][] Tetrahedra =
    {
        new[] {0,5,1,7}, new[] {0,1,3,7}, new[] {0,3,2,7},
        new[] {0,2,6,7}, new[] {0,6,4,7}, new[] {0,4,5,7}
    };

    sealed class Surface
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] indices = { new List<int>(), new List<int>() };

        public void Face(int material, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc,
            Vector2 ua = default, Vector2 ub = default, Vector2 uc = default)
        {
            if (Vector3.Cross(b-a,c-a).sqrMagnitude < 1e-12f) return;
            int first = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            normals.Add(na); normals.Add(nb); normals.Add(nc);
            uv.Add(ua); uv.Add(ub); uv.Add(uc);
            indices[material].Add(first); indices[material].Add(first+1); indices[material].Add(first+2);
        }

        public Mesh Save(string name)
        {
            if (indices.Any(i => i.Count == 0)) throw new InvalidOperationException("Surface lost a material: " + name);
            string path = Folder + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = !mesh;
            if (isNew) mesh = new Mesh { name = name };
            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;
            for (int i=0;i<2;i++) mesh.SetTriangles(indices[i],i);
            mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh,path);
            else
            {
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }
            return mesh;
        }
    }

    static float HillHeight(Vector3 p)
    {
        float along = p.z / 4.64f;
        float center = .53f + .24f * along;
        float across = (p.x - center) / 4.39f;
        float oval = 1f - across * across - Mathf.Pow(Mathf.Abs(along), 3.2f);
        if (oval <= 0f) return -.19f + oval * 2f;

        float edge = Mathf.Pow(oval,.57f);
        float rise = (p.x - (1.93f + .23f * p.z)) / 1.66f;
        float alongRidge = (p.z - .42f) / 3.04f;
        float summit = 1.84f * Mathf.Exp(-rise * rise - alongRidge * alongRidge);
        float shelfLine = p.x - 2.60f - .26f * p.z;
        float cornice = .59f * Mathf.Exp(-Mathf.Abs(shelfLine) * 1.88f - (p.z - .88f)*(p.z - .88f)*.17f);
        float contour = (.17f * Mathf.Sin(p.x * 1.9f + p.z * .59f) +
            .12f * Mathf.Cos(p.z * 1.62f - p.x * .74f)) * edge;
        return -.19f + (3.98f + summit + cornice) * edge + contour;
    }

    static float HillField(Vector3 p) => Mathf.Max(p.y - HillHeight(p), -.19f - p.y);

    static float OpeningField(Vector3 p)
    {
        float mouth = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(2.38f,4.25f,Mathf.Abs(p.z)));
        float center = -.21f + .10f * Mathf.Sin(p.z * .82f) - .12f * mouth;
        float width = 1.51f + .21f * mouth;
        float rise = Mathf.Max(p.y - .74f, 0f) / (1.90f + .14f * mouth);
        float side = (p.x - center) / width;
        return (Mathf.Sqrt(side*side+rise*rise)-1f)*1.5f;
    }

    static float Field(Vector3 p)
    {
        float shell = HillField(p);
        float cut = -OpeningField(p);
        const float roundedLip = .15f;
        float blend = Mathf.Max(roundedLip - Mathf.Abs(shell - cut), 0f) / roundedLip;
        return Mathf.Max(shell, cut) + blend * blend * roundedLip * .25f;
    }

    static Vector3 FieldNormal(Vector3 p)
    {
        const float d = .014f;
        return new Vector3(
            Field(p+Vector3.right*d)-Field(p-Vector3.right*d),
            Field(p+Vector3.up*d)-Field(p-Vector3.up*d),
            Field(p+Vector3.forward*d)-Field(p-Vector3.forward*d)).normalized;
    }

    static Mesh SnowHill()
    {
        var surface = new Surface();
        var min = new Vector3(-4.83f,-.59f,-5.07f);
        const float spacing = .245f;
        const int nx = 43, ny = 28, nz = 43;
        var values = new float[nx,ny,nz];
        Vector3 Position(int x,int y,int z) => min + new Vector3(x,y,z)*spacing;
        for(int x=0;x<nx;x++)
            for(int y=0;y<ny;y++)
                for(int z=0;z<nz;z++) values[x,y,z] = Field(Position(x,y,z));
        var points = new Vector3[8];
        var samples = new float[8];
        var inside = new int[4];
        var outside = new int[4];

        Vector3 Cut(int a,int b) => Vector3.LerpUnclamped(points[a],points[b],samples[a]/(samples[a]-samples[b]));
        void Emit(Vector3 a,Vector3 b,Vector3 c,Vector3 outward)
        {
            var cross = Vector3.Cross(b-a,c-a);
            if (cross.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(cross,outward)<0)
            {
                var swap=b; b=c; c=swap;
                cross=-cross;
            }
            var face = cross.normalized;
            var center = (a+b+c)/3f;
            int material = -OpeningField(center) >= HillField(center) && center.y > -.15f ? 1 : 0;
            Vector3 Normal(Vector3 p) => Vector3.Lerp(face,FieldNormal(p),.90f).normalized;
            surface.Face(material,a,b,c,Normal(a),Normal(b),Normal(c));
        }

        for (int x=0;x<nx-1;x++)
            for(int y=0;y<ny-1;y++)
                for(int z=0;z<nz-1;z++)
                {
                    int filled = 0;
                    for(int i=0;i<8;i++)
                    {
                        int sx=x+(i&1), sy=y+((i>>1)&1), sz=z+((i>>2)&1);
                        points[i]=Position(sx,sy,sz);
                        samples[i]=values[sx,sy,sz];
                        if(samples[i]<0) filled++;
                    }
                    if(filled==0 || filled==8) continue;
                    foreach(var tetra in Tetrahedra)
                    {
                        int a=0,b=0;
                        var insideCenter = Vector3.zero;
                        var outsideCenter = Vector3.zero;
                        foreach(int i in tetra)
                        {
                            if(samples[i]<0) {inside[a++]=i; insideCenter+=points[i];}
                            else {outside[b++]=i; outsideCenter+=points[i];}
                        }
                        if(a==0||b==0) continue;
                        var outward=outsideCenter/b-insideCenter/a;
                        if(a==1) Emit(Cut(inside[0],outside[0]),Cut(inside[0],outside[1]),Cut(inside[0],outside[2]),outward);
                        else if(b==1) Emit(Cut(inside[0],outside[0]),Cut(inside[1],outside[0]),Cut(inside[2],outside[0]),outward);
                        else
                        {
                            var p=Cut(inside[0],outside[0]);
                            var q=Cut(inside[0],outside[1]);
                            var r=Cut(inside[1],outside[1]);
                            var s=Cut(inside[1],outside[0]);
                            Emit(p,q,r,outward);
                            Emit(p,r,s,outward);
                        }
                    }
                }
        return surface.Save("Wind_Carved_Snow_Hill");
    }

    static Material TunnelSnowMaterial(Material snow)
    {
        string path = Folder + "/Blue_Packed_Snow.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(snow) { name = "Blue_Packed_Snow", color = new Color(.62f,.79f,.89f) };
        material.SetFloat("_Glossiness",.08f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor",new Color(.037f,.062f,.092f));
        AssetDatabase.CreateAsset(material,path);
        return material;
    }

    static void ReplaceTunnel(Material snow)
    {
        var mesh=SnowHill();
        float radial = mesh.vertices.Max(p=>new Vector2(p.x,p.z).magnitude);
        var bounds=mesh.bounds;
        if(radial > 6f || Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x)) > 5.1f ||
            Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z)) > 5f)
            throw new InvalidOperationException("Snow hill exceeds the existing route clearance " + bounds + " radius=" + radial);

        var prefab=PrefabUtility.LoadPrefabContents(TunnelPath);
        try
        {
            var shell=prefab.transform.Find("Walkthrough_Shell");
            var trim=prefab.transform.Find("Tunnel_Snow_Accents");
            if(trim) Object.DestroyImmediate(trim.gameObject);
            shell.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=shell.GetComponent<MeshRenderer>();
            renderer.sharedMaterials=new[] { snow, TunnelSnowMaterial(snow) };
            renderer.receiveShadows=false;
            var collision = shell.GetComponent<MeshCollider>();
            collision.sharedMesh=null;
            collision.sharedMesh=mesh;
            var trigger=prefab.GetComponent<BoxCollider>();
            trigger.center=bounds.center;
            trigger.size=bounds.size;
            if(!PrefabUtility.SaveAsPrefabAsset(prefab,TunnelPath)) throw new InvalidOperationException("Tunnel save failed");
            audit.Add($"Snow hill {bounds.min}..{bounds.max}; radial extent {radial:F2} m; continuous surface and same open shell collision.");
        }
        finally {PrefabUtility.UnloadPrefabContents(prefab);}
    }

    readonly struct Point
    {
        public readonly Vector3 position, normal;
        public readonly Vector2 uv;
        public readonly float heightAboveEdge;
        public Point(Vector3 pos,Vector3 normal,Vector2 uv,float height)
        {
            position=pos; this.normal=normal; this.uv=uv; heightAboveEdge=height;
        }
        public Point ToEdge(Point b)
        {
            float t = heightAboveEdge / (heightAboveEdge-b.heightAboveEdge);
            return new Point(Vector3.LerpUnclamped(position,b.position,t),
                Vector3.LerpUnclamped(normal,b.normal,t).normalized,
                Vector2.LerpUnclamped(uv,b.uv,t),0f);
        }
    }

    static Mesh SnowCappedRock(Mesh original)
    {
        var surface=new Surface();
        var verts=original.vertices;
        var normals=original.normals;
        var uv=original.uv;
        var tris=original.triangles;
        Bounds bounds=original.bounds;
        float Edge(Vector3 p)
        {
            float x=(p.x-bounds.center.x)/bounds.extents.x;
            float z=(p.z-bounds.center.z)/bounds.extents.z;
            float h=(p.y-bounds.min.y)/bounds.size.y;
            float snowLine=.695f+.055f*Mathf.Sin(x*5.4f+z*2.1f)+
                .040f*Mathf.Cos(z*6.3f-x*2.2f)+.038f*x;
            return h-snowLine;
        }
        void Clip(int material,Point a,Point b,Point c)
        {
            var points=new[] {a,b,c};
            var clipped=new List<Point>(4);
            for(int i=0;i<3;i++)
            {
                var from=points[i];
                var to=points[(i+1)%3];
                bool keepFrom=material==1?from.heightAboveEdge>=0:from.heightAboveEdge<=0;
                bool keepTo=material==1?to.heightAboveEdge>=0:to.heightAboveEdge<=0;
                if(keepFrom) clipped.Add(from);
                if(keepFrom!=keepTo) clipped.Add(from.ToEdge(to));
            }
            for(int i=1;i+1<clipped.Count;i++)
            {
                var p=clipped[0]; var q=clipped[i]; var r=clipped[i+1];
                surface.Face(material,p.position,q.position,r.position,p.normal,q.normal,r.normal,p.uv,q.uv,r.uv);
            }
        }
        const int subdivisions=6;
        for(int i=0;i<tris.Length;i+=3)
        {
            int a=tris[i],b=tris[i+1],c=tris[i+2];
            Point At(int x,int y)
            {
                float u=x/(float)subdivisions, v=y/(float)subdivisions, w=1f-u-v;
                var p=verts[a]*w+verts[b]*u+verts[c]*v;
                return new Point(p,(normals[a]*w+normals[b]*u+normals[c]*v).normalized,
                    uv.Length==verts.Length?uv[a]*w+uv[b]*u+uv[c]*v:Vector2.zero,Edge(p));
            }
            for(int x=0;x<subdivisions;x++)
                for(int y=0;y<subdivisions-x;y++)
                {
                    var p=At(x,y); var q=At(x+1,y); var r=At(x,y+1);
                    Clip(0,p,q,r); Clip(1,p,q,r);
                    if(x+y+2>subdivisions) continue;
                    var s=At(x+1,y+1);
                    Clip(0,q,s,r); Clip(1,q,s,r);
                }
        }
        var result=surface.Save(original.name+"_WinterCap");
        if(Vector3.Distance(result.bounds.min,original.bounds.min)>.0001f ||
            Vector3.Distance(result.bounds.max,original.bounds.max)>.0001f)
            throw new InvalidOperationException("Rock silhouette changed: "+original.name);
        audit.Add($"{original.name}: continuous snow line, upper cap {result.GetIndexCount(1)/3} triangles.");
        return result;
    }

    static void ApplyMountains(Scene scene, Material snow)
    {
        var land=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<LendController>(true)).Single();
        var rocks=land.locations[0].groundSegments.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true))
            .Where(f=>f.name.StartsWith("rock",StringComparison.Ordinal)).ToArray();
        if(rocks.Length!=30) throw new InvalidOperationException("Unexpected mountain count "+rocks.Length);
        var groups=rocks.GroupBy(f=>f.sharedMesh).ToArray();
        if(groups.Length!=3) throw new InvalidOperationException("Unexpected mountain mesh variants");
        foreach(var group in groups)
        {
            var source=group.Key;
            var original=source.name.EndsWith("_WinterCap",StringComparison.Ordinal)?
                AssetDatabase.LoadAllAssetsAtPath("Assets/Import/kenney_fantasy-town-kit/Models/OBJ format/"+source.name.Replace("_WinterCap","")+".obj").OfType<Mesh>().Single():source;
            var mesh=SnowCappedRock(original);
            foreach(var filter in group)
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                var stone=renderer.sharedMaterials[0];
                filter.sharedMesh=mesh;
                renderer.sharedMaterials=new[] {stone,snow};
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        audit.Add($"{rocks.Length} mountain renderers overridden in {scene.name} only; original collision and stone material retained.");
    }

    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool openedHere=!scene.isLoaded;
        if(openedHere) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            if(scene.isDirty) throw new InvalidOperationException("LevelEvent has unsaved changes");
            if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Events/NewYear","Route182");
            audit.Clear();
            var snow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Snow.mat");
            ReplaceTunnel(snow);
            ApplyMountains(scene,snow);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("LevelEvent save failed");
            File.WriteAllLines(Report,audit);
            return string.Join(" | ",audit);
        }
        finally {if(openedHere) EditorSceneManager.CloseScene(scene,true);}
    }
}


