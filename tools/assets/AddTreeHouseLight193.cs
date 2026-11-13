using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddTreeHouseLight193
{
    const string Path = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";

    static void AddLamp(Transform parent, string name, Vector3 position, float range, float intensity)
    {
        var lamp = new GameObject(name);
        SceneManager.MoveGameObjectToScene(lamp, parent.gameObject.scene);
        lamp.transform.SetParent(parent, true);
        lamp.transform.position = position;
        var light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .86f, .70f);
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = .65f;
        light.shadowBias = .08f;
        light.shadowNormalBias = .18f;
    }

    public static string Main()
    {
        var house = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            if (house.transform.Find("Interior_Lighting")) throw new InvalidOperationException("Lighting already added");
            var lighting = new GameObject("Interior_Lighting");
            SceneManager.MoveGameObjectToScene(lighting, house.scene);
            lighting.transform.SetParent(house.transform, false);
            AddLamp(lighting.transform, "Lower_Room_Warm_Fill", new Vector3(-55.6f, 7.8f, 18.6f), 7.0f, 2.6f);
            AddLamp(lighting.transform, "Upper_Room_Warm_Fill", new Vector3(-60.0f, 11.6f, 4.3f), 8.2f, 2.9f);
            if (!PrefabUtility.SaveAsPrefabAsset(house, Path)) throw new InvalidOperationException("Could not save lighting");
            return "Saved two warm interior lights";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(house);
        }
    }
}
