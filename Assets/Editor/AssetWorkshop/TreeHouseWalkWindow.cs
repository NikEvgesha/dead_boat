using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TreeHouseWalkWindow : EditorWindow
{
    protected virtual string LocationPath => "Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
    protected virtual Vector3 GroundPosition => new(-58, -.3f, 12);
    protected virtual Vector3 GroundSize => new(100, 1, 100);
    protected virtual string GroundMaterialPath => "Assets/Prefabs/Workshop/Locations/House1Workshop_Ground.mat";
    protected virtual bool IncludeGround => true;
    protected virtual float SunIntensity => 1.35f;
    protected virtual LightShadows WalkSunShadows => LightShadows.None;
    protected virtual float FallLimit => -3;
    const float PickupReach = 2.2f;
    readonly HashSet<KeyCode> keys = new();
    readonly List<(GameObject model, string label)> items = new();
    Scene preview;
    CharacterController player;
    Camera walkCamera;
    RenderTexture frame;
    Vector3 walkStart;
    float startYaw;
    double lastTick;
    float yaw, pitch, verticalSpeed;
    bool looking, jump;

    [MenuItem("Tools/Asset Workshop/Walk Tree House")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = GetWindow<TreeHouseWalkWindow>("Прогулка по домику на дереве");
        window.minSize = new Vector2(700, 440);
        if (!window.player) window.Setup();
        window.Show();
        window.Focus();
    }

    protected void OnEnable()
    {
        lastTick = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    protected void OnLostFocus()
    {
        keys.Clear();
        looking = jump = false;
    }

    protected void OnDisable()
    {
        EditorApplication.update -= Tick;
        ReleasePreview();
    }

    void ReleasePreview()
    {
        if (walkCamera) walkCamera.targetTexture = null;
        if (frame) DestroyImmediate(frame);
        if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        preview = default;
        player = null;
        walkCamera = null;
        frame = null;
        items.Clear();
    }

    GameObject Make(string label)
    {
        var obj = new GameObject(label);
        SceneManager.MoveGameObjectToScene(obj, preview);
        return obj;
    }

    protected void Setup()
    {
        preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var house = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LocationPath), preview);
            foreach (var behaviour in house.GetComponentsInChildren<MonoBehaviour>())
                if (behaviour) behaviour.enabled = false;

            if (IncludeGround)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SceneManager.MoveGameObjectToScene(floor, preview);
                floor.transform.position = GroundPosition;
                floor.transform.localScale = GroundSize;
                floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            }

            var sun = Make("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = SunIntensity;
            sun.shadows = WalkSunShadows;
            sun.transform.rotation = Quaternion.Euler(45, -30, 0);

            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
            player = Make("Walk controller").AddComponent<CharacterController>();
            player.height = source.height;
            player.radius = source.radius;
            player.center = source.center;
            player.stepOffset = source.stepOffset;
            player.skinWidth = source.skinWidth;
            player.slopeLimit = source.slopeLimit;

            walkCamera = Make("Walk camera").AddComponent<Camera>();
            walkCamera.transform.SetParent(player.transform, false);
            walkCamera.transform.localPosition = Vector3.up * 1.6f;
            walkCamera.enabled = false;
            walkCamera.clearFlags = CameraClearFlags.SolidColor;
            walkCamera.backgroundColor = new Color(.47f, .63f, .74f);
            walkCamera.nearClipPlane = .05f;
            walkCamera.farClipPlane = 120f;
            walkCamera.fieldOfView = 70f;
            walkCamera.scene = preview;

            SetupRoute(house);
        }
        catch
        {
            ReleasePreview();
            throw;
        }
    }

    protected virtual void SetupRoute(GameObject location)
    {
        var anchors = location.transform.Find("Workshop_Walk_Anchors");
        AddItem("coin", "Монета", anchors.Find("Coin").position);
        AddItem("goblet", "Кубок", anchors.Find("Goblet").position);
        AddItem("Lantern", "Фонарь", anchors.Find("Lantern").position);
        SetStart(anchors.Find("Start").position, 180);
    }

    protected void SetStart(Vector3 position, float heading)
    {
        walkStart = position;
        startYaw = heading;
        ResetWalk();
    }

    protected virtual void DrawRouteButtons() {}

    protected void AddItem(string prefab, string label, Vector3 surface)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Items/{prefab}.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, preview);
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>())
            if (behaviour) behaviour.enabled = false;
        foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var light in model.GetComponentsInChildren<Light>()) light.enabled = false;
        model.transform.position = surface;
        var renderers = model.GetComponentsInChildren<MeshRenderer>().Where(renderer => renderer.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float scale = .48f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        model.transform.localScale *= scale;
        model.transform.position += (surface - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)) * scale;
        items.Add((model, label));
    }

    void ResetWalk()
    {
        player.enabled = false;
        player.transform.position = walkStart;
        player.enabled = true;
        yaw = startYaw;
        pitch = verticalSpeed = 0;
        player.transform.rotation = Quaternion.Euler(0, yaw, 0);
        walkCamera.transform.localRotation = Quaternion.identity;
        OnLostFocus();
        foreach (var item in items) item.model.SetActive(true);
        Physics.SyncTransforms();
        Repaint();
    }

    void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = Mathf.Clamp((float)(now - lastTick), 0, .035f);
        lastTick = now;
        if (!player || focusedWindow != this) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Close(); return; }
        if (player.isGrounded) verticalSpeed = jump ? 5.2f : -1.1f;
        else verticalSpeed -= 19f * dt;
        jump = false;
        var direction = new Vector3((keys.Contains(KeyCode.D) ? 1 : 0) - (keys.Contains(KeyCode.A) ? 1 : 0),
            0, (keys.Contains(KeyCode.W) ? 1 : 0) - (keys.Contains(KeyCode.S) ? 1 : 0)).normalized;
        float speed = keys.Contains(KeyCode.LeftShift) || keys.Contains(KeyCode.RightShift) ? 7f : 4f;
        player.Move((player.transform.TransformDirection(direction) * speed + Vector3.up * verticalSpeed) * dt);
        if (player.transform.position.y < FallLimit) ResetWalk();
        Repaint();
    }

    int NearbyItem()
    {
        int nearest = -1;
        float distance = PickupReach;
        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i].model.activeSelf) continue;
            var offset = items[i].model.transform.position - player.transform.position;
            if (Mathf.Abs(offset.y) > 1.25f || offset.magnitude >= distance) continue;
            distance = offset.magnitude;
            nearest = i;
        }
        return nearest;
    }

    void CollectNearby()
    {
        int index = NearbyItem();
        if (index >= 0) items[index].model.SetActive(false);
    }

    protected void OnGUI()
    {
        if (!player)
        {
            if (GUILayout.Button("Начать прогулку")) Setup();
            return;
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("К началу", EditorStyles.toolbarButton)) ResetWalk();
            DrawRouteButtons();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Собрано {items.Count(item => !item.model.activeSelf)} из {items.Count}");
        }
        EditorGUILayout.LabelField("WASD — движение   Shift — бег   ПКМ + мышь — обзор   Пробел — прыжок   E — подобрать");
        int nearby = NearbyItem();
        EditorGUILayout.LabelField(nearby >= 0 ? $"E — подобрать: {items[nearby].label}" : "Подойди к предмету, чтобы подобрать его.");
        var rect = GUILayoutUtility.GetRect(64, 10000, 64, 10000);
        HandleInput(rect, Event.current);
        if (Event.current.type != EventType.Repaint) return;

        int width = Mathf.Clamp((int)rect.width, 64, 1600);
        int height = Mathf.Clamp((int)rect.height, 64, 1000);
        if (!frame || frame.width != width || frame.height != height)
        {
            if (frame) DestroyImmediate(frame);
            frame = new RenderTexture(width, height, 24) { hideFlags = HideFlags.HideAndDontSave };
        }
        walkCamera.targetTexture = frame;
        try { walkCamera.Render(); }
        finally { walkCamera.targetTexture = null; }
        GUI.DrawTexture(rect, frame, ScaleMode.ScaleToFit);
    }

    void HandleInput(Rect rect, Event input)
    {
        if (input.type == EventType.KeyDown)
        {
            bool pressed = keys.Add(input.keyCode);
            if (pressed && input.keyCode == KeyCode.Space) jump = true;
            if (pressed && input.keyCode == KeyCode.E) CollectNearby();
            if (input.keyCode == KeyCode.Escape) OnLostFocus();
            input.Use();
        }
        else if (input.type == EventType.KeyUp)
        {
            keys.Remove(input.keyCode);
            input.Use();
        }
        else if (input.type == EventType.MouseDown && input.button == 1 && rect.Contains(input.mousePosition))
        {
            looking = true;
            Focus();
            input.Use();
        }
        else if (input.type == EventType.MouseUp && input.button == 1)
        {
            looking = false;
            input.Use();
        }
        else if (input.type == EventType.MouseDrag && looking)
        {
            yaw += input.delta.x * .22f;
            pitch = Mathf.Clamp(pitch + input.delta.y * .22f, -80, 80);
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            walkCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            input.Use();
        }
    }
}
