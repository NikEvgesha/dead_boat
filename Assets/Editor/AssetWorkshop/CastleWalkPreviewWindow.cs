using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Edit Mode scale inspection using the actual Player prefab. No game-script stubs,
// no persisted test player, and no changes to the source Player or its components.
public sealed class CastleWalkPreviewWindow : EditorWindow
{
    GameObject player;
    CharacterController controller;
    Camera viewCamera;
    RenderTexture texture;
    readonly HashSet<KeyCode> keys = new HashSet<KeyCode>();
    double lastTick;
    float yaw, pitch, verticalSpeed;
    float speed, sprint, jump, gravity, fall;
    bool looking, jumpRequested;
    int bank = -1;

    [MenuItem("Tools/Asset Workshop/Walk Castle with Project Player")]
    public static void Open()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/CastleServicesWorkshop.unity")
            throw new InvalidOperationException("Open CastleServicesWorkshop first.");
        var w = GetWindow<CastleWalkPreviewWindow>();
        w.titleContent = new GUIContent("Осмотр замка — Player");
        w.minSize = new Vector2(740, 450);
        w.Show(); w.Focus();
        if (w.player == null) w.StartSession();
    }

    void OnEnable() { lastTick = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
    void OnDisable() { EditorApplication.update -= Tick; EndSession(); }
    void OnLostFocus() { keys.Clear(); looking = false; }
    void EndSession()
    {
        if (player != null) DestroyImmediate(player);
        if (texture != null) { texture.Release(); DestroyImmediate(texture); }
        player = null; controller = null; viewCamera = null; texture = null;
        keys.Clear(); looking = false;
    }
    float SourceValue(string source, string field)
    {
        var m = Regex.Match(source, @"^  " + Regex.Escape(field) + @": ([\d.]+)$", RegexOptions.Multiline);
        if (!m.Success) throw new InvalidOperationException("Missing Player parameter: " + field);
        return float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }
    void StartSession()
    {
        EndSession();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (source == null) throw new InvalidOperationException("Project Player prefab not found.");
        var data = File.ReadAllText("Assets/Prefabs/Player.prefab").Replace("\r\n", "\n");
        speed = SourceValue(data, "_moveSpeed"); sprint = SourceValue(data, "_sprintMultiplier");
        jump = SourceValue(data, "_jumpPower"); gravity = SourceValue(data, "_gravity"); fall = SourceValue(data, "_fallSpeed");
        // Instantiate a connected source without unpacking its missing scripts.
        player = (GameObject)PrefabUtility.InstantiatePrefab(source);
        player.name = "Temporary_Project_Player_Scale_Preview";
        foreach (var t in player.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        controller = player.GetComponent<CharacterController>();
        viewCamera = player.GetComponentInChildren<Camera>(true);
        if (controller == null || viewCamera == null) { EndSession(); throw new InvalidOperationException("Player camera/controller missing."); }
        foreach (var col in player.GetComponentsInChildren<Collider>(true)) if (col != controller) col.enabled = false;
        foreach (var body in player.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
        foreach (var listener in player.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        foreach (var canvas in player.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
        viewCamera.enabled = false;
        // The source Player is inactive until the game's PlayerManager spawns it.
        player.SetActive(true);
        ResetSpawn(bank);
        lastTick = EditorApplication.timeSinceStartup;
    }
    void ResetSpawn(int side)
    {
        bank = side;
        if (player == null) return;
        controller.enabled = false;
        player.transform.position = new Vector3(side * 6, .3f, -20);
        yaw = 0; pitch = 0; verticalSpeed = 0;
        player.transform.rotation = Quaternion.identity;
        viewCamera.transform.localRotation = Quaternion.identity;
        controller.enabled = true; Physics.SyncTransforms();
        keys.Clear(); looking = false; Repaint();
    }
    void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = Mathf.Clamp((float)(now - lastTick), 0, .04f); lastTick = now;
        if (player == null) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EndSession(); return; }
        if (focusedWindow != this) { keys.Clear(); looking = false; return; }
        if (controller.isGrounded) { verticalSpeed = -.5f; if (jumpRequested) verticalSpeed = jump; }
        else verticalSpeed -= gravity * fall * dt;
        jumpRequested = false;
        float x = (keys.Contains(KeyCode.D) ? 1 : 0) - (keys.Contains(KeyCode.A) ? 1 : 0);
        float z = (keys.Contains(KeyCode.W) ? 1 : 0) - (keys.Contains(KeyCode.S) ? 1 : 0);
        var direction = new Vector3(x, 0, z).normalized;
        float mult = keys.Contains(KeyCode.LeftShift) || keys.Contains(KeyCode.RightShift) ? sprint : 1;
        var motion = player.transform.TransformDirection(direction) * speed * mult;
        controller.Move((motion + Vector3.up * verticalSpeed) * dt);
        if (player.transform.position.y < -8) ResetSpawn(bank);
        Repaint();
    }
    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Левый берег", EditorStyles.toolbarButton)) { if (player == null) StartSession(); ResetSpawn(-1); }
            if (GUILayout.Button("Правый берег", EditorStyles.toolbarButton)) { if (player == null) StartSession(); ResetSpawn(1); }
            if (GUILayout.Button("К началу", EditorStyles.toolbarButton)) { if (player == null) StartSession(); ResetSpawn(bank); }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Завершить осмотр", EditorStyles.toolbarButton)) { Close(); GUIUtility.ExitGUI(); }
        }
        EditorGUILayout.LabelField("WASD — ходить · ПКМ + движение мыши — смотреть · Shift — бег · Space — прыжок", EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox("Осмотр через настоящий Player.prefab: камера 1.6 м, коллайдер 1.8 м. Это Edit Mode; игровые механики и Play Mode требуют исправления сборки проекта.", MessageType.Info);
        Rect area = GUILayoutUtility.GetRect(100, 10000, 100, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        var e = Event.current;
        if (e.type == EventType.KeyDown) { keys.Add(e.keyCode); if (e.keyCode == KeyCode.Space) jumpRequested = true; if (e.keyCode == KeyCode.Escape) { looking = false; keys.Clear(); } e.Use(); }
        if (e.type == EventType.KeyUp) { keys.Remove(e.keyCode); e.Use(); }
        if (e.type == EventType.MouseDown && e.button == 1 && area.Contains(e.mousePosition)) { looking = true; Focus(); e.Use(); }
        if (e.type == EventType.MouseUp && e.button == 1) { looking = false; e.Use(); }
        if (e.type == EventType.MouseDrag && looking && player != null)
        {
            yaw += e.delta.x * .22f; pitch = Mathf.Clamp(pitch + e.delta.y * .22f, -80, 80);
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0); e.Use();
        }
        if (e.type == EventType.Repaint && viewCamera != null)
        {
            int width = Mathf.Clamp((int)area.width, 64, 1600), height = Mathf.Clamp((int)area.height, 64, 1000);
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null) { texture.Release(); DestroyImmediate(texture); }
                texture = new RenderTexture(width, height, 24) { hideFlags = HideFlags.HideAndDontSave };
            }
            var canvases = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).ToArray();
            var states = canvases.Select(c => c.enabled).ToArray();
            try
            {
                // Gameplay's static missing-script UI is huge before initialization.
                foreach (var c in canvases) c.enabled = false;
                viewCamera.targetTexture = texture; viewCamera.Render();
            }
            finally { viewCamera.targetTexture = null; for (int i = 0; i < canvases.Length; i++) if (canvases[i] != null) canvases[i].enabled = states[i]; }
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit);
        }
        if (player != null) EditorGUILayout.LabelField("Player: 1.8 × 0.8 м · " + speed.ToString("F1") + " м/с · " + (controller.isGrounded ? "на земле" : "в воздухе"));
    }
    // Native API validation without synthesizing user keyboard/mouse input.
    public static void ValidateSession()
    {
        var w = GetWindow<CastleWalkPreviewWindow>();
        if (w.player == null) w.StartSession();
        var pos = w.player.transform.position;
        Physics.SyncTransforms();
        for (int i = 0; i < 60; i++) w.controller.Move(new Vector3(0, -.04f, .025f));
        if (w.player.transform.position.z < pos.z + 1 || !w.controller.isGrounded) throw new InvalidOperationException("Player preview movement/ground failed.");
        if (Mathf.Abs(w.controller.height - 1.8f) > .001f || Mathf.Abs(w.viewCamera.transform.localPosition.y - 1.6f) > .001f) throw new InvalidOperationException("Player scale mismatch.");
        File.AppendAllText("Docs/assets/examples/CASTLE_TERRITORY_WALK_VALIDATION.txt", "Project Player editor inspection: original connected prefab, camera 1.6m, controller 1.8m/radius .4; native movement/ground PASS. Play Mode remains blocked by project compilation errors.\n");
        w.ResetSpawn(-1);
    }
}
