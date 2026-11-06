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


    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(UseButton);
    }
    private void UseButton()
    {
        if (_claimPending || !LevelUpUi.Instance.TryClaimCard()) return;
        _claimPending = true;
        //var values = Enum.GetValues(typeof(RareType)) as RareType[];
       // var lastValue = values[values.Length - 1];
       if (_isAds)
        {
            AdsManager.Instance.ShowRewardedAd(
            "ChoiseCard",
            (success) =>
            {
                if (success)
                {
                    StartCoroutine(AddRevard());
                }
                else { _claimPending = false; LevelUpUi.Instance.ReleaseCardClaim(); }
            });
        }
        else if (!LevelStatManager.Instance.HaveLevelUp())
        {
            AdsManager.Instance.ShowInterstitialAd(
                (success) =>
                {
                    StartCoroutine(AddRevard());
                }
                );
        }
        else
        {
            StartCoroutine(AddRevard());
        }

        /*if (_rare == lastValue)
        {
            _item.AddBoost(_rare);
            LevelUpUi.Instance.Close();
            return;
        }
        LevelUpUi.Instance.ChoiceBoost(_item,_rare);*/
    }
    private IEnumerator AddRevard()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        _item.AddBoost(_rare);
        LevelUpUi.Instance.Close();
    }
    public void AddItemInButton(BoostItem item,RareType rare,bool isAds)
    {
        _claimPending = false;
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
        if (CombatCardRules.IsSpecial(item.BoostType))
        {
            int rank = CombatCardRules.Rank(LevelStatManager.Instance.Stats, item.BoostType);
            float maximum = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.MaxHealth : 100;
            float after = item.BoostType == BoostType.BloodPact && rank == 0 ? CombatCardRules.MaximumHealth(maximum, 1) : maximum;
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
