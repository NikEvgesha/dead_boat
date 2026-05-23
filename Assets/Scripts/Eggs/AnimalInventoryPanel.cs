using UnityEngine;
using UnityEngine.UI;

public class AnimalInventoryPanel : MonoBehaviour
{
    private static AnimalInventoryPanel _instance;
    public static AnimalInventoryPanel Instance => _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _slotsRoot;
    [SerializeField] private AnimalInventorySlot _slotPrefab;
    [SerializeField] private Text _headerLabel;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private Button _closeButton;
    [SerializeField] private bool _setCursorWhenOpen = true;

    private EggHatchingManager _manager;
    private bool _opened;

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

        EnsureCloseButton();
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

    private void Update()
    {
        if (!_opened)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            Close(true);
    }

    public void OpenInventory()
    {
        BindWindowCloseEvent();
        TryBindManager();

        if (_manager == null || !_manager.HasHatchedAnimal())
            return;

        SetOpen(true, true);
        RebuildSlots();

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

        ClearSlots();
        SetOpen(false, releaseCursor);
    }

    private void HandleStateChanged()
    {
        if (!_opened)
            return;

        RebuildSlots();
    }

    private void RebuildSlots()
    {
        ClearSlots();

        if (_headerLabel != null)
            _headerLabel.text = EggFeatureLocalization.Text("UI/AnimalInventory/Title", "Животные", "Animals");

        bool anyAvailable = false;

        if (_manager != null && _slotPrefab != null && _slotsRoot != null)
        {
            foreach (AnimalInventoryEntry owned in _manager.GetOwnedAnimals())
            {
                if (owned == null || owned.amount <= 0)
                    continue;

                int stage = Mathf.Max(1, owned.stage);
                if (!_manager.TryGetAnimalDetails(owned.animalId, stage, out _, out string detail))
                    continue;

                if (!_manager.TryGetAnimalDefinition(owned.animalId, out AnimalDefinition definition))
                    continue;

                AnimalInventorySlot slot = Instantiate(_slotPrefab, _slotsRoot);
                if (slot == null)
                    continue;

                slot.Init(definition, stage, owned.amount, detail);
                anyAvailable = true;
            }
        }

        if (_emptyState != null)
            _emptyState.SetActive(!anyAvailable);

        if (_slotsRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_slotsRoot);
            if (_scrollRect != null)
                _scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private void ClearSlots()
    {
        if (_slotsRoot == null)
            return;

        for (int i = _slotsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _slotsRoot.GetChild(i);
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

        if (ControlManager.Instance != null && !ControlManager.Instance.UseTouchControl)
            ControlManager.Instance.CursorActive = open;
    }

    private static void ForceReleaseCursor()
    {
        if (ControlManager.Instance != null && !ControlManager.Instance.UseTouchControl)
            ControlManager.Instance.CursorActive = false;
    }
}
