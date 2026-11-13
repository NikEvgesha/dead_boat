using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShortenTreeHouse187
{
    const string PrefabPath = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    const string MeshPath = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Compact187.asset";
    const float RoomBase = 17.45f;
    const float CrownBase = 28f;

    public static Vector3 Compact(Vector3 point)
    {
        float y = .2f + (Mathf.Min(point.y, RoomBase) - .2f) * .66f;
        y += Mathf.Max(0, Mathf.Min(point.y, CrownBase) - RoomBase) * .98f;
        y += Mathf.Max(0, point.y - CrownBase) * .68f;
        return new Vector3(-57 + (point.x + 57) * .8f, y, 9 + (point.z - 9) * .8f);
    }

    static void Mark(Transform target)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    static void ResizeDetail(Transform part)
    {
        var old = part.position;
        var size = part.localScale;
        if (part.name == "Stair_Walk_Surface" || part.name == "Timber_Handrail")
        {
            var half = part.forward * size.z * .5f;
            var start = Compact(old - half);
            var end = Compact(old + half);
            part.SetPositionAndRotation((start + end) * .5f, Quaternion.LookRotation(end - start));
            part.localScale = new Vector3(size.x * .8f, size.y * .88f, (end - start).magnitude);
        }
        else
        {
            part.position = Compact(old);
            if (part.name == "Handrail_Post")
            {
                float bottom = Compact(old - Vector3.up * size.y * .5f).y;
                part.position = new Vector3(part.position.x, bottom + size.y * .45f, part.position.z);
                part.localScale = Vector3.Scale(size, new Vector3(.8f,.9f,.8f));
            }
        }
        Mark(part);
    }

    static void Anchor(Transform parent, string label, Vector3 original)
    {
        var anchor = new GameObject(label);
        SceneManager.MoveGameObjectToScene(anchor,parent.gameObject.scene);
        anchor.transform.SetParent(parent,true);
        anchor.transform.position = Compact(original);
    }

    public static string Main()
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath)) throw new InvalidOperationException("Compact mesh already exists");
        var house = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var filter = house.GetComponentsInChildren<MeshFilter>().First(f => f.name == "dom");
            var source = filter.sharedMesh;
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = "TreeHouse_Compact";
            var vertices = source.vertices;
            var normals = source.normals;
            var toWorld = filter.transform.localToWorldMatrix;
            var toLocal = toWorld.inverse;
            for(int i=0;i<vertices.Length;i++)
            {
                var world = toWorld.MultiplyPoint3x4(vertices[i]);
                vertices[i] = toLocal.MultiplyPoint3x4(Compact(world));
                if (normals.Length != vertices.Length) continue;
                var normal = toLocal.transpose.MultiplyVector(normals[i]);
                float yScale = world.y < RoomBase ? .66f : world.y < CrownBase ? .98f : .68f;
                normal = new Vector3(normal.x / .8f, normal.y / yScale, normal.z / .8f);
                normals[i] = toWorld.transpose.MultiplyVector(normal).normalized;
            }
            mesh.vertices = vertices;
            if (normals.Length == vertices.Length) mesh.normals = normals;
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            filter.sharedMesh = mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            var collider = filter.GetComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);

            var details = house.transform.Find("185_Stair_And_Landing_Details");
            foreach (Transform part in details) ResizeDetail(part);
            var spawner = house.GetComponent<LocationContentSpawner>();
            foreach (var point in spawner.itemSpawnPoints.Select(p => p.spawnTransform).Concat(spawner.enemySpawnPoints))
            {
                point.position = Compact(point.position);
                Mark(point);
            }

            var infill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(infill,house.scene);
            infill.name = "Upper_Room_Closed_Far_Door";
            infill.transform.SetParent(details,true);
            var lower = Compact(new Vector3(-63.89f,21.73f,-3.755f));
            var upper = Compact(new Vector3(-60.79f,24.03f,-3.46f));
            infill.transform.position = (lower+upper)*.5f;
            infill.transform.localScale = upper-lower;
            infill.GetComponent<Renderer>().sharedMaterial = filter.GetComponent<Renderer>().sharedMaterials[0];

            var anchors = new GameObject("Workshop_Walk_Anchors");
            SceneManager.MoveGameObjectToScene(anchors,house.scene);
            anchors.transform.SetParent(house.transform,true);
            Anchor(anchors.transform,"Start",new Vector3(-56.645f,.28f,22.6f));
            Anchor(anchors.transform,"Coin",new Vector3(-55.7f,5.50f,10.65f));
            Anchor(anchors.transform,"Goblet",new Vector3(-54.0f,9.55f,18.85f));
            Anchor(anchors.transform,"Lantern",new Vector3(-58.67f,21.50f,11.75f));
            if (!PrefabUtility.SaveAsPrefabAsset(house,PrefabPath)) throw new InvalidOperationException("Could not save compact variant");
            return $"Upper terrace at {Compact(new Vector3(0,21.5f,0)).y:F2}; closed rear doorway";
        }
        finally { PrefabUtility.UnloadPrefabContents(house); }
    }
}
