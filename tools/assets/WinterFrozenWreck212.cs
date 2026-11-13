using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class WinterFrozenWreck212
{
    const string Event="Assets/Events/NewYear/";
    const string Prefab=Event+"ShorePOI/POI_03_Frozen_Grotto.prefab";
    const string Folder=Event+"ShorePOI/FrozenWreck212";
    static Vector3 V(float x,float y,float z)=>new(x,y,z);
    static Material Mat(string path)=>AssetDatabase.LoadAssetAtPath<Material>(Event+path+".mat")
        ?? throw new InvalidOperationException("Missing material "+path);

    sealed class Facets
    {
        readonly List<Vector3> vertices=new();
        readonly List<int>[] indices;
        public Facets(int slots)=>indices=Enumerable.Range(0,slots).Select(_=>new List<int>()).ToArray();
        public void Face(int slot,Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0) (b,c)=(c,b);
            int i=vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            indices[slot].Add(i); indices[slot].Add(i+1); indices[slot].Add(i+2);
        }
        public void Quad(int slot,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            Face(slot,a,b,c,normal); Face(slot,a,c,d,normal);
        }
        public Mesh Save(string name)
        {
            string path=Folder+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool exists=mesh;
            if(exists) mesh.Clear(); else mesh=new Mesh {name=name};
            mesh.SetVertices(vertices); mesh.subMeshCount=indices.Length;
            for(int i=0;i<indices.Length;i++) mesh.SetTriangles(indices[i],i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if(exists) { EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh); }
            else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
    }

    static Transform Group(Transform parent,string name,Vector3 local)
    {
        var result=new GameObject(name).transform;
        result.SetParent(parent,false); result.localPosition=local;
        return result;
    }

    static Transform Part(Transform parent,string name,Vector3 p,Mesh mesh,bool solid,params Material[] mats)
    {
        var t=Group(parent,name,p);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=mats;
        if(solid) t.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
        return t;
    }

    static void Timber(Transform parent,string name,Vector3 a,Vector3 b,float width,float depth,Material mat)
    {
        var timber=GameObject.CreatePrimitive(PrimitiveType.Cube);
        timber.name=name;
        Object.DestroyImmediate(timber.GetComponent<Collider>());
        var t=timber.transform; t.SetParent(parent,false);
        t.localPosition=(a+b)*.5f; t.localRotation=Quaternion.LookRotation(b-a);
        t.localScale=V(width,depth,Vector3.Distance(a,b)+.035f);
        timber.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }

    static Mesh Hull()
    {
        var f=new Facets(4);
        var rows=new (float Z,float Width,float Keel,float Top)[]
        { (-3.60f,1.63f,.18f,2.50f),(-1.95f,2.08f,.04f,2.63f),(.2f,1.97f,.06f,2.75f),
            (1.98f,1.53f,.29f,2.91f),(3.32f,.88f,.74f,3.21f),(4.48f,.08f,1.46f,3.47f) };
        float[] breadth={.12f,.47f,.73f,.91f,1f,1f};
        Vector3 Point(int row,int band,int side)
        {
            var r=rows[row];
            return V(side*r.Width*breadth[band],Mathf.Lerp(r.Keel,r.Top,band/5f),r.Z);
        }
        for(int r=0;r<rows.Length-1;r++)
        {
            foreach(int side in new[]{-1,1})
                for(int band=0;band<5;band++)
                {
                    if(r==0 && band<2) continue;
                    var a=Point(r,band,side); var b=Point(r+1,band,side);
                    var c=Point(r+1,band+1,side); var d=Point(r,band+1,side);
                    var normal=V(side,-.3f,r>3?.25f:0);
                    f.Quad((band+r/2)%3,a,b,c,d,normal);
                    var seam=Vector3.up*.029f;
                    f.Quad(2,a+normal*.025f,b+normal*.025f,b+normal*.025f+seam,a+normal*.025f+seam,normal);
                }
            f.Quad(2,Point(r,0,-1),Point(r,0,1),Point(r+1,0,1),Point(r+1,0,-1),Vector3.down);
            var l=Point(r,5,-1); var rr=Point(r,5,1);
            var nextL=Point(r+1,5,-1); var nextR=Point(r+1,5,1);
            f.Quad(0,l,rr,nextR,nextL,Vector3.up);
            foreach(int side in new[]{-1,1})
            {
                var a=Point(r,5,side); var b=Point(r+1,5,side);
                float width=r==0?.56f:.25f+(r%2)*.22f;
                var inner=V(-side*width,.035f,0);
                f.Quad(3,a+V(0,.065f,0),b+V(0,.11f,0),b+inner,a+inner,Vector3.up);
            }
        }
        for(int band=0;band<5;band++)
            f.Quad(2,Point(5,band,-1),Point(5,band,1),Point(5,band+1,1),Point(5,band+1,-1),Vector3.forward);
        return f.Save("Wreck_Bow_Planking");
    }

    static Mesh FrozenMass(string name,bool snowCap)
    {
        var f=new Facets(snowCap?3:2);
        var perimeter=new[]{V(-1,0,-.28f),V(-.56f,0,-.88f),V(.25f,0,-.96f),V(.95f,0,-.35f),
            V(.83f,0,.63f),V(.08f,0,.91f),V(-.81f,0,.59f)};
        Vector3 Shoulder(int i)=>perimeter[i]*.74f+V(.07f,.65f+.19f*Mathf.Sin(i*2.4f),.10f);
        Vector3 Crown(int i)=>perimeter[i]*.36f+V(.22f,.88f+.08f*Mathf.Cos(i*1.7f),.03f);
        var peak=V(.18f,1.06f,-.11f);
        for(int i=0;i<perimeter.Length;i++)
        {
            int j=(i+1)%perimeter.Length;
            var outward=(perimeter[i]+perimeter[j]).normalized;
            f.Quad(i%2,perimeter[i],perimeter[j],Shoulder(j),Shoulder(i),outward);
            f.Quad(snowCap && i%3!=0?2:i%2,Shoulder(i),Shoulder(j),Crown(j),Crown(i),outward+Vector3.up);
            f.Face(snowCap?2:i%2,Crown(i),Crown(j),peak,Vector3.up);
            f.Face(0,perimeter[i],perimeter[j],Vector3.zero,Vector3.down);
        }
        return f.Save(name);
    }

    static void DressShip(Transform frame)
    {
        var wood=Mat("Models/Wood_Brown"); var warm=Mat("Route179/Log_Exposed_Wood"); var dark=Mat("Route179/Log_Bark"); var snow=Mat("Snow");
        var ship=Group(frame,"Buried_Ship",Vector3.zero);
        Part(ship,"Wooden_Bow",Vector3.zero,Hull(),true,wood,warm,dark,snow);
        foreach(int side in new[]{-1,1})
        {
            var ribs=new[]{V(side*.21f,-.21f,0),V(side*.95f,.10f,-.08f),V(side*1.54f,.68f,-.16f),V(side*1.84f,1.59f,-.25f),V(side*1.97f,2.55f,-.38f)};
            for(int rib=0;rib<3;rib++)
                for(int i=0;i<ribs.Length-1;i++)
                {
                    var offset=V(0,.03f*rib,-4.24f+rib*.73f);
                    Timber(ship,$"Exposed_Rib_{side}_{rib}_{i}",ribs[i]+offset,ribs[i+1]+offset,.24f,.25f,rib==0?dark:wood);
                }
            Timber(ship,$"Broken_Gunwale_{side}",V(side*1.96f,2.72f,-2.4f),V(side*1.91f,2.85f,.24f),.19f,.18f,dark);
            Timber(ship,$"Forward_Gunwale_{side}",V(side*1.88f,2.85f,.24f),V(side*.94f,3.32f,3.33f),.18f,.18f,dark);
        }
        Timber(ship,"Heavy_Stem",V(0,1.38f,4.39f),V(0,3.68f,4.56f),.22f,.28f,dark);
        var mastBase=V(-.37f,2.53f,-.72f); var mastEnd=V(-.97f,6.48f,-1.24f);
        Timber(ship,"Broken_Mast",mastBase,mastEnd,.38f,.39f,dark);
        Timber(ship,"Fractured_Mast_Tip",mastEnd-V(0,.23f,0),mastEnd+V(-.10f,.29f,-.03f),.26f,.27f,warm);
        var yardStart=V(-2.37f,5.95f,-.96f); var yardEnd=V(.85f,5.73f,-1.21f);
        Timber(ship,"Broken_Yard",yardStart,yardEnd,.20f,.23f,wood);
        Timber(ship,"Snow_On_Yard",yardStart+V(0,.13f,0),yardEnd+V(0,.13f,0),.20f,.09f,snow);
    }

    static void PoseWreck(Transform frame)
    {
        var ship=frame.Find("Buried_Ship");
        ship.localPosition=V(1.60f,8.35f,-6.90f);
        ship.localRotation=Quaternion.Euler(-13,210,-9);
        var keelIce=frame.Find("Ice_Gripping_Keel");
        keelIce.localPosition=V(1.60f,6.15f,-6.10f);
        keelIce.localScale=V(3.25f,2.54f,2.55f);
        var sideIce=frame.Find("Ice_Beside_Bow");
        sideIce.localPosition=V(4.50f,7.40f,-5.00f);
        sideIce.localScale=V(1.90f,2f,2.40f);
    }

    static string Save(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;
        foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        var content=root.GetComponent<LocationContentSpawner>();
        foreach(var point in content.itemSpawnPoints.Select(p=>p.spawnTransform).Concat(content.enemySpawnPoints))
            bounds.Encapsulate(point.position);
        if(bounds.max.x>-6f) throw new InvalidOperationException("Wreck reaches the river: "+bounds);
        var zone=root.GetComponent<BoxCollider>();
        zone.center=bounds.center; zone.size=bounds.size+V(1.6f,1.2f,2.4f);
        var cleanup=root.transform.Find("Keep_Background_Decor_Clear");
        cleanup.localPosition=zone.center; cleanup.GetComponent<BoxCollider>().size=zone.size;
        PrefabUtility.SaveAsPrefabAsset(root,Prefab,out var saved);
        if(!saved) throw new InvalidOperationException("Failed to save frozen wreck");
        return $"Frozen wreck saved, bounds {bounds.min:F2} to {bounds.max:F2}; original tunnel mesh retained";
    }

    public static string Refine()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            PoseWreck(root.transform.Find("Frozen_Wreck"));
            return Save(root);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static string Build()
    {
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Event+"ShorePOI","FrozenWreck212");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            if(root.transform.Find("Frozen_Wreck")) throw new InvalidOperationException("Frozen wreck already installed");
            var art=root.transform.GetChild(0);
            var spawner=root.GetComponent<LocationContentSpawner>();
            var points=spawner.itemSpawnPoints.Select(p=>p.spawnTransform).Concat(spawner.enemySpawnPoints).ToArray();
            var positions=points.Select(p=>art.InverseTransformPoint(p.position)).ToArray();
            art.localPosition=V(-22,0,0);
            art.localRotation=Quaternion.Euler(0,-29,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(art);
            for(int i=0;i<points.Length;i++) points[i].position=art.TransformPoint(positions[i]);

            var frame=Group(root.transform,"Frozen_Wreck",art.localPosition);
            frame.localRotation=art.localRotation;
            var stone=Mat("Route181/Grotto_Stone"); var dark=Mat("Route181/Grotto_Stone_Dark"); var snow=Mat("Snow");
            var cliff=FrozenMass("Wreck_Snowbound_Rock",true);
            var flank=Part(frame,"Snowbound_Left_Cliff",V(-6.72f,-.15f,.77f),cliff,true,stone,dark,snow);
            flank.localScale=V(5.15f,7.5f,7.7f); flank.localRotation=Quaternion.Euler(0,11,0);
            var shoulder=Part(frame,"Back_Ridge",V(4.5f,3.3f,2.2f),cliff,false,stone,dark,snow);
            shoulder.localScale=V(4.15f,8.1f,5.8f); shoulder.localRotation=Quaternion.Euler(0,-14,0);
            var ice=FrozenMass("Wreck_Blue_Ice",false);
            var cyan=Mat("Route181/Satin_Ice_Cyan"); var blue=Mat("Route181/Satin_Ice_Blue");
            void Ice(string name,Vector3 pos,Vector3 scale,float yaw)
            {
                var piece=Part(frame,name,pos,ice,false,cyan,blue);
                piece.localScale=scale; piece.localRotation=Quaternion.Euler(0,yaw,0);
            }
            Ice("Ice_Gripping_Keel",Vector3.zero,Vector3.one,34);
            Ice("Ice_Beside_Bow",Vector3.zero,Vector3.one,-32);
            Ice("Frozen_Spill_Left",V(-5.77f,-.13f,-6.0f),V(1.76f,1.18f,1.82f),40);
            Ice("Frozen_Spill_Right",V(6.15f,-.14f,-8.4f),V(2.34f,1.64f,2.06f),-20);
            Ice("Frozen_Spill_Shard",V(7.65f,-.10f,-7.6f),V(1.37f,1.01f,1.30f),14);
            DressShip(frame);
            PoseWreck(frame);
            return Save(root);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
