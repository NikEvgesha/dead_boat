using UnityEditor;
using UnityEngine;

public sealed class Ruins1WalkWindow : TreeHouseWalkWindow
{
    protected override string LocationPath => "Assets/Prefabs/Workshop/Ruins1/Ruins1_Refined.prefab";
    protected override Vector3 GroundPosition => new(-102, -.5f, 7);
    protected override Vector3 GroundSize => new(123, 1, 110);
    protected override float FallLimit => underground ? -51 : -3;

    bool underground;

    [MenuItem("Tools/Asset Workshop/Walk Ruins 1")]
    public new static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = CreateInstance<Ruins1WalkWindow>();
        window.titleContent = new GUIContent("Прогулка — Ruins 1");
        window.minSize = new Vector2(700, 440);
        window.Setup();
        window.ShowUtility();
        window.Focus();
    }

    protected override void SetupRoute(GameObject location)
    {
        AddItem("coin", "Монета", new Vector3(-110.5f, .03f, -2.7f));
        AddItem("goblet", "Кубок", new Vector3(-85.53f, 13.91f, 25.3f));
        AddItem("Lantern", "Фонарь", new Vector3(-113.8f, -45.48f, -29.2f));
        GoToCourtyard();
    }

    void GoToCourtyard()
    {
        underground = false;
        SetStart(new Vector3(-92.5f, .14f, -11f), 307);
    }

    protected override void DrawRouteButtons()
    {
        if (GUILayout.Button("Двор", EditorStyles.toolbarButton)) GoToCourtyard();
        if (GUILayout.Button("Подземелье", EditorStyles.toolbarButton))
        {
            underground = true;
            SetStart(new Vector3(-114f, -45.48f, -3.1f), 180);
        }
    }
}
