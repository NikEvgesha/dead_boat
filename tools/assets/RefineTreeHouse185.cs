using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RefineTreeHouse185
{
    const string Source = "Assets/Prefabs/Locations/Enemy/TreeHouse_1.prefab";
    const string Variant = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    const string Workshop = "Assets/Scenes/TreeHouseVariantsWorkshop.unity";

    // Measured endpoints on the five original flights, in the source prefab's world placement.
    static readonly (Vector3 start, Vector3 end, float railX)[] Flights = {
        (new Vector3(-56.645f,.20f,19.03f),new Vector3(-56.645f,5.50f,12.23f),-58.45f),
        (new Vector3(-52.60f,5.50f,12.23f),new Vector3(-52.60f,9.55f,16.99f),-50.80f),
        (new Vector3(-56.645f,9.55f,16.99f),new Vector3(-56.645f,13.60f,12.23f),-58.45f),
        (new Vector3(-52.60f,13.60f,12.23f),new Vector3(-52.60f,17.70f,16.99f),-50.80f),
        (new Vector3(-58.67f,17.70f,16.99f),new Vector3(-58.67f,21.50f,12.84f),-60.48f)
    };

    static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(block, parent.gameObject.scene);
        block.name = name;
        block.transform.SetParent(parent, true);
        block.transform.position = position;
        block.transform.localScale = size;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    static void AddDetail(Transform parent, string assetPath, string name, Vector3 position, float height, float yaw)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (!asset) throw new FileNotFoundException(assetPath);
        var detail = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        detail.name = name;
        detail.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        var renderers = detail.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        detail.transform.localScale *= height / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        detail.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = SceneManager.GetSceneByPath(Workshop);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(Workshop, OpenSceneMode.Additive);
        var original = scene.GetRootGameObjects().Single(o => o.name == "00_Original_TreeHouse");
        if (scene.GetRootGameObjects().Any(o => o.name == "TreeHouse_Original_Refined") || File.Exists(Variant))
            throw new InvalidOperationException("Refined treehouse already exists.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/Roof_InteriorBrown.mat");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var house = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
            var refinements = new GameObject("185_Stair_And_Landing_Details");
            SceneManager.MoveGameObjectToScene(refinements, preview);
            refinements.transform.SetParent(house.transform, true);
            foreach (var flight in Flights)
            {
                var start = flight.start;
                var end = flight.end;
                var delta = end - start;
                var walkStart = start + Vector3.up * .015f;
                var walkEnd = end + Vector3.up * .015f;
                if (start.y > 1f)
                {
                    walkStart.z -= Mathf.Sign(delta.z) * .68f;
                    walkEnd.y += .035f;
                }
                var walkDirection = walkEnd - walkStart;
                var ramp = Block(refinements.transform, "Stair_Walk_Surface", (walkStart + walkEnd) / 2,
                    new Vector3(3.30f,.12f,walkDirection.magnitude+.10f), wood);
                ramp.transform.rotation = Quaternion.LookRotation(walkDirection);
                ramp.GetComponent<Renderer>().enabled = false;

                start.x = flight.railX;
                end.x = flight.railX;
                var rail = Block(refinements.transform, "Timber_Handrail", (start + end) / 2 + Vector3.up * 1.05f,
                    new Vector3(.16f,.14f,delta.magnitude), wood);
                rail.transform.rotation = Quaternion.LookRotation(delta);
                for (int i = 0; i < 3; i++)
                    Block(refinements.transform, "Handrail_Post", Vector3.Lerp(start,end,i/2f) + Vector3.up * .51f,
                        new Vector3(.15f,1.12f,.15f), wood);
            }

            AddDetail(refinements.transform, "Assets/Import/LowpolyDungeonAssets/Assets/Prefabs/Props/Props_Crate.prefab",
                "Landing_Supply_Crate", new Vector3(-51.1f,9.55f,20.85f), .66f, 14);
            AddDetail(refinements.transform, "Assets/Import/LowPolyMedievalPropsLite/Prefabs/Bucket_01.prefab",
                "Balcony_Bucket", new Vector3(-43.1f,17.45f,23.35f), .48f, -18);
            var spawner = house.GetComponent<LocationContentSpawner>();
            if (spawner.itemSpawnPoints.Count != 5 || spawner.enemySpawnPoints.Count != 5)
                throw new InvalidOperationException("Original spawn points missing.");
            if (!PrefabUtility.SaveAsPrefabAsset(house, Variant)) throw new IOException("Prefab save failed.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }

        SceneManager.SetActiveScene(scene);
        var refined = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Variant), scene);
        refined.name = "TreeHouse_Original_Refined";
        refined.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        refined.transform.localScale = original.transform.localScale;
        foreach (var behaviour in refined.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!behaviour) continue;
            behaviour.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
        }
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Workshop save failed.");

        var visibility = SceneVisibilityManager.instance;
        visibility.ExitIsolation();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root == refined || root.name == "Workshop_Ground" || root.name == "Workshop_Sun")
                visibility.Show(root, true);
            else visibility.Hide(root, true);
        }
        const ulong mask = 1UL << 57;
        EditorSceneManager.SetSceneCullingMask(scene, mask);
        var view = EditorWindow.GetWindow<SceneView>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(SceneView).GetProperty("customScene",flags).SetValue(view,scene);
        typeof(SceneView).GetProperty("overrideSceneCullingMask",flags).SetValue(view,mask);
        view.camera.overrideSceneCullingMask = mask;
        Tools.visibleLayers = -1;
        Selection.activeGameObject = refined;
        view.in2DMode = false;
        var center = new Vector3(-58,14.5f,7) + original.transform.position - source.transform.position;
        view.LookAt(center, Quaternion.LookRotation(new Vector3(-1,-.24f,1)), 21f, true, true);
        view.Focus();
        view.Repaint();
        return "Saved original-derived treehouse, five stair flights refined, 5 item and 5 enemy points retained";
    }
}
