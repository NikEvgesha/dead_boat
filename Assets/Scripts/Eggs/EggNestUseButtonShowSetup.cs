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
    [SerializeField] private bool _raiseInfoCanvas;
    [SerializeField] private bool _renderInfoCanvasOnTop = true;
    [SerializeField] private Material _alwaysOnTopMaterial;
    [SerializeField] private float _infoCanvasMinLocalHeight = 0.2f;
    [SerializeField] private int _infoCanvasSortingOrder = 120;
    [Header("Incubation boost hint")]
    [SerializeField] private bool _showBoostAdHint = true;
    [SerializeField] private Sprite _boostAdIconSprite;
    [SerializeField] private Font _font;
    [SerializeField] private GameObject _boostAdRoot;
    [SerializeField] private Image _boostAdIcon;
    [SerializeField] private Text _boostAdText;

    private static readonly FieldInfo InfoCanvasField = typeof(UseButtonShow).GetField("_infoCanvas", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TouchPanelField = typeof(UseButtonShow).GetField("_touchPanel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo OpenProgressField = typeof(UseButtonShow).GetField("_openProgress", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SourceField = typeof(UseButtonShow).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);
    private float _nextBoostHintRefreshTime;

    private void Awake()
    {
        ResolveReferences();
        ResolveUseButtonReferences();
        ConfigureUseButtonShow();
    }

    private void ResolveReferences()
    {
        if (_nestPoint == null)
            _nestPoint = GetComponent<EggNestPoint>();

        if (_useButtonShow == null)
            _useButtonShow = GetComponentInChildren<UseButtonShow>(true);

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void ResolveUseButtonReferences()
    {
        if (_useButtonShow == null)
            return;

        if (_infoCanvas == null)
            _infoCanvas = _useButtonShow.GetComponentInChildren<Canvas>(true);

        if (_touchPanel == null && _infoCanvas != null)
            _touchPanel = _infoCanvas.GetComponentInChildren<BuyTouchHandler>(true);

        if (_openProgress == null && _infoCanvas != null)
            _openProgress = FindProgressImage(_infoCanvas.transform);
    }

    private void ConfigureUseButtonShow()
    {
        if (_useButtonShow == null || _nestPoint == null)
            return;

        InfoCanvasField?.SetValue(_useButtonShow, _infoCanvas);
        TouchPanelField?.SetValue(_useButtonShow, _touchPanel);
        OpenProgressField?.SetValue(_useButtonShow, _openProgress);
        SourceField?.SetValue(_useButtonShow, _audioSource);
        _useButtonShow.UseButtonShowBool = true;
        ConfigureBoostAdHint();
        ConfigureInfoCanvasVisibility();
        RefreshBoostAdHint();

        UnityEvent activate = _useButtonShow.Activate;
        if (activate == null)
        {
            activate = new UnityEvent();
            typeof(UseButtonShow).GetField("Activate", BindingFlags.Instance | BindingFlags.Public)?.SetValue(_useButtonShow, activate);
        }

        activate.RemoveListener(_nestPoint._Use);
        if (!HasPersistentUseListener(activate, _nestPoint))
            activate.AddListener(_nestPoint._Use);
    }

    private void Update()
    {
        if (_boostAdRoot == null || Time.unscaledTime < _nextBoostHintRefreshTime)
            return;

        _nextBoostHintRefreshTime = Time.unscaledTime + 0.5f;
        RefreshBoostAdHint();
    }

    private void ConfigureInfoCanvasVisibility()
    {
        if (_infoCanvas == null)
            return;

        _infoCanvas.overrideSorting = true;
        _infoCanvas.sortingOrder = _infoCanvasSortingOrder;
        _infoCanvas.transform.SetAsLastSibling();

        if (_renderInfoCanvasOnTop)
            ApplyAlwaysOnTopMaterial(_infoCanvas);

        if (!_raiseInfoCanvas)
            return;

        RectTransform canvasRect = _infoCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
            return;

        Vector3 localPosition = canvasRect.localPosition;
        if (localPosition.y < _infoCanvasMinLocalHeight)
            localPosition.y = _infoCanvasMinLocalHeight;

        canvasRect.localPosition = localPosition;
    }

    private void ConfigureBoostAdHint()
    {
        if (!_showBoostAdHint || _infoCanvas == null)
            return;

        Transform parent = ResolveInfoRoot(_infoCanvas.transform);

        if (_boostAdRoot == null)
        {
            Transform existing = parent.Find("EggBoostAdHint");
            if (existing == null)
                return;

            _boostAdRoot = existing.gameObject;
        }

        if (_boostAdIcon == null)
            _boostAdIcon = _boostAdRoot.GetComponentInChildren<Image>(true);

        if (_boostAdText == null)
            _boostAdText = _boostAdRoot.GetComponentInChildren<Text>(true);

        if (_boostAdIcon != null)
        {
            if (_boostAdIconSprite != null)
                _boostAdIcon.sprite = _boostAdIconSprite;

            _boostAdIcon.material = null;
            _boostAdIcon.preserveAspect = true;
            _boostAdIcon.raycastTarget = false;
        }

        ConfigureBoostAdText();
    }

    private void RefreshBoostAdHint()
    {
        if (_boostAdRoot == null)
            return;

        bool visible = ShouldShowBoostAdHint();
        _boostAdRoot.SetActive(visible);

        if (_boostAdText != null)
            _boostAdText.text = $"-{GetBoostMinutes()}";
    }

    private bool ShouldShowBoostAdHint()
    {
        if (!_showBoostAdHint || _nestPoint == null || !_nestPoint.BoostIncubationWithRewardedAd)
            return false;

        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || string.IsNullOrWhiteSpace(_nestPoint.NestId))
            return false;

        return manager.GetNestState(_nestPoint.NestId) != null && manager.GetRemainingSeconds(_nestPoint.NestId) > 0;
    }

    private int GetBoostMinutes()
    {
        if (_nestPoint == null)
            return 30;

        return Mathf.Max(1, Mathf.CeilToInt(_nestPoint.IncubationBoostSeconds / 60f));
    }

    private void ConfigureBoostAdText()
    {
        if (_boostAdText == null)
            return;

        _boostAdText.font = GetDefaultFont(_font != null ? _font : _boostAdText.font);
        _boostAdText.fontStyle = FontStyle.Normal;
        _boostAdText.resizeTextForBestFit = true;
        _boostAdText.resizeTextMinSize = 10;
        _boostAdText.resizeTextMaxSize = 28;
        _boostAdText.alignment = TextAnchor.MiddleLeft;
        _boostAdText.color = Color.white;
        _boostAdText.raycastTarget = false;
        _boostAdText.text = $"-{GetBoostMinutes()}";

        UnityEngine.UI.Outline outline = _boostAdText.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
            outline = _boostAdText.gameObject.AddComponent<UnityEngine.UI.Outline>();

        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        outline.useGraphicAlpha = true;
    }

    private static Transform ResolveInfoRoot(Transform canvasTransform)
    {
        Transform info = canvasTransform.Find("Info");
        return info != null ? info : canvasTransform;
    }

    private void ApplyAlwaysOnTopMaterial(Canvas canvas)
    {
        if (_alwaysOnTopMaterial == null)
            return;

        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null || graphic == _boostAdIcon || IsBoostAdIconGraphic(graphic))
                continue;

            graphic.material = _alwaysOnTopMaterial;
        }
    }

    private static bool IsBoostAdIconGraphic(Graphic graphic)
    {
        if (graphic is not Image || graphic.transform == null || graphic.transform.parent == null)
            return false;

        return graphic.transform.name == "AdIcon" && graphic.transform.parent.name == "EggBoostAdHint";
    }

    private static bool HasPersistentUseListener(UnityEvent activate, EggNestPoint nestPoint)
    {
        if (activate == null || nestPoint == null)
            return false;

        for (int i = 0; i < activate.GetPersistentEventCount(); i++)
        {
            if (activate.GetPersistentTarget(i) == nestPoint &&
                activate.GetPersistentMethodName(i) == nameof(EggNestPoint._Use))
            {
                return true;
            }
        }

        return false;
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

    private static Font GetDefaultFont(Font configuredFont)
    {
        if (configuredFont != null)
            return configuredFont;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

}
