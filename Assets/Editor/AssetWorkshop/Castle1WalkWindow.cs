using UnityEditor;
using UnityEngine;

public sealed class Castle1WalkWindow : TreeHouseWalkWindow
{
    static readonly (string Prefab, string Label)[] Loot =
    {
        ("coin", "Монета"),
        ("goblet", "Кубок"),
        ("Lantern", "Фонарь"),
        ("silverCup", "Серебряная чаша"),
        ("coin", "Монета"),
        ("skull", "Череп"),
        ("goldCup", "Золотая чаша"),
        ("cup", "Чаша")
    };

    protected override string LocationPath => "Assets/Prefabs/Workshop/Castle1/Castle1_Overgrown.prefab";
    protected override Vector3 GroundPosition => new(-84.63f, -.5f, 10.22f);
    protected override Vector3 GroundSize => new(132, 1, 128);
    protected override string GroundMaterialPath => "Assets/Prefabs/Workshop/Castle1/Courtyard_Meadow.mat";
    protected override float SunIntensity => .68f;

    [MenuItem("Tools/Asset Workshop/Walk Castle 1")]
    public new static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = CreateInstance<Castle1WalkWindow>();
        window.titleContent = new GUIContent("Прогулка — Castle 1");
        window.minSize = new Vector2(700, 440);
        window.Setup();
        window.ShowUtility();
        window.Focus();
    }

    protected override void SetupRoute(GameObject location)
    {
        var anchors = location.transform.Find("Workshop_Walk_Anchors");
        for (int i = 0; i < Loot.Length; i++)
        {
            var anchor = anchors.Find($"Item{i + 1:00}");
            AddItem(Loot[i].Prefab, Loot[i].Label, anchor.position);
        }
        var start = anchors.Find("Start");
        SetStart(start.position, start.eulerAngles.y);
    }
}
