using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Collections;

public static class TileRoofsAndBuildStoneCottage
{
    const string Data="Assets/Prefabs/Workshop/CottageGeometry/";
    const string Town="Assets/Import/kenney_fantasy-town-kit/Models/FBX format/";
    const string WoodPath="Assets/Prefabs/Workshop/WoodenCottage_01.prefab";
    const string StonePath="Assets/Prefabs/Workshop/StoneCottage_01.prefab";
    class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<int>[] tris={new List<int>(),new List<int>(),new List<int>()};
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 outward,int mat)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0){var swap=b;b=c;c=swap;}
            int index=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);tris[mat].AddRange(new[]{index,index+1,index+2});
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward,int mat)
        {Triangle(a,b,c,outward,mat);Triangle(a,c,d,outward,mat);}
        public Mesh Save(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.subMeshCount=3;
            for(int i=0;i<3;i++)mesh.SetTriangles(tris[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Data+name+".asset");return mesh;
        }
    }
    static Material Mat(string name,Color color)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Material>(Data+name+".mat");if(existing!=null)return existing;
        var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(m,Data+name+".mat");return m;
    }
    static Mesh Tiles()
    {
        var b=new Builder();const int rows=9,columns=15;float slope=Mathf.Sqrt(3.3f*3.3f+1.8f*1.8f);
        float step=slope/rows,width=6.5f/columns-.008f,length=step+.06f;
        foreach(float side in new[]{-1f,1f})
        {
            var along=new Vector3(-side*3.3f/slope,1.8f/slope,0);var normal=new Vector3(side*1.8f/slope,3.3f/slope,0);
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                var origin=new Vector3(side*3.3f,3.2f,-3.25f+(col+.5f)*6.5f/columns)+along*(row*step);
                int material=(row+col*2+(side>0?1:0))%3;
                Vector3[] lo=new Vector3[5],hi=new Vector3[5],endLo=new Vector3[5],endHi=new Vector3[5];
                for(int k=0;k<5;k++)
                {
                    float v=-width*.5f+k*width/4,profile=.018f+.035f*(1-4*v*v/(width*width));
                    // Successive rows sit slightly higher so the overlap reads as individual clay tiles.
                    lo[k]=origin+Vector3.forward*v+normal*(.016f+row*.006f);
                    hi[k]=lo[k]+normal*profile;endLo[k]=lo[k]+along*length;endHi[k]=hi[k]+along*length;
                }
                for(int k=0;k<4;k++)
                {
                    b.Quad(hi[k],endHi[k],endHi[k+1],hi[k+1],normal,material);
                    b.Quad(lo[k],lo[k+1],endLo[k+1],endLo[k],-normal,material);
                    b.Quad(lo[k],hi[k],hi[k+1],lo[k+1],-along,material);
                    b.Quad(endLo[k],endLo[k+1],endHi[k+1],endHi[k],along,material);
                }
                b.Quad(lo[0],endLo[0],endHi[0],hi[0],Vector3.back,material);
                b.Quad(lo[4],hi[4],endHi[4],endLo[4],Vector3.forward,material);
            }
        }
        // Fifteen half-round ridge caps cover the junction of the two tiled slopes.
        for(int col=0;col<columns;col++)
        {
            float z0=-3.25f+col*6.5f/columns,z1=z0+6.5f/columns;
            Vector3[] front=new Vector3[7],back=new Vector3[7];
            for(int k=0;k<7;k++){float angle=Mathf.PI*k/6;front[k]=new Vector3(.18f*Mathf.Cos(angle),5.055f+.18f*Mathf.Sin(angle),z0);back[k]=new Vector3(front[k].x,front[k].y,z1);}
            for(int k=0;k<6;k++)
            {
                var n=new Vector3((front[k].x+front[k+1].x)*.5f,(front[k].y+front[k+1].y)*.5f-5.055f,0);
                b.Quad(front[k],back[k],back[k+1],front[k+1],n,col%3);
                b.Triangle(new Vector3(0,5.055f,z0),front[k],front[k+1],Vector3.back,col%3);
                b.Triangle(new Vector3(0,5.055f,z1),back[k],back[k+1],Vector3.forward,col%3);
            }
            b.Quad(front[0],front[6],back[6],back[0],Vector3.down,col%3);
        }
        return b.Save("CottageRoofTiles");
    }
    static Mesh DoubleMesh(Mesh source,string name)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Data+name+".asset");if(existing!=null)return existing;
        using(var all=MeshUtility.AcquireReadOnlyMeshData(source))
        {
            var d=all[0];int count=d.vertexCount;var vv=new NativeArray<Vector3>(count,Allocator.Temp);var nn=new NativeArray<Vector3>(count,Allocator.Temp);var uv=new NativeArray<Vector2>(count,Allocator.Temp);
            d.GetVertices(vv);d.GetNormals(nn);if(d.HasVertexAttribute(VertexAttribute.TexCoord0))d.GetUVs(0,uv);
            var vertices=new Vector3[count*2];var normals=new Vector3[count*2];var uvs=new Vector2[count*2];
            for(int i=0;i<count;i++){vertices[i]=vertices[i+count]=vv[i];normals[i]=nn[i];normals[i+count]=-nn[i];uvs[i]=uvs[i+count]=uv[i];}
            var mesh=new Mesh{name=name,vertices=vertices,normals=normals,uv=uvs,subMeshCount=d.subMeshCount};
            for(int s=0;s<d.subMeshCount;s++)
            {
                var ids=new NativeArray<int>(d.GetSubMesh(s).indexCount,Allocator.Temp);d.GetIndices(ids,s);var both=new int[ids.Length*2];
                for(int i=0;i<ids.Length;i+=3){both[i]=ids[i];both[i+1]=ids[i+1];both[i+2]=ids[i+2];both[ids.Length+i]=ids[i]+count;both[ids.Length+i+1]=ids[i+2]+count;both[ids.Length+i+2]=ids[i+1]+count;}
                mesh.SetTriangles(both,s);ids.Dispose();
            }
            vv.Dispose();nn.Dispose();uv.Dispose();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+name+".asset");return mesh;
        }
    }
    static void Replace(GameObject part,string model,Material stone,Material dark)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Town+model+".fbx");
        var sourceMesh=source.GetComponentInChildren<MeshFilter>();var mf=part.GetComponentInChildren<MeshFilter>();
        mf.sharedMesh=DoubleMesh(sourceMesh.sharedMesh,model+"_StoneInterior");
        part.GetComponentInChildren<Renderer>().sharedMaterials=sourceMesh.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name.StartsWith("stoneDark")?dark:m.name.StartsWith("stone")?stone:m).ToArray();
        var collider=mf.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=mf.sharedMesh;
    }
    static void StoneGrounds(Transform house,Material stone,Material dark)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Data+"StoneFloorSlab.asset");
        if(mesh==null)
        {
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh=UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(temp);
            mesh.name="StoneFloorSlab";var vv=mesh.vertices;for(int i=0;i<vv.Length;i++)vv[i].y=(vv[i].y+.5f)*.06f;mesh.vertices=vv;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"StoneFloorSlab.asset");
        }
        foreach(string group in new[]{"01_Floor","05_Porch"})foreach(Transform part in house.Find(group))
        {
            if(part.name.Contains("stairs")){Replace(part.gameObject,"stairsStone",stone,dark);part.name="Stone_Steps";continue;}
            var mf=part.GetComponentInChildren<MeshFilter>();mf.sharedMesh=mesh;mf.GetComponent<Renderer>().sharedMaterials=new[]{stone};
            var collider=mf.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=mesh;
            part.name="Stone_Slab";
        }
    }
    public static void FinishStone()
    {
        var s=SceneManager.GetActiveScene();var house=s.GetRootGameObjects().First(x=>x.name=="01_Assembly_Area").transform.Find("StoneCottage_01");
        StoneGrounds(house,AssetDatabase.LoadAssetAtPath<Material>(Data+"CottageStone_Gray.mat"),AssetDatabase.LoadAssetAtPath<Material>(Data+"CottageStone_Dark.mat"));
        PrefabUtility.ApplyPrefabInstance(house.gameObject,InteractionMode.AutomatedAction);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
    }
    public static void Main()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/AssetWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open workshop in Edit Mode");
        var area=scene.GetRootGameObjects().First(x=>x.name=="01_Assembly_Area").transform;var wood=area.Find("WoodenCottage_01");
        if(area.Find("StoneCottage_01")!=null||File.Exists(StonePath)||wood.Find("04_Roof/Sealed_Roof_Assembly/Clay_Tiles")!=null)throw new Exception("Already prepared; preserve both houses");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Черепица и каменный дом");
        var tileMesh=Tiles();
        var clay=new[]{Mat("ClayTiles_Brown",new Color(.36f,.2f,.13f)),Mat("ClayTiles_Light",new Color(.42f,.24f,.16f)),Mat("ClayTiles_Dark",new Color(.3f,.16f,.105f))};
        var tiles=new GameObject("Clay_Tiles");tiles.transform.SetParent(wood.Find("04_Roof/Sealed_Roof_Assembly"),false);Undo.RegisterCreatedObjectUndo(tiles,"Черепица");
        tiles.AddComponent<MeshFilter>().sharedMesh=tileMesh;tiles.AddComponent<MeshRenderer>().sharedMaterials=clay;
        PrefabUtility.ApplyPrefabInstance(wood.gameObject,InteractionMode.AutomatedAction);
        var stoneHouse=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WoodPath),scene);
        stoneHouse.name="StoneCottage_01";stoneHouse.transform.SetParent(area,false);stoneHouse.transform.localPosition=Vector3.zero;
        Undo.RegisterCreatedObjectUndo(stoneHouse,"Каменный дом");PrefabUtility.UnpackPrefabInstance(stoneHouse,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
        var stone=Mat("CottageStone_Gray",new Color(.58f,.59f,.56f));var dark=Mat("CottageStone_Dark",new Color(.38f,.4f,.38f));
        foreach(Transform part in stoneHouse.transform.Find("02_Walls"))
        {
            if(!part.gameObject.activeSelf)continue;
            Replace(part.gameObject,part.name.Contains("Window")?"wallWindowGlass":"wall",stone,dark);
            if(part.name.StartsWith("wallWood"))part.name=part.name.Replace("wallWood","StoneWall");
        }
        foreach(Transform part in stoneHouse.transform.Find("03_Frame")){Replace(part.gameObject,"pillarStone",stone,dark);part.name="Stone_Corner";}
        var sealedRoof=stoneHouse.transform.Find("04_Roof/Sealed_Roof_Assembly");
        foreach(var name in new[]{"Front_Gable","Rear_Gable"})sealedRoof.Find(name).GetComponent<Renderer>().sharedMaterial=stone;
        sealedRoof.Find("Continuous_Pitched_Roof").GetComponent<Renderer>().sharedMaterials=new[]{dark,dark};
        sealedRoof.Find("Clay_Tiles").name="Slate_Tiles";
        sealedRoof.Find("Slate_Tiles").GetComponent<Renderer>().sharedMaterials=new[]{Mat("SlateTiles_Gray",new Color(.25f,.29f,.31f)),Mat("SlateTiles_Light",new Color(.31f,.35f,.37f)),Mat("SlateTiles_Dark",new Color(.2f,.23f,.25f))};
        StoneGrounds(stoneHouse.transform,stone,dark);
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(stoneHouse,StonePath,InteractionMode.AutomatedAction);if(prefab==null)throw new Exception("Could not save stone prefab");
        stoneHouse.transform.localPosition=new Vector3(11,0,0);
        Physics.SyncTransforms();
        foreach(var origin in new[]{Vector3.zero,new Vector3(11,0,0)})
            for(float z=-4.5f;z<=2.2f;z+=.1f)
                if(Physics.CheckCapsule(origin+new Vector3(0,.8f,z),origin+new Vector3(0,2,z),.3f,~0,QueryTriggerInteraction.Ignore))throw new Exception("House passage blocked at "+origin+" z="+z);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(group);
        Selection.activeGameObject=stoneHouse;SceneView.lastActiveSceneView?.LookAt(new Vector3(5.5f,2,-.5f),Quaternion.Euler(25,-20,0),12,false,true);
        File.WriteAllText("Temp/two-cottages-result.txt","PASS wooden prefab saved with 270 tiles + 15 ridge caps; stone prefab saved at "+StonePath+"; both passages clear; scene saved");
    }
}
