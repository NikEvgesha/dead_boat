using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RareBack
{
    public RareType Type;
    public Sprite sprite;
}
public class LevelUpUi : MonoBehaviour
{
    public static LevelUpUi Instance;
    [SerializeField] private GameObject _panel;
    [SerializeField] private Animator _anim;
    [SerializeField] private GameObject _panelAdsChoise;
    [SerializeField] private GameObject _buttonLevelUp;
    [SerializeField] private Text _countLevelUpUi;
    [SerializeField] private List<LevelUpButtomUI> _buttons;
    [SerializeField] private List<RareBack> _rareBack;


    [SerializeField] private int _price = 1;
    [SerializeField] Text _textPrice;

    private readonly Dictionary<GameObject, bool> _scriptedHidden = new();
    private void ScriptedControls(bool hide)
    {
        if (!hide)
        {
            foreach (var pair in _scriptedHidden) if (pair.Key) pair.Key.SetActive(pair.Value);
            _scriptedHidden.Clear();
            return;
        }
        foreach (var button in GetComponentsInChildren<Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                string method = button.onClick.GetPersistentMethodName(i);
                if (method != "_RefreshClickAds" && method != "_RefreshClickGems" && method != "_NewLevel") continue;
                if (!_scriptedHidden.ContainsKey(button.gameObject)) _scriptedHidden.Add(button.gameObject, button.gameObject.activeSelf);
                button.gameObject.SetActive(false);
            }
    }
    private bool _scriptedChoice;
    private Action<BoostType> _scriptedChosen;
    private readonly HashSet<BoostItem> _scriptedCards = new();
    public bool IsScriptedChoice => _scriptedChoice;
    public bool IsChoiceOpen => _panel.activeSelf;
    public bool OpenScriptedChoice(List<ItemWithRare> items, Action<BoostType> chosen)
    {
        if (_panel.activeSelf || items == null || items.Count != _buttons.Count || items.Exists(x => x.Item == null)) return false;
        _scriptedCards.Clear();
        foreach (var item in items) _scriptedCards.Add(item.Item);
        _scriptedChosen = chosen;
        NewLevel(items, true);
        return true;
    }
    public void CancelScriptedChoice()
    {
        if (!_scriptedChoice) return;
        InvalidateRefresh();
        _scriptedChoice = false;
        _scriptedChosen = null;
        _scriptedCards.Clear();
        _panel.SetActive(false);
        _panelAdsChoise.SetActive(false);
        ScriptedControls(false);
    }
    public void FinishScriptedChoice(BoostItem chosen, RareType rarity)
    {
        if (!_scriptedChoice || !_cardClaimed || !_scriptedCards.Contains(chosen)) return;
        var callback = _scriptedChosen;
        _scriptedChosen = null;
        _scriptedCards.Clear();
        chosen.AddBoost(rarity);
        ShowCardChoice(chosen.BoostIcon, rarity);
        _scriptedChoice = false;
        _panel.SetActive(false);
        ScriptedControls(false);
        _panelAdsChoise.SetActive(false);
        PauseManager.Instance.SetPause(false, false);
        ControlManager.Instance.CursorActive = false;
        callback?.Invoke(chosen.BoostType);
    }
    private BoostItem _item;
    private RareType _rare;
    private int _countLevelUp;
    private bool _cardClaimed;
    private bool _cardApplied;
    private int _offerRevision;
    private bool _refreshPending;
    private bool _upgradePending;
    private void InvalidateRefresh()
    {
        _offerRevision++;
        _refreshPending = false;
        _upgradePending = false;
    }
    private void OnDisable() => InvalidateRefresh();
    private bool CanRefresh => isActiveAndEnabled && IsChoiceOpen && !_scriptedChoice && !_cardClaimed && !_refreshPending;
    private bool BeginRefresh(out int revision)
    {
        revision = 0;
        if (!CanRefresh) return false;
        revision = ++_offerRevision;
        _refreshPending = true;
        return true;
    }
    private void CompleteRefresh(int revision, bool success)
    {
        if (this == null || revision != _offerRevision || !_refreshPending) return;
        _refreshPending = false;
        if (success && CanRefresh) Refresh();
    }
    private CardChoiceFeedback _choiceFeedback;
    private double _nextChoiceAt;
    public void ShowCardChoice(Sprite icon, RareType rarity)
    {
        _nextChoiceAt = Time.unscaledTimeAsDouble + .25; // Prevent click-through to the next offer, independent of FX.
        _choiceFeedback?.Show(icon, rarity);
    }
    public bool TryClaimCard() { if (_refreshPending || _cardClaimed || Time.unscaledTimeAsDouble < _nextChoiceAt) return false; _cardClaimed = true; return true; }
    public void ReleaseCardClaim() { _cardClaimed = false; }
    public bool ApplyNormalChoice(BoostItem item, RareType rarity)
    {
        if (_scriptedChoice || !IsChoiceOpen || !_cardClaimed || _cardApplied) return false;
        _cardApplied = true;
        if (LevelStatManager.Instance.ApplyEarnedChoice(item, rarity)) return true;
        _cardApplied = false;
        return false;
    }
    public RareBack GetRareBackUI(RareType rare) 
    {
        return _rareBack.Find(r => r.Type == rare);
    }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _panel.SetActive(false);
        _choiceFeedback = gameObject.AddComponent<CardChoiceFeedback>();
        _choiceFeedback.Initialize(transform);
        _textPrice.text = _price.ToString();
    }
    public void AddNewLevelCount(int count)
    {
        _buttonLevelUp.SetActive(true);
        _countLevelUp = count;
        _countLevelUpUi.text = _countLevelUp.ToString();
    }
    public void NewLevel(List<ItemWithRare> items, bool scripted = false)
    {
        if (_scriptedChoice || IsChoiceOpen) return;
        InvalidateRefresh();
        _cardClaimed = false;
        _cardApplied = false;
        _scriptedChoice = scripted;
        if (!scripted && TryUpgradeHighestRarity(items, out var upgraded))
        {
            Debug.Log($"Повысили: теперь {upgraded.Rare}");
        }

        PauseManager.Instance.SetPause(true, false);
        ControlManager.Instance.CursorActive = true;
        _panel.SetActive(true);
        if (scripted) ScriptedControls(true);
        _anim.SetTrigger("Start");
        int i = 0;
        foreach (var item in items)
        {
            _buttons[i].AddItemInButton(item.Item, item.Rare,item.IsAds);
            i++;
        }
    }
    public void RefreshLevel(List<ItemWithRare> items)
    {
        if (_scriptedChoice || !IsChoiceOpen) return;
        InvalidateRefresh();
        _cardClaimed = false;
        _cardApplied = false;
        int i = 0;
        foreach (var item in items)
        {
            _buttons[i].AddItemInButton(item.Item, item.Rare,item.IsAds);
            i++;
        }
    }

    public void Close()
    {
        if (_scriptedChoice) return;
        InvalidateRefresh();
        PauseManager.Instance.SetPause(false, false);
        ControlManager.Instance.CursorActive = false;
        _panel.SetActive(false);
        _panelAdsChoise.SetActive(false);
        _price = 1;
        _textPrice.text = _price.ToString();
        bool applied = _cardApplied;
        _cardApplied = false;
        int remaining = LevelStatManager.Instance.PendingUpgrades;
        if (remaining > 0) AddNewLevelCount(remaining);
        else RestartAll();
        if (applied) LevelStatManager.Instance.CheckNewLevel();
    }
    public void RestartAll()
    {
        InvalidateRefresh();
        if (!_scriptedChoice)
        {
            // Recovery/reset must invalidate the old buttons and their ad callbacks too.
            if (IsChoiceOpen)
            {
                PauseManager.Instance?.SetPause(false, false);
                if (ControlManager.Instance != null) ControlManager.Instance.CursorActive = false;
            }
            _panel.SetActive(false);
            _panelAdsChoise.SetActive(false);
            _cardClaimed = _cardApplied = false;
        }
        _buttonLevelUp.SetActive(false);
        _countLevelUp = 0;
    }
    public void _NewLevel()
    {
        if (_scriptedChoice) return;
        LevelStatManager.Instance.CheckNewLevel();
    }
    public void _RefreshClickAds()
    {
        if (DeadBoat.Online.SharedAdProtection.IsShowing || !BeginRefresh(out int revision)) return;
        AdsManager.Instance.ShowRewardedAd(
            "Refresh",
            success => CompleteRefresh(revision, success));
        
    }

    public void _RefreshClickGems()
    {
        if (!CanRefresh || DeadBoat.Online.SharedAdProtection.IsShowing) return;
        if (CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Gems, _price))
        {
            CurrencyManager.Instance.RemoveCurrency(CurrencyType.Gems, _price);
            Refresh();
            _price++;
            _textPrice.text = _price.ToString();

        }

    }
    public void Refresh()
    {
        if (!CanRefresh) return;
        LevelStatManager.Instance.Refresh();
    }
    public void ChoiceBoost(BoostItem item, RareType rare) 
    {
        if (!IsChoiceOpen || _scriptedChoice || !_cardClaimed && !TryClaimCard()) return;
        _item = item;
        _rare = rare;
        if (CombatCardRules.IsSpecial(item.BoostType)) { _NoUpClick(); return; }
        _panelAdsChoise.SetActive(true);
    }
    public void _UpClickAds()
    {
        if (_scriptedChoice || !IsChoiceOpen || _upgradePending || _item == null ||
            _rare >= RareType.Mythic || CombatCardRules.IsSpecial(_item.BoostType)) return;
        _upgradePending = true;
        int revision = _offerRevision;
        var chosen = _item;
        var rarity = _rare;
        bool completed = false;
        AdsManager.Instance.ShowRewardedAd(
            "Refresh",
            (success) =>
            {
                if (this == null || completed || revision != _offerRevision || !IsChoiceOpen || _item != chosen) return;
                completed = true;
                _upgradePending = false;
                if (success)
                {
                    _rare = (RareType)((int)rarity + 1);
                    _NoUpClick();
                }
            });
    }
    public void _NoUpClick()
    {
        if (_scriptedChoice || _upgradePending) return;
        if (!ApplyNormalChoice(_item, _rare)) return;
        Close();
    }
    public static bool TryUpgradeHighestRarity(List<ItemWithRare> list, out ItemWithRare result)
    {
        result = default;

        if (list == null || list.Count == 0)
            return false;

        int numericCount = 0;
        int bestIndex = -1;
        RareType bestRare = RareType.RareType;

        // находим индекс элемента с наибольшей редкостью (первый при равенстве)
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Item == null || CombatCardRules.IsSpecial(list[i].Item.BoostType)) continue;
            numericCount++;
            if (list[i].Rare > bestRare)
            {
                bestRare = list[i].Rare;
                bestIndex = i;
            }
        }

        if (bestIndex < 0 || numericCount < 2) return false; // Keep a free numeric alternative.
        var entry = list[bestIndex]; // struct — копия
        result = entry;

        // пробуем повысить редкость, если не максимум
        if (entry.Rare < RareType.Mythic)
        {
            entry.Rare = (RareType)((int)entry.Rare + 1);
            entry.IsAds = true;
            list[bestIndex] = entry; // важно: положить изменённую копию обратно
            result = entry;
            return true; // удалось повысить
        }

        return false; // уже максимум, повысить нельзя
    }



}

