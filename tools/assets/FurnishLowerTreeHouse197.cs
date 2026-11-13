using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FurnishLowerTreeHouse197
{
    const string House = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    const string Props = "Assets/Import/LowpolyDungeonAssets/Assets/Prefabs/Props/";
    const float LowerFloor = 5.65f;

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
        model.transform.SetPositionAndRotation(new Vector3(x, LowerFloor, z), Quaternion.Euler(0, yaw, 0));
        var bounds = RendererBounds(model);
        model.transform.localScale *= height / bounds.size.y;
        bounds = RendererBounds(model);
        model.transform.position += new Vector3(x - bounds.center.x, LowerFloor - bounds.min.y, z - bounds.center.z);
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
        var root=PrefabUtility.LoadPrefabContents(House);
        try
        {
            if(root.transform.Find("Lower_Room_Furniture")) throw new InvalidOperationException("Lower room already furnished");
            var furniture=new GameObject("Lower_Room_Furniture");
            SceneManager.MoveGameObjectToScene(furniture,root.scene);
            furniture.transform.SetParent(root.transform,false);
            var bench=Place(furniture.transform,"Assets/Import/3D Cartoon Village/Prefabs/Bench.prefab",
                "Lower_Room_Bench",-54.50f,20.68f,.71f,0f);
            var crate=Place(furniture.transform,"Props_Crate","Lower_Room_Pickup_Crate",-57.05f,20.02f,.70f,12f);
            var surface=new Vector3(crate.center.x,crate.max.y+.025f,crate.center.z-.08f);
            root.transform.Find("Workshop_Walk_Anchors/Goblet").position=surface;
            var item=root.GetComponent<LocationContentSpawner>().itemSpawnPoints.Select(p=>p.spawnTransform)
                .Where(p=>p.position.y>4.8f && p.position.y<8.5f)
                .OrderBy(p=>(p.position-surface).sqrMagnitude).First();
            item.position=surface+Vector3.up*.04f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            if(!PrefabUtility.SaveAsPrefabAsset(root,House)) throw new InvalidOperationException("Could not save lower furniture");
            return "Bench "+bench+"; crate "+crate+"; goblet on crate";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
