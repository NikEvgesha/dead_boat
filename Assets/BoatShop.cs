using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SpecialShopItem
{
    public PickableItem item;
    public int price;
    public CurrencyType currencyType;
}

public class BoatShop : MonoBehaviour
{
    [SerializeField] private List<SpecialShopItem> _items;
    [SerializeField] private Canvas _shopCanvas;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private BoatShopSlot _slotPrefab;

    private bool _isOpen;
    public bool Opened => _isOpen;

    public Action<PickableItem> ItemPurchased;


    private void Start()
    {
        InitSlots();
    }


    public void InitSlots()
    {
        foreach (SpecialShopItem item in _items)
        {
            BoatShopSlot slot = _grid.SpawnObject<BoatShopSlot>(_slotPrefab.gameObject);
            slot.Init(item, this);
        }
    }

    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        _shopCanvas.gameObject.SetActive(_isOpen);
        ControlManager.Instance.CursorActive = _isOpen;
        CurrencyManager.Instance.ShowGems?.Invoke(_isOpen);
    }


    public void TryBuy(SpecialShopItem itemData)
    {
        if (CurrencyManager.Instance.CheckEnoughCurrency(itemData.currencyType, itemData.price))
        {
            ItemPurchased?.Invoke(itemData.item);
            CurrencyManager.Instance.RemoveCurrency(itemData.currencyType, itemData.price);
        }
    }

}
