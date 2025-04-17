using UnityEngine;
using UnityEngine.UI;

public class SpecialShopSlot : MonoBehaviour
{
    [SerializeField] private Text _name;
    [SerializeField] private Text _price;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _currencyIcon;
    public void Init(SpecialShopItem itemData)
    {
        _name.text = _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(itemData.item.Data.Name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());
        _icon.sprite = itemData.item.Data.IMG;
        _price.text = itemData.price.ToString();
        _currencyIcon.sprite = CurrencyManager.Instance.GetCurrencyIcon(itemData.currencyType);
    }
}
