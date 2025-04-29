using UnityEngine;
using UnityEngine.UI;

public abstract class SpecialShopSlot : MonoBehaviour
{
    [SerializeField] private Text _name;
    [SerializeField] private Text _price;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _currencyIcon;

    public void Init(ItemData itemData, float price, CurrencyType type)
    {
        _name.text = _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(itemData.Name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());
        _icon.sprite = itemData.IMG;

        _price.text = price.ToString();
        _currencyIcon.sprite = CurrencyManager.Instance.GetCurrencyIcon(type);
    }

    public abstract void OnClick();
}
