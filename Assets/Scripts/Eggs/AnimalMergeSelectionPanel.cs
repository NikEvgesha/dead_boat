using UnityEngine;
using UnityEngine.UI;

public class AnimalMergeSelectionPanel : MonoBehaviour
{
    private static AnimalMergeSelectionPanel _instance;
    public static AnimalMergeSelectionPanel Instance => _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private AnimalPlacementSelectionSlot _slotPrefab;
    [SerializeField] private Text _headerLabel;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private bool _setCursorWhenOpen = true;
    [SerializeField] private bool _createTemporaryUiIfMissing = true;

    private EggHatchingManager _manager;
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

    public void Open()
    {
        BindWindowCloseEvent();
        TryBindManager();

        if (_manager == null)
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

    private void CloseByOtherWindow(MonoBehaviour other)
    {
        if (_opened && other != this)
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
        if (_opened)
            RebuildSlots();
    }

    private void RebuildSlots()
    {
        ClearSlots();

        if (_headerLabel != null)
            _headerLabel.text = "Merge animals";

        bool anyAvailable = false;
        if (_manager != null && _grid != null && _slotPrefab != null)
        {
            foreach (AnimalInventoryEntry owned in _manager.GetOwnedAnimals())
            {
                if (owned == null || owned.amount < 2)
                    continue;

                string animalId = owned.animalId;
                int stage = Mathf.Max(1, owned.stage);
                if (!_manager.CanMergeAnimal(animalId, stage))
                    continue;

                if (!_manager.TryGetAnimalDetails(animalId, stage, out string title, out _))
                    continue;

                AnimalPlacementSelectionSlot slot = _grid.SpawnObject<AnimalPlacementSelectionSlot>(_slotPrefab.gameObject);
                if (slot == null)
                    continue;

                slot.Init(animalId, stage, title, $"Merge to S{stage + 1}", owned.amount, TryMergeAnimal);
                anyAvailable = true;
            }
        }

        if (_emptyState != null)
            _emptyState.SetActive(!anyAvailable);

        if (_grid != null)
            _grid.RefreshLayout();
    }

    private void TryMergeAnimal(string animalId, int stage)
    {
        if (_manager != null && _manager.TryMergeAnimal(animalId, stage))
            RebuildSlots();
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
            _grid = EggTemporaryUIFactory.EnsureGrid(_panel, "TemporaryAnimalMergeGrid");

        if (_slotPrefab == null)
            _slotPrefab = EggTemporaryUIFactory.EnsureAnimalSlotTemplate(this);

        if (_emptyState == null)
            _emptyState = EggTemporaryUIFactory.EnsureEmptyState(_panel, "No mergeable animals");

        if (_headerLabel == null)
            _headerLabel = EggTemporaryUIFactory.EnsureHeader(_panel, "TemporaryAnimalMergeHeader", "Merge animals");

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
