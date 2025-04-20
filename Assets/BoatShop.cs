using System;
using System.Collections.Generic;
using UnityEngine;

/*[Serializable]
public struct SpecialShopItem
{
    public PickableItem item;
    public int price;
    public CurrencyType currencyType;
}*/

public class BoatShop : MonoBehaviour
{
    [SerializeField] private List<PickableItem> _items;
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
        foreach (PickableItem item in _items)
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


    public void TryBuy(PickableItem itemData)
    {
        if (itemData.TryGetComponent<StoreItem>(out StoreItem item))
        {
            if (CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Gems, item.GemPrice))
            {
                ItemPurchased?.Invoke(itemData);
                CurrencyManager.Instance.RemoveCurrency(CurrencyType.Gems, item.GemPrice);
            }
        }

    }

}
