using UnityEditor;
using UnityEngine;

public sealed class Clocks1WalkWindow : TreeHouseWalkWindow
{
    static readonly (string Prefab, string Label)[] Loot =
    {
        ("coin", "Монета"),
        ("silverCup", "Серебряная чаша"),
        ("Lantern", "Фонарь"),
        ("goblet", "Кубок"),
        ("goldCup", "Золотая чаша")
    };

    protected override string LocationPath => "Assets/Prefabs/Workshop/Clocks1/Clocks1_OvergrownCourt.prefab";
    protected override bool IncludeGround => false;
    protected override float SunIntensity => .9f;
    protected override LightShadows WalkSunShadows => LightShadows.Soft;

    [MenuItem("Tools/Asset Workshop/Walk Clocks 1")]
    public new static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = CreateInstance<Clocks1WalkWindow>();
        window.titleContent = new GUIContent("Прогулка — Clocks 1");
        window.minSize = new Vector2(700, 440);
        window.Setup();
        window.ShowUtility();
        window.Focus();
    }

    protected override void SetupRoute(GameObject location)
    {
        var anchors = location.transform.Find("Workshop_Walk_Anchors");
        for (int i = 0; i < Loot.Length; i++)
            AddItem(Loot[i].Prefab, Loot[i].Label, anchors.Find($"Item{i + 1:00}").position);
        var start = anchors.Find("Start");
        SetStart(start.position, start.eulerAngles.y);
    }
}

