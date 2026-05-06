using UnityEngine;
using UnityEngine.UI;

public class EggNestSelectionPanel : MonoBehaviour
{
    private static EggNestSelectionPanel _instance;
    public static EggNestSelectionPanel Instance => _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private EggNestSelectionSlot _slotPrefab;
    [SerializeField] private Text _nestLabel;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private bool _setCursorWhenOpen = true;
    [SerializeField] private bool _createTemporaryUiIfMissing = true;

    private EggHatchingManager _manager;
    private string _currentNestId;
    private bool _opened;

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

        EnsureTemporaryUiIfNeeded();
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

    public void CloseFromButton()
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
        ClearSlots();
        SetOpen(false, releaseCursor);
    }

    private void HandleStateChanged()
    {
        if (!_opened)
            return;

        if (_manager == null || string.IsNullOrWhiteSpace(_currentNestId))
        {
            Close(false);
            return;
        }

        if (_manager.GetNestState(_currentNestId) != null)
        {
            Close(false);
            return;
        }

        RebuildSlots();
    }

    private void RebuildSlots()
    {
        ClearSlots();

        if (_nestLabel != null)
            _nestLabel.text = EggTemporaryUIFactory.FormatHeader("Select egg", _currentNestId);

        bool anyAvailable = false;

        if (_manager != null && _grid != null && _slotPrefab != null)
        {
            foreach (EggInventoryEntry owned in _manager.GetOwnedEggs())
            {
                if (owned == null || owned.amount <= 0)
                    continue;

                if (!_manager.TryGetDefinition(owned.eggId, out EggDefinition definition))
                    continue;

                EggNestSelectionSlot slot = _grid.SpawnObject<EggNestSelectionSlot>(_slotPrefab.gameObject);
                if (slot == null)
                    continue;

                slot.Init(definition, owned.amount, TrySelectEgg);
                anyAvailable = true;
            }
        }

        if (_emptyState != null)
            _emptyState.SetActive(!anyAvailable);

        if (_grid != null)
            _grid.RefreshLayout();
    }

    private void ClearSlots()
    {
        if (_grid == null)
            return;

        Transform root = _grid.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform row = root.GetChild(i);
            row.SetParent(null, false);
            Destroy(row.gameObject);
        }
    }

    private void TrySelectEgg(string eggId)
    {
        if (string.IsNullOrWhiteSpace(eggId))
            return;

        if (_manager == null || string.IsNullOrWhiteSpace(_currentNestId))
            return;

        if (_manager.TryStartIncubation(_currentNestId, eggId))
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

    private void EnsureTemporaryUiIfNeeded()
    {
        if (!_createTemporaryUiIfMissing || _panel == null)
            return;

        if (_grid == null)
            _grid = EggTemporaryUIFactory.EnsureGrid(_panel, "TemporaryEggSelectionGrid");

        if (_slotPrefab == null)
            _slotPrefab = EggTemporaryUIFactory.EnsureEggSlotTemplate(this);

        if (_emptyState == null)
            _emptyState = EggTemporaryUIFactory.EnsureEmptyState(_panel, "No eggs in storage");

        if (_nestLabel == null)
            _nestLabel = EggTemporaryUIFactory.EnsureHeader(_panel, "TemporaryEggSelectionHeader", "Select egg");

        EggTemporaryUIFactory.EnsureCloseButton(_panel, CloseFromButton);
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

        if (ControlManager.Instance != null && !ControlManager.Instance.UseTouchControl)
            ControlManager.Instance.CursorActive = open;
    }
}
