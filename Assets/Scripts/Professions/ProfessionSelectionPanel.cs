using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfessionSelectionPanel : MonoBehaviour
{
    private const float DefaultProfessionListButtonHeight = 88f;
    private const float StarterItemRowHeight = 58f;
    private static readonly Color ActionButtonTextColor = Color.white;
    private static readonly Color ActionButtonTextOutlineColor = Color.black;
    private static readonly Vector2 ActionButtonTextOutlineDistance = new Vector2(1.8f, -1.8f);

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
    [SerializeField] private Text _unlockRandomPriceText;
    [SerializeField] private Text _unlockPriceText;

    [Header("Visuals")]
    [SerializeField] private Image _professionIcon;
    [SerializeField] private Image _unlockRandomPriceIcon;
    [SerializeField] private Image _unlockPriceIcon;
    [SerializeField] private Sprite _softCurrencyPriceIcon;
    [SerializeField] private Sprite _coinsCurrencyIcon;
    [SerializeField] private Sprite _gemsCurrencyIcon;
    [SerializeField] private GameObject _lockObject;

    [Header("Starter Items")]
    [SerializeField] private RectTransform _starterItemsRoot;

    [Header("Profession List")]
    [SerializeField] private RectTransform _professionListRoot;
    [SerializeField] private Button _professionListButtonPrefab;
    [SerializeField] private Color _professionListNormalColor = new(0.094f, 0.106f, 0.129f, 0.96f);
    [SerializeField] private Color _professionListSelectedColor = new(0.161f, 0.169f, 0.2f, 0.96f);
    [SerializeField] private Color _professionListLockedColor = new(0.094f, 0.106f, 0.129f, 0.66f);
    [SerializeField] private Color _professionListRandomHighlightColor = new(0.12f, 0.68f, 0.2f, 0.96f);
    [SerializeField] private Color _professionListTextColor = Color.white;
    [SerializeField] private Color _professionListLockedTextColor = new(0.722f, 0.761f, 0.82f, 1f);
    [SerializeField] private Color _professionListRandomHighlightTextColor = Color.white;

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
    [SerializeField] private bool _scaleUnlockPriceByOpenedCount;
    [SerializeField] private int _unlockPriceStepCoins = 100;
    [SerializeField] private int _unlockPriceMaxCoins = 2000;
    [SerializeField] private int _randomUnlockMinHighlightSteps = 14;
    [SerializeField] private float _randomUnlockStartDelay = 0.045f;
    [SerializeField] private float _randomUnlockEndDelay = 0.32f;

    private readonly List<ProfessionDefinition> _definitions = new();
    private readonly List<Button> _professionListButtons = new();
    private readonly List<Text> _professionListLabels = new();
    private readonly List<GameObject> _starterItemRows = new();
    private int _currentIndex;
    private bool _opened;
    private Coroutine _randomUnlockRoutine;
    private bool _randomUnlockInProgress;
    private string _randomUnlockHighlightProfessionId;

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
        ClearRandomUnlockAnimation();

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
        if (_randomUnlockInProgress)
            return;

        if (_definitions.Count == 0)
            return;

        _currentIndex++;
        if (_currentIndex >= _definitions.Count)
            _currentIndex = 0;

        RefreshView();
    }

    private void ShowPreviousProfession()
    {
        if (_randomUnlockInProgress)
            return;

        if (_definitions.Count == 0)
            return;

        _currentIndex--;
        if (_currentIndex < 0)
            _currentIndex = _definitions.Count - 1;

        RefreshView();
    }

    private void ApplySelectedProfession()
    {
        if (_randomUnlockInProgress)
            return;

        ProfessionDefinition definition = GetCurrentDefinition();
        if (definition == null)
        {
            return;
        }

        if (!ProfessionService.TrySelectProfession(definition.professionId))
            return;

        SetMessage(ProfessionLocalization.FormatSelectedProfession(ProfessionLocalization.DefinitionTitle(definition)));
        RefreshView();
    }

    private void UnlockRandomProfession()
    {
        if (_randomUnlockInProgress)
            return;

        int unlockPrice = GetUnlockPrice();
        List<ProfessionDefinition> randomLockedDefinitions = BuildRandomLockedDefinitions();

        if (randomLockedDefinitions.Count == 0)
        {
            SetMessage(ProfessionLocalization.MessageAllUnlocked);
            RefreshView();
            return;
        }

        bool animateUnlock = randomLockedDefinitions.Count > 1;
        if (animateUnlock)
        {
            _randomUnlockInProgress = true;
            _randomUnlockHighlightProfessionId = string.Empty;
            RefreshRandomUnlockControls();
        }

        if (!ProfessionService.TryUnlockRandomLockedProfession(unlockPrice, _unlockRandomCurrencyType, out ProfessionDefinition unlockedDefinition))
        {
            ClearRandomUnlockAnimation();
            SetMessage(ProfessionLocalization.MessageUnlockFailed);
            RefreshView();
            return;
        }

        if (!animateUnlock)
        {
            CompleteRandomUnlock(unlockedDefinition);
            return;
        }

        _randomUnlockRoutine = StartCoroutine(PlayRandomUnlockAnimation(randomLockedDefinitions, unlockedDefinition));
    }

    private void DirectBuyProfession()
    {
        if (_randomUnlockInProgress)
            return;

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

        if (!CanUseSoftCurrencyDirectPurchase(definition))
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

        RefreshStarterItems(definition);

        if (_perksText != null)
        {
            _perksText.supportRichText = true;
            _perksText.text = ProfessionService.BuildPerksSummary(definition);
        }

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
            _applyButton.interactable = unlocked && !selected && !_randomUnlockInProgress;
        }

        RefreshRandomUnlockControls();
        RefreshDirectBuyButton(definition, unlocked);
        RefreshDirectBuyPrice(definition, unlocked);
        RefreshProfessionList();
        RefreshNavigationButtons();
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

    private void RefreshStarterItems(ProfessionDefinition definition)
    {
        ClearStarterItemRows();

        if (definition == null)
        {
            ShowStarterItemsText(ProfessionLocalization.NoStarterItems);
            return;
        }

        List<StarterItemEntry> entries = BuildStarterItemEntries(definition);
        if (entries.Count == 0)
        {
            ShowStarterItemsText(ProfessionLocalization.NoStarterItems);
            return;
        }

        RectTransform root = ResolveStarterItemsRoot();
        if (root == null)
        {
            ShowStarterItemsText(ProfessionService.BuildStarterItemsSummary(definition));
            return;
        }

        if (_starterItemsText != null)
            _starterItemsText.gameObject.SetActive(false);

        for (int i = 0; i < entries.Count; i++)
            CreateStarterItemRow(root, entries[i], i, entries.Count);
    }

    private List<StarterItemEntry> BuildStarterItemEntries(ProfessionDefinition definition)
    {
        List<StarterItemEntry> entries = new();
        if (definition == null)
            return entries;

        if (definition.starterItems != null)
        {
            for (int i = 0; i < definition.starterItems.Count; i++)
            {
                ProfessionStarterItem starterItem = definition.starterItems[i];
                if (starterItem == null || starterItem.itemPrefab == null)
                    continue;

                Sprite icon = starterItem.itemPrefab.Data != null ? starterItem.itemPrefab.Data.IMG : null;
                int amount = Mathf.Max(1, starterItem.amount);
                entries.Add(new StarterItemEntry(icon, $"x{amount}"));
            }
        }

        if (definition.startCoinsBonus > 0)
            entries.Add(new StarterItemEntry(ResolveSoftCurrencyIcon(CurrencyType.Coins), $"+{definition.startCoinsBonus}"));

        if (definition.startGemsBonus > 0)
            entries.Add(new StarterItemEntry(ResolveSoftCurrencyIcon(CurrencyType.Gems), $"+{definition.startGemsBonus}"));

        return entries;
    }

    private void CreateStarterItemRow(RectTransform root, StarterItemEntry entry, int index, int total)
    {
        GameObject rowObject = new($"StarterItemRow_{index + 1}");
        rowObject.layer = root.gameObject.layer;
        rowObject.transform.SetParent(root, false);
        _starterItemRows.Add(rowObject);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        ConfigureStarterItemRowRect(rowRect, root, index, total);

        GameObject iconObject = new("Icon");
        iconObject.layer = rowObject.layer;
        iconObject.transform.SetParent(rowObject.transform, false);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = entry.icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = entry.icon != null;

        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.16f, 0.04f);
        iconRect.anchorMax = new Vector2(0.43f, 0.96f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        GameObject amountObject = new("Amount");
        amountObject.layer = rowObject.layer;
        amountObject.transform.SetParent(rowObject.transform, false);

        Text amountText = amountObject.AddComponent<Text>();
        CopyStarterTextStyle(amountText);
        amountText.text = entry.amountText;
        amountText.alignment = TextAnchor.MiddleLeft;
        amountText.raycastTarget = false;

        RectTransform amountRect = amountText.GetComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(0.48f, 0f);
        amountRect.anchorMax = new Vector2(0.92f, 1f);
        amountRect.offsetMin = Vector2.zero;
        amountRect.offsetMax = Vector2.zero;
    }

    private void ConfigureStarterItemRowRect(RectTransform rowRect, RectTransform root, int index, int total)
    {
        Vector2 areaMin = new(0.1f, 0.08f);
        Vector2 areaMax = new(0.9f, 0.68f);

        if (_starterItemsText != null && _starterItemsText.transform.parent == root)
        {
            RectTransform textRect = _starterItemsText.rectTransform;
            areaMin = textRect.anchorMin;
            areaMax = textRect.anchorMax;
        }

        int safeTotal = Mathf.Max(1, total);
        float areaHeight = Mathf.Max(0.01f, areaMax.y - areaMin.y);
        float gap = Mathf.Min(0.03f, areaHeight / (safeTotal * 5f));
        float rowHeight = Mathf.Min(0.32f, (areaHeight - (gap * (safeTotal - 1))) / safeTotal);
        float top = areaMax.y - (index * (rowHeight + gap));
        float bottom = Mathf.Max(areaMin.y, top - rowHeight);

        rowRect.anchorMin = new Vector2(areaMin.x, bottom);
        rowRect.anchorMax = new Vector2(areaMax.x, top);
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        LayoutElement layoutElement = rowRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = StarterItemRowHeight;
        layoutElement.preferredHeight = StarterItemRowHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private void CopyStarterTextStyle(Text target)
    {
        if (target == null)
            return;

        if (_starterItemsText != null)
        {
            target.font = _starterItemsText.font;
            target.fontSize = Mathf.Max(24, _starterItemsText.fontSize);
            target.fontStyle = _starterItemsText.fontStyle;
            target.color = _starterItemsText.color;
            target.material = _starterItemsText.material;
        }

        if (target.font == null)
        {
            target.font = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (target.font == null)
                target.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        target.supportRichText = false;
        target.horizontalOverflow = HorizontalWrapMode.Overflow;
        target.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private RectTransform ResolveStarterItemsRoot()
    {
        if (_starterItemsRoot != null)
            return _starterItemsRoot;

        if (_starterItemsText != null && _starterItemsText.transform.parent is RectTransform parent)
            return parent;

        return null;
    }

    private void ShowStarterItemsText(string text)
    {
        ClearStarterItemRows();

        if (_starterItemsText == null)
            return;

        _starterItemsText.gameObject.SetActive(true);
        _starterItemsText.text = text ?? string.Empty;
    }

    private void ClearStarterItemRows()
    {
        for (int i = 0; i < _starterItemRows.Count; i++)
        {
            GameObject row = _starterItemRows[i];
            if (row == null)
                continue;

            if (Application.isPlaying)
                Destroy(row);
            else
                DestroyImmediate(row);
        }

        _starterItemRows.Clear();
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

        RefreshStarterItems(null);

        if (_perksText != null)
        {
            _perksText.supportRichText = true;
            _perksText.text = ProfessionLocalization.NoSpecialAbilities;
        }

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

        RefreshRandomUnlockControls();

        if (_directBuyButton != null)
            _directBuyButton.gameObject.SetActive(false);

        SetPriceVisible(_unlockPriceText, _unlockPriceIcon, false);

        RefreshProfessionList();
        RefreshNavigationButtons();
    }

    private void RefreshRandomUnlockControls()
    {
        bool hasRandomLockedProfessions = ProfessionService.HasRandomLockedProfessions();
        bool showRandomPrice = hasRandomLockedProfessions || _randomUnlockInProgress;

        if (_unlockRandomButton != null)
            _unlockRandomButton.interactable = hasRandomLockedProfessions && !_randomUnlockInProgress;

        SetSoftCurrencyPrice(
            _unlockRandomPriceText,
            _unlockRandomPriceIcon,
            GetUnlockPrice(),
            _unlockRandomCurrencyType,
            showRandomPrice);
    }

    private void RefreshDirectBuyButton(ProfessionDefinition definition, bool unlocked)
    {
        if (_directBuyButton == null)
            return;

        bool canBuy = !_randomUnlockInProgress && !unlocked && (CanUseRealPurchase(definition) || CanUseSoftCurrencyDirectPurchase(definition));
        _directBuyButton.gameObject.SetActive(canBuy);
        _directBuyButton.interactable = canBuy;
    }

    private void RefreshDirectBuyPrice(ProfessionDefinition definition, bool unlocked)
    {
        if (unlocked || definition == null)
        {
            SetPriceVisible(_unlockPriceText, _unlockPriceIcon, false);
            return;
        }

        if (CanUseRealPurchase(definition))
        {
            PurchaseData purchaseData = PurchasesManager.Instance.GetPurchaseData(definition.purchaseProductId);
            if (purchaseData != null && !string.IsNullOrWhiteSpace(purchaseData.Price))
            {
                SetTextPrice(_unlockPriceText, _unlockPriceIcon, purchaseData.Price, true);
                return;
            }
        }

        if (CanUseSoftCurrencyDirectPurchase(definition))
        {
            SetSoftCurrencyPrice(
                _unlockPriceText,
                _unlockPriceIcon,
                definition.directSoftCurrencyCost,
                definition.directSoftCurrencyType,
                true);
            return;
        }

        SetPriceVisible(_unlockPriceText, _unlockPriceIcon, false);
    }

    private void SetSoftCurrencyPrice(Text priceText, Image priceIcon, int amount, CurrencyType currencyType, bool visible)
    {
        SetTextPrice(priceText, priceIcon, Mathf.Max(0, amount).ToString(), visible);

        if (priceIcon == null)
            return;

        Sprite icon = ResolveSoftCurrencyIcon(currencyType);
        priceIcon.sprite = icon;
        priceIcon.preserveAspect = true;
        priceIcon.gameObject.SetActive(visible && icon != null);
        priceIcon.enabled = visible && icon != null;
    }

    private void SetTextPrice(Text priceText, Image priceIcon, string value, bool visible)
    {
        if (priceText != null)
        {
            priceText.gameObject.SetActive(visible);
            priceText.text = visible ? value ?? string.Empty : string.Empty;
        }

        if (priceIcon != null)
            priceIcon.gameObject.SetActive(false);
    }

    private void SetPriceVisible(Text priceText, Image priceIcon, bool visible)
    {
        if (priceText != null)
        {
            priceText.gameObject.SetActive(visible);
            if (!visible)
                priceText.text = string.Empty;
        }

        if (priceIcon != null)
            priceIcon.gameObject.SetActive(visible && priceIcon.sprite != null);
    }

    private Sprite ResolveSoftCurrencyIcon(CurrencyType currencyType)
    {
        if (currencyType == CurrencyType.Coins && _coinsCurrencyIcon != null)
            return _coinsCurrencyIcon;

        if (currencyType == CurrencyType.Gems && _gemsCurrencyIcon != null)
            return _gemsCurrencyIcon;

        if (currencyType == CurrencyType.Gems && _softCurrencyPriceIcon != null)
            return _softCurrencyPriceIcon;

        return CurrencyManager.Instance != null
            ? CurrencyManager.Instance.GetCurrencyIcon(currencyType)
            : null;
    }

    private bool CanUseRealPurchase(ProfessionDefinition definition)
    {
        return definition != null &&
               PurchasesManager.Instance != null &&
               PurchasesManager.Instance.PurchasesAvailable() &&
               !string.IsNullOrWhiteSpace(definition.purchaseProductId);
    }

    private bool CanUseSoftCurrencyDirectPurchase(ProfessionDefinition definition)
    {
        if (definition == null || !definition.allowSoftCurrencyFallbackWhenPurchasesUnavailable)
            return false;

        if (definition.directSoftCurrencyType == CurrencyType.Real)
            return false;

        return definition.directSoftCurrencyCost >= 0;
    }

    private void HandleProfessionStateChanged()
    {
        if (!_opened)
            return;

        RebuildDefinitions();
        RebuildProfessionList();
        if (_randomUnlockInProgress)
        {
            RefreshRandomUnlockControls();
            return;
        }

        RefreshView();
    }

    private void SelectProfessionIndex(int index)
    {
        if (_randomUnlockInProgress)
            return;

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
            EnsureButtonSound(button);
            EnsureButtonLabelStyle(button);
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
            bool randomHighlighted = definition != null &&
                _randomUnlockInProgress &&
                string.Equals(definition.professionId, _randomUnlockHighlightProfessionId, StringComparison.Ordinal);

            if (button.targetGraphic is Image image)
                image.color = randomHighlighted
                    ? _professionListRandomHighlightColor
                    : selected
                    ? _professionListSelectedColor
                    : unlocked ? _professionListNormalColor : _professionListLockedColor;

            if (i < _professionListLabels.Count && _professionListLabels[i] != null)
            {
                Text label = _professionListLabels[i];
                label.text = definition == null
                    ? ProfessionLocalization.NoProfessionTitle
                    : ProfessionLocalization.DefinitionTitle(definition);
                label.color = randomHighlighted
                    ? _professionListRandomHighlightTextColor
                    : unlocked ? _professionListTextColor : _professionListLockedTextColor;
            }
        }
    }

    private List<ProfessionDefinition> BuildRandomLockedDefinitions()
    {
        List<ProfessionDefinition> result = new();
        IReadOnlyList<ProfessionDefinition> definitions = ProfessionService.GetDefinitions();

        for (int i = 0; i < definitions.Count; i++)
        {
            ProfessionDefinition definition = definitions[i];
            if (definition == null)
                continue;

            if (!definition.availableInRandomUnlockPool || definition.randomUnlockWeight <= 0)
                continue;

            if (!ProfessionService.IsUnlocked(definition.professionId))
                result.Add(definition);
        }

        return result;
    }

    private IEnumerator PlayRandomUnlockAnimation(List<ProfessionDefinition> candidates, ProfessionDefinition unlockedDefinition)
    {
        if (candidates == null || candidates.Count == 0 || unlockedDefinition == null)
        {
            _randomUnlockRoutine = null;
            CompleteRandomUnlock(unlockedDefinition);
            yield break;
        }

        int targetIndex = FindCandidateIndex(candidates, unlockedDefinition.professionId);
        if (targetIndex < 0)
            targetIndex = 0;

        int minSteps = Mathf.Max(4, _randomUnlockMinHighlightSteps);
        int maxSteps = Mathf.Max(minSteps, 28);
        int steps = Mathf.Clamp((candidates.Count * 3) + 8, minSteps, maxSteps);
        int cursor = Mod(targetIndex - steps, candidates.Count);

        float startDelay = Mathf.Max(0.01f, _randomUnlockStartDelay);
        float endDelay = Mathf.Max(startDelay, _randomUnlockEndDelay);

        for (int i = 0; i < steps; i++)
        {
            cursor = (cursor + 1) % candidates.Count;
            _randomUnlockHighlightProfessionId = candidates[cursor].professionId;
            RefreshProfessionList();
            PlayRandomUnlockTickSound();

            float t = steps <= 1 ? 1f : i / (steps - 1f);
            float eased = t * t;
            yield return new WaitForSecondsRealtime(Mathf.Lerp(startDelay, endDelay, eased));
        }

        _randomUnlockHighlightProfessionId = unlockedDefinition.professionId;
        RefreshProfessionList();
        PlayRandomUnlockTickSound();
        yield return new WaitForSecondsRealtime(0.25f);

        _randomUnlockRoutine = null;
        CompleteRandomUnlock(unlockedDefinition);
    }

    private static void PlayRandomUnlockTickSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUIClick();
    }

    private int FindCandidateIndex(List<ProfessionDefinition> candidates, string professionId)
    {
        if (candidates == null || string.IsNullOrWhiteSpace(professionId))
            return -1;

        for (int i = 0; i < candidates.Count; i++)
        {
            ProfessionDefinition definition = candidates[i];
            if (definition != null && string.Equals(definition.professionId, professionId, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static int Mod(int value, int length)
    {
        if (length <= 0)
            return 0;

        int result = value % length;
        return result < 0 ? result + length : result;
    }

    private void CompleteRandomUnlock(ProfessionDefinition unlockedDefinition)
    {
        ClearRandomUnlockAnimation();

        if (unlockedDefinition != null)
        {
            _currentIndex = FindProfessionIndex(unlockedDefinition.professionId);
            SetMessage(ProfessionLocalization.FormatUnlockedProfession(ProfessionLocalization.DefinitionTitle(unlockedDefinition)));
        }

        RefreshView();
    }

    private void ClearRandomUnlockAnimation()
    {
        if (_randomUnlockRoutine != null)
        {
            StopCoroutine(_randomUnlockRoutine);
            _randomUnlockRoutine = null;
        }

        _randomUnlockInProgress = false;
        _randomUnlockHighlightProfessionId = null;
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
            EnsureButtonSound(_nextButton);
            EnsureButtonLabelStyle(_nextButton);
            _nextButton.onClick.RemoveListener(ShowNextProfession);
            _nextButton.onClick.AddListener(ShowNextProfession);
        }

        if (_prevButton != null)
        {
            EnsureButtonSound(_prevButton);
            EnsureButtonLabelStyle(_prevButton);
            _prevButton.onClick.RemoveListener(ShowPreviousProfession);
            _prevButton.onClick.AddListener(ShowPreviousProfession);
        }

        if (_applyButton != null)
        {
            EnsureButtonSound(_applyButton);
            EnsureButtonLabelStyle(_applyButton);
            _applyButton.onClick.RemoveListener(ApplySelectedProfession);
            _applyButton.onClick.AddListener(ApplySelectedProfession);
        }

        if (_unlockRandomButton != null)
        {
            EnsureButtonSound(_unlockRandomButton);
            EnsureButtonLabelStyle(_unlockRandomButton);
            _unlockRandomButton.onClick.RemoveListener(UnlockRandomProfession);
            _unlockRandomButton.onClick.AddListener(UnlockRandomProfession);
        }

        if (_directBuyButton != null)
        {
            EnsureButtonSound(_directBuyButton);
            EnsureButtonLabelStyle(_directBuyButton);
            _directBuyButton.onClick.RemoveListener(DirectBuyProfession);
            _directBuyButton.onClick.AddListener(DirectBuyProfession);
        }

        if (_closeButton != null)
        {
            EnsureButtonSound(_closeButton);
            EnsureButtonLabelStyle(_closeButton);
            _closeButton.onClick.RemoveListener(CloseFromButton);
            _closeButton.onClick.AddListener(CloseFromButton);
        }
    }

    private static void EnsureButtonSound(Button button)
    {
        if (button == null)
            return;

        UISound sound = button.GetComponent<UISound>();
        if (sound == null)
            sound = button.gameObject.AddComponent<UISound>();

        sound.Rebind();
    }

    private static void EnsureButtonLabelStyle(Button button)
    {
        if (button == null)
            return;

        Text[] labels = button.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            Text label = labels[i];
            if (label == null)
                continue;

            label.color = ActionButtonTextColor;
            label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.raycastTarget = false;

            UnityEngine.UI.Outline outline = label.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
                outline = label.gameObject.AddComponent<UnityEngine.UI.Outline>();

            outline.effectColor = ActionButtonTextOutlineColor;
            outline.effectDistance = ActionButtonTextOutlineDistance;
            outline.useGraphicAlpha = true;
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

    private struct StarterItemEntry
    {
        public readonly Sprite icon;
        public readonly string amountText;

        public StarterItemEntry(Sprite icon, string amountText)
        {
            this.icon = icon;
            this.amountText = amountText;
        }
    }
}
