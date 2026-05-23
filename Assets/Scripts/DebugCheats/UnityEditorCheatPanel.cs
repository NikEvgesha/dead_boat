#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UnityEditorCheatPanel : MonoBehaviour
{
    private const string RootName = "[Unity Editor Cheat Panel]";
    private const int SortingOrder = 32000;

    [SerializeField] private KeyCode _toggleKey = KeyCode.BackQuote;
    [SerializeField, Min(1)] private int _softAmount = 1000;
    [SerializeField, Min(1)] private int _hardAmount = 100;
    [SerializeField, Min(1)] private int _levelAmount = 1;
    [SerializeField, Min(1)] private int _bigLevelAmount = 5;

    private Canvas _canvas;
    private GameObject _panel;
    private Text _statusText;
    private bool _visible;
    private CursorLockMode _previousCursorLockMode;
    private bool _previousCursorVisible;
    private bool _hasCursorSnapshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        UnityEditorCheatPanel existing = FindFirstObjectByType<UnityEditorCheatPanel>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        root.AddComponent<UnityEditorCheatPanel>();
    }

    private void Awake()
    {
        if (FindObjectsByType<UnityEditorCheatPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        EnsureEventSystem();
        EnsureUi();
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            SetVisible(!_visible);
    }

    private void EnsureUi()
    {
        if (_canvas != null)
            return;

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        _panel = CreateRect("Panel", transform);
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.69f, 0.43f);
        panelRect.anchorMax = new Vector2(0.98f, 0.96f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.06f, 0.08f, 0.94f);
        panelImage.raycastTarget = true;

        UnityEngine.UI.Outline outline = _panel.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreateText(_panel.transform, "Title", "Editor Cheats (~)", 24, FontStyle.Normal, Color.white);
        SetLayout(title.gameObject, 42f);

        CreateButton(_panel.transform, $"+{_levelAmount} level", () => AddLevels(_levelAmount));
        CreateButton(_panel.transform, $"+{_bigLevelAmount} levels", () => AddLevels(_bigLevelAmount));
        CreateButton(_panel.transform, $"+{_softAmount} soft", () => AddCurrency(CurrencyType.Coins, _softAmount));
        CreateButton(_panel.transform, $"+{_softAmount * 10} soft", () => AddCurrency(CurrencyType.Coins, _softAmount * 10));
        CreateButton(_panel.transform, $"+{_hardAmount} hard", () => AddCurrency(CurrencyType.Gems, _hardAmount));
        CreateButton(_panel.transform, $"+{_hardAmount * 10} hard", () => AddCurrency(CurrencyType.Gems, _hardAmount * 10));
        CreateButton(_panel.transform, "+1 random egg", AddRandomEgg);

        _statusText = CreateText(_panel.transform, "Status", string.Empty, 16, FontStyle.Normal, new Color(0.78f, 0.86f, 0.94f));
        SetLayout(_statusText.gameObject, 54f);
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;

        if (_panel != null)
            _panel.SetActive(_visible);

        if (_visible)
        {
            _previousCursorLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _hasCursorSnapshot = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetStatus("Ready");
        }
        else if (_hasCursorSnapshot)
        {
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
            _hasCursorSnapshot = false;
        }
    }

    private void AddCurrency(CurrencyType currencyType, int amount)
    {
        if (CurrencyManager.Instance == null)
        {
            SetStatus("CurrencyManager is missing");
            return;
        }

        CurrencyManager.Instance.AddCurrency(currencyType, Mathf.Max(1, amount));
        SetStatus($"{currencyType} +{amount}. Balance: {CurrencyManager.Instance.GetBalance(currencyType)}");
    }

    private void AddLevels(int count)
    {
        int safeCount = Mathf.Max(1, count);
        PlayerStatsManager player = PlayerStatsManager.Instance;

        if (player != null)
        {
            for (int i = 0; i < safeCount; i++)
            {
                int expToNextLevel = Mathf.Max(1, player.Experience().ExpToUp);
                player.AddExp(expToNextLevel);
            }

            if (LevelStatManager.Instance == null && SaveManager.Instance != null)
                SaveManager.Instance.SaveLevelUp(SaveManager.Instance.LoadLevelUp() + safeCount);

            SetStatus($"Levels +{safeCount}. Current level: {player.Experience().CurrentLevel}");
            return;
        }

        if (SaveManager.Instance == null)
        {
            SetStatus("PlayerStatsManager is missing");
            return;
        }

        (int exp, int level) = SaveManager.Instance.LoadPlayerExperience();
        SaveManager.Instance.SavePlayerExperience(exp, level + safeCount);
        SaveManager.Instance.SaveLevelUp(SaveManager.Instance.LoadLevelUp() + safeCount);
        SetStatus($"Saved levels +{safeCount}. Current saved level: {level + safeCount}");
    }

    private void AddRandomEgg()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
        {
            SetStatus("EggHatchingManager is missing");
            return;
        }

        SetStatus(manager.TryGrantRandomEgg(1) ? "Random egg +1" : "Random egg failed");
    }

    private void SetStatus(string value)
    {
        if (_statusText != null)
            _statusText.text = value;
    }

    private static Button CreateButton(Transform parent, string label, UnityAction action)
    {
        GameObject root = CreateRect(label, parent);
        SetLayout(root, 44f);

        Image image = root.AddComponent<Image>();
        image.color = new Color(0.14f, 0.17f, 0.24f, 0.98f);
        image.raycastTarget = true;

        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.88f, 1f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        button.colors = colors;

        UISound sound = root.AddComponent<UISound>();
        sound.Rebind();

        Text text = CreateText(root.transform, "Label", label, 18, FontStyle.Normal, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(action);
        return button;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color)
    {
        GameObject root = CreateRect(name, parent);
        Text text = root.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = size;
        text.fontStyle = style;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = size;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        return root;
    }

    private static void SetLayout(GameObject target, float preferredHeight)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout == null)
            layout = target.AddComponent<LayoutElement>();

        layout.minHeight = preferredHeight;
        layout.preferredHeight = preferredHeight;
        layout.flexibleHeight = 0f;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject root = new GameObject("EventSystem");
        root.AddComponent<EventSystem>();
        root.AddComponent<StandaloneInputModule>();
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.Load<Font>("Fonts/RussoOne-Regular");
        if (font != null)
            return font;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
#endif
