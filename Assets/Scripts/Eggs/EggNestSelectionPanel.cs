using UnityEngine;
using UnityEngine.UI;

public class EggNestSelectionPanel : MonoBehaviour
{
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

        _currentNestId = nestId;
        SetOpen(true, true);
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
        }

        if (_emptyState != null)
            _emptyState.SetActive(!anyAvailable);

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
