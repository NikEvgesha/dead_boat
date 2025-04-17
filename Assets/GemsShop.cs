using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CurrencyPackItem
{
    public CurrencyPackData packData;
    public int price;
    public CurrencyType currencyType;
}
public class GemsShop : MonoBehaviour
{
    [SerializeField] private List<CurrencyPackItem> _items;
    [SerializeField] private Canvas _shopCanvas;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private GemsShopSlot _slotPrefab;

    private bool _isOpen;
    public bool Opened => _isOpen;


    private void Start()
    {
        InitSlots();
        CurrencyManager.Instance.NoGems += ToggleOpen;
    }

    private void OnDisable()
    {
        CurrencyManager.Instance.NoGems -= ToggleOpen;
    }


    public void InitSlots()
    {
        foreach (CurrencyPackItem item in _items)
        {
            GemsShopSlot slot = _grid.SpawnObject<GemsShopSlot>(_slotPrefab.gameObject);
           slot.Init(item, this);
        }
    }

    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        _shopCanvas.gameObject.SetActive(_isOpen);
        //ControlManager.Instance.CursorActive = _isOpen;
    }


    public void TryBuy(CurrencyPackItem packData)
    {
        // TODO: purchase

        CurrencyManager.Instance.AddCurrency(packData.packData.CurrencyType, packData.packData.Amount);
    }
}
