using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FillUpperTreeHouse194
{
    const string Path = "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";

    public static string Main()
    {
        var house = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var lighting = house.transform.Find("Interior_Lighting");
            if (!lighting) throw new InvalidOperationException("Interior lighting group missing");
            const string label = "Upper_Room_Window_Wall_Fill";
            var fixture = lighting.Find(label);
            if (!fixture)
            {
                var source = new GameObject(label);
                SceneManager.MoveGameObjectToScene(source, house.scene);
                fixture = source.transform;
                fixture.SetParent(lighting, true);
                source.AddComponent<Light>();
            }
            fixture.position = new Vector3(-61.6f, 11.05f, 1.28f);
            var fill = fixture.GetComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, .92f, .81f);
            fill.range = 6.2f;
            fill.intensity = 3.3f;
            fill.shadows = LightShadows.None;
            if (!PrefabUtility.SaveAsPrefabAsset(house, Path)) throw new InvalidOperationException("Could not save lighting");
            return "Added local fill to the upper room window wall and ceiling";
        }
        finally { PrefabUtility.UnloadPrefabContents(house); }
    }
}
