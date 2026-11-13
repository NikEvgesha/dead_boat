using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WinterFair206
{
    const string Event = "Assets/Events/NewYear/";
    const string Point = Event+"ShorePOI/POI_06_Fir_Arch.prefab";
    const string Arch = Event+"Route181/Bank_06_Fir_Arch.prefab";
    const string PathMesh = Event+"ShorePOI/Winter_Fair_Paths.asset";
    const float Radius = 11.2f;
    static readonly Vector3 Center = new(-22f,0f,0f);

    static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path)
        ?? throw new InvalidOperationException("Missing asset "+path);

    static Material Mat(string name) => Asset<Material>(Event+name+".mat");
    static Vector3 At(float x,float y,float z) => new(x,y,z);
    static Vector3 Direction(float degrees) => Quaternion.Euler(0,degrees,0)*Vector3.forward;

    static Transform Group(Transform parent,string name,Vector3 position)
    {
        var group=new GameObject(name).transform;
        group.SetParent(parent,false);
        group.localPosition=position;
        return group;
    }

    static Transform Prop(Transform parent,string path,string name)
    {
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(Asset<GameObject>(path),parent);
        instance.name=name;
        instance.tag="Untagged";
        var oldZone=instance.GetComponent<BoxCollider>();
        oldZone.enabled=false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(oldZone);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        return instance.transform;
    }

    static Transform Block(Transform parent,string name,Material mat,Vector3 position,Vector3 size,bool solid=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;
        var part=go.transform;
        part.SetParent(parent,false);
        part.localPosition=position;
        part.localScale=size;
        go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        if (!solid) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return part;
    }

    static void Stall(Transform fair,int index,Material stripe)
    {
        var stall=Group(fair,$"Market_Stall_{index+1:00}",Vector3.zero);
        PlaceStall(stall,index);
        var wood=Mat("Models/Wood_Brown");
        var dark=Mat("Route179/Log_Bark");
        var cream=Mat("Models/Warm_Cream");
        for (int side=-1;side<=1;side+=2)
        {
            for (int end=-1;end<=1;end+=2)
                Block(stall,$"Post_{side}_{end}",dark,At(side*1.31f,1.23f,end*.67f),At(.14f,2.52f,.14f));
            Block(stall,$"Eave_{side}",dark,At(0,2.47f,side*.91f),At(3.32f,.12f,.14f));
        }

        Block(stall,"Counter_Base",wood,At(0,.48f,.48f),At(2.66f,.96f,.55f),true);
        Block(stall,"Counter_Top",dark,At(0,1.01f,.48f),At(2.89f,.13f,.76f));
        for (int plank=0;plank<5;plank++)
            Block(stall,$"Front_Board_{plank+1}",plank%2==0?wood:dark,At((plank-2)*.52f,.48f,.77f),At(.45f,.85f,.07f));

        for (int band=0;band<7;band++)
        {
            var color=band%2==0?stripe:cream;
            float x=(band-3)*.48f;
            for (int side=-1;side<=1;side+=2)
            {
                var roof=Block(stall,$"Canvas_{band}_{side}",color,At(x,2.68f,side*.54f),At(.48f,.07f,1.12f));
                roof.localRotation=Quaternion.Euler(side*21f,0,0);
            }
            Block(stall,$"Valance_{band}",color,At(x,2.39f,1.04f),At(.48f,band%2==0?.19f:.14f,.06f));
        }
        Block(stall,"Snow_On_Ridge",Mat("Snow"),At(0,2.91f,-.07f),At(2.75f,.07f,.24f));
        Block(stall,"Lantern_Foot",dark,At(.95f,1.105f,.51f),At(.28f,.07f,.28f));
        Block(stall,"Lantern_Glass",Mat("WinterTown169/Warm_Light"),At(.95f,1.26f,.51f),At(.18f,.24f,.18f));
        Block(stall,"Lantern_Cap",dark,At(.95f,1.405f,.51f),At(.25f,.06f,.25f));
        Gift(stall,"Display_Gift_A",Mat("Ruby"),At(-.74f,1.26f,.50f),.38f);
        Gift(stall,"Display_Gift_B",stripe,At(-.20f,1.22f,.42f),.30f);
    }

    static void PlaceStall(Transform stall,int index)
    {
        var direction=Direction(22.5f+index*90f);
        stall.localPosition=direction*7.05f;
        stall.localRotation=Quaternion.LookRotation(-direction);
    }

    static void Gift(Transform parent,string name,Material color,Vector3 center,float size)
    {
        var gift=Group(parent,name,center);
        gift.localRotation=Quaternion.Euler(0,12,0);
        var gold=Mat("Gold");
        Block(gift,"Box",color,Vector3.zero,Vector3.one*size);
        Block(gift,"Ribbon_X",gold,At(0,size*.51f,0),At(size*.19f,.025f,size*1.02f));
        Block(gift,"Ribbon_Z",gold,At(0,size*.52f,0),At(size*1.02f,.025f,size*.19f));
    }

    static void Path(Transform fair)
    {
        var mesh=new Mesh {name="Winter Fair Paths"};
        const int count=48;
        var vertices=new Vector3[count*2+count+1];
        var triangles=new int[count*9];
        int center=count*3;
        vertices[center]=At(0,.026f,0);
        for (int i=0;i<count;i++)
        {
            var d=Direction(i*360f/count);
            vertices[i*2]=d*9.55f+Vector3.up*.028f;
            vertices[i*2+1]=d*12.75f+Vector3.up*.028f;
            vertices[count*2+i]=d*3.85f+Vector3.up*.026f;
            int a=i*2,b=(i+1)%count*2,t=i*9;
            triangles[t]=a; triangles[t+1]=a+1; triangles[t+2]=b+1;
            triangles[t+3]=a; triangles[t+4]=b+1; triangles[t+5]=b;
            triangles[t+6]=center; triangles[t+7]=count*2+i; triangles[t+8]=count*2+(i+1)%count;
        }
        mesh.vertices=vertices;
        mesh.triangles=triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(PathMesh);
        if (existing)
        {
            EditorUtility.CopySerialized(mesh,existing);
            Object.DestroyImmediate(mesh);
            mesh=existing;
        }
        else AssetDatabase.CreateAsset(mesh,PathMesh);
        var path=Group(fair,"Packed_Snow_Paths",Vector3.zero).gameObject;
        path.AddComponent<MeshFilter>().sharedMesh=mesh;
        path.AddComponent<MeshRenderer>().sharedMaterial=Mat("Route182/Blue_Packed_Snow");
        AssetDatabase.SaveAssetIfDirty(mesh);
    }

    static void Gate(Transform root,Transform fair,int index)
    {
        var direction=Direction(90+index*45);
        var gate=index==0?root.GetChild(0):Prop(fair,Arch,$"Fir_Gate_{index+1:00}");
        gate.position=fair.TransformPoint(direction*Radius);
        var model=gate.Find("Snowy_Fir_Arch");
        gate.rotation=Quaternion.LookRotation(direction)*Quaternion.Inverse(model.localRotation);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gate);

        var mesh=model.GetComponent<MeshFilter>().sharedMesh;
        var vertices=mesh.vertices;
        var bark=mesh.GetTriangles(0).Distinct().Select(i=>vertices[i]).ToArray();
        for (int side=-1;side<=1;side+=2)
        {
            var baseVertices=bark.Where(v=>v.x*side>1.45f && v.y<.02f).ToArray();
            var foot=baseVertices.Aggregate(Vector3.zero,(sum,v)=>sum+v)/baseVertices.Length;
            var local=fair.InverseTransformPoint(model.TransformPoint(foot));
            local.y=.68f;
            var trunk=Group(fair,$"Gate_{index+1:00}_Trunk_{side}",local).gameObject.AddComponent<CapsuleCollider>();
            trunk.radius=.28f;
            trunk.height=1.36f;
        }
    }

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var root=PrefabUtility.LoadPrefabContents(Point);
        try
        {
            if (root.transform.Find("Winter_Fair")) throw new InvalidOperationException("Fair already exists on 06");
            var content=root.GetComponent<LocationContentSpawner>();
            var fair=Group(root.transform,"Winter_Fair",Center);
            Path(fair);
            for (int i=0;i<8;i++) Gate(root.transform,fair,i);

            var tree=Prop(fair,Event+"Route178/Bank_Single_Fir.prefab","Market_Christmas_Tree");
            tree.localScale=Vector3.one*1.38f;
            tree.localPosition=Vector3.zero;
            tree.localRotation=Quaternion.identity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tree);
            var treeTrunk=Group(fair,"Center_Tree_Trunk",At(0,.9f,0)).gameObject.AddComponent<CapsuleCollider>();
            treeTrunk.height=1.8f;
            treeTrunk.radius=.34f;

            var colors=new[]{Mat("Models/Holiday_Red"),Mat("Castle175/Tower_Pine_Green"),
                Mat("WinterTown170/Gift_Blue"),Mat("Castle175/Tower_Cranberry_Red")};
            for (int i=0;i<colors.Length;i++) Stall(fair,i,colors[i]);

            content.itemSpawnPoints[0].spawnTransform.localPosition=Center+At(4.1f,.22f,3.65f);
            content.itemSpawnPoints[1].spawnTransform.localPosition=Center+At(-4.1f,.22f,-3.65f);
            content.enemySpawnPoints[0].localPosition=Center+At(-1.5f,.03f,5.2f);

            var zone=new Bounds(Center,Vector3.zero);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>()) zone.Encapsulate(renderer.bounds);
            if (zone.max.x>=-6f) throw new InvalidOperationException("Fair extends into the channel");
            var region=root.GetComponent<BoxCollider>();
            region.center=zone.center;
            region.size=zone.size+At(1.6f,1.2f,2.4f);
            var cleaner=root.transform.Find("Keep_Background_Decor_Clear");
            cleaner.localPosition=region.center;
            cleaner.GetComponent<BoxCollider>().size=region.size;
            PrefabUtility.SaveAsPrefabAsset(root,Point,out var saved);
            if (!saved) throw new InvalidOperationException("Could not save the fair");
            return $"Eight gates, four stalls, fair bounds {zone.min:F2} .. {zone.max:F2}";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
