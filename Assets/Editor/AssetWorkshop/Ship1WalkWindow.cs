using UnityEditor;
using UnityEngine;

public sealed class Ship1WalkWindow : TreeHouseWalkWindow
{
    protected override string LocationPath => "Assets/Prefabs/Workshop/Ship1/Ship1_Overgrown.prefab";
    protected override bool IncludeGround => false;
    protected override float SunIntensity => .48f;

    [MenuItem("Tools/Asset Workshop/Walk Ship 1")]
    public new static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = CreateInstance<Ship1WalkWindow>();
        window.titleContent = new GUIContent("Прогулка — Ship 1");
        window.minSize = new Vector2(700, 440);
        window.Setup();
        window.ShowUtility();
        window.Focus();
    }

    protected override void SetupRoute(GameObject location)
    {
        var route = location.transform.Find("Workshop_Walk_Anchors");
        var start = route.Find("Start");
        AddItem("coin", "Монета", route.Find("Coin").position);
        AddItem("goblet", "Кубок", route.Find("Goblet").position);
        AddItem("Lantern", "Фонарь", route.Find("Lantern").position);
        SetStart(start.position, start.eulerAngles.y);
    }
}
