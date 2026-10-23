// Run via Unity MCP script-execute; kept outside Assets to avoid gameplay compilation dependencies.
using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildFirstCottage
{
    const string Town = "Assets/Import/kenney_fantasy-town-kit/Models/FBX format/";
    const string PrefabPath = "Assets/Prefabs/Workshop/WoodenCottage_01.prefab";
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new Exception("No geometry: " + go.name);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
    }
    static GameObject Place(string name, Transform parent, Vector3 center, Vector3 scale, float yaw = 0)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Town + name + ".fbx");
        if (source == null) throw new Exception("Missing model: " + name);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent.gameObject.scene);
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var bounds = BoundsOf(go);
        go.transform.position += new Vector3(center.x - bounds.center.x, center.y - bounds.min.y, center.z - bounds.center.z);
        return go;
    }
    public static void Main()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/AssetWorkshop.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Open AssetWorkshop in Edit Mode first.");
        var area = scene.GetRootGameObjects().First(x => x.name == "01_Assembly_Area").transform;
        if (area.Find("WoodenCottage_01") != null || File.Exists(PrefabPath))
            throw new Exception("Cottage already exists; preserve it rather than overwrite.");
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Собрать первый деревянный дом");
        var house = new GameObject("WoodenCottage_01");
        house.transform.SetParent(area, false);
        Undo.RegisterCreatedObjectUndo(house, "Создать дом");
        try
        {
            var floor = Group("01_Floor", house.transform);
            var walls = Group("02_Walls", house.transform);
            var frame = Group("03_Frame", house.transform);
            var roof = Group("04_Roof", house.transform);
            var porch = Group("05_Porch", house.transform);
            var details = Group("06_Details", house.transform);
            for (int x = -2; x <= 2; x += 2)
                for (int z = -2; z <= 2; z += 2)
                    Place("planks", floor, new Vector3(x, .36f, z), Vector3.one * 2);
            for (int i = -2; i <= 2; i += 2)
            {
                Place(i == 0 ? "wallWoodDoor" : "wallWoodWindowShutters", walls, new Vector3(i, .48f, -2.9f), Vector3.one * 2, -90);
                Place("wallWood", walls, new Vector3(i, .48f, 2.9f), Vector3.one * 2, 90);
                Place(i == 0 ? "wallWoodWindowShutters" : "wallWood", walls, new Vector3(-2.9f, .48f, i), Vector3.one * 2);
                Place(i == 0 ? "wallWoodWindowShutters" : "wallWood", walls, new Vector3(2.9f, .48f, i), Vector3.one * 2, 180);
            }
            foreach (float x in new[] { -2.9f, 2.9f })
                foreach (float z in new[] { -2.9f, 2.9f })
                    Place("pillarWood", frame, new Vector3(x, .36f, z), new Vector3(2, 2.12f, 2));
            for (int z = -2; z <= 2; z += 2)
                Place(z == 0 ? "roofGable" : "roofGableDetail", roof, new Vector3(0, 2.48f, z), new Vector3(2, 3.2f, 6), z == 2 ? -90 : 90);
            Place("planks", porch, new Vector3(0, .36f, -4), Vector3.one * 2);
            Place("stairsWood", porch, new Vector3(0, 0, -5.75f), new Vector3(1.5f, .48f, 3), 90);
            Place("chimney", details, new Vector3(1.6f, 3.2f, 1.3f), new Vector3(2, 1.4f, 2));
            // Place decorative lantern geometry beside the entrance; no gameplay scripts or lighting added.
            Place("lantern", details, new Vector3(-3.75f, 0, -4.1f), Vector3.one * 1.5f);
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Workshop")) AssetDatabase.CreateFolder("Assets/Prefabs", "Workshop");
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(house, PrefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new Exception("Could not save prefab");
            var camera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>()).First();
            camera.transform.position = new Vector3(10, 8, -13);
            camera.transform.LookAt(new Vector3(0, 1.7f, -.8f));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject = house;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1.5f, -.8f), Quaternion.Euler(25, -35, 0), 7, false, true);
            var bounds = BoundsOf(house);
            var meshes = house.GetComponentsInChildren<MeshFilter>();
            var materials = house.GetComponentsInChildren<Renderer>().SelectMany(x => x.sharedMaterials).ToArray();
            File.WriteAllText("Temp/first-cottage-result.txt", "PASS\nPrefab=" + PrefabPath + "\nMeshParts=" + meshes.Length + "\nBounds=" + bounds.size.ToString("F3") + "\nNullMaterials=" + materials.Count(x => x == null) + "\nUnsupportedShaders=" + materials.Count(x => x != null && (x.shader == null || !x.shader.isSupported)) + "\nSceneDirty=" + scene.isDirty);
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
    }
}
