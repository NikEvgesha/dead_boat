using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfessionSelectionPanel : MonoBehaviour
{
    private static ProfessionSelectionPanel _instance;
    public static ProfessionSelectionPanel Instance => _instance;

    [Header("Data")]
    [SerializeField] private ProfessionCatalog _catalog;

    [Header("Root")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private bool _setCursorWhenOpen = true;
    [SerializeField] private bool _createTemporaryUiIfMissing = true;

    [Header("Texts")]
    [SerializeField] private Text _professionTitleText;
    [SerializeField] private Text _professionDescriptionText;
    [SerializeField] private Text _starterItemsText;
    [SerializeField] private Text _perksText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _messageText;
    [SerializeField] private Text _unlockPriceText;

    [Header("Visuals")]
    [SerializeField] private Image _professionIcon;
    [SerializeField] private GameObject _lockObject;

    [Header("Buttons")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _unlockRandomButton;
    [SerializeField] private Button _closeButton;

    [Header("Unlock")]
    [SerializeField] private int _unlockRandomPriceGems = 25;
    [SerializeField] private bool _scaleUnlockPriceByOpenedCount = true;
    [SerializeField] private int _unlockPriceStepGems = 5;
    [SerializeField] private int _unlockPriceMaxGems = 120;

    private readonly List<ProfessionDefinition> _definitions = new();
    private int _currentIndex;
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

        EnsureTemporaryUiIfNeeded();
        _panel.SetActive(false);
    }

    private void Start()
    {
        BindButtons();
        BindWindowCloseEvent();
        ProfessionService.ConfigureCatalog(_catalog);
        ProfessionService.StateChanged += HandleProfessionStateChanged;
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

        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(ShowNextProfession);

        if (_prevButton != null)
            _prevButton.onClick.RemoveListener(ShowPreviousProfession);

        if (_applyButton != null)
            _applyButton.onClick.RemoveListener(ApplySelectedProfession);

        if (_unlockRandomButton != null)
            _unlockRandomButton.onClick.RemoveListener(UnlockRandomProfession);

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(CloseFromButton);

        ProfessionService.StateChanged -= HandleProfessionStateChanged;
    }

    public void Open()
    {
        ProfessionService.ConfigureCatalog(_catalog);
        RebuildDefinitions();

        if (_definitions.Count == 0)
            return;

        _currentIndex = Mathf.Clamp(FindProfessionIndex(ProfessionService.CurrentProfessionId), 0, _definitions.Count - 1);
        SetOpen(true, true);
        RefreshView();

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

    private void ShowNextProfession()
    {
        if (_definitions.Count == 0)
            return;

        _currentIndex++;
        if (_currentIndex >= _definitions.Count)
            _currentIndex = 0;

        RefreshView();
    }

    private void ShowPreviousProfession()
    {
        if (_definitions.Count == 0)
            return;

        _currentIndex--;
        if (_currentIndex < 0)
            _currentIndex = _definitions.Count - 1;

        RefreshView();
    }

    private void ApplySelectedProfession()
    {
        ProfessionDefinition definition = GetCurrentDefinition();
        if (definition == null)
            return;

        if (!ProfessionService.TrySelectProfession(definition.professionId))
            return;

        SetMessage(ProfessionLocalization.FormatSelectedProfession(definition.title));
        RefreshView();
    }

    private void UnlockRandomProfession()
    {
        int unlockPrice = GetUnlockPriceGems();

        if (!ProfessionService.HasLockedProfessions())
        {
            SetMessage(ProfessionLocalization.MessageAllUnlocked);
            RefreshView();
            return;
        }

        if (!ProfessionService.TryUnlockRandomLockedProfession(unlockPrice, out ProfessionDefinition unlockedDefinition))
        {
            SetMessage(ProfessionLocalization.MessageUnlockFailed);
            RefreshView();
            return;
        }

        _currentIndex = FindProfessionIndex(unlockedDefinition.professionId);
        SetMessage(ProfessionLocalization.FormatUnlockedProfession(unlockedDefinition.title));
        RefreshView();
    }

    private void RefreshView()
    {
        ProfessionDefinition definition = GetCurrentDefinition();
        if (definition == null)
            return;

        bool unlocked = ProfessionService.IsUnlocked(definition.professionId);
        bool selected = ProfessionService.HasExplicitProfessionChoice() &&
            string.Equals(ProfessionService.CurrentProfessionId, definition.professionId, StringComparison.Ordinal);

        if (_professionTitleText != null)
            _professionTitleText.text = string.IsNullOrWhiteSpace(definition.title) ? definition.professionId : definition.title;

        if (_professionDescriptionText != null)
            _professionDescriptionText.text = string.IsNullOrWhiteSpace(definition.description)
                ? ProfessionLocalization.NoDescription
                : definition.description;

        if (_starterItemsText != null)
            _starterItemsText.text = ProfessionService.BuildStarterItemsSummary(definition);

        if (_perksText != null)
            _perksText.text = ProfessionService.BuildPerksSummary(definition);

        if (_statusText != null)
        {
            if (selected)
                _statusText.text = ProfessionLocalization.StatusEquipped;
            else if (unlocked)
                _statusText.text = ProfessionLocalization.StatusOpened;
            else
                _statusText.text = ProfessionLocalization.StatusLocked;
        }

        if (_professionIcon != null)
        {
            _professionIcon.enabled = definition.icon != null;
            _professionIcon.sprite = definition.icon;
        }

        if (_lockObject != null)
            _lockObject.SetActive(!unlocked);

        if (_applyButton != null)
        {
            _applyButton.gameObject.SetActive(unlocked);
            _applyButton.interactable = unlocked && !selected;
        }

        if (_unlockRandomButton != null)
            _unlockRandomButton.interactable = ProfessionService.HasLockedProfessions();

        if (_unlockPriceText != null)
            _unlockPriceText.text = GetUnlockPriceGems().ToString();
    }

    private void RebuildDefinitions()
    {
        _definitions.Clear();

        IReadOnlyList<ProfessionDefinition> definitions = ProfessionService.GetDefinitions();
        for (int i = 0; i < definitions.Count; i++)
        {
            ProfessionDefinition definition = definitions[i];
            if (definition != null)
                _definitions.Add(definition);
        }
    }

    private int FindProfessionIndex(string professionId)
    {
        if (string.IsNullOrWhiteSpace(professionId))
            return 0;

        for (int i = 0; i < _definitions.Count; i++)
        {
            if (string.Equals(_definitions[i].professionId, professionId, StringComparison.Ordinal))
                return i;
        }

        return 0;
    }

    private ProfessionDefinition GetCurrentDefinition()
    {
        if (_definitions.Count == 0)
            return null;

        _currentIndex = Mathf.Clamp(_currentIndex, 0, _definitions.Count - 1);
        return _definitions[_currentIndex];
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

        SetOpen(false, releaseCursor);
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

    private void SetMessage(string message)
    {
        if (_messageText == null)
            return;

        _messageText.text = message ?? string.Empty;
    }

    private int GetUnlockPriceGems()
    {
        int basePrice = Mathf.Max(0, _unlockRandomPriceGems);
        if (!_scaleUnlockPriceByOpenedCount)
            return basePrice;

        int extraOpened = Mathf.Max(0, ProfessionService.GetUnlockedCount() - 1);
        int scaledPrice = basePrice + (Mathf.Max(0, _unlockPriceStepGems) * extraOpened);

        int maxPrice = Mathf.Max(basePrice, _unlockPriceMaxGems);
        return Mathf.Clamp(scaledPrice, basePrice, maxPrice);
    }

    private void HandleProfessionStateChanged()
    {
        if (!_opened)
            return;

        RebuildDefinitions();
        RefreshView();
    }

    private void BindButtons()
    {
        EnsureTemporaryUiIfNeeded();

        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(ShowNextProfession);
            _nextButton.onClick.AddListener(ShowNextProfession);
        }

        if (_prevButton != null)
        {
            _prevButton.onClick.RemoveListener(ShowPreviousProfession);
            _prevButton.onClick.AddListener(ShowPreviousProfession);
        }

        if (_applyButton != null)
        {
            _applyButton.onClick.RemoveListener(ApplySelectedProfession);
            _applyButton.onClick.AddListener(ApplySelectedProfession);
        }

        if (_unlockRandomButton != null)
        {
            _unlockRandomButton.onClick.RemoveListener(UnlockRandomProfession);
            _unlockRandomButton.onClick.AddListener(UnlockRandomProfession);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(CloseFromButton);
            _closeButton.onClick.AddListener(CloseFromButton);
        }
    }

    private void BindWindowCloseEvent()
    {
        if (PlayerInput.Instance == null)
            return;

        PlayerInput.Instance.AOpenWindow -= CloseByOtherWindow;
        PlayerInput.Instance.AOpenWindow += CloseByOtherWindow;
    }

    private void EnsureTemporaryUiIfNeeded()
    {
        if (!_createTemporaryUiIfMissing || _panel == null)
            return;

        bool hasMainText = _professionTitleText != null
            && _professionDescriptionText != null
            && _starterItemsText != null
            && _perksText != null;

        bool hasMainButtons = _applyButton != null
            && _nextButton != null
            && _prevButton != null
            && _unlockRandomButton != null
            && _closeButton != null;

        if (hasMainText && hasMainButtons)
            return;

        ProfessionTemporaryUIFactory.PanelRefs refs = ProfessionTemporaryUIFactory.EnsurePanel(_panel, CloseFromButton);
        if (refs == null)
            return;

        _professionTitleText ??= refs.titleText;
        _professionDescriptionText ??= refs.descriptionText;
        _starterItemsText ??= refs.starterItemsText;
        _perksText ??= refs.perksText;
        _statusText ??= refs.statusText;
        _messageText ??= refs.messageText;
        _unlockPriceText ??= refs.unlockPriceText;
        _professionIcon ??= refs.icon;
        _lockObject ??= refs.lockObject;
        _applyButton ??= refs.applyButton;
        _nextButton ??= refs.nextButton;
        _prevButton ??= refs.prevButton;
        _unlockRandomButton ??= refs.unlockRandomButton;
        _closeButton ??= refs.closeButton;
    }
}
