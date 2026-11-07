using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class LevelUpButtomUI : MonoBehaviour
{
    [SerializeField] BoostItem _item;
    [SerializeField] RareType _rare;
    [SerializeField] Button _button;
    [SerializeField] Text _title;
    [SerializeField] Image _icon;
    [SerializeField] Text _rareText;
    [SerializeField] Image _rareBack;
    [SerializeField] GameObject _adsIcon;
    [SerializeField] Text _boostText;
    [SerializeField] Text _description;
    bool _isAds;
    bool _claimPending;
    bool _rewardGranted;
    int _offerVersion;
    CardFeelFeedback _feel;


    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(UseButton);
        _feel = gameObject.AddComponent<CardFeelFeedback>();
        var visual = transform.Find("Point/LevelUpElement");
        _feel.Initialize(visual != null ? visual : _icon.transform);
    }
    private void UseButton()
    {
        if (DeadBoat.Online.SharedAdProtection.IsShowing || _claimPending || !LevelUpUi.Instance.TryClaimCard()) return;
        _claimPending = true;
        int version = _offerVersion;
        var chosen = _item;
        var rarity = _rare;
        _feel?.Press();
        if (GetComponent<UISound>() == null) SoundManager.Instance?.PlayUIClick();
        if (_isAds)
            AdsManager.Instance.ShowRewardedAd("ChoiseCard", success => CompleteChoice(version, chosen, rarity, success));
        else if (!LevelUpUi.Instance.IsScriptedChoice && !LevelStatManager.Instance.HaveLevelUp())
            AdsManager.Instance.ShowInterstitialAd(success => CompleteChoice(version, chosen, rarity, true));
        else
            CompleteChoice(version, chosen, rarity, true);
    }
    private void CompleteChoice(int version, BoostItem chosen, RareType rarity, bool success)
    {
        // A callback from an old/closed offer cannot apply to a new card or grant twice.
        if (this == null || !isActiveAndEnabled || version != _offerVersion || !_claimPending || _rewardGranted) return;
        if (!success)
        {
            _claimPending = false;
            LevelUpUi.Instance.ReleaseCardClaim();
            _feel?.ResetVisual();
            return;
        }
        _rewardGranted = true;
        if (LevelUpUi.Instance.IsScriptedChoice)
        {
            LevelUpUi.Instance.FinishScriptedChoice(chosen, rarity);
            return;
        }
        if (!LevelUpUi.Instance.ApplyNormalChoice(chosen, rarity))
        {
            _rewardGranted = false;
            _claimPending = false;
            LevelUpUi.Instance.ReleaseCardClaim();
            return;
        }
        LevelUpUi.Instance.ShowCardChoice(chosen.BoostIcon, rarity);
        LevelUpUi.Instance.Close();
    }
    private void OnDisable()
    {
        _offerVersion++;
        if (_claimPending && LevelUpUi.Instance != null) LevelUpUi.Instance.ReleaseCardClaim();
        _claimPending = false;
    }
    public void AddItemInButton(BoostItem item,RareType rare,bool isAds)
    {
        _offerVersion++;
        _claimPending = false;
        _rewardGranted = false;
        _item = item;
        _rare = rare;
        _isAds = isAds && !CombatCardRules.IsSpecial(item.BoostType);
        _adsIcon.SetActive(_isAds);
        _title.text = GetText(item.BoostType.ToString(), BoostUI.Title) ;
        _icon.sprite = item.BoostIcon;
        RareBack rareBack = LevelUpUi.Instance.GetRareBackUI(rare);
        _rareText.text = GetText(rare);
        _rareBack.sprite = rareBack.sprite;
        _boostText.text = item.GetReward(rare) +" "+ GetText(item.BoostType.ToString(), BoostUI.Useble);
        _description.text = GetText(item.BoostType.ToString(), BoostUI.Discription);
        _description.resizeTextForBestFit = CombatCardRules.IsSpecial(item.BoostType);
        _feel?.Present(rare);
        if (CombatCardRules.IsSpecial(item.BoostType))
        {
            int rank = CombatCardRules.Rank(LevelStatManager.Instance.Stats, item.BoostType);
            float maximum = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.MaxHealth : 100;
            float after = CombatCardRules.PreviewHealth(maximum, LevelStatManager.Instance.Stats.BloodPactRank, LevelStatManager.Instance.Stats.HeavyCoatRank, item.BoostType);
            _title.text = CombatCardText.Title(item.BoostType);
            _boostText.text = CombatCardText.RankPreview(rank);
            _description.resizeTextMinSize = 12;
            _description.resizeTextMaxSize = _description.fontSize;
            _description.text = CombatCardText.Description(item.BoostType, rank + 1, maximum, after);
        }
    }
    private string GetText(string key, BoostUI boostUI)
    {
        if (System.Enum.TryParse<BoostType>(key, out var special) && CombatCardRules.IsSpecial(special)) return CombatCardText.Title(special);
        if (key == nameof(BoostType.InventorySlots)) return InventoryExpansionText.Get(boostUI);
        LocalizationData lData = LocalizationManager.Instance.LocalizationData;

        return lData.GetTranslation(lData.GetTranslation(BoostType.Boost.ToString()) + key +"/"+ boostUI.ToString());
    }

    private string GetText(RareType boostUI)
    {
        LocalizationData lData = LocalizationManager.Instance.LocalizationData;

        return lData.GetTranslation(lData.GetTranslation(BoostType.Boost.ToString()) + lData.GetTranslation(RareType.RareType.ToString()) + boostUI.ToString());
    }
}
