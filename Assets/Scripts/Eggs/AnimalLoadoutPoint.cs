using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AnimalLoadoutPoint : MonoBehaviour
{
    [SerializeField] private int _slotIndex = -1;
    [SerializeField] private UseButtonShow _useButtonShow;
    [SerializeField] private AnimalLoadoutPointInfoCard _infoCard;
    [SerializeField] private Canvas _useButtonCanvas;
    [SerializeField] private Canvas _cardCanvas;
    [SerializeField] private BuyTouchHandler _touchPanel;
    [SerializeField] private Image _openProgress;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private bool _renderCanvasesOnTop = true;
    [SerializeField] private Material _alwaysOnTopMaterial;
    [SerializeField] private int _useButtonSortingOrder = 120;
    [SerializeField] private int _cardSortingOrder = 121;

    private static readonly FieldInfo InfoCanvasField = typeof(UseButtonShow).GetField("_infoCanvas", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TouchPanelField = typeof(UseButtonShow).GetField("_touchPanel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo OpenProgressField = typeof(UseButtonShow).GetField("_openProgress", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SourceField = typeof(UseButtonShow).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);

    private EggHatchingManager _manager;
    private float _nextRefreshTime;

    private void Awake()
    {
        if (_slotIndex < 0)
            _slotIndex = InferSlotIndexFromName();

        ResolveReferences();
        ConfigureUseButtonShow();
        ConfigureInfoCanvasVisibility();
    }

    private void OnEnable()
    {
        BindManager();
        RefreshInfoCard();
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void Update()
    {
        BindManager();

        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + 0.5f;
        RefreshInfoCard();
    }

    public void Configure(int slotIndex, UseButtonShow useButtonShow, AnimalLoadoutPointInfoCard infoCard)
    {
        _slotIndex = slotIndex;

        if (useButtonShow != null)
            _useButtonShow = useButtonShow;

        if (infoCard != null)
            _infoCard = infoCard;

        ResolveReferences();
        ConfigureUseButtonShow();
        ConfigureInfoCanvasVisibility();
        RefreshInfoCard();
    }

    public void OpenSelection()
    {
        AnimalLoadoutPanel.Instance?.OpenForSlot(_slotIndex);
    }

    private void ResolveReferences()
    {
        if (_useButtonShow == null)
            _useButtonShow = FindUseButtonShow();

        if (_audioSource == null && _useButtonShow != null)
            _audioSource = _useButtonShow.GetComponent<AudioSource>();

        if (_audioSource == null && _useButtonShow != null)
            _audioSource = _useButtonShow.gameObject.AddComponent<AudioSource>();

        if (_useButtonCanvas == null && _useButtonShow != null)
            _useButtonCanvas = _useButtonShow.GetComponentInChildren<Canvas>(true);

        if (_touchPanel == null && _useButtonCanvas != null)
            _touchPanel = _useButtonCanvas.GetComponentInChildren<BuyTouchHandler>(true);

        if (_openProgress == null && _useButtonCanvas != null)
            _openProgress = FindProgressImage(_useButtonCanvas.transform);

        if (_infoCard == null)
            _infoCard = FindInfoCard();

        if (_cardCanvas == null && _infoCard != null)
            _cardCanvas = _infoCard.GetComponentInParent<Canvas>(true);
    }

    private UseButtonShow FindUseButtonShow()
    {
        UseButtonShow child = GetComponentInChildren<UseButtonShow>(true);
        if (child != null)
            return child;

        Transform parent = transform.parent;
        if (parent == null)
            return null;

        Transform sibling = parent.Find($"{name}_UseButtonShow");
        return sibling != null ? sibling.GetComponent<UseButtonShow>() : null;
    }

    private AnimalLoadoutPointInfoCard FindInfoCard()
    {
        Transform parent = transform.parent;
        if (parent != null)
        {
            Transform sibling = parent.Find($"{name}_InfoCanvas");
            if (sibling != null)
            {
                AnimalLoadoutPointInfoCard siblingCard = sibling.GetComponentInChildren<AnimalLoadoutPointInfoCard>(true);
                if (siblingCard != null)
                    return siblingCard;
            }
        }

        return GetComponentInChildren<AnimalLoadoutPointInfoCard>(true);
    }

    private void ConfigureUseButtonShow()
    {
        if (_useButtonShow == null)
            return;

        InfoCanvasField?.SetValue(_useButtonShow, _useButtonCanvas);
        TouchPanelField?.SetValue(_useButtonShow, _touchPanel);
        OpenProgressField?.SetValue(_useButtonShow, _openProgress);
        SourceField?.SetValue(_useButtonShow, _audioSource);
        _useButtonShow.UseButtonShowBool = true;

        _useButtonShow.Activate ??= new UnityEvent();
        _useButtonShow.Activate.RemoveListener(OpenSelection);
        _useButtonShow.Activate.AddListener(OpenSelection);
    }

    private void ConfigureInfoCanvasVisibility()
    {
        ConfigureCanvas(_useButtonCanvas, _useButtonSortingOrder);
        ConfigureCanvas(_cardCanvas, _cardSortingOrder);
    }

    private void ConfigureCanvas(Canvas canvas, int sortingOrder)
    {
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (!_renderCanvasesOnTop || _alwaysOnTopMaterial == null)
            return;

        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null && !ShouldKeepDefaultMaterial(graphic))
                graphic.material = _alwaysOnTopMaterial;
        }
    }

    private static bool ShouldKeepDefaultMaterial(Graphic graphic)
    {
        if (graphic is not Image || graphic.transform == null)
            return false;

        string objectName = graphic.transform.name;
        return objectName == "Icon" || objectName == "AdIcon";
    }

    private void BindManager()
    {
        EggHatchingManager current = EggHatchingManager.Instance;
        if (current == _manager)
            return;

        UnbindManager();

        _manager = current;
        if (_manager != null)
            _manager.StateChanged += RefreshInfoCard;
    }

    private void UnbindManager()
    {
        if (_manager != null)
            _manager.StateChanged -= RefreshInfoCard;

        _manager = null;
    }

    private void RefreshInfoCard()
    {
        if (_infoCard == null)
            return;

        if (_manager == null)
        {
            _infoCard.SetEmpty();
            return;
        }

        PlacedAnimalState placed = _manager.GetAnimalLoadoutSlot(_slotIndex);
        if (placed == null ||
            !_manager.TryGetAnimalDefinition(placed.animalId, out AnimalDefinition definition) ||
            !_manager.TryGetAnimalDetails(placed.animalId, placed.stage, out _, out string detail))
        {
            _infoCard.SetEmpty();
            return;
        }

        _infoCard.SetAnimal(definition, placed.stage, detail);
    }

    private int InferSlotIndexFromName()
    {
        const string prefix = "Animal_";
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith(prefix))
            return -1;

        string numberText = name.Substring(prefix.Length);
        if (!int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int slotNumber))
            return -1;

        return Mathf.Max(0, slotNumber - 1);
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
}
