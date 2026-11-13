using System;
using System.Collections.Generic;
using UnityEngine;
using DeadBoat.Events.NewYear;

/// <summary>Runtime-only authored review; added by the editor launcher, never by normal generation.</summary>
public sealed class EventRouteReview248 : MonoBehaviour
{
    [Serializable] public sealed class Stop
    {
        public GameObject Root;
        public string AssetPath, Title;
        public float Center, Start, End;
    }
    public List<Stop> Stops = new();
    public GameObject SecondCastle;
    public SleighBossController Boss;
    public float SecondCastleZ, BossEnableDistance, BoatStopDistance;
    public bool BossReleased { get; private set; }
    public float WarmupUntil;
    private readonly List<TextMesh> signs = new();

    public void AddSign(Transform parent,string text,Vector3 world)
    {
        var label = new GameObject("Route_Stop_Label"); label.transform.SetParent(parent,false); label.transform.position=world;
        var mesh=label.AddComponent<TextMesh>(); mesh.text=text; mesh.anchor=TextAnchor.MiddleCenter;
        mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
        mesh.alignment=TextAlignment.Center;mesh.fontSize=40;mesh.characterSize=.14f;
        mesh.color=new Color(1,.88f,.45f); signs.Add(mesh);
    }

    public void TickRoute()
    {
        if(!BoardController.Instance||!PlayerMovement.Instance)return;
        float player=PlayerMovement.Instance.zPositionFix;
        foreach(var stop in Stops)
        {
            bool near=player>stop.Start-320&&player<stop.End+400;
            if(stop.Root.activeSelf!=near)stop.Root.SetActive(near);
        }
        bool castleNear=Mathf.Abs(player-SecondCastleZ)<550;
        if(SecondCastle.activeSelf!=castleNear)SecondCastle.SetActive(castleNear);
        if(!BossReleased&&Mathf.Max(player,BoardController.Instance.TotalDistanceTraveled)>=BossEnableDistance)
        {
            BossReleased=true;Boss.gameObject.SetActive(true);
        }
    }
    private void Update()
    {
        if(Time.unscaledTime>=WarmupUntil)TickRoute();
    }
    private void LateUpdate()
    {
        var camera=Camera.main;if(!camera)return;
        foreach(var sign in signs)
            if(sign&&sign.gameObject.activeInHierarchy)
                sign.transform.rotation=Quaternion.LookRotation(sign.transform.position-camera.transform.position);
    }
    private void OnGUI()
    {
        if(!BoardController.Instance||!PlayerMovement.Instance)return;
        float z=PlayerMovement.Instance.zPositionFix;
        int passed=0;foreach(var stop in Stops)if(z>stop.End)passed++;
        float width=Mathf.Min(380,Screen.width*.4f);
        var rect=new Rect(Screen.width-width-16,Screen.height-150,width,130);
        GUI.Box(rect,GUIContent.none);
        var style=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,alignment=TextAnchor.UpperLeft};
        string stage=BossReleased?"Босс за вторым замком — можно вступать в бой":
            z>=SecondCastleZ-40?"Второй замок. Босс ждёт за выходом":$"Остановок позади: {passed}/{Stops.Count}\nДо второго замка: {Mathf.Max(0,SecondCastleZ-z):0} м";
        GUI.Label(new Rect(rect.x+12,rect.y+8,width-24,115),"Ивентовый маршрут\n"+stage+"\nW — газ, S — тормоз, пробел — выйти из-за руля",style);
    }
}
