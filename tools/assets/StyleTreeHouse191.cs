using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StyleTreeHouse191
{
    const string PrefabPath = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    const string MeshPath = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Oak191.asset";
    const string OakPath = "Assets/Prefabs/Locations/Decoration/Hilly_Prop_Tree_Oak_4.prefab";

    static bool IsGroundDeck(Vector3 point)
    {
        return point.x >= -58.67f && point.x <= -51.53f &&
            point.z >= 15.36f && point.z <= 19.36f && point.y >= .105f && point.y <= .24f;
    }

    static void AddLeafCluster(Transform root, MeshRenderer reference, Vector3 center, Vector3 size, float yaw)
    {
        var cluster = new GameObject("Oak_Leaf_Cluster");
        SceneManager.MoveGameObjectToScene(cluster,root.gameObject.scene);
        cluster.transform.SetParent(root,true);
        var mesh = reference.GetComponent<MeshFilter>().sharedMesh;
        cluster.AddComponent<MeshFilter>().sharedMesh = mesh;
        cluster.AddComponent<MeshRenderer>().sharedMaterial = reference.sharedMaterial;
        var rotation = Quaternion.Euler(0,yaw,0);
        var scale = new Vector3(size.x/mesh.bounds.size.x,size.y/mesh.bounds.size.y,size.z/mesh.bounds.size.z);
        cluster.transform.localScale = scale;
        cluster.transform.SetPositionAndRotation(center - rotation * Vector3.Scale(mesh.bounds.center,scale),rotation);
    }

    public static string Main()
    {
        if(AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath)) throw new InvalidOperationException("Oak mesh already exists.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var oak = AssetDatabase.LoadAssetAtPath<GameObject>(OakPath);
            var references = oak.GetComponentsInChildren<MeshRenderer>();
            var leaves = references.Where(r=>r.sharedMaterial.name=="Oak_Leaves").ToArray();
            var bark = references.Single(r=>r.sharedMaterial.name=="Wood_Dark").sharedMaterial;
            if(leaves.Length<3) throw new InvalidOperationException("Oak foliage assets missing");

            var filter = root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="dom");
            var source = filter.sharedMesh;
            var points = source.vertices.Select(filter.transform.TransformPoint).ToArray();
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = "TreeHouse_Oak";
            var triangles = source.GetTriangles(2);
            var groundRemoved = 0;
            var retained = new System.Collections.Generic.List<int>(triangles.Length);
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i], b=triangles[i+1], c=triangles[i+2];
                if(IsGroundDeck(points[a]) && IsGroundDeck(points[b]) && IsGroundDeck(points[c])) {groundRemoved++;continue;}
                retained.Add(a);
                retained.Add(b);
                retained.Add(c);
            }
            if(groundRemoved<2) throw new InvalidOperationException("Could not identify ground deck");
            mesh.SetTriangles(retained,2,false);
            mesh.SetTriangles(Array.Empty<int>(),4,false);
            mesh.SetTriangles(Array.Empty<int>(),5,false);
            AssetDatabase.CreateAsset(mesh,MeshPath);
            filter.sharedMesh=mesh;
            var collider=filter.GetComponent<MeshCollider>();
            collider.sharedMesh=mesh;
            var renderer=filter.GetComponent<Renderer>();
            var materials=renderer.sharedMaterials;
            materials[3]=bark;
            renderer.sharedMaterials=materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

            var canopy=new GameObject("Oak_Canopy_191");
            SceneManager.MoveGameObjectToScene(canopy,root.scene);
            canopy.transform.SetParent(root.transform,true);
            // The six outer clusters cover the measured branch tips. The seventh ties the crown together.
            AddLeafCluster(canopy.transform,leaves[0],new Vector3(-73.8f,30.0f,7.7f),new Vector3(17,11,16),12);
            AddLeafCluster(canopy.transform,leaves[1],new Vector3(-64.7f,30.6f,19.5f),new Vector3(17,11,18),27);
            AddLeafCluster(canopy.transform,leaves[2],new Vector3(-53.0f,28.1f,20.0f),new Vector3(15,10,15),73);
            AddLeafCluster(canopy.transform,leaves[1],new Vector3(-62.8f,28.3f,-10.2f),new Vector3(17,10,17),-21);
            AddLeafCluster(canopy.transform,leaves[0],new Vector3(-53.3f,27.3f,-5.6f),new Vector3(15,10,15),41);
            AddLeafCluster(canopy.transform,leaves[2],new Vector3(-59.0f,28.5f,7.3f),new Vector3(15,12,18),-17);
            AddLeafCluster(canopy.transform,leaves[1],new Vector3(-65.3f,33.2f,6.0f),new Vector3(18,11,19),17);

            if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new InvalidOperationException("Save failed");
            return $"Removed {groundRemoved} ground deck triangles; rebuilt canopy from seven oak clusters";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
