using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EggNestUseButtonShowSetup : MonoBehaviour
{
    [SerializeField] private EggNestPoint _nestPoint;
    [SerializeField] private UseButtonShow _useButtonShow;
    [SerializeField] private Canvas _infoCanvas;
    [SerializeField] private BuyTouchHandler _touchPanel;
    [SerializeField] private Image _openProgress;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private bool _createTemporaryUiIfMissing = true;

    private static readonly FieldInfo InfoCanvasField = typeof(UseButtonShow).GetField("_infoCanvas", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TouchPanelField = typeof(UseButtonShow).GetField("_touchPanel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo OpenProgressField = typeof(UseButtonShow).GetField("_openProgress", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SourceField = typeof(UseButtonShow).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo HintDesktopField = typeof(BuyTouchHandler).GetField("_hintDesctop", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo HintTouchField = typeof(BuyTouchHandler).GetField("_hintTouch", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        ResolveReferences();
        EnsureTemporaryUiIfNeeded();
        ConfigureUseButtonShow();
    }

    private void ResolveReferences()
    {
        if (_nestPoint == null)
            _nestPoint = GetComponent<EggNestPoint>();

        if (_useButtonShow == null)
            _useButtonShow = GetComponent<UseButtonShow>();

        if (_useButtonShow == null)
            _useButtonShow = gameObject.AddComponent<UseButtonShow>();

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void EnsureTemporaryUiIfNeeded()
    {
        if (!_createTemporaryUiIfMissing)
            return;

        if (_infoCanvas == null)
            _infoCanvas = CreateInfoCanvas();

        if (_touchPanel == null)
            _touchPanel = _infoCanvas.GetComponentInChildren<BuyTouchHandler>(true);

        if (_openProgress == null)
            _openProgress = FindProgressImage(_infoCanvas.transform);

        ConfigureTouchPanelHints();
    }

    private void ConfigureUseButtonShow()
    {
        if (_useButtonShow == null || _nestPoint == null)
            return;

        InfoCanvasField?.SetValue(_useButtonShow, _infoCanvas);
        TouchPanelField?.SetValue(_useButtonShow, _touchPanel);
        OpenProgressField?.SetValue(_useButtonShow, _openProgress);
        SourceField?.SetValue(_useButtonShow, _audioSource);

        UnityEvent activate = _useButtonShow.Activate;
        if (activate == null)
        {
            activate = new UnityEvent();
            typeof(UseButtonShow).GetField("Activate", BindingFlags.Instance | BindingFlags.Public)?.SetValue(_useButtonShow, activate);
        }

        activate.RemoveListener(_nestPoint._Use);
        activate.AddListener(_nestPoint._Use);
    }

    private Canvas CreateInfoCanvas()
    {
        GameObject root = new GameObject("EggNestUseButtonHint", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.position = transform.position + Vector3.up * 1.6f;
        root.transform.localRotation = Quaternion.identity;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;

        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(220f, 88f);
        ApplyWorldSpaceCanvasScale(canvasRect, 0.01f);

        root.AddComponent<GraphicRaycaster>();

        GameObject background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.86f);

        _touchPanel = background.AddComponent<BuyTouchHandler>();

        GameObject desktopHint = CreateLabel(background.transform, "HintDesktop", "Hold E");
        GameObject touchHint = CreateLabel(background.transform, "HintTouch", "Hold");
        touchHint.SetActive(false);

        GameObject progress = new GameObject("Progress", typeof(RectTransform));
        progress.transform.SetParent(background.transform, false);
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.1f, 0.12f);
        progressRect.anchorMax = new Vector2(0.9f, 0.26f);
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;

        _openProgress = progress.AddComponent<Image>();
        _openProgress.color = new Color(0.98f, 0.78f, 0.18f, 0.95f);
        _openProgress.type = Image.Type.Filled;
        _openProgress.fillMethod = Image.FillMethod.Horizontal;
        _openProgress.fillOrigin = (int)Image.OriginHorizontal.Left;
        _openProgress.fillAmount = 0f;
        AssignTouchPanelHints(_touchPanel, desktopHint, touchHint);

        canvas.gameObject.SetActive(false);
        return canvas;
    }

    private void ConfigureTouchPanelHints()
    {
        if (_touchPanel == null)
            return;

        GameObject desktopHint = FindChild(_touchPanel.transform, "HintDesktop");
        GameObject touchHint = FindChild(_touchPanel.transform, "HintTouch");

        if (desktopHint == null)
            desktopHint = CreateLabel(_touchPanel.transform, "HintDesktop", "Hold E");

        if (touchHint == null)
            touchHint = CreateLabel(_touchPanel.transform, "HintTouch", "Hold");

        AssignTouchPanelHints(_touchPanel, desktopHint, touchHint);
    }

    private static void AssignTouchPanelHints(BuyTouchHandler touchPanel, GameObject desktopHint, GameObject touchHint)
    {
        if (touchPanel == null)
            return;

        HintDesktopField?.SetValue(touchPanel, desktopHint);
        HintTouchField?.SetValue(touchPanel, touchHint);
    }

    private static GameObject CreateLabel(Transform parent, string name, string value)
    {
        GameObject label = new GameObject(name, typeof(RectTransform));
        label.transform.SetParent(parent, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.08f, 0.34f);
        labelRect.anchorMax = new Vector2(0.92f, 0.92f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = label.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = 24;
        text.color = Color.white;
        text.raycastTarget = false;

        return label;
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private static Image FindProgressImage(Transform root)
    {
        if (root == null)
            return null;

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].type == Image.Type.Filled)
                return images[i];
        }

        return null;
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
