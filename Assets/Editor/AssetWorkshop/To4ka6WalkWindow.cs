using UnityEditor;
using UnityEngine;

public sealed class To4ka6WalkWindow : TreeHouseWalkWindow
{
    static readonly (string Prefab,string Label)[] Loot={
        ("coin","Монета"),("silverCup","Серебряная чаша"),("goblet","Кубок"),
        ("goldCup","Золотая чаша"),("Lantern","Фонарь")};
    protected override string LocationPath=>"Assets/Prefabs/Workshop/To4ka6/To4ka6_OvergrownCourt.prefab";
    protected override bool IncludeGround=>false;
    protected override float SunIntensity=>.9f;
    protected override LightShadows WalkSunShadows=>LightShadows.Soft;
    [MenuItem("Tools/Asset Workshop/Walk To4ka6")]
    public new static void Open()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var window=CreateInstance<To4ka6WalkWindow>();window.titleContent=new GUIContent("Прогулка — To4ka6 1");
        window.minSize=new Vector2(700,440);window.Setup();window.ShowUtility();window.Focus();
    }
    protected override void SetupRoute(GameObject location)
    {
        var anchors=location.transform.Find("Workshop_Walk_Anchors");
        for(int i=0;i<Loot.Length;i++)AddItem(Loot[i].Prefab,Loot[i].Label,anchors.Find($"Item{i+1:00}").position);
        SetStart(anchors.Find("Start").position,0);
    }
}
