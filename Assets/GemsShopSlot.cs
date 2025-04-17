
public class GemsShopSlot : SpecialShopSlot
{
    CurrencyPackItem _packData;
    private GemsShop _shop;
    public void Init(CurrencyPackItem packData, GemsShop shop)
    {
        base.Init(packData.packData.Data, packData.price, packData.currencyType);
        _packData = packData;
        _shop = shop;
    }

    public override void OnClick()
    {
        _shop.TryBuy(_packData);
    }
}
