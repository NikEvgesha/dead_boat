
using UnityEngine;

public class GemsShopSlot : SpecialShopSlot
{
    CurrencyPackData _packData;
    PurchaseData _purchaseData;
    private GemsShop _shop;
    public void Init(CurrencyPackData packData, PurchaseData purchaseData, GemsShop shop)
    {
        base.Init(packData.Data, purchaseData.Price, packData.PriceCurrencyType);
        _packData = packData;
        _purchaseData = purchaseData;
        _shop = shop;
    }

    public override void OnClick()
    {
        _shop.TryBuy(_purchaseData, _packData);
    }

    public void InitImage(Sprite image)
    {
        _currencyIcon.sprite = image;
    }

}
