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
    [SerializeField] private List<LevelUpButtomUI> _buttons;
    [SerializeField] private List<RareBack> _rareBack;

    [SerializeField] private int _price = 1;
    [SerializeField] Text _textPrice;
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

        PauseManager.Instance.SetPause(true, false);
        ControlManager.Instance.CursorActive = true;
        _panel.SetActive(true);
        int i = 0;
        foreach (var item in items)
        {
            _buttons[i].AddItemInButton(item.Item, item.Rare);
            i++;
        }
    }
    public void RefreshLevel(List<ItemWithRare> items)
    {
        int i = 0;
        foreach (var item in items)
        {
            _buttons[i].AddItemInButton(item.Item, item.Rare);
            i++;
        }
    }

    public void Close()
    {
        PauseManager.Instance.SetPause(false, false);
        ControlManager.Instance.CursorActive = false;
        _panel.SetActive(false);
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
        


}

