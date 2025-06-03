using System.Collections.Generic;
using UnityEngine;

public class GemsShop : MonoBehaviour
{
    [SerializeField] private List<CurrencyPackData> _items;
    [SerializeField] private GameObject _shopCanvas;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private GemsShopSlot _slotPrefab;

    private bool _isOpen;
    public bool Opened => _isOpen;

    private static GemsShop _instance;
    public static GemsShop Instance => _instance;


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("GemsShop уже существует! Удаляем дубликат.");
            Destroy(gameObject);
        }
    }


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
        foreach (CurrencyPackData item in _items)
        {
            GemsShopSlot slot = _grid.SpawnObject<GemsShopSlot>(_slotPrefab.gameObject);
           slot.Init(item, this);
        }
    }

    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        _shopCanvas.gameObject.SetActive(_isOpen);
        ControlManager.Instance.CursorActive = _isOpen;
        if (_isOpen)
            CurrencyManager.Instance.ShowGems?.Invoke(true);
    }


    public void TryBuy(CurrencyPackData packData)
    {
        // TODO: purchase

        PurchasesManager.Instance.BuyPurchase(
            packData.CurrencyType.ToString() + "_" + packData.Amount,
            (success) =>
            {
                if (success)
                {
                    CurrencyManager.Instance.AddCurrency(packData.CurrencyType, packData.Amount);
                }
            });

        
    }
}
