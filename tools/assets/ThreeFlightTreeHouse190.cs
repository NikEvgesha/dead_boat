using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ThreeFlightTreeHouse190
{
    const string PrefabPath = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    const string MeshPath = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_ThreeFlights190.asset";
    const float Cut = 6.30f;
    const float Drop = 6.171f;

    struct Vertex
    {
        public Vector3 position, normal;
        public Vector2 uv;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex
        {
            position = Vector3.Lerp(a.position,b.position,t),
            normal = Vector3.Lerp(a.normal,b.normal,t).normalized,
            uv = Vector2.Lerp(a.uv,b.uv,t)
        };
    }

    static void Record(UnityEngine.Object obj) => PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
    static void Lower(Transform part)
    {
        part.position -= Vector3.up * Drop;
        Record(part);
    }

    static Mesh ShortMesh(MeshFilter filter)
    {
        var source = filter.sharedMesh;
        var original = source.vertices;
        var normals = source.normals;
        var uv = source.uv;
        var world = original.Select(filter.transform.TransformPoint).ToArray();
        var result = new List<Vector3>();
        var resultNormals = new List<Vector3>();
        var resultUV = new List<Vector2>();
        var submeshes = new List<int[]>();

        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            var input = source.GetTriangles(sub);
            var indices = new List<int>();
            // This material contains the low-poly trunk and roots. Retain the root flare.
            bool trunk = filter.GetComponent<Renderer>().sharedMaterials[sub].name == "Wedge5Mtl";
            var polygon = new List<Vertex>(4);
            for (int i = 0; i < input.Length; i += 3)
            {
                polygon.Clear();
                for (int j = 0; j < 3; j++)
                {
                    int aIndex = input[i+j];
                    int bIndex = input[i+(j+1)%3];
                    var a = new Vertex { position=world[aIndex], normal=normals[aIndex], uv=uv[aIndex] };
                    var b = new Vertex { position=world[bIndex], normal=normals[bIndex], uv=uv[bIndex] };
                    if (trunk || a.position.y >= Cut) polygon.Add(a);
                    if (!trunk && (a.position.y >= Cut) != (b.position.y >= Cut))
                        polygon.Add(Vertex.Lerp(a,b,(Cut-a.position.y)/(b.position.y-a.position.y)));
                }
                if (polygon.Count < 3) continue;
                int first = result.Count;
                foreach (var vertex in polygon)
                {
                    var p = vertex.position;
                    p.y = trunk && p.y < 8.371f ? .2f + (p.y-.2f)*(2f/8.171f) : p.y-Drop;
                    result.Add(filter.transform.InverseTransformPoint(p));
                    resultNormals.Add(vertex.normal);
                    resultUV.Add(vertex.uv);
                }
                for(int j = 1; j < polygon.Count-1; j++)
                {
                    indices.Add(first);
                    indices.Add(first+j);
                    indices.Add(first+j+1);
                }
            }
            submeshes.Add(indices.ToArray());
        }
        var mesh = new Mesh { name="TreeHouse_ThreeFlights", indexFormat=IndexFormat.UInt32 };
        mesh.SetVertices(result);
        mesh.SetNormals(resultNormals);
        mesh.SetUVs(0,resultUV);
        mesh.subMeshCount=submeshes.Count;
        for(int sub=0;sub<submeshes.Count;sub++) mesh.SetTriangles(submeshes[sub],sub,false);
        mesh.RecalculateBounds();
        return mesh;
    }

    public static string Main()
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath)) throw new InvalidOperationException("Three-flight mesh already exists.");
        var house=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var filter=house.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="dom");
            var mesh=ShortMesh(filter);
            AssetDatabase.CreateAsset(mesh,MeshPath);
            filter.sharedMesh=mesh;
            Record(filter);
            var collision=filter.GetComponent<MeshCollider>();
            collision.sharedMesh=mesh;
            Record(collision);

            var details=house.transform.Find("185_Stair_And_Landing_Details");
            var parts=details.Cast<Transform>().ToArray();
            // Each original flight consists of one walk surface, one rail, and three posts.
            foreach(var part in parts.Take(10))
            {
                part.gameObject.SetActive(false);
                Record(part.gameObject);
            }
            foreach(var part in parts.Skip(10)) Lower(part);

            var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(panel,house.scene);
            panel.name="Lower_Room_Closed_Side_Opening";
            panel.transform.SetParent(details,true);
            panel.transform.position=new Vector3(-61.42f,13.90f-Drop,17.82f);
            panel.transform.localScale=new Vector3(.28f,4.20f,4.17f);
            panel.GetComponent<Renderer>().sharedMaterial=filter.GetComponent<Renderer>().sharedMaterials[1];

            var spawner=house.GetComponent<LocationContentSpawner>();
            foreach(var point in spawner.itemSpawnPoints.Select(p=>p.spawnTransform).Concat(spawner.enemySpawnPoints))
            {
                Lower(point);
                if(point.position.y<.28f)
                {
                    var p=point.position;
                    p.y=.28f;
                    point.position=p;
                    Record(point);
                }
            }

            var anchors=house.transform.Find("Workshop_Walk_Anchors");
            anchors.Find("Start").position=new Vector3(-56.716f,.28f,17.42f);
            anchors.Find("Coin").position=new Vector3(-55.48f,.20f,16.68f);
            anchors.Find("Goblet").position=new Vector3(-55.55f,2.873f,10.30f);
            Lower(anchors.Find("Lantern"));
            if(!PrefabUtility.SaveAsPrefabAsset(house,PrefabPath)) throw new InvalidOperationException("Save failed.");
            return "Three active flights, upper landing lowered by 6.17 m, side opening closed";
        }
        finally { PrefabUtility.UnloadPrefabContents(house); }
    }
}
