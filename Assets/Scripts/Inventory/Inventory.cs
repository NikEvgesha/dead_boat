using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _capacity = 10;
    [SerializeField] private int _available = 25;
    [SerializeField] private int _quickSlotsCapacity = 5;
    [SerializeField] private Transform _dropOutPoint;

    private static Inventory _instance;
    public static Inventory Instance { get { return _instance; } }

    private List<PickableItem> _bagItems;
    private List<PickableItem> _quickPanelItems;

    public Action<PickableItem> ItemDropOut;

    public int QuickPanelCapacity { get { return _quickSlotsCapacity; } }


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
        _bagItems = new();
        _quickPanelItems = new();
        //InventoryUI.Instance.SpawnSlots(_capacityTotal - _quickSlotsCapacity, _quickSlotsCapacity);
        //InventoryUI.Instance.UpdateCapacity(_items.Count, _capacityTotal);
    }



    public bool AddItem(PickableItem item)
    {
        bool added = false;
        
        if (item.gameObject.GetComponent<UsableItem>() != null)
        {
           
            if (_quickPanelItems.Count < _quickSlotsCapacity)
            {
                Debug.Log("add usable");
                _quickPanelItems.Add(item);
                InventoryUI.Instance.AddItemToQuickPanel(item);
                added = true;
            } 
        }

        if (!added)
        {
            if (_bagItems.Count < _capacity)
            {
                _bagItems.Add(item);
                InventoryUI.Instance.AddItemToBag(item);
                InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
                added = true;
            }
        }

        if (added)
        {
            item.transform.SetParent(transform);
            return true;
        }

        return false;
    }


    public void DropOutItem(PickableItem item, InventorySlot slot)
    {
        item.DropOutFromInventory(_dropOutPoint);
        if (_quickPanelItems.Contains(item)) {
            _quickPanelItems.Remove(item);
        } else
        {
            _bagItems.Remove(item);
            InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
        }

        slot.InitSlot(null);
            
        //ItemDropOut?.Invoke(item);
    }

}
