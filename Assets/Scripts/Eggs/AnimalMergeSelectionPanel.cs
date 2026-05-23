using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnimalMergeSelectionPanel : MonoBehaviour
{
    private enum SelectionSide
    {
        Left,
        Right
    }

    private static AnimalMergeSelectionPanel _instance;
    public static AnimalMergeSelectionPanel Instance => _instance;

    [Header("Root")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Text _headerLabel;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private bool _setCursorWhenOpen = true;

    [Header("Merge Slots")]
    [SerializeField] private Button _leftSlotButton;
    [SerializeField] private Button _rightSlotButton;
    [SerializeField] private Image _leftAnimalIcon;
    [SerializeField] private Image _rightAnimalIcon;
    [SerializeField] private Text _leftAnimalLabel;
    [SerializeField] private Text _rightAnimalLabel;
    [SerializeField] private Text _leftStageLabel;
    [SerializeField] private Text _rightStageLabel;

    [Header("Actions")]
    [SerializeField] private Button _centerSelectButton;
    [SerializeField] private Button _mergeButton;
    [SerializeField] private Button _collectButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _skipAdButton;
    [SerializeField] private Button _skipCoinsButton;
    [SerializeField] private Text _centerButtonText;
    [SerializeField] private Text _mergeButtonText;
    [SerializeField] private Text _collectButtonText;
    [SerializeField] private Text _cancelButtonText;
    [SerializeField] private Text _skipAdButtonText;
    [SerializeField] private Text _skipCoinsButtonText;
    [SerializeField] private Text _timerText;

    [Header("Inventory Picker")]
    [SerializeField] private GameObject _inventoryRoot;
    [SerializeField] private ScrollRect _inventoryScrollRect;
    [SerializeField] private RectTransform _inventorySlotsRoot;
    [SerializeField] private AnimalMergeInventorySlot _inventorySlotPrefab;
    [SerializeField] private Button _inventoryCloseButton;
    [SerializeField] private Text _inventoryHeaderLabel;
    [SerializeField] private GameObject _inventoryEmptyState;

    [Header("Ads")]
    [SerializeField] private string _skipRewardId = "AnimalMergeSkip";

    [Header("Result Animation")]
    [SerializeField, Min(0.05f)] private float _resultMergeMoveDuration = 0.55f;
    [SerializeField, Min(0f)] private float _resultMergeHoldDuration = 0.12f;
    [SerializeField, Min(1f)] private float _resultPopScale = 1.2f;
    [SerializeField, Min(0.05f)] private float _resultPopDuration = 0.28f;

    private EggHatchingManager _manager;
    private bool _opened;
    private string _leftAnimalId;
    private int _leftStage = 1;
    private string _rightAnimalId;
    private int _rightStage = 1;
    private SelectionSide _selectionSide;
    private bool _pickerOpen;
    private bool _skipAdInProgress;
    private Coroutine _resultAnimationCoroutine;
    private bool _resultAnimationPlaying;
    private RectTransform _leftSlotRect;
    private RectTransform _rightSlotRect;
    private Vector2 _leftSlotStartPosition;
    private Vector2 _rightSlotStartPosition;
    private Vector3 _leftSlotStartScale = Vector3.one;
    private Vector3 _rightSlotStartScale = Vector3.one;
    private bool _slotLayoutCaptured;

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
        CaptureSlotLayout();
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

        StopResultAnimation();

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
            return;
        }

        RefreshTimer();
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

        ClearInventorySlots();
        _pickerOpen = false;
        ClearSelection();
        StopResultAnimation();

        if (_inventoryRoot != null)
            _inventoryRoot.SetActive(false);

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
        AnimalMergeState merge = _manager != null ? _manager.GetAnimalMergeState() : null;
        bool hasMerge = merge != null;
        bool ready = hasMerge && _manager.IsAnimalMergeReady();
        bool canStart = !hasMerge && CanStartSelectedMerge();

        if (!ready)
            StopResultAnimation();

        if (_headerLabel != null)
            _headerLabel.text = EggFeatureLocalization.Text("UI/AnimalMerge/Title", "Слияние животных", "Merge animals");

        if (hasMerge && ready)
        {
            if (!_resultAnimationPlaying && !merge.resultAnimationShown)
                StartReadyResultAnimation(merge);
            else if (!_resultAnimationPlaying)
                ShowReadyResult(merge);
        }
        else if (hasMerge)
        {
            RestoreSlotLayout();
            SetSlotVisual(_leftAnimalIcon, _leftAnimalLabel, _leftStageLabel, merge.animalId, merge.stage);
            SetSlotVisual(_rightAnimalIcon, _rightAnimalLabel, _rightStageLabel, merge.animalId, merge.stage);
        }
        else
        {
            RestoreSlotLayout();
            SetSlotVisual(_leftAnimalIcon, _leftAnimalLabel, _leftStageLabel, _leftAnimalId, _leftStage);
            SetSlotVisual(_rightAnimalIcon, _rightAnimalLabel, _rightStageLabel, _rightAnimalId, _rightStage);
        }

        SetButtonActive(_leftSlotButton, true);
        SetButtonActive(_rightSlotButton, !hasMerge || !ready || _resultAnimationPlaying);
        SetButtonInteractable(_leftSlotButton, !hasMerge);
        SetButtonInteractable(_rightSlotButton, !hasMerge);
        SetButtonActive(_centerSelectButton, !hasMerge && (!HasLeftSelection() || !HasRightSelection()));
        SetButtonActive(_mergeButton, !hasMerge && canStart);
        SetButtonActive(_collectButton, ready);
        SetButtonActive(_cancelButton, hasMerge && !ready);
        SetButtonActive(_skipAdButton, hasMerge && !ready);
        SetButtonActive(_skipCoinsButton, hasMerge && !ready);

        if (_mergeButton != null)
            _mergeButton.interactable = canStart;
        if (_collectButton != null)
            _collectButton.interactable = ready && !_resultAnimationPlaying;
        if (_skipCoinsButton != null)
        {
            int skipCost = _manager != null ? _manager.GetAnimalMergeSkipCostCoins() : 0;
            _skipCoinsButton.interactable = _manager != null &&
                                            (skipCost <= 0 ||
                                             CurrencyManager.Instance != null &&
                                             CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Coins, skipCost));
        }

        if (_centerButtonText != null)
            _centerButtonText.text = EggFeatureLocalization.Text("UI/AnimalMerge/Select", "Выбрать", "Select");
        if (_mergeButtonText != null)
            _mergeButtonText.text = EggFeatureLocalization.Text("UI/AnimalMerge/Merge", "Скрестить", "Merge");
        if (_collectButtonText != null)
            _collectButtonText.text = EggFeatureLocalization.Text("UI/AnimalMerge/Collect", "Забрать", "Collect");
        if (_cancelButtonText != null)
            _cancelButtonText.text = EggFeatureLocalization.Text("UI/AnimalMerge/Cancel", "Отмена", "Cancel");
        if (_skipAdButtonText != null)
            _skipAdButtonText.text = EggFeatureLocalization.Text("UI/AnimalMerge/SkipAd", "Пропустить за рекламу", "Skip ad");
        if (_skipCoinsButtonText != null && _manager != null)
            _skipCoinsButtonText.text = EggFeatureLocalization.Format("UI/AnimalMerge/SkipCoinsFormat", "Пропустить {0}", "Skip {0}", Mathf.Max(0, _manager.GetAnimalMergeSkipCostCoins()));

        if (_emptyState != null)
            _emptyState.SetActive(!hasMerge && !HasAnyMergeCandidate());

        RefreshTimer();
    }

    private void StartReadyResultAnimation(AnimalMergeState merge)
    {
        if (merge == null)
            return;

        StopResultAnimation();
        RestoreSlotLayout();
        SetSlotVisual(_leftAnimalIcon, _leftAnimalLabel, _leftStageLabel, merge.animalId, merge.stage);
        SetSlotVisual(_rightAnimalIcon, _rightAnimalLabel, _rightStageLabel, merge.animalId, merge.stage);

        _resultAnimationPlaying = true;
        merge.resultAnimationShown = _manager != null && _manager.MarkAnimalMergeResultAnimationShown();
        _resultAnimationCoroutine = StartCoroutine(PlayReadyResultAnimation(merge.animalId, merge.stage));
    }

    private IEnumerator PlayReadyResultAnimation(string animalId, int sourceStage)
    {
        CaptureSlotLayout();

        Vector2 leftStart = _leftSlotRect != null ? _leftSlotRect.anchoredPosition : _leftSlotStartPosition;
        Vector2 rightStart = _rightSlotRect != null ? _rightSlotRect.anchoredPosition : _rightSlotStartPosition;
        Vector2 center = Vector2.zero;
        float duration = Mathf.Max(0.05f, _resultMergeMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;

            if (_leftSlotRect != null)
            {
                _leftSlotRect.anchoredPosition = Vector2.LerpUnclamped(leftStart, center, t);
                _leftSlotRect.localScale = _leftSlotStartScale * pulse;
            }

            if (_rightSlotRect != null)
            {
                _rightSlotRect.anchoredPosition = Vector2.LerpUnclamped(rightStart, center, t);
                _rightSlotRect.localScale = _rightSlotStartScale * pulse;
            }

            elapsed += GetAnimationDeltaTime();
            yield return null;
        }

        if (_leftSlotRect != null)
        {
            _leftSlotRect.anchoredPosition = center;
            _leftSlotRect.localScale = _leftSlotStartScale;
        }

        if (_rightSlotRect != null)
        {
            _rightSlotRect.anchoredPosition = center;
            _rightSlotRect.localScale = _rightSlotStartScale;
        }

        if (_resultMergeHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(_resultMergeHoldDuration);

        AnimalMergeState currentMerge = _manager != null ? _manager.GetAnimalMergeState() : null;
        if (currentMerge != null && _manager.IsAnimalMergeReady())
        {
            ShowReadyResult(currentMerge);
            SetButtonActive(_rightSlotButton, false);
            yield return PlayResultPopAnimation();
        }

        _resultAnimationPlaying = false;
        _resultAnimationCoroutine = null;

        AnimalMergeState merge = _manager != null ? _manager.GetAnimalMergeState() : null;
        if (merge != null && _manager.IsAnimalMergeReady())
            RefreshView();
    }

    private IEnumerator PlayResultPopAnimation()
    {
        if (_leftSlotRect == null)
            yield break;

        Vector3 baseScale = _leftSlotStartScale;
        Vector3 targetScale = baseScale * Mathf.Max(1f, _resultPopScale);
        float halfDuration = Mathf.Max(0.025f, _resultPopDuration * 0.5f);

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            _leftSlotRect.localScale = Vector3.LerpUnclamped(baseScale, targetScale, t);
            elapsed += GetAnimationDeltaTime();
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
            _leftSlotRect.localScale = Vector3.LerpUnclamped(targetScale, baseScale, t);
            elapsed += GetAnimationDeltaTime();
            yield return null;
        }

        _leftSlotRect.localScale = baseScale;
    }

    private void ShowReadyResult(AnimalMergeState merge)
    {
        if (merge == null)
            return;

        CaptureSlotLayout();
        SetSlotVisual(_leftAnimalIcon, _leftAnimalLabel, _leftStageLabel, merge.animalId, merge.stage + 1);
        SetSlotVisual(_rightAnimalIcon, _rightAnimalLabel, _rightStageLabel, null, 1);

        if (_leftSlotRect != null)
        {
            _leftSlotRect.anchoredPosition = Vector2.zero;
            _leftSlotRect.localScale = _leftSlotStartScale;
        }

        if (_rightSlotRect != null)
        {
            _rightSlotRect.anchoredPosition = _rightSlotStartPosition;
            _rightSlotRect.localScale = _rightSlotStartScale;
        }
    }

    private void StopResultAnimation()
    {
        if (_resultAnimationCoroutine != null)
        {
            StopCoroutine(_resultAnimationCoroutine);
            _resultAnimationCoroutine = null;
        }

        _resultAnimationPlaying = false;
    }

    private void CaptureSlotLayout()
    {
        if (_slotLayoutCaptured)
            return;

        _leftSlotRect = _leftSlotButton != null ? _leftSlotButton.transform as RectTransform : null;
        _rightSlotRect = _rightSlotButton != null ? _rightSlotButton.transform as RectTransform : null;

        if (_leftSlotRect != null)
        {
            _leftSlotStartPosition = _leftSlotRect.anchoredPosition;
            _leftSlotStartScale = _leftSlotRect.localScale;
        }

        if (_rightSlotRect != null)
        {
            _rightSlotStartPosition = _rightSlotRect.anchoredPosition;
            _rightSlotStartScale = _rightSlotRect.localScale;
        }

        _slotLayoutCaptured = true;
    }

    private void RestoreSlotLayout()
    {
        CaptureSlotLayout();

        if (_leftSlotRect != null)
        {
            _leftSlotRect.anchoredPosition = _leftSlotStartPosition;
            _leftSlotRect.localScale = _leftSlotStartScale;
        }

        if (_rightSlotRect != null)
        {
            _rightSlotRect.anchoredPosition = _rightSlotStartPosition;
            _rightSlotRect.localScale = _rightSlotStartScale;
        }
    }

    private void RefreshTimer()
    {
        if (_timerText == null || _manager == null)
            return;

        AnimalMergeState merge = _manager.GetAnimalMergeState();
        if (merge == null)
        {
            _timerText.text = string.Empty;
            return;
        }

        int remaining = _manager.GetAnimalMergeRemainingSeconds();
        _timerText.text = remaining <= 0
            ? EggFeatureLocalization.Text("Eggs/ReadyToCollect", "Готово", "Ready")
            : FormatSeconds(remaining);
    }

    private void OpenLeftPicker()
    {
        OpenInventoryPicker(SelectionSide.Left);
    }

    private void OpenRightPicker()
    {
        OpenInventoryPicker(SelectionSide.Right);
    }

    private void OpenCenterPicker()
    {
        OpenInventoryPicker(SelectionSide.Left);
    }

    private void OpenInventoryPicker(SelectionSide side)
    {
        if (_manager == null || _manager.GetAnimalMergeState() != null)
            return;

        _selectionSide = side;
        _pickerOpen = true;

        if (_inventoryRoot != null)
            _inventoryRoot.SetActive(true);

        RebuildInventoryPicker();
    }

    private void CloseInventoryPicker()
    {
        _pickerOpen = false;
        ClearInventorySlots();

        if (_inventoryRoot != null)
            _inventoryRoot.SetActive(false);
    }

    private void RebuildInventoryPicker()
    {
        ClearInventorySlots();

        if (_inventoryHeaderLabel != null)
            _inventoryHeaderLabel.text = EggFeatureLocalization.Text("UI/AnimalMerge/SelectAnimal", "Выбери животное", "Select animal");

        bool anyAvailable = false;

        if (_manager != null && _inventorySlotPrefab != null && _inventorySlotsRoot != null)
        {
            foreach (AnimalInventoryEntry owned in _manager.GetOwnedAnimals())
            {
                if (!CanShowInventoryEntry(owned))
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

    private bool CanShowInventoryEntry(AnimalInventoryEntry owned)
    {
        if (owned == null || owned.amount <= 0)
            return false;

        int stage = Mathf.Max(1, owned.stage);
        return _manager.CanMergeAnimal(owned.animalId, stage);
    }

    private void SelectAnimal(string animalId, int stage)
    {
        if (string.IsNullOrWhiteSpace(animalId))
            return;

        int safeStage = Mathf.Max(1, stage);
        _leftAnimalId = animalId;
        _leftStage = safeStage;
        _rightAnimalId = animalId;
        _rightStage = safeStage;

        CloseInventoryPicker();
        RefreshView();
    }

    private void StartMerge()
    {
        if (_manager == null || !CanStartSelectedMerge())
            return;

        if (_manager.TryStartAnimalMerge(_leftAnimalId, _leftStage))
        {
            ClearSelection();
            RefreshView();
        }
    }

    private void CollectMerge()
    {
        if (_manager != null && _manager.TryCollectAnimalMergeResult())
            RefreshView();
    }

    private void CancelMerge()
    {
        if (_manager != null && _manager.TryCancelAnimalMerge())
            RefreshView();
    }

    private void SkipWithCoins()
    {
        if (_manager != null && _manager.TrySkipAnimalMergeWithCoins())
            RefreshView();
    }

    private void SkipWithAd()
    {
        if (_manager == null || _skipAdInProgress)
            return;

        if (AdsManager.Instance == null)
        {
            _manager.TryFinishAnimalMergeNow();
            RefreshView();
            return;
        }

        _skipAdInProgress = true;
        AdsManager.Instance.ShowRewardedAd(
            _skipRewardId,
            success =>
            {
                _skipAdInProgress = false;
                if (success)
                {
                    _manager.TryFinishAnimalMergeNow();
                    RefreshView();
                }
            });
    }

    private bool CanStartSelectedMerge()
    {
        return _manager != null &&
               HasLeftSelection() &&
               HasRightSelection() &&
               _leftAnimalId == _rightAnimalId &&
               _leftStage == _rightStage &&
               _manager.CanMergeAnimal(_leftAnimalId, _leftStage);
    }

    private bool HasAnyMergeCandidate()
    {
        if (_manager == null)
            return false;

        foreach (AnimalInventoryEntry owned in _manager.GetOwnedAnimals())
        {
            if (owned == null)
                continue;

            if (_manager.CanMergeAnimal(owned.animalId, Mathf.Max(1, owned.stage)))
                return true;
        }

        return false;
    }

    private void SetSlotVisual(Image icon, Text title, Text stageLabel, string animalId, int stage)
    {
        AnimalDefinition definition = null;
        bool hasAnimal = !string.IsNullOrWhiteSpace(animalId) &&
                         _manager != null &&
                         _manager.TryGetAnimalDefinition(animalId, out definition);

        if (hasAnimal)
        {
            if (icon != null)
            {
                icon.sprite = definition.icon;
                icon.enabled = definition.icon != null;
            }

            if (title != null)
                title.text = EggFeatureLocalization.AnimalTitle(definition);

            if (stageLabel != null)
                stageLabel.text = EggFeatureLocalization.StageShort(stage);

            return;
        }

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (title != null)
            title.text = EggFeatureLocalization.Text("UI/AnimalMerge/Select", "Выбрать", "Select");

        if (stageLabel != null)
            stageLabel.text = string.Empty;
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
        BindButton(_leftSlotButton, OpenLeftPicker);
        BindButton(_rightSlotButton, OpenRightPicker);
        BindButton(_centerSelectButton, OpenCenterPicker);
        BindButton(_mergeButton, StartMerge);
        BindButton(_collectButton, CollectMerge);
        BindButton(_cancelButton, CancelMerge);
        BindButton(_skipAdButton, SkipWithAd);
        BindButton(_skipCoinsButton, SkipWithCoins);
        BindButton(_inventoryCloseButton, CloseInventoryPicker);
    }

    private bool HasLeftSelection()
    {
        return !string.IsNullOrWhiteSpace(_leftAnimalId);
    }

    private bool HasRightSelection()
    {
        return !string.IsNullOrWhiteSpace(_rightAnimalId);
    }

    private void ClearSelection()
    {
        _leftAnimalId = string.Empty;
        _leftStage = 1;
        _rightAnimalId = string.Empty;
        _rightStage = 1;
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

    private static void SetButtonActive(Button button, bool active)
    {
        if (button != null)
            button.gameObject.SetActive(active);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int seconds = Mathf.Max(0, totalSeconds);
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int secs = seconds % 60;
        return $"{hours:00}:{minutes:00}:{secs:00}";
    }

    private static float GetAnimationDeltaTime()
    {
        return Mathf.Max(Time.unscaledDeltaTime, 0.016f);
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
