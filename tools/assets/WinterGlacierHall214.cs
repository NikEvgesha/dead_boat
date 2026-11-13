using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WinterGlacierHall214
{
    const string Event="Assets/Events/NewYear/";
    const string Prefab=Event+"ShorePOI/POI_03_Frozen_Grotto.prefab";
    const string Folder=Event+"ShorePOI/GlacierHall214";
    static Vector3 V(float x,float y,float z)=>new(x,y,z);
    static Material Mat(string path)=>AssetDatabase.LoadAssetAtPath<Material>(Event+path+".mat")
        ?? throw new InvalidOperationException("Missing material "+path);

    sealed class Faces
    {
        readonly List<Vector3> vertices=new();
        readonly List<int>[] slots;
        public Faces(int count)=>slots=Enumerable.Range(0,count).Select(_=>new List<int>()).ToArray();
        public void Triangle(int slot,Vector3 a,Vector3 b,Vector3 c,Vector3 outward)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0) (b,c)=(c,b);
            int i=vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            slots[slot].Add(i); slots[slot].Add(i+1); slots[slot].Add(i+2);
        }
        public void Quad(int slot,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
        {
            Triangle(slot,a,b,c,outward); Triangle(slot,a,c,d,outward);
        }
        public Mesh Save(string name)
        {
            string path=Folder+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool existed=mesh;
            if(existed) mesh.Clear(); else mesh=new Mesh{name=name};
            mesh.SetVertices(vertices);
            mesh.subMeshCount=slots.Length;
            for(int i=0;i<slots.Length;i++) mesh.SetTriangles(slots[i],i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if(existed) {EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);}
            else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
    }

    static Transform Group(Transform parent,string name,Vector3 position)
    {
        var t=new GameObject(name).transform;
        t.SetParent(parent,false); t.localPosition=position;
        return t;
    }

    static Transform Part(Transform parent,string name,Vector3 position,Mesh mesh,Vector3 scale,params Material[] materials)
    {
        var part=Group(parent,name,position);
        part.localScale=scale;
        part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        part.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;
        return part;
    }

    static Mesh Masonry()
    {
        var f=new Faces(1);
        var corners=new[]{V(-.38f,0,-.5f),V(.38f,0,-.5f),V(.5f,0,-.38f),V(.5f,0,.38f),
            V(.38f,0,.5f),V(-.38f,0,.5f),V(-.5f,0,.38f),V(-.5f,0,-.38f)};
        Vector3 Rim(int i,float y,float s)=>corners[i]*s+V(0,y,0);
        for(int i=0;i<corners.Length;i++)
        {
            int j=(i+1)%corners.Length;
            var n=corners[i]+corners[j];
            f.Quad(0,Rim(i,-.4f,1),Rim(j,-.4f,1),Rim(j,.4f,1),Rim(i,.4f,1),n);
            f.Quad(0,Rim(i,.4f,1),Rim(j,.4f,1),Rim(j,.5f,.86f),Rim(i,.5f,.86f),n+Vector3.up);
            f.Quad(0,Rim(i,-.5f,.86f),Rim(j,-.5f,.86f),Rim(j,-.4f,1),Rim(i,-.4f,1),n+Vector3.down);
            f.Triangle(0,V(0,.5f,0),Rim(i,.5f,.86f),Rim(j,.5f,.86f),Vector3.up);
            f.Triangle(0,V(0,-.5f,0),Rim(i,-.5f,.86f),Rim(j,-.5f,.86f),Vector3.down);
        }
        return f.Save("Glacier_Hall_Masonry");
    }

    static Mesh GlacialRoof()
    {
        var f=new Faces(4);
        float[] x={-9.30f,-7.05f,-4.48f,-1.9f,.85f,3.58f,6.25f,8.45f};
        float[] ridge={6.2f,8.15f,9.8f,10.35f,11.15f,11.64f,10.28f,7.55f};
        float[] soffit={3.8f,5.22f,5.92f,6.68f,7.16f,6.58f,5.62f,4.2f};
        float[] front={-10.1f,-11.78f,-12.20f,-11.15f,-10.72f,-11.43f,-11.02f,-8.4f};
        Vector3 Point(int row,int i,bool upper)
        {
            float z=row==0?front[i]:row==1?-7.20f+.28f*Mathf.Sin(i*2.1f):-3.15f+.35f*Mathf.Cos(i*1.3f);
            float y=(upper?ridge[i]:soffit[i])+(row==1?.56f:row==2?-.48f:0);
            return V(x[i]*(row==2?.85f:1),y,z);
        }
        for(int row=0;row<2;row++)
        {
            for(int i=0;i<x.Length-1;i++)
            {
                f.Quad(3,Point(row,i,true),Point(row,i+1,true),Point(row+1,i+1,true),Point(row+1,i,true),Vector3.up);
                var a=Point(row,i,false); var b=Point(row,i+1,false);
                var c=Point(row+1,i+1,false); var d=Point(row+1,i,false);
                var center=(a+b+c+d)*.25f+Vector3.down*(.34f+.15f*Mathf.Sin(i*1.8f+row));
                f.Triangle(i%2,a,b,center,Vector3.down); f.Triangle((i+1)%2,b,c,center,Vector3.down);
                f.Triangle(i%2,c,d,center,Vector3.down); f.Triangle((i+1)%2,d,a,center,Vector3.down);
            }
            foreach(int edge in new[]{0,x.Length-1})
                f.Quad(2,Point(row,edge,false),Point(row+1,edge,false),Point(row+1,edge,true),Point(row,edge,true),V(edge==0?-1:1,0,0));
        }
        for(int i=0;i<x.Length-1;i++)
        {
            var a=Point(0,i,false); var b=Point(0,i+1,false);
            var d=Point(0,i,true); var c=Point(0,i+1,true);
            var middleA=Vector3.Lerp(a,d,.62f)+V(0,0,-.38f);
            var middleB=Vector3.Lerp(b,c,.62f)+V(0,0,-.38f);
            f.Quad(i%2,a,b,middleB,middleA,Vector3.back);
            f.Triangle(i%3==0?2:3,middleA,middleB,d,Vector3.back);
            f.Triangle(3,middleB,c,d,Vector3.back);
            f.Quad(2,Point(2,i,false),Point(2,i+1,false),Point(2,i+1,true),Point(2,i,true),Vector3.forward);
        }
        return f.Save("Glacier_Hall_Canopy");
    }

    static Mesh Bluff()
    {
        var f=new Faces(3);
        var skirt=new[]{V(-1,0,-.24f),V(-.65f,0,-.81f),V(.19f,0,-1),V(.88f,0,-.56f),
            V(.96f,0,.38f),V(.28f,0,.91f),V(-.73f,0,.72f)};
        Vector3 Mid(int i)=>skirt[i]*.82f+V(-.04f,.56f+.12f*Mathf.Sin(i*2.45f),.12f);
        Vector3 Snow(int i)=>skirt[i]*.52f+V(.06f,.81f+.13f*Mathf.Cos(i*1.6f),.13f);
        for(int i=0;i<skirt.Length;i++)
        {
            int j=(i+1)%skirt.Length;
            var normal=skirt[i]+skirt[j];
            f.Quad(i%2,skirt[i],skirt[j],Mid(j),Mid(i),normal);
            f.Triangle((i+1)%2,Mid(i),Mid(j),Snow(i),normal+Vector3.up);
            f.Triangle(i%3==0?2:i%2,Mid(j),Snow(j),Snow(i),normal+Vector3.up);
            f.Triangle(2,Snow(i),Snow(j),V(.25f,1.08f,.1f),Vector3.up);
        }
        return f.Save("Glacier_Hall_Rock");
    }

    public static string Build()
    {
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Event+"ShorePOI","GlacierHall214");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            if(root.transform.Find("Glacier_Hall")) throw new InvalidOperationException("Hall already installed");
            var art=root.transform.GetChild(0);
            var content=root.GetComponent<LocationContentSpawner>();
            var marks=content.itemSpawnPoints.Select(p=>p.spawnTransform).Concat(content.enemySpawnPoints).ToArray();
            var local=marks.Select(p=>art.InverseTransformPoint(p.position)).ToArray();
            art.localPosition=V(-24.5f,0,0);
            art.localRotation=Quaternion.Euler(0,-40f,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(art);
            for(int i=0;i<marks.Length;i++) marks[i].position=art.TransformPoint(local[i]);
            var prior=root.transform.Find("Frozen_Wreck");
            if(prior) prior.gameObject.SetActive(false);

            var frame=Group(root.transform,"Glacier_Hall",art.localPosition);
            frame.localRotation=art.localRotation;
            var stone=Mat("Route181/Grotto_Stone"); var dark=Mat("Route181/Grotto_Stone_Dark"); var snow=Mat("Snow");
            var cyan=Mat("Route181/Satin_Ice_Cyan"); var blue=Mat("Route181/Satin_Ice_Blue");
            var roof=GlacialRoof();
            var canopy=Part(frame,"Ice_Vault",Vector3.zero,roof,Vector3.one,cyan,blue,dark,snow);
            canopy.gameObject.AddComponent<MeshCollider>().sharedMesh=roof;
            var bluff=Bluff();
            var left=Part(frame,"Left_Glacier_Buttress",V(-8.05f,-.17f,-5.95f),bluff,V(4.2f,8.25f,7.45f),dark,stone,snow);
            left.localRotation=Quaternion.Euler(0,11,0);
            left.gameObject.AddComponent<MeshCollider>().sharedMesh=bluff;
            var right=Part(frame,"Right_Snow_Ridge",V(7.12f,-.16f,-3.85f),bluff,V(3.55f,9.8f,6.1f),stone,dark,snow);
            right.localRotation=Quaternion.Euler(0,-14,0);
            right.gameObject.AddComponent<MeshCollider>().sharedMesh=bluff;

            var block=Masonry();
            void Stone(Transform parent,string name,Vector3 position,Vector3 size,Material mat,float yaw=0f,bool solid=true)
            {
                var t=Part(parent,name,position,block,size,mat);
                t.localRotation=Quaternion.Euler(0,yaw,0);
                if(solid) t.gameObject.AddComponent<BoxCollider>();
            }
            void Pillar(string name,Vector3 p,float height)
            {
                var pillar=Group(frame,name,p);
                Stone(pillar,"Plinth",V(0,.21f,0),V(2.02f,.48f,1.77f),dark);
                for(int i=0;i<4;i++)
                {
                    float unit=(height-.95f)/4f;
                    Stone(pillar,$"Stone_Course_{i+1}",V(.035f*Mathf.Sin(i),.47f+(i+.5f)*unit,0),
                        V(1.39f-i*.033f,unit-.036f,1.34f-i*.027f),i%2==0?stone:dark,2.1f-i*1.4f);
                }
                Stone(pillar,"Capital",V(0,height-.24f,0),V(1.99f,.55f,1.76f),stone);
            }
            Pillar("Entrance_Left_Pillar",V(-4.96f,0,-11.30f),5.96f);
            Pillar("Entrance_Right_Pillar",V(4.46f,0,-10.89f),6.25f);
            Pillar("Inner_Pillar",V(2.82f,0,-6.65f),6.44f);
            Stone(frame,"Fractured_Lintel_Left",V(-4.12f,6.16f,-11.31f),V(3.69f,.91f,1.55f),dark,-6,false);
            Stone(frame,"Fractured_Lintel_Inner_Edge",V(-2.28f,6.03f,-11.16f),V(1.26f,.63f,1.36f),stone,9,false);
            Stone(frame,"Fractured_Lintel_Right",V(3.82f,6.47f,-10.91f),V(3.65f,.83f,1.55f),dark,7,false);
            Stone(frame,"Inner_Lintel",V(3.60f,6.68f,-6.65f),V(3.38f,.70f,1.25f),dark,-10,false);
            for(int i=0;i<5;i++)
            {
                float x=-6.85f+i*.39f, z=-8.36f+i*.76f;
                Stone(frame,$"West_Rubble_{i}",V(x,.27f,z),V(1.42f,.58f,1.05f),i%2==0?dark:stone,8f+i*13f);
                if(i%2==0) Stone(frame,$"Frost_On_West_Rubble_{i}",V(x,.57f,z),V(1.16f,.15f,.89f),snow,8f+i*13f,false);
            }
            Stone(frame,"East_Rubble",V(5.73f,.30f,-9.26f),V(1.71f,.63f,1.14f),dark,-24);
            Stone(frame,"East_Rubble_Snow",V(5.73f,.64f,-9.26f),V(1.40f,.14f,.96f),snow,-24,false);
            var light=Group(frame,"Light_Through_Ice",V(.15f,5.25f,-8.9f)).gameObject.AddComponent<Light>();
            light.type=LightType.Point; light.color=new Color(.36f,.79f,1f); light.intensity=1.20f; light.range=8.4f;
            light.renderMode=LightRenderMode.ForcePixel;

            DressFront(frame,art);
            return Save(root);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void DressFront(Transform frame,Transform art)
    {
        var waterfall=art.Find("Frozen_Waterfall");
        if(!frame.Find("Front_Frozen_Cascade"))
            Part(frame,"Front_Frozen_Cascade",V(2.90f,0,-3.05f),waterfall.GetComponent<MeshFilter>().sharedMesh,
                V(2,2.1f,2),waterfall.GetComponent<MeshRenderer>().sharedMaterials);
        for(int i=0;i<5;i++)
        {
            float z=-13.0f+i*.71f;
            var rubble=frame.Find($"West_Rubble_{i}");
            var cap=frame.Find($"Frost_On_West_Rubble_{i}");
            var p=rubble.localPosition;
            rubble.localPosition=V(p.x,p.y,z);
            if(cap) cap.localPosition=V(p.x,cap.localPosition.y,z);
        }
    }

    static string Save(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;
        foreach(var r in renderers) bounds.Encapsulate(r.bounds);
        var content=root.GetComponent<LocationContentSpawner>();
        foreach(var mark in content.itemSpawnPoints.Select(s=>s.spawnTransform).Concat(content.enemySpawnPoints)) bounds.Encapsulate(mark.position);
        if(bounds.max.x>-6f) throw new InvalidOperationException("Hall encroaches on the river: "+bounds);
        var trigger=root.GetComponent<BoxCollider>();
        trigger.center=bounds.center; trigger.size=bounds.size+V(1.6f,1.2f,2.4f);
        var clear=root.transform.Find("Keep_Background_Decor_Clear");
        clear.localPosition=trigger.center; clear.GetComponent<BoxCollider>().size=trigger.size;
        PrefabUtility.SaveAsPrefabAsset(root,Prefab,out var saved);
        if(!saved) throw new InvalidOperationException("Could not save glacier hall");
        return $"Glacier hall saved: {bounds.size:F1} m; three pillars and original grotto passage";
    }

    public static string RefineFront()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            DressFront(root.transform.Find("Glacier_Hall"),root.transform.GetChild(0));
            return Save(root);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
