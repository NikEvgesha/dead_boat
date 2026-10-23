using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetWorkshopWindow : EditorWindow
{
    public const string ScenePath = "Assets/Scenes/AssetWorkshop.unity";
    [Serializable] public class Item { public string name, path, pack, kind, category, usage; }
    [Serializable] public class Inventory { public Item[] items; }
    Item[] items = Array.Empty<Item>();
    Item[] filtered = Array.Empty<Item>();
    string query = "";
    string[] categories = { "Все категории" };
    int category;
    bool environmentOnly = true, preferredFormats = true;
    Vector2 scroll;
    Item selected;

    [MenuItem("Tools/Dead Boat/Каталог ассетов и мастерская")]
    public static void Open()
    {
        var window = GetWindow<AssetWorkshopWindow>("Каталог ассетов");
        window.minSize = new Vector2(560, 550);
        window.Show();
    }
    void OnEnable() { Reload(); }
    void Reload()
    {
        string path = Path.GetFullPath("Docs/assets/inventory.csv");
        items = File.Exists(path) ? ReadCsv(File.ReadAllText(path)).Skip(1).Where(r => r.Length == 6)
            .Select(r => new Item { name = r[0], path = r[1], pack = r[2], kind = r[3], category = r[4], usage = r[5] }).ToArray() : Array.Empty<Item>();
        categories = new[] { "Все категории" }.Concat(items.Select(x => x.category).Distinct().OrderBy(x => x)).ToArray();
        category = Mathf.Clamp(category, 0, categories.Length - 1);
        Filter();
    }
    static IEnumerable<string[]> ReadCsv(string text)
    {
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { row.Add(field.ToString()); field.Clear(); }
            else if ((c == '\r' || c == '\n') && !quoted)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear(); yield return row.ToArray(); row.Clear();
            }
            else if (c != '\uFEFF') field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); yield return row.ToArray(); }
    }
    void Filter()
    {
        filtered = items.Where(x => (!environmentOnly || (x.category != "UI и интерфейс" && x.category != "Персонажи и игровые объекты" && x.category != "Эффекты"))
            && (!preferredFormats || x.kind == "Prefab" || x.path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            && (category == 0 || x.category == categories[category])
            && (string.IsNullOrWhiteSpace(query) || (x.name + " " + x.path + " " + x.category + " " + x.usage).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
    }
    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Открыть мастерскую")) OpenWorkshop();
            if (GUILayout.Button("Обновить список")) Reload();
        }
        EditorGUI.BeginChangeCheck();
        query = EditorGUILayout.TextField("Поиск", query);
        category = EditorGUILayout.Popup("Категория", category, categories);
        environmentOnly = EditorGUILayout.Toggle("Окружение и пропсы", environmentOnly);
        preferredFormats = EditorGUILayout.Toggle("Префабы и FBX", preferredFormats);
        if (EditorGUI.EndChangeCheck()) Filter();
        EditorGUILayout.LabelField($"Найдено {filtered.Length} / {items.Length} файлов. Назначение — подсказка по имени.", EditorStyles.wordWrappedMiniLabel);
        float listHeight = Mathf.Max(120, position.height - 300);
        // Render only visible rows, so thousands of inventory entries do not allocate GUI controls every frame.
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(listHeight));
        const float rowHeight = 25;
        Rect area = GUILayoutUtility.GetRect(0, filtered.Length * rowHeight, GUILayout.ExpandWidth(true));
        int start = Mathf.Max(0, (int)(scroll.y / rowHeight) - 1);
        int end = Mathf.Min(filtered.Length, start + (int)(listHeight / rowHeight) + 4);
        for (int i = start; i < end; i++)
        {
            var item = filtered[i];
            Rect row = new Rect(area.x, area.y + i * rowHeight, area.width, rowHeight - 2);
            if (GUI.Button(row, item.name + "  ·  " + item.kind + "  ·  " + item.category)) selected = item;
        }
        EditorGUILayout.EndScrollView();
        if (selected == null) return;
        EditorGUILayout.LabelField(selected.name, EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(selected.path, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(40));
        EditorGUILayout.LabelField(selected.usage, EditorStyles.wordWrappedLabel);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(selected.path);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Найти в Project"))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(selected.path);
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            using (new EditorGUI.DisabledScope(asset == null || SceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Добавить в мастерскую")) AddSelected();
            }
        }
        if (asset == null) EditorGUILayout.HelpBox("Файл не импортирован как GameObject. Выберите FBX или префаб либо найдите файл в Project.", MessageType.Info);
    }

    public void AddSelected()
    {
        var scene = SceneManager.GetActiveScene();
        if (selected == null || scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(selected.path);
        if (asset == null) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Добавить предмет в мастерскую");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
        Undo.RegisterCreatedObjectUndo(instance, "Добавить предмет");
        var parent = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "01_Assembly_Area");
        if (parent != null) Undo.SetTransformParent(instance.transform, parent.transform, "Поместить в сборку");
        instance.transform.position = Vector3.zero;
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = instance;
        SceneView.lastActiveSceneView?.FrameSelected();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    public static void OpenWorkshop()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play Mode перед открытием мастерской.");
        var existing = SceneManager.GetSceneByPath(ScenePath);
        if (existing.IsValid() && existing.isLoaded) { SceneManager.SetActiveScene(existing); Open(); return; }
        var previous = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; i++) previous.Add(SceneManager.GetSceneAt(i));
        Scene scene;
        if (File.Exists(ScenePath)) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var assembly = new GameObject("01_Assembly_Area");
            SceneManager.MoveGameObjectToScene(assembly, scene);
            var gallery = new GameObject("02_Reference_Gallery");
            SceneManager.MoveGameObjectToScene(gallery, scene);
            BuildGallery(scene, gallery.transform);
            var camera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>()).First();
            camera.transform.position = new Vector3(15, 24, -25);
            camera.transform.LookAt(new Vector3(15, 0, 10));
            camera.backgroundColor = new Color(.18f, .22f, .27f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        SceneManager.SetActiveScene(scene);
        // Keep any unsaved user scene loaded; never discard or silently save it.
        foreach (var old in previous) if (!old.isDirty) EditorSceneManager.CloseScene(old, true);
        var view = SceneView.GetWindow<SceneView>();
        view.LookAt(new Vector3(14, 0, 8), Quaternion.Euler(45, -25, 0), 30);
        Open();
    }

    static void BuildGallery(Scene scene, Transform parent)
    {
        const string town = "Assets/Import/kenney_fantasy-town-kit/Models/FBX format/";
        string[] names = { "pillarWood", "planks", "planksHalf", "planksOpening", "wallWood", "wallWoodDoor", "wallWoodWindowShutters", "roof", "roofGable", "chimney", "fence", "fenceGate", "cart", "lantern", "pillarStone", "stairsWood" };
        int index = 0;
        foreach (string name in names)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(town + name + ".fbx");
            if (asset == null) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            instance.transform.SetParent(parent, true);
            Vector3 target = new Vector3(20 + (index % 4) * 6, 0, (index / 4) * 6);
            instance.transform.position = target;
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                instance.transform.position += new Vector3(target.x - bounds.center.x, -bounds.min.y, target.z - bounds.center.z);
            }
            var label = new GameObject("Label_" + name);
            SceneManager.MoveGameObjectToScene(label, scene);
            label.transform.SetParent(parent, true);
            label.transform.position = target + new Vector3(-2, .02f, -2);
            label.transform.rotation = Quaternion.Euler(90, 0, 0);
            var text = label.AddComponent<TextMesh>();
            text.text = name; text.characterSize = .1f; text.fontSize = 36; text.color = Color.white;
            index++;
        }
    }
}
