using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EggNestProgressDisplay : MonoBehaviour
{
    [SerializeField] private EggNestPoint _nest;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Text _timerText;
    [SerializeField] private Text _titleText;
    [SerializeField] private Image _progressFill;
    [SerializeField] private GameObject _incubatingRoot;
    [SerializeField] private GameObject _readyRoot;
    [SerializeField] private bool _createTemporaryUiIfMissing = true;

    private EggHatchingManager _manager;
    private float _nextRefreshTime;

    private void Awake()
    {
        if (_nest == null)
            _nest = GetComponent<EggNestPoint>();

        EnsureTemporaryUiIfNeeded();
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
            _titleText.text = ready ? "Ready to collect" : state.eggId;

        if (_progressFill != null)
        {
            int duration = Mathf.Max(1, state.durationSeconds);
            float progress = ready ? 1f : Mathf.Clamp01(1f - remaining / (float)duration);
            _progressFill.fillAmount = progress;
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
        root.transform.position = transform.position + Vector3.up * 2.05f;
        root.transform.localRotation = Quaternion.identity;

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 29;

        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(240f, 96f);
        ApplyWorldSpaceCanvasScale(canvasRect, 0.01f);

        GameObject background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.05f, 0.06f, 0.82f);

        _incubatingRoot = background;

        _titleText = CreateText(background.transform, "Title", new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.92f), "Egg");
        _timerText = CreateText(background.transform, "Timer", new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.58f), "00:00:00");

        GameObject progress = new GameObject("Progress", typeof(RectTransform));
        progress.transform.SetParent(background.transform, false);
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.1f, 0.13f);
        progressRect.anchorMax = new Vector2(0.9f, 0.25f);
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;

        _progressFill = progress.AddComponent<Image>();
        _progressFill.color = new Color(0.46f, 0.86f, 0.42f, 0.95f);
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _progressFill.fillAmount = 0f;

        _canvas.gameObject.SetActive(false);
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string value)
    {
        GameObject label = new GameObject(name, typeof(RectTransform));
        label.transform.SetParent(parent, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = anchorMin;
        labelRect.anchorMax = anchorMax;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = label.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 20;
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

    private static Font GetDefaultFont()
    {
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
