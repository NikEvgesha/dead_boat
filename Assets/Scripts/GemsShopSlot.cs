
public class GemsShopSlot : SpecialShopSlot
{
    CurrencyPackData _packData;
    private GemsShop _shop;
    public void Init(CurrencyPackData packData, GemsShop shop)
    {
        base.Init(packData.Data, packData.Price, packData.PriceCurrencyType);
        _packData = packData;
        _shop = shop;
    }

    public override void OnClick()
    {
        _shop.TryBuy(_packData);
    }
}
