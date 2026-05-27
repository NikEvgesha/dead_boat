using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EggNestProgressDisplay : MonoBehaviour
{
    private const float ProgressFillMinX = 0f;
    private const float ProgressFillMaxX = 1f;
    private static readonly Vector2 ProgressFillAnchorMin = new(ProgressFillMinX, 0f);
    private static readonly Vector2 ProgressFillAnchorMax = new(ProgressFillMaxX, 1f);

    [SerializeField] private EggNestPoint _nest;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Text _timerText;
    [SerializeField] private Text _titleText;
    [SerializeField] private Image _progressFill;
    [SerializeField] private Image _progressBackground;
    [SerializeField] private Sprite _progressFillSprite;
    [SerializeField] private Sprite _progressBackgroundSprite;
    [SerializeField] private GameObject _incubatingRoot;
    [SerializeField] private GameObject _readyRoot;
    [SerializeField] private bool _createTemporaryUiIfMissing = true;
    [SerializeField] private bool _faceCamera = true;
    [SerializeField] private Vector3 _worldOffset = new(0f, 2.05f, 0f);
    [SerializeField] private Font _font;
    [SerializeField] private LocalizationData _localizationData;

    private EggHatchingManager _manager;
    private float _nextRefreshTime;
    private Camera _camera;

    private void Awake()
    {
        if (_nest == null)
            _nest = GetComponent<EggNestPoint>();

        EnsureTemporaryUiIfNeeded();
        ConfigureVisuals();
    }

    private void Start()
    {
        TryBindManager();
        Refresh();
    }

    private void OnEnable()
    {
        TryBindManager();
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + 1f;
        Refresh();
    }

    private void LateUpdate()
    {
        if (!_faceCamera || _canvas == null || !_canvas.gameObject.activeInHierarchy)
            return;

        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
            return;

        Transform canvasTransform = _canvas.transform;
        canvasTransform.position = transform.position + _worldOffset;
        canvasTransform.LookAt(canvasTransform.position + _camera.transform.rotation * Vector3.forward, _camera.transform.rotation * Vector3.up);
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.StateChanged -= Refresh;
    }

    public void Refresh()
    {
        TryBindManager();

        if (_nest == null || _manager == null)
        {
            SetVisible(false);
            return;
        }

        EggNestState state = _manager.GetNestState(_nest.NestId);
        if (state == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        int remaining = _manager.GetRemainingSeconds(_nest.NestId);
        bool ready = remaining <= 0;

        if (_incubatingRoot != null)
            _incubatingRoot.SetActive(!ready);

        if (_readyRoot != null)
            _readyRoot.SetActive(ready);

        if (_timerText != null)
            _timerText.text = ready ? "Ready" : FormatSeconds(remaining);

        if (_titleText != null)
            _titleText.text = ready ? GetLocalizedText("Eggs/ReadyToCollect", "Ready") : GetEggTitle(state.eggId);

        if (_progressFill != null)
        {
            int duration = Mathf.Max(1, state.durationSeconds);
            float progress = ready ? 1f : Mathf.Clamp01(1f - remaining / (float)duration);
            SetProgress(progress);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
            _manager.StateChanged -= Refresh;

        _manager = manager;
        _manager.StateChanged += Refresh;
    }

    private void EnsureTemporaryUiIfNeeded()
    {
        if (!_createTemporaryUiIfMissing || _canvas != null)
            return;

        GameObject root = new GameObject("EggNestProgress", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.position = transform.position + _worldOffset;
        root.transform.localRotation = Quaternion.identity;

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 29;

        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(220f, 88f);
        ApplyWorldSpaceCanvasScale(canvasRect, 0.01f);

        GameObject background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = Resources.Load<Sprite>("Components/Frame/BasicFrame_SquareSolid01_White");
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = new Color(0.03f, 0.04f, 0.05f, 0.88f);

        _incubatingRoot = background;

        _titleText = CreateText(background.transform, "Title", new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.9f), "Egg", 14, 22, _font);
        _timerText = CreateText(background.transform, "Timer", new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.58f), "00:00:00", 16, 26, _font);

        GameObject progress = new GameObject("Progress", typeof(RectTransform));
        progress.transform.SetParent(background.transform, false);
        _progressBackground = progress.AddComponent<Image>();
        _progressBackground.raycastTarget = false;

        _canvas.gameObject.SetActive(false);
    }

    private void ConfigureVisuals()
    {
        ConfigureProgressFill();
        ConfigureText(_titleText);
        ConfigureText(_timerText);
    }

    private void ConfigureProgressFill()
    {
        Image background = _progressBackground != null ? _progressBackground : _progressFill;
        if (background == null)
            return;

        float currentProgress = GetCurrentProgress(background);

        _progressBackground = background;
        ConfigureProgressBackground(background);

        _progressFill = ResolveProgressFillImage(background);
        if (_progressFill == null)
        {
            _progressFill = background;
            SetProgress(currentProgress);
            return;
        }

        RectTransform fillRect = _progressFill.rectTransform;
        fillRect.anchorMin = ProgressFillAnchorMin;
        fillRect.anchorMax = ProgressFillAnchorMax;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Sprite fillSprite = ResolveProgressFillSprite();
        if (fillSprite != null)
        {
            _progressFill.sprite = fillSprite;
            _progressFill.color = _progressFillSprite != null
                ? Color.white
                : new Color(0.98f, 0.78f, 0.18f, 0.95f);
        }
        else
        {
            _progressFill.color = new Color(0.98f, 0.78f, 0.18f, 0.95f);
        }

        _progressFill.type = Image.Type.Sliced;
        _progressFill.fillAmount = 1f;
        _progressFill.raycastTarget = false;

        SetProgress(currentProgress);
    }

    private void ConfigureProgressBackground(Image background)
    {
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.07f, 0.08f);
        backgroundRect.anchorMax = new Vector2(0.93f, 0.315f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Sprite backgroundSprite = ResolveProgressBackgroundSprite();
        if (backgroundSprite != null)
        {
            background.sprite = backgroundSprite;
            background.color = _progressBackgroundSprite != null
                ? Color.white
                : new Color(0.03f, 0.035f, 0.045f, 0.92f);
        }
        else
        {
            background.color = new Color(0.03f, 0.035f, 0.045f, 0.92f);
        }

        background.type = Image.Type.Sliced;
        background.fillAmount = 1f;
        background.raycastTarget = false;
    }

    private void SetProgress(float progress)
    {
        if (_progressFill == null)
            return;

        progress = Mathf.Clamp01(progress);

        if (_progressFill == _progressBackground)
        {
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _progressFill.fillClockwise = true;
            _progressFill.fillAmount = progress;
            return;
        }

        RectTransform fillRect = _progressFill.rectTransform;
        fillRect.anchorMin = ProgressFillAnchorMin;
        fillRect.anchorMax = new Vector2(Mathf.Lerp(ProgressFillMinX, ProgressFillMaxX, progress), ProgressFillAnchorMax.y);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        _progressFill.enabled = progress > 0.001f;
    }

    private float GetCurrentProgress(Image background)
    {
        if (_progressFill == null)
            return 0f;

        if (_progressFill == background)
            return Mathf.Clamp01(_progressFill.fillAmount);

        RectTransform fillRect = _progressFill.rectTransform;
        float width = ProgressFillMaxX - ProgressFillMinX;
        if (width <= 0.0001f)
            return Mathf.Clamp01(_progressFill.fillAmount);

        return Mathf.Clamp01((fillRect.anchorMax.x - ProgressFillMinX) / width);
    }

    private Image ResolveProgressFillImage(Image background)
    {
        if (background == null)
            return null;

        if (_progressFill != null && _progressFill.transform.parent == background.transform && _progressFill != _progressBackground)
            return _progressFill;

        Transform existing = background.transform.Find("ProgressFill");
        if (existing != null && existing.TryGetComponent(out Image existingImage))
            return existingImage;

        return null;
    }

    private void ConfigureText(Text text)
    {
        if (text == null)
            return;

        text.font = GetDefaultFont(_font != null ? _font : text.font);
        text.fontStyle = FontStyle.Normal;
        text.resizeTextForBestFit = true;
        text.raycastTarget = false;

        UnityEngine.UI.Outline outline = text.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
            outline = text.gameObject.AddComponent<UnityEngine.UI.Outline>();

        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        outline.useGraphicAlpha = true;
    }

    private Sprite ResolveProgressFillSprite()
    {
        if (_progressFillSprite != null)
            return _progressFillSprite;

        return Resources.Load<Sprite>("Components/Slider/Slider_Basic02_White_Fill");
    }

    private Sprite ResolveProgressBackgroundSprite()
    {
        if (_progressBackgroundSprite != null)
            return _progressBackgroundSprite;

        return Resources.Load<Sprite>("Components/Slider/Slider_Basic02_Demo_Bg");
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string value, int minSize, int maxSize, Font font)
    {
        GameObject label = new GameObject(name, typeof(RectTransform));
        label.transform.SetParent(parent, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = anchorMin;
        labelRect.anchorMax = anchorMax;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = label.AddComponent<Text>();
        text.font = GetDefaultFont(font);
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int seconds = Mathf.Max(0, totalSeconds);
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int secs = seconds % 60;

        return $"{hours:00}:{minutes:00}:{secs:00}";
    }

    private string GetEggTitle(string eggId)
    {
        if (_manager != null && _manager.TryGetDefinition(eggId, out EggDefinition definition) && definition != null)
        {
            string titleKey = string.IsNullOrWhiteSpace(definition.title) ? definition.eggId : definition.title;
            return GetLocalizedText(titleKey, titleKey);
        }

        return eggId;
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (_localizationData == null || string.IsNullOrWhiteSpace(key))
            return fallback;

        string language = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage
            : "Ru";

        return _localizationData.TryGetTranslation(key, language, out string value)
            ? value
            : fallback;
    }

    private static Font GetDefaultFont(Font configuredFont)
    {
        if (configuredFont != null)
            return configuredFont;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void ApplyWorldSpaceCanvasScale(RectTransform canvasRect, float targetWorldScale)
    {
        if (canvasRect == null)
            return;

        Vector3 parentScale = canvasRect.parent != null ? canvasRect.parent.lossyScale : Vector3.one;
        canvasRect.localScale = new Vector3(
            GetSafeInverseScale(parentScale.x, targetWorldScale),
            GetSafeInverseScale(parentScale.y, targetWorldScale),
            GetSafeInverseScale(parentScale.z, targetWorldScale));
    }

    private static float GetSafeInverseScale(float parentAxisScale, float targetWorldScale)
    {
        float scale = Mathf.Abs(parentAxisScale);
        return scale > 0.0001f ? targetWorldScale / scale : targetWorldScale;
    }
}
