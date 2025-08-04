
using UnityEngine;

public class BoatShopSlot : SpecialShopSlot
{
    private SpecialShopItem _shopItem;
    private BoatShop _shop;
    private bool _adReward;

    [SerializeField] GameObject _adRewardButton;
    [SerializeField] GameObject _gemsButton;

    public void Init(SpecialShopItem itemData, BoatShop shop)
    {
        if (itemData.item.TryGetComponent<StoreItem>(out StoreItem item))
        {
            base.Init(itemData.item.Data, item.GemPrice.ToString(), CurrencyType.Gems);
            _shopItem = itemData;
            _shop = shop;
            _adReward = itemData.adReward;
            _adRewardButton.SetActive(itemData.adReward);
            _gemsButton.SetActive(!itemData.adReward);
        }
    }

    public override void OnClick()
    {
       _shop.TryBuy(_shopItem);       
    }
}
