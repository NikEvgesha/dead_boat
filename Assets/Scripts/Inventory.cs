using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _capacityTotal = 25;
    [SerializeField] private int _available = 25;
    [SerializeField] private int _quickSlotsCapacity = 5;
    [SerializeField] private Transform _dropOutPoint;

    private static Inventory _instance;
    public static Inventory Instance { get { return _instance; } }

    private List<PickableItem> _items;


    public Action<PickableItem> ItemDropOut;


    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        _items = new();
        InventoryUI.Instance.SpawnSlots(_capacityTotal - _quickSlotsCapacity, _quickSlotsCapacity);
        InventoryUI.Instance.UpdateCapacity(_items.Count, _capacityTotal);
    }



    public bool AddItem(PickableItem item)
    {

        if (_available <= _items.Count)
        {
            return false;
        }

        _items.Add(item);
        item.transform.SetParent(transform);

        InventoryUI.Instance.AddItem(item);
        InventoryUI.Instance.UpdateCapacity(_items.Count, _capacityTotal);
        return true;
    }


    public void DropOutItem(PickableItem item)
    {
        item.DropOutFromInventory(_dropOutPoint);
        _items.Remove(item);
        InventoryUI.Instance.UpdateCapacity(_items.Count, _capacityTotal);
        //ItemDropOut?.Invoke(item);
    }

}
