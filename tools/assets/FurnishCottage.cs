using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FurnishCottage
{
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    static GameObject Place(string path, string name, Transform parent, Vector3 position, float height, float yaw = 0, bool solid = true)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null) throw new Exception("Missing asset: " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent.gameObject.scene);
        go.name = name; go.transform.SetParent(parent, false); go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var bounds = BoundsOf(go);
        go.transform.localScale *= height / bounds.size.y;
        bounds = BoundsOf(go);
        go.transform.position += new Vector3(position.x - bounds.center.x, position.y - bounds.min.y, position.z - bounds.center.z);
        if (solid)
        {
            var collider = go.AddComponent<BoxCollider>();
            bounds = BoundsOf(go);
            collider.center = go.transform.InverseTransformPoint(bounds.center);
            // All furniture uses yaw in multiples of 90 degrees; convert world AABB corners to local size.
            var size = go.transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
        return go;
    }
    public static void Main()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/AssetWorkshop.unity" || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Open workshop in Edit Mode.");
        var house = scene.GetRootGameObjects().First(x => x.name == "01_Assembly_Area").transform.Find("WoodenCottage_01");
        if (house.Find("08_Interior_Props") != null) throw new Exception("Furniture already exists; preserve manual changes.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Обставить дом");
        var props = new GameObject("08_Interior_Props"); props.transform.SetParent(house, false); Undo.RegisterCreatedObjectUndo(props, "Добавить обстановку");
        try
        {
            const string medieval = "Assets/Import/LowPolyMedievalPropsLite/Models/";
            const string dungeon = "Assets/Import/CatBorg Studio/3D Dungeon Lowpoly Pack/Models/";
            var table = Place(dungeon + "01_DiningHall_01_WoodenTable#1.fbx", "Dining_Table", props.transform, new Vector3(-1.65f, .48f, .35f), .78f, 90);
            Place("Assets/Models/Item/stool.prefab", "Stool_Front", props.transform, new Vector3(-1.65f, .48f, -1.35f), .45f);
            Place("Assets/Models/Item/stool.prefab", "Stool_Back", props.transform, new Vector3(-1.65f, .48f, 2.05f), .45f);
            Place(dungeon + "01_Library_01_BookShelve_1.fbx", "Bookshelf", props.transform, new Vector3(1.8f, .48f, 2.4f), 1.65f, 180);
            Place(medieval + "Barrel_01.fbx", "Storage_Barrel", props.transform, new Vector3(2.2f, .48f, -1.85f), .9f);
            Place(medieval + "Box_01.fbx", "Storage_Crate", props.transform, new Vector3(2.1f, .48f, -.65f), .65f, 15);
            // Use exact geometry for the crate's rotated collider rather than the yaw-only box approximation.
            var crate = props.transform.Find("Storage_Crate");
            var crateCollider = crate.GetComponent<BoxCollider>();
            var meshBounds = crate.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            crateCollider.center = meshBounds.center; crateCollider.size = meshBounds.size;
            float top = BoundsOf(table).max.y + .005f;
            Place(medieval + "Cup_01.fbx", "Table_Cup", props.transform, new Vector3(-1.4f, top, -.35f), .14f, 0, false);
            Place(medieval + "Bottle_01.fbx", "Table_Bottle", props.transform, new Vector3(-1.9f, top, .35f), .28f, 0, false);
            Place(medieval + "Candle_01.fbx", "Table_Candle", props.transform, new Vector3(-1.65f, top, 1.1f), .22f, 0, false);
            Physics.SyncTransforms();
            for (float z = -4.5f; z <= 2.25f; z += .1f)
                if (Physics.CheckCapsule(new Vector3(0, .8f, z), new Vector3(0, 2, z), .3f, ~0, QueryTriggerInteraction.Ignore)) throw new Exception("Central passage blocked: " + z);
            var materials = props.GetComponentsInChildren<Renderer>().SelectMany(x => x.sharedMaterials).ToArray();
            if (materials.Any(x => x == null || x.shader == null || !x.shader.isSupported)) throw new Exception("Missing or unsupported furniture material");
            PrefabUtility.ApplyPrefabInstance(house.gameObject, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = props;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(-.3f, 1.4f, 1), Quaternion.LookRotation(new Vector3(-1.2f, -.25f, 3.5f)), 2, false, true);
            File.WriteAllText("Temp/cottage-furniture-result.txt", "PASS: 9 props; 6 furniture colliders; central capsule passage clear; all materials supported; scene/prefab saved");
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }
}
