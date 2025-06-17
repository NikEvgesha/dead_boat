
public class BoatShopSlot : SpecialShopSlot
{
    private PickableItem _itemData;
    private BoatShop _shop;

    public void Init(PickableItem itemData, BoatShop shop)
    {
        if (itemData.TryGetComponent<StoreItem>(out StoreItem item))
        {
            base.Init(itemData.Data, item.GemPrice.ToString(), CurrencyType.Gems);
            _itemData = itemData;
            _shop = shop;
        }
    }

    public override void OnClick()
    {
        _shop.TryBuy(_itemData);
    }
}
