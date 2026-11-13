using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FurnishTreeHouse195
{
    const string House = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    const string Props = "Assets/Import/LowpolyDungeonAssets/Assets/Prefabs/Props/";
    const float UpperFloor = 9.62f;

    static Bounds RendererBounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("No mesh in " + model.name);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static Bounds Place(Transform parent, string file, string label, float x, float z, float height, float yaw)
    {
        var path = file.StartsWith("Assets/", StringComparison.Ordinal) ? file : Props + file + ".prefab";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!asset) throw new InvalidOperationException("Furniture prefab missing: " + file);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        model.name = label;
        model.transform.SetPositionAndRotation(new Vector3(x, UpperFloor, z), Quaternion.Euler(0, yaw, 0));
        var bounds = RendererBounds(model);
        model.transform.localScale *= height / bounds.size.y;
        bounds = RendererBounds(model);
        model.transform.position += new Vector3(x - bounds.center.x, UpperFloor - bounds.min.y, z - bounds.center.z);
        bounds = RendererBounds(model);
        if (!model.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger))
        {
            var colliders = new GameObject("Furniture_Collider");
            SceneManager.MoveGameObjectToScene(colliders, parent.gameObject.scene);
            colliders.transform.SetParent(parent, true);
            colliders.transform.position = bounds.center;
            colliders.AddComponent<BoxCollider>().size = bounds.size;
        }
        return bounds;
    }

    public static string Main()
    {
        var root = PrefabUtility.LoadPrefabContents(House);
        try
        {
            if (root.transform.Find("Upper_Room_Furniture")) throw new InvalidOperationException("Room already furnished.");
            var furnishing = new GameObject("Upper_Room_Furniture");
            SceneManager.MoveGameObjectToScene(furnishing, root.scene);
            furnishing.transform.SetParent(root.transform, false);

            var table = Place(furnishing.transform, "Props_WoodenTable_A", "Collection_Table", -64.60f, 1.35f, .86f, 90f);
            var chair = Place(furnishing.transform, "Assets/Import/3D Cartoon Village/Prefabs/Chair.prefab", "Table_Chair", -64.64f, 2.95f, .87f, 180f);
            var crateA = Place(furnishing.transform, "Props_Crate", "Small_Storage_Crate", -65.62f, 4.52f, .61f, 8f);
            var crateB = Place(furnishing.transform, "Props_Crate", "Large_Storage_Crate", -65.56f, 5.34f, .72f, -6f);

            var anchor = root.transform.Find("Workshop_Walk_Anchors/Lantern");
            var tabletop = new Vector3(table.center.x + .9f, table.max.y + .025f, table.center.z);
            anchor.position = tabletop;
            var itemPoint = root.GetComponent<LocationContentSpawner>().itemSpawnPoints
                .Select(p => p.spawnTransform)
                .Where(point => point.position.y > 9f)
                .OrderBy(point => (point.position - tabletop).sqrMagnitude)
                .First();
            itemPoint.position = tabletop + Vector3.up * .04f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(itemPoint);
            if (!PrefabUtility.SaveAsPrefabAsset(root, House)) throw new InvalidOperationException("Could not save furniture");
            return "Table " + table + "; chair " + chair + "; two crates " + crateA + " and " + crateB +
                "; preview lantern and existing item point placed on table";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
