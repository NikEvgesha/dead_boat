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
    [SerializeField] private List<LevelUpButtomUI> _buttons;
    [SerializeField] private List<RareBack> _rareBack;

    [SerializeField] private int _price = 1;
    [SerializeField] Text _textPrice;

    private BoostItem _item;
    private RareType _rare;
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
            Destroy(Instance.gameObject);
        }
        _panel.SetActive(false);
        _textPrice.text = _price.ToString();
    }
    public void NewLevel(List<ItemWithRare> items)
    {
        if (TryUpgradeHighestRarity(items, out var upgraded))
        {
            Debug.Log($"Повысили: теперь {upgraded.Rare}");
        }

        PauseManager.Instance.SetPause(true, false);
        ControlManager.Instance.CursorActive = true;
        _panel.SetActive(true);
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
        int i = 0;
        foreach (var item in items)
        {
            _buttons[i].AddItemInButton(item.Item, item.Rare,item.IsAds);
            i++;
        }
    }

    public void Close()
    {
        PauseManager.Instance.SetPause(false, false);
        ControlManager.Instance.CursorActive = false;
        _panel.SetActive(false);
        _panelAdsChoise.SetActive(false);
        _price = 1;
        _textPrice.text = _price.ToString();
    }
    public void _RefreshClickAds()
    {
        AdsManager.Instance.ShowRewardedAd(
            "Refresh",
            (success) =>
            {
                if (success)
                    Refresh();
            });
        
    }

    public void _RefreshClickGems()
    {
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
        LevelStatManager.Instance.Refresh();
    }
    public void ChoiceBoost(BoostItem item, RareType rare) 
    {
        _item = item;
        _rare = rare;
        _panelAdsChoise.SetActive(true);
    }
    public void _UpClickAds()
    {
        AdsManager.Instance.ShowRewardedAd(
            "Refresh",
            (success) =>
            {
                if (success)
                {
                    _rare = (RareType)((int)_rare + 1);
                    _NoUpClick();
                }
            });
    }
    public void _NoUpClick()
    {
        _item.AddBoost(_rare);
        Close();
    }
    public static bool TryUpgradeHighestRarity(List<ItemWithRare> list, out ItemWithRare result)
    {
        result = default;

        if (list == null || list.Count == 0)
            return false;

        int bestIndex = 0;
        RareType bestRare = list[0].Rare;

        // находим индекс элемента с наибольшей редкостью (первый при равенстве)
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].Rare > bestRare)
            {
                bestRare = list[i].Rare;
                bestIndex = i;
            }
        }

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

