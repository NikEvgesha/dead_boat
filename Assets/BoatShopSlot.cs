
public class BoatShopSlot : SpecialShopSlot
{
    private SpecialShopItem _itemData;
    private BoatShop _shop;

    public void Init(SpecialShopItem itemData, BoatShop shop)
    {
        base.Init(itemData.item.Data, itemData.price, itemData.currencyType);
        _itemData = itemData;
        _shop = shop;
    }

    public override void OnClick()
    {
        _shop.TryBuy(_itemData);
    }
}
