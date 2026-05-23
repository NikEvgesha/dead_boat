using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfessionSelectionPanel : MonoBehaviour
{
    private const float DefaultProfessionListButtonHeight = 88f;

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

    [Header("Profession List")]
    [SerializeField] private RectTransform _professionListRoot;
    [SerializeField] private Button _professionListButtonPrefab;
    [SerializeField] private Color _professionListNormalColor = new(0.094f, 0.106f, 0.129f, 0.96f);
    [SerializeField] private Color _professionListSelectedColor = new(0.161f, 0.169f, 0.2f, 0.96f);
    [SerializeField] private Color _professionListLockedColor = new(0.094f, 0.106f, 0.129f, 0.66f);
    [SerializeField] private Color _professionListTextColor = Color.white;
    [SerializeField] private Color _professionListLockedTextColor = new(0.722f, 0.761f, 0.82f, 1f);

    [Header("Buttons")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _unlockRandomButton;
    [SerializeField] private Button _directBuyButton;
    [SerializeField] private Button _closeButton;

    [Header("Unlock")]
    [SerializeField] private int _unlockRandomPriceCoins = 250;
    [SerializeField] private CurrencyType _unlockRandomCurrencyType = CurrencyType.Coins;
    [SerializeField] private bool _scaleUnlockPriceByOpenedCount = true;
    [SerializeField] private int _unlockPriceStepCoins = 100;
    [SerializeField] private int _unlockPriceMaxCoins = 2000;

    private readonly List<ProfessionDefinition> _definitions = new();
    private readonly List<Button> _professionListButtons = new();
    private readonly List<Text> _professionListLabels = new();
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

        if (_directBuyButton != null)
            _directBuyButton.onClick.RemoveListener(DirectBuyProfession);

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
        RebuildProfessionList();
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
        {
            ProfessionService.TrySelectNoProfession();
            SetMessage(ProfessionLocalization.StatusNoProfession);
            RefreshView();
            return;
        }

        if (!ProfessionService.TrySelectProfession(definition.professionId))
            return;

        SetMessage(ProfessionLocalization.FormatSelectedProfession(ProfessionLocalization.DefinitionTitle(definition)));
        RefreshView();
    }

    private void UnlockRandomProfession()
    {
        int unlockPrice = GetUnlockPrice();

        if (!ProfessionService.HasRandomLockedProfessions())
        {
            SetMessage(ProfessionLocalization.MessageAllUnlocked);
            RefreshView();
            return;
        }

        if (!ProfessionService.TryUnlockRandomLockedProfession(unlockPrice, _unlockRandomCurrencyType, out ProfessionDefinition unlockedDefinition))
        {
            SetMessage(ProfessionLocalization.MessageUnlockFailed);
            RefreshView();
            return;
        }

        _currentIndex = FindProfessionIndex(unlockedDefinition.professionId);
        SetMessage(ProfessionLocalization.FormatUnlockedProfession(ProfessionLocalization.DefinitionTitle(unlockedDefinition)));
        RefreshView();
    }

    private void DirectBuyProfession()
    {
        ProfessionDefinition definition = GetCurrentDefinition();
        if (definition == null || ProfessionService.IsUnlocked(definition.professionId))
            return;

        if (CanUseRealPurchase(definition))
        {
            PauseManager.Instance.SetPause(true, true);
            PurchasesManager.Instance.BuyPurchase(
                definition.purchaseProductId,
                success =>
                {
                    if (success)
                    {
                        ProfessionService.UnlockProfessionFromPurchase(definition.professionId);
                        SetMessage(ProfessionLocalization.FormatUnlockedProfession(ProfessionLocalization.DefinitionTitle(definition)));
                    }
                    else
                    {
                        SetMessage(ProfessionLocalization.MessageUnlockFailed);
                    }

                    PauseManager.Instance.SetPause(false, true);
                    RefreshView();
                });
            return;
        }

        if (!CanUseSoftCurrencyFallback(definition))
        {
            SetMessage(ProfessionLocalization.MessagePurchaseUnavailable);
            RefreshView();
            return;
        }

        if (!ProfessionService.TryUnlockProfessionWithSoftCurrency(
                definition.professionId,
                definition.directSoftCurrencyCost,
                definition.directSoftCurrencyType))
        {
            SetMessage(ProfessionLocalization.MessageUnlockFailed);
            RefreshView();
            return;
        }

        SetMessage(ProfessionLocalization.FormatUnlockedProfession(ProfessionLocalization.DefinitionTitle(definition)));
        RefreshView();
    }

    private void RefreshView()
    {
        ProfessionDefinition definition = GetCurrentDefinition();
        if (definition == null)
        {
            RefreshNoProfessionView();
            return;
        }

        bool unlocked = ProfessionService.IsUnlocked(definition.professionId);
        bool selected = ProfessionService.HasExplicitProfessionChoice() &&
            string.Equals(ProfessionService.CurrentProfessionId, definition.professionId, StringComparison.Ordinal);

        if (_professionTitleText != null)
            _professionTitleText.text = ProfessionLocalization.DefinitionTitle(definition);

        if (_professionDescriptionText != null)
            _professionDescriptionText.text = ProfessionLocalization.DefinitionDescription(definition);

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

        bool hasIcon = definition.icon != null;

        if (_professionIcon != null)
        {
            _professionIcon.enabled = hasIcon;
            _professionIcon.sprite = definition.icon;
        }

        if (_lockObject != null)
            _lockObject.SetActive(!unlocked && hasIcon);

        if (_applyButton != null)
        {
            _applyButton.gameObject.SetActive(unlocked);
            _applyButton.interactable = unlocked && !selected;
        }

        if (_unlockRandomButton != null)
            _unlockRandomButton.interactable = ProfessionService.HasRandomLockedProfessions();

        if (_unlockPriceText != null)
            _unlockPriceText.text = BuildPriceText(definition, unlocked);

        RefreshDirectBuyButton(definition, unlocked);
        RefreshProfessionList();
        RefreshNavigationButtons();
    }

    private void RebuildDefinitions()
    {
        _definitions.Clear();
        _definitions.Add(null);

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
        if (string.IsNullOrWhiteSpace(professionId) || ProfessionService.IsNoProfessionSelected())
            return 0;

        for (int i = 0; i < _definitions.Count; i++)
        {
            if (_definitions[i] != null && string.Equals(_definitions[i].professionId, professionId, StringComparison.Ordinal))
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

    private int GetUnlockPrice()
    {
        int basePrice = Mathf.Max(0, _unlockRandomPriceCoins);
        if (!_scaleUnlockPriceByOpenedCount)
            return basePrice;

        int extraOpened = Mathf.Max(0, ProfessionService.GetUnlockedCount());
        int scaledPrice = basePrice + (Mathf.Max(0, _unlockPriceStepCoins) * extraOpened);

        int maxPrice = Mathf.Max(basePrice, _unlockPriceMaxCoins);
        return Mathf.Clamp(scaledPrice, basePrice, maxPrice);
    }

    private void RefreshNoProfessionView()
    {
        bool selected = ProfessionService.IsNoProfessionSelected();

        if (_professionTitleText != null)
            _professionTitleText.text = ProfessionLocalization.NoProfessionTitle;

        if (_professionDescriptionText != null)
            _professionDescriptionText.text = ProfessionLocalization.NoProfessionDescription;

        if (_starterItemsText != null)
            _starterItemsText.text = ProfessionLocalization.NoStarterItems;

        if (_perksText != null)
            _perksText.text = ProfessionLocalization.NoSpecialAbilities;

        if (_statusText != null)
            _statusText.text = selected ? ProfessionLocalization.StatusEquipped : ProfessionLocalization.StatusNoProfession;

        if (_professionIcon != null)
        {
            _professionIcon.enabled = false;
            _professionIcon.sprite = null;
        }

        if (_lockObject != null)
            _lockObject.SetActive(false);

        if (_applyButton != null)
        {
            _applyButton.gameObject.SetActive(true);
            _applyButton.interactable = !selected;
        }

        if (_unlockRandomButton != null)
            _unlockRandomButton.interactable = ProfessionService.HasRandomLockedProfessions();

        if (_directBuyButton != null)
            _directBuyButton.gameObject.SetActive(false);

        if (_unlockPriceText != null)
            _unlockPriceText.text = ProfessionLocalization.FormatSoftPrice(GetUnlockPrice(), _unlockRandomCurrencyType);

        RefreshProfessionList();
        RefreshNavigationButtons();
    }

    private void RefreshDirectBuyButton(ProfessionDefinition definition, bool unlocked)
    {
        if (_directBuyButton == null)
            return;

        bool canBuy = !unlocked && (CanUseRealPurchase(definition) || CanUseSoftCurrencyFallback(definition));
        _directBuyButton.gameObject.SetActive(canBuy);
        _directBuyButton.interactable = canBuy;
    }

    private string BuildPriceText(ProfessionDefinition definition, bool unlocked)
    {
        if (unlocked)
            return ProfessionLocalization.FormatSoftPrice(GetUnlockPrice(), _unlockRandomCurrencyType);

        if (CanUseRealPurchase(definition))
        {
            PurchaseData purchaseData = PurchasesManager.Instance.GetPurchaseData(definition.purchaseProductId);
            if (purchaseData != null && !string.IsNullOrWhiteSpace(purchaseData.Price))
                return purchaseData.Price;
        }

        if (CanUseSoftCurrencyFallback(definition))
            return ProfessionLocalization.FormatSoftPrice(definition.directSoftCurrencyCost, definition.directSoftCurrencyType);

        return ProfessionLocalization.FormatSoftPrice(GetUnlockPrice(), _unlockRandomCurrencyType);
    }

    private bool CanUseRealPurchase(ProfessionDefinition definition)
    {
        return definition != null &&
               PurchasesManager.Instance != null &&
               PurchasesManager.Instance.PurchasesAvailable() &&
               !string.IsNullOrWhiteSpace(definition.purchaseProductId);
    }

    private bool CanUseSoftCurrencyFallback(ProfessionDefinition definition)
    {
        if (definition == null || !definition.allowSoftCurrencyFallbackWhenPurchasesUnavailable)
            return false;

        if (definition.directSoftCurrencyType == CurrencyType.Real)
            return false;

        bool purchasesAvailable = PurchasesManager.Instance != null && PurchasesManager.Instance.PurchasesAvailable();
        return !purchasesAvailable && definition.directSoftCurrencyCost >= 0;
    }

    private void HandleProfessionStateChanged()
    {
        if (!_opened)
            return;

        RebuildDefinitions();
        RebuildProfessionList();
        RefreshView();
    }

    private void SelectProfessionIndex(int index)
    {
        if (_definitions.Count == 0)
            return;

        _currentIndex = Mathf.Clamp(index, 0, _definitions.Count - 1);
        SetMessage(string.Empty);
        RefreshView();
    }

    private void RebuildProfessionList()
    {
        ClearProfessionList();

        if (_professionListRoot == null || _professionListButtonPrefab == null)
            return;

        _professionListButtonPrefab.gameObject.SetActive(false);

        for (int i = 0; i < _definitions.Count; i++)
        {
            int index = i;
            Button button = Instantiate(_professionListButtonPrefab, _professionListRoot);
            button.gameObject.SetActive(true);
            EnsureProfessionListButtonLayout(button);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectProfessionIndex(index));

            Text label = button.GetComponentInChildren<Text>(true);
            _professionListButtons.Add(button);
            _professionListLabels.Add(label);
        }

        RefreshProfessionList();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_professionListRoot);
    }

    private void EnsureProfessionListButtonLayout(Button button)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && rect.sizeDelta.y < 1f)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, DefaultProfessionListButtonHeight);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = button.gameObject.AddComponent<LayoutElement>();

        if (layoutElement.minHeight < 1f)
            layoutElement.minHeight = DefaultProfessionListButtonHeight;

        if (layoutElement.preferredHeight < 1f)
            layoutElement.preferredHeight = DefaultProfessionListButtonHeight;

        layoutElement.flexibleHeight = 0f;
    }

    private void ClearProfessionList()
    {
        for (int i = _professionListRoot != null ? _professionListRoot.childCount - 1 : -1; i >= 0; i--)
        {
            Transform child = _professionListRoot.GetChild(i);
            if (_professionListButtonPrefab != null && child == _professionListButtonPrefab.transform)
                continue;

            child.SetParent(null, false);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        _professionListButtons.Clear();
        _professionListLabels.Clear();
    }

    private void RefreshProfessionList()
    {
        if (_professionListButtons.Count == 0)
            return;

        for (int i = 0; i < _professionListButtons.Count; i++)
        {
            Button button = _professionListButtons[i];
            if (button == null)
                continue;

            ProfessionDefinition definition = i >= 0 && i < _definitions.Count ? _definitions[i] : null;
            bool selected = i == _currentIndex;
            bool unlocked = definition == null || ProfessionService.IsUnlocked(definition.professionId);

            if (button.targetGraphic is Image image)
                image.color = selected
                    ? _professionListSelectedColor
                    : unlocked ? _professionListNormalColor : _professionListLockedColor;

            if (i < _professionListLabels.Count && _professionListLabels[i] != null)
            {
                Text label = _professionListLabels[i];
                label.text = definition == null
                    ? ProfessionLocalization.NoProfessionTitle
                    : ProfessionLocalization.DefinitionTitle(definition);
                label.color = unlocked ? _professionListTextColor : _professionListLockedTextColor;
            }
        }
    }

    private void RefreshNavigationButtons()
    {
        bool useListNavigation = _professionListRoot != null && _professionListButtonPrefab != null;

        if (_prevButton != null)
            _prevButton.gameObject.SetActive(!useListNavigation);

        if (_nextButton != null)
            _nextButton.gameObject.SetActive(!useListNavigation);
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

        if (_directBuyButton != null)
        {
            _directBuyButton.onClick.RemoveListener(DirectBuyProfession);
            _directBuyButton.onClick.AddListener(DirectBuyProfession);
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
            && _directBuyButton != null
            && _closeButton != null;

        bool hasList = _professionListRoot != null
            && _professionListButtonPrefab != null;

        if (hasMainText && hasMainButtons && hasList)
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
        _professionListRoot ??= refs.professionListRoot;
        _professionListButtonPrefab ??= refs.professionListButtonPrefab;
        _applyButton ??= refs.applyButton;
        _nextButton ??= refs.nextButton;
        _prevButton ??= refs.prevButton;
        _unlockRandomButton ??= refs.unlockRandomButton;
        _directBuyButton ??= refs.directBuyButton;
        _closeButton ??= refs.closeButton;
    }
}
