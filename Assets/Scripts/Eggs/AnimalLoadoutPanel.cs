using UnityEngine;
using UnityEngine.UI;

public sealed class AnimalLoadoutPanel : MonoBehaviour
{
    private static AnimalLoadoutPanel _instance;
    public static AnimalLoadoutPanel Instance => _instance;

    [Header("Root")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Text _headerLabel;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private bool _setCursorWhenOpen = true;

    [Header("Slots")]
    [SerializeField] private AnimalLoadoutSlotView[] _slotViews;

    [Header("Inventory Picker")]
    [SerializeField] private GameObject _inventoryRoot;
    [SerializeField] private ScrollRect _inventoryScrollRect;
    [SerializeField] private RectTransform _inventorySlotsRoot;
    [SerializeField] private AnimalMergeInventorySlot _inventorySlotPrefab;
    [SerializeField] private Button _inventoryCloseButton;
    [SerializeField] private Text _inventoryHeaderLabel;
    [SerializeField] private GameObject _inventoryEmptyState;

    private EggHatchingManager _manager;
    private bool _opened;
    private bool _pickerOpen;
    private int _selectedSlotIndex = -1;

    public bool IsOpen => _opened;

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

        BindButtons();
        _panel.SetActive(false);

        if (_inventoryRoot != null)
            _inventoryRoot.SetActive(false);
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

    private void Update()
    {
        if (!_opened)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (_pickerOpen)
                CloseInventoryPicker();
            else
                Close(true);
        }
    }

    public void Open()
    {
        BindWindowCloseEvent();
        TryBindManager();

        if (_manager == null || !_manager.HasHatchedAnimal())
            return;

        SetOpen(true, true);
        RefreshView();

        if (PlayerInput.Instance != null)
            PlayerInput.Instance.AOpenWindow?.Invoke(this);
    }

    public void CloseFromButton()
    {
        Close(true);
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

        CloseInventoryPicker();
        SetOpen(false, releaseCursor);
    }

    private void HandleStateChanged()
    {
        if (!_opened)
            return;

        RefreshView();
        if (_pickerOpen)
            RebuildInventoryPicker();
    }

    private void RefreshView()
    {
        if (_headerLabel != null)
            _headerLabel.text = "Animal team";

        int slotCount = _manager != null ? _manager.AnimalLoadoutSlotCount : 0;
        bool anyPlaced = false;

        if (_slotViews != null)
        {
            for (int i = 0; i < _slotViews.Length; i++)
            {
                AnimalLoadoutSlotView slotView = _slotViews[i];
                if (slotView == null)
                    continue;

                bool active = i < slotCount;
                slotView.gameObject.SetActive(active);
                if (!active)
                    continue;

                PlacedAnimalState placed = _manager.GetAnimalLoadoutSlot(i);
                anyPlaced |= placed != null;
                slotView.Init(i, placed, _manager, OpenInventoryPicker, ClearSlot);
            }
        }

        if (_emptyState != null)
            _emptyState.SetActive(!anyPlaced);
    }

    private void OpenInventoryPicker(int slotIndex)
    {
        if (_manager == null || slotIndex < 0 || slotIndex >= _manager.AnimalLoadoutSlotCount)
            return;

        _selectedSlotIndex = slotIndex;
        _pickerOpen = true;

        if (_inventoryRoot != null)
            _inventoryRoot.SetActive(true);

        RebuildInventoryPicker();
    }

    private void CloseInventoryPicker()
    {
        _pickerOpen = false;
        _selectedSlotIndex = -1;
        ClearInventorySlots();

        if (_inventoryRoot != null)
            _inventoryRoot.SetActive(false);
    }

    private void RebuildInventoryPicker()
    {
        ClearInventorySlots();

        if (_inventoryHeaderLabel != null)
            _inventoryHeaderLabel.text = "Select animal";

        bool anyAvailable = false;
        if (_manager != null && _inventorySlotPrefab != null && _inventorySlotsRoot != null)
        {
            foreach (AnimalInventoryEntry owned in _manager.GetOwnedAnimals())
            {
                if (owned == null || owned.amount <= 0)
                    continue;

                int stage = Mathf.Max(1, owned.stage);
                if (!_manager.TryGetAnimalDefinition(owned.animalId, out AnimalDefinition definition))
                    continue;

                if (!_manager.TryGetAnimalDetails(owned.animalId, stage, out _, out string detail))
                    continue;

                AnimalMergeInventorySlot slot = Instantiate(_inventorySlotPrefab, _inventorySlotsRoot);
                if (slot == null)
                    continue;

                slot.Init(definition, stage, owned.amount, detail, SelectAnimal);
                anyAvailable = true;
            }
        }

        if (_inventoryEmptyState != null)
            _inventoryEmptyState.SetActive(!anyAvailable);

        if (_inventorySlotsRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_inventorySlotsRoot);
            if (_inventoryScrollRect != null)
                _inventoryScrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private void SelectAnimal(string animalId, int stage)
    {
        if (_manager == null || _selectedSlotIndex < 0)
            return;

        if (_manager.TryAssignAnimalToLoadoutSlot(_selectedSlotIndex, animalId, stage))
        {
            CloseInventoryPicker();
            RefreshView();
        }
        else
        {
            RebuildInventoryPicker();
        }
    }

    private void ClearSlot(int slotIndex)
    {
        if (_manager != null && _manager.TryClearAnimalLoadoutSlot(slotIndex))
            RefreshView();
    }

    private void ClearInventorySlots()
    {
        if (_inventorySlotsRoot == null)
            return;

        for (int i = _inventorySlotsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _inventorySlotsRoot.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
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

    private void BindButtons()
    {
        BindButton(_closeButton, CloseFromButton);
        BindButton(_inventoryCloseButton, CloseInventoryPicker);
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

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
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
