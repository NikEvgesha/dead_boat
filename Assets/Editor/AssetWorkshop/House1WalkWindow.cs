using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class House1WalkWindow : EditorWindow
{
    static readonly string[] VariantPaths = {
        "Assets/Prefabs/Locations/Enemy/house1_1.prefab",
        "Assets/Prefabs/Workshop/Locations/House1_2_LocationVariant.prefab",
        "Assets/Prefabs/Workshop/Locations/House1_3_LocationVariant.prefab"
    };
    [SerializeField] int variant;
    Scene scene;
    Transform entrance;
    CharacterController player;
    Camera cameraView;
    RenderTexture frame;
    readonly HashSet<KeyCode> keys = new HashSet<KeyCode>();
    float yaw, pitch, vertical;
    bool looking, jump;
    double last;

    [MenuItem("Tools/Asset Workshop/Walk House 1-1")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = GetWindow<House1WalkWindow>("РџСЂРѕРіСѓР»РєР° вЂ” РґРѕРјР° 1-1 / 1-2 / 1-3");
        window.minSize = new Vector2(660, 420);
        window.Show();
        window.Focus();
        if (!window.player) window.Setup();
    }

    void OnEnable() { last=EditorApplication.timeSinceStartup; EditorApplication.update+=Tick; }
    void OnLostFocus() { keys.Clear(); looking=false; jump=false; }
    void OnDisable()
    {
        EditorApplication.update-=Tick;
        ClearSession();
    }

    void ClearSession()
    {
        keys.Clear(); looking=false; jump=false;
        if (cameraView) cameraView.targetTexture=null;
        if (frame) { frame.Release(); DestroyImmediate(frame); }
        if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        scene=default; player=null; cameraView=null; entrance=null; frame=null;
    }

    GameObject Make(string name)
    {
        var go=EditorUtility.CreateGameObjectWithHideFlags(name,HideFlags.HideAndDontSave);
        SceneManager.MoveGameObjectToScene(go,scene);
        return go;
    }

    void Setup()
    {
        ClearSession();
        scene=EditorSceneManager.NewPreviewScene();
        try {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(VariantPaths[Mathf.Clamp(variant,0,2)]);
            var location=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
            entrance=location.GetComponentsInChildren<Transform>().Single(t=>t.name=="Stone_House_Open_Entrance");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor,scene);
            floor.transform.position=new Vector3(entrance.position.x,-.3f,entrance.position.z);
            floor.transform.localScale=new Vector3(54,1,46);
            floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/Locations/House1Workshop_Ground.mat");
            var sun=Make("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.25f;sun.transform.rotation=Quaternion.Euler(48,-35,0);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
            player=Make("Walk controller").AddComponent<CharacterController>();
            player.height=source.height;player.radius=source.radius;player.center=source.center;player.stepOffset=source.stepOffset;player.skinWidth=source.skinWidth;player.slopeLimit=source.slopeLimit;
            cameraView=Make("Walk camera").AddComponent<Camera>();cameraView.transform.SetParent(player.transform,false);
            cameraView.transform.localPosition=new Vector3(0,1.6f,0);cameraView.enabled=false;cameraView.nearClipPlane=.06f;cameraView.farClipPlane=90;cameraView.fieldOfView=68;
            cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.46f,.62f,.73f);cameraView.scene=scene;
            ResetSpawn();
        } catch { EditorSceneManager.ClosePreviewScene(scene);scene=default;throw; }
    }

    void ResetSpawn()
    {
        player.enabled=false;
        player.transform.SetPositionAndRotation(entrance.TransformPoint(new Vector3(0,.55f,-8)),Quaternion.Euler(0,entrance.eulerAngles.y,0));
        player.enabled=true;yaw=entrance.eulerAngles.y;pitch=vertical=0;cameraView.transform.localRotation=Quaternion.identity;keys.Clear();
        Physics.SyncTransforms(); Repaint();
    }

    void Tick()
    {
        double now=EditorApplication.timeSinceStartup;
        float dt=Mathf.Clamp((float)(now-last),0,.04f);last=now;
        if (!player) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Close();return; }
        if(focusedWindow!=this) return;
        if(player.isGrounded)vertical=jump?5.2f:-1;
        else vertical-=19*dt;
        jump=false;
        var direction=new Vector3((keys.Contains(KeyCode.D)?1:0)-(keys.Contains(KeyCode.A)?1:0),0,(keys.Contains(KeyCode.W)?1:0)-(keys.Contains(KeyCode.S)?1:0)).normalized;
        float speed=keys.Contains(KeyCode.LeftShift)||keys.Contains(KeyCode.RightShift)?7:4;
        player.Move((player.transform.TransformDirection(direction)*speed+Vector3.up*vertical)*dt);
        if(player.transform.position.y < entrance.position.y-5)ResetSpawn();
        Repaint();
    }

    void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)){
            for(int i=0;i<3;i++)
                if(GUILayout.Toggle(variant==i,"Р”РѕРј 1-"+(i+1),EditorStyles.toolbarButton) && (variant!=i || !player))
                { variant=i; Setup(); }
            if(GUILayout.Button("Рљ РЅР°С‡Р°Р»Сѓ",EditorStyles.toolbarButton)&&player)ResetSpawn();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Р—Р°РєСЂС‹С‚СЊ",EditorStyles.toolbarButton)){Close();GUIUtility.ExitGUI();}
        }
        EditorGUILayout.LabelField("WASD вЂ” РґРІРёР¶РµРЅРёРµ В· Shift вЂ” Р±РµРі В· РџРљРњ + РјС‹С€СЊ вЂ” РѕР±Р·РѕСЂ В· РџСЂРѕР±РµР» вЂ” РїСЂС‹Р¶РѕРє");
        if(!player){if(GUILayout.Button("РќР°С‡Р°С‚СЊ РїСЂРѕРіСѓР»РєСѓ"))Setup();return;}
        var rect=GUILayoutUtility.GetRect(64,10000,64,10000);
        var e=Event.current;
        if(e.type==EventType.KeyDown){
            bool first=keys.Add(e.keyCode);
            if(first&&e.keyCode==KeyCode.Space)jump=true;
            if(e.keyCode==KeyCode.Escape)OnLostFocus();
            e.Use();
        }
        if(e.type==EventType.KeyUp){keys.Remove(e.keyCode);e.Use();}
        if(e.type==EventType.MouseDown&&e.button==1&&rect.Contains(e.mousePosition)){looking=true;Focus();e.Use();}
        if(e.type==EventType.MouseUp&&e.button==1){looking=false;e.Use();}
        if(e.type==EventType.MouseDrag&&looking){yaw+=e.delta.x*.22f;pitch=Mathf.Clamp(pitch+e.delta.y*.22f,-80,80);player.transform.rotation=Quaternion.Euler(0,yaw,0);cameraView.transform.localRotation=Quaternion.Euler(pitch,0,0);e.Use();}
        if(e.type==EventType.Repaint){
            int w=Mathf.Clamp((int)rect.width,64,1600),h=Mathf.Clamp((int)rect.height,64,1000);
            if(!frame||frame.width!=w||frame.height!=h){if(frame){frame.Release();DestroyImmediate(frame);}frame=new RenderTexture(w,h,24){hideFlags=HideFlags.HideAndDontSave};}
            cameraView.targetTexture=frame;
            try{cameraView.Render();}finally{cameraView.targetTexture=null;}
            GUI.DrawTexture(rect,frame,ScaleMode.ScaleToFit);
        }
    }

    public static string Verify()
    {
        Open();
        var w=GetWindow<House1WalkWindow>();
        try {
            for(int variant=0;variant<3;variant++){
                w.variant=variant; w.Setup(); w.ResetSpawn();
                int steps=Mathf.CeilToInt(w.entrance.TransformVector(Vector3.forward*10).magnitude/.045f)+20;
                for(int i=0;i<steps && w.entrance.InverseTransformPoint(w.player.transform.position).z<1.5f;i++)
                    w.player.Move(w.entrance.forward*.045f+Vector3.down*.025f);
                var inside=w.entrance.InverseTransformPoint(w.player.transform.position);
                if(inside.z<1.1f || !w.player.isGrounded)throw new InvalidOperationException("Walk blocked in variant "+(variant+1)+": "+inside);
            }
            return "PASS: entered all three variants; variant switching recreated isolated scenes and controllers";
        } finally {w.variant=0;w.Setup();w.ResetSpawn();}
    }
}
