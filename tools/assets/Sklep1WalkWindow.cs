using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class Sklep1WalkWindow : TreeHouseWalkWindow
{
    GameObject location;
    bool gatesClosed;
    protected override string LocationPath=>"Assets/Prefabs/Workshop/Sklep1/Sklep1_OvergrownRuins.prefab";
    protected override bool IncludeGround=>false;
    protected override float SunIntensity=>.95f;
    protected override LightShadows WalkSunShadows=>LightShadows.Soft;
    [MenuItem("Tools/Asset Workshop/Walk Sklep 1")]
    public new static void Open()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var w=CreateInstance<Sklep1WalkWindow>();w.titleContent=new GUIContent("Прогулка — Sklep 1");
        w.minSize=new Vector2(900,560);w.Setup();w.ShowUtility();w.Focus();
    }
    protected override void SetupRoute(GameObject root)
    {
        location=root;
        var fill=new GameObject("Soft_Outdoor_Fill");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fill,root.scene);
        fill.transform.rotation=Quaternion.Euler(48,148,0);var fillLight=fill.AddComponent<Light>();
        fillLight.type=LightType.Directional;fillLight.intensity=.25f;fillLight.shadows=LightShadows.None;
        // Original gameplay loot is retained in the saved prefab; use five collectible preview models here.
        foreach(var item in root.GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b&&b.GetType().Name=="PickableItem"))item.gameObject.SetActive(false);
        foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
        var anchors=root.transform.Find("Workshop_Walk_Anchors");
        var loot=new[]{("coin","Монета"),("silverCup","Серебряная чаша"),("goblet","Кубок"),("goldCup","Золотая чаша"),("Lantern","Фонарь")};
        for(int i=0;i<loot.Length;i++)AddItem(loot[i].Item1,loot[i].Item2,anchors.Find("Item"+(i+1).ToString("00")).position);
        SetStart(anchors.Find("Start").position,90);
    }
    protected override void DrawRouteButtons()
    {
        if(GUILayout.Button("Показать решётки",EditorStyles.toolbarButton))
        {
            location.transform.Find("Trap/zoneTrap").gameObject.SetActive(true);
            location.transform.Find("Trap/zoneTrap/WallActive").gameObject.SetActive(true);gatesClosed=true;
        }
        var player=location.scene.GetRootGameObjects().SingleOrDefault(g=>g.name=="Walk controller");
        var lever=location.transform.Find("Lever");
        using(new EditorGUI.DisabledScope(!player||Vector3.Distance(player.transform.position+Vector3.up,lever.position)>3.4f))
        {
            if(GUILayout.Button("Рычаг: открыть",EditorStyles.toolbarButton))
            {
                // Sample the source clips; this editor walk previews gates without running enemy AI.
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Action/CustleOpen.anim");
                clip.SampleAnimation(location.transform.Find("Trap").gameObject,0);
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Action/Activate.anim").SampleAnimation(lever.gameObject,1);
                gatesClosed=false;
            }
        }
        GUILayout.Label(gatesClosed?"Решётки закрыты · предпросмотр":"Прогулка без врагов");
    }
}
