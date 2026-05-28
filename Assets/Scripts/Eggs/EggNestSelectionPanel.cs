using UnityEngine;
using UnityEngine.UI;

public class EggNestSelectionPanel : MonoBehaviour
{
    private const string SelectionTitleKey = "Egg/UI/SelectEgg";
    private const string InventoryTitleKey = "Egg/UI/InventoryTitle";
    private const string EmptyStateKey = "Egg/UI/NoEggs";
    private const string SelectionTitleFallbackRu = "\u0412\u044B\u0431\u0435\u0440\u0438 \u044F\u0439\u0446\u043E";
    private const string InventoryTitleFallbackRu = "\u0418\u043D\u0432\u0435\u043D\u0442\u0430\u0440\u044C \u044F\u0438\u0446";
    private const string EmptyStateFallbackRu = "\u041D\u0435\u0442 \u0434\u043E\u0441\u0442\u0443\u043F\u043D\u044B\u0445 \u044F\u0438\u0446";

    private static EggNestSelectionPanel _instance;
    public static EggNestSelectionPanel Instance => _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _slotsRoot;
    [SerializeField] private EggNestSelectionSlot _slotPrefab;
    [SerializeField] private Text _nestLabel;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private Button _closeButton;
    [SerializeField] private bool _setCursorWhenOpen = true;

    private EggHatchingManager _manager;
    private string _currentNestId;
    private bool _opened;
    private bool _selectingEgg;
    private bool _inventoryViewOnly;

    public bool IsOpen => _opened;
    public bool IsInventoryViewOnly => _opened && _inventoryViewOnly;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (_panel == null)
            _panel = gameObject;

        ConfigurePrefabReferences();
        _panel.SetActive(false);
    }

    private void Start()
    {
        BindWindowCloseEvent();
        TryBindManager();
    }

    private void OnEnable()
    {
        BindWindowCloseEvent();
    }

    private void OnDisable()
    {
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.AOpenWindow -= CloseByOtherWindow;

        if (_opened)
            ForceReleaseCursor();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        if (_manager != null)
            _manager.StateChanged -= HandleStateChanged;
    }

    public void OpenForNest(string nestId)
    {
        if (string.IsNullOrWhiteSpace(nestId))
            return;

        BindWindowCloseEvent();
        TryBindManager();

        if (_manager == null)
            return;

        if (_manager.GetNestState(nestId) != null)
            return;

        _inventoryViewOnly = false;
        _currentNestId = nestId;
        SetOpen(true, true);
        RefreshTitle();
        RebuildSlots();

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.AOpenWindow?.Invoke(this);
    }

    public void OpenInventory()
    {
        BindWindowCloseEvent();
        TryBindManager();

        if (_manager == null)
            return;

        _inventoryViewOnly = true;
        _currentNestId = string.Empty;
        SetOpen(true, true);
        RefreshTitle();
        RebuildSlots();

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.AOpenWindow?.Invoke(this);
    }

    public void CloseFromButton()
    {
        Close(true);
    }

    public void CloseFromExternal()
    {
        Close(false);
    }

    public void CloseFromShortcut()
    {
        Close(true);
    }

    private void CloseByOtherWindow(MonoBehaviour other)
    {
        if (!_opened)
            return;

        if (other != this)
            Close(false);
    }

    private void Close(bool releaseCursor)
    {
        if (!_opened)
            return;

        _currentNestId = string.Empty;
        _inventoryViewOnly = false;
        ClearSlots();
        SetOpen(false, releaseCursor);
    }

    private void Update()
    {
        if (!_opened)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            Close(true);
    }

    private void HandleStateChanged()
    {
        if (!_opened)
            return;

        if (_selectingEgg)
            return;

        if (!_inventoryViewOnly && (_manager == null || string.IsNullOrWhiteSpace(_currentNestId)))
        {
            Close(false);
            return;
        }

        if (!_inventoryViewOnly && _manager.GetNestState(_currentNestId) != null)
        {
            Close(false);
            return;
        }

        RebuildSlots();
    }

    private void RebuildSlots()
    {
        ClearSlots();

        bool anyAvailable = false;

        if (_manager != null && _slotPrefab != null && TryGetSlotParent(out Transform slotParent))
        {
            foreach (EggInventoryEntry owned in _manager.GetOwnedEggs())
            {
                if (owned == null || owned.amount <= 0)
                    continue;

                if (!_manager.TryGetDefinition(owned.eggId, out EggDefinition definition))
                    continue;

                EggNestSelectionSlot slot = Instantiate(_slotPrefab, slotParent);
                if (slot == null)
                    continue;

                slot.Init(definition, owned.amount, _inventoryViewOnly ? null : TrySelectEgg, !_inventoryViewOnly);
                anyAvailable = true;
            }

            if (_inventoryViewOnly)
            {
                foreach (EggNestState nest in _manager.GetActiveNests())
                {
                    if (nest == null || string.IsNullOrWhiteSpace(nest.eggId))
                        continue;

                    if (!_manager.TryGetDefinition(nest.eggId, out EggDefinition definition))
                        continue;

                    EggNestSelectionSlot slot = Instantiate(_slotPrefab, slotParent);
                    if (slot == null)
                        continue;

                    slot.InitIncubating(definition, _manager.GetRemainingSeconds(nest.nestId));
                    anyAvailable = true;
                }
            }
        }

        if (_emptyState != null)
        {
            RefreshEmptyState();
            _emptyState.SetActive(!anyAvailable);
        }

        RefreshSlotsLayout();
    }

    private void ClearSlots()
    {
        if (!TryGetSlotParent(out Transform root))
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void TrySelectEgg(string eggId)
    {
        if (string.IsNullOrWhiteSpace(eggId))
            return;

        if (_inventoryViewOnly || _manager == null || string.IsNullOrWhiteSpace(_currentNestId))
            return;

        bool started;
        _selectingEgg = true;
        try
        {
            started = _manager.TryStartIncubation(_currentNestId, eggId);
        }
        finally
        {
            _selectingEgg = false;
        }

        if (started)
        {
            Close(true);
            return;
        }

        RebuildSlots();
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
            _manager.StateChanged -= HandleStateChanged;

        _manager = manager;
        _manager.StateChanged += HandleStateChanged;
    }

    private void ConfigurePrefabReferences()
    {
        EnsureCloseButton();
    }

    private bool TryGetSlotParent(out Transform slotParent)
    {
        if (_slotsRoot != null)
        {
            slotParent = _slotsRoot;
            return true;
        }

        slotParent = null;
        return false;
    }

    private void RefreshSlotsLayout()
    {
        if (_slotsRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_slotsRoot);
            if (_scrollRect != null)
                _scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private void RefreshTitle()
    {
        if (_nestLabel == null)
            return;

        _nestLabel.text = _inventoryViewOnly
            ? EggFeatureLocalization.Text(InventoryTitleKey, InventoryTitleFallbackRu, "Egg inventory")
            : EggFeatureLocalization.Text(SelectionTitleKey, SelectionTitleFallbackRu, "Select egg");
    }

    private void RefreshEmptyState()
    {
        if (_emptyState == null)
            return;

        RectTransform rect = _emptyState.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.18f, 0.38f);
            rect.anchorMax = new Vector2(0.82f, 0.58f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        Text text = _emptyState.GetComponent<Text>();
        if (text == null)
            return;

        Font font = Resources.Load<Font>("Fonts/RussoOne-Regular");
        if (font != null)
            text.font = font;

        text.text = EggFeatureLocalization.Text(EmptyStateKey, EmptyStateFallbackRu, "No eggs available");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontStyle = FontStyle.Normal;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = 32;
        text.raycastTarget = false;
    }

    private void EnsureCloseButton()
    {
        if (_closeButton == null)
            return;

        _closeButton.onClick.RemoveListener(CloseFromButton);
        _closeButton.onClick.AddListener(CloseFromButton);
    }

    private void BindWindowCloseEvent()
    {
        if (PlayerInput.Instance == null)
            return;

        PlayerInput.Instance.AOpenWindow -= CloseByOtherWindow;
        PlayerInput.Instance.AOpenWindow += CloseByOtherWindow;
    }

    private void SetOpen(bool open, bool updateCursor)
    {
        _opened = open;

        if (_panel != null)
            _panel.SetActive(open);

        if (!updateCursor || !_setCursorWhenOpen)
            return;

        if (ControlManager.Instance == null || ControlManager.Instance.UseTouchControl)
            return;

        if (open)
            ControlManager.Instance.CursorActive = true;
        else
            ForceReleaseCursor();
    }

    private static void ForceReleaseCursor()
    {
        if (ControlManager.Instance != null && !ControlManager.Instance.UseTouchControl)
        {
            for (int i = 0; i < 8 && ControlManager.Instance.CursorActive; i++)
                ControlManager.Instance.CursorActive = false;
        }
    }
}
