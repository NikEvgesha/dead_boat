using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _capacity = 10;
    //[SerializeField] private int _available = 25;
    [SerializeField] private int _quickSlotsCapacity = 5;
    [SerializeField] private Transform _dropOutPoint;
    [SerializeField] private ActiveItemManager _activeItemManager;

    private static Inventory _instance;
    public static Inventory Instance { get { return _instance; } }

    private List<PickableItem> _bagItems = new();
    private List<PickableItem> _quickPanelItems = new();
    private StarterPackManager _starterPack;


    public Action<PickableItem> ItemDropOut;

    public int QuickPanelCapacity { get { return _quickSlotsCapacity; } }
    public int BagCapacity { get { return _capacity; } }

    public Action<PickableItem> ItemActivation;



    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _activeItemManager = GetComponent<ActiveItemManager>();
        _starterPack = GetComponent<StarterPackManager>();
    }

    private void Start()
    {
        InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
        //InventoryUI.Instance.SpawnSlots(_capacityTotal - _quickSlotsCapacity, _quickSlotsCapacity);
        //InventoryUI.Instance.UpdateCapacity(_items.Count, _capacityTotal);
        StartCoroutine(SaveInventory());
    }


    private IEnumerator SaveInventory()
    {
        while (this.enabled && LoadingManager.Instance.CurrentLocation == Location.Game)
        {
            yield return new WaitForSeconds(1);
            SaveManager.Instance.SaveInventory();
        }
    }

    private IEnumerator AddStarterPack()
    {
        yield return null;
        SetStartItems(_starterPack.GetStartItems());
    }

    public void ResetInventory()
    {
        _activeItemManager.SwitchActiveItem(null);
        _bagItems.Clear();
        _quickPanelItems.Clear();
        InventoryUI.Instance.Reset();
        SetStartItems(_starterPack.GetStartItems());
    }

    public void SetStartItems(ReadOnlyCollection<PickableItem> items)
    {
        foreach (PickableItem item in items) {

            PickableItem itemObj = Instantiate(item, null);
            AddItem(itemObj);
        }

    }


    public bool AddItem(PickableItem item)
    {
        bool added = false;

        if (item.GetComponent<AmmoItem>() != null)
        {
            item.PutToInventory();
            return true;
        }
        
        if (item.Usable)
        {
           
            if (_quickPanelItems.Count < _quickSlotsCapacity)
            {
                _quickPanelItems.Add(item);
                InventoryUI.Instance.AddItemToQuickPanel(item);
                added = true;
                if (_activeItemManager.Active == null)
                {
                    item.PutToInventory();
                    _activeItemManager.SwitchActiveItem(item);
                    return true;
                }
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
            } else
            {
                InventoryUI.Instance.DisplayNoSpaceHint();
            }
        }



        if (added)
        {
            item.PutToInventory();
            item.transform.SetParent(transform);
            item.gameObject.SetActive(false);  
            return true;
        }

        return false;
    }


    public void DropOutItem(PickableItem item, InventorySlot slot)
    {
        if (item == null || LoadingManager.Instance.CurrentLocation == Location.Lobby) return;
        if (_quickPanelItems.Contains(item)) {
            _quickPanelItems.Remove(item);
            slot.InitSlot(null);
            PickableItem nextItem = InventoryUI.Instance.GetNextUsable(item);
            nextItem = nextItem == item ? null : nextItem;
            _activeItemManager.SwitchActiveItem(nextItem);
        } else
        {
            _bagItems.Remove(item);
            InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
            slot.InitSlot(null);
        }
        
        item.DropOutFromInventory(_dropOutPoint);
        item.gameObject.SetActive(true);



        //ItemDropOut?.Invoke(item);
    }




    public void TrySwitch(InventorySlot dropped, InventorySlot origin)
    {
        bool activeUpdated = false;
        if (_activeItemManager.Active == origin.CurrentItem || _activeItemManager == dropped.CurrentItem)
        {
            activeUpdated = true;
        }
        // Перенос из сумки
        if (!dropped.QuickSlot)
        {
            // из сумки в сумку перенести нельзя
            if (!origin.QuickSlot) return;

            // из сумки в пустой слот быстрого доступа
            if (origin.Empty)
            {
                _bagItems.Remove(dropped.CurrentItem);
                _quickPanelItems.Add(dropped.CurrentItem);
                InventoryUI.Instance.AddItemToQuickPanel(dropped.CurrentItem, origin.ID);
                dropped.InitSlot(null);
                InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
            }
            else   // из сумки в занятый слот быстрого доступа
            {
                if (_activeItemManager.Active == origin.CurrentItem)
                {
                    activeUpdated = true;
                }
                PickableItem tmp = dropped.CurrentItem;
                _bagItems.Remove(dropped.CurrentItem);
                dropped.InitSlot(null);
                InventoryUI.Instance.AddItemToBag(origin.CurrentItem);
                _quickPanelItems.Remove(origin.CurrentItem);
                InventoryUI.Instance.AddItemToQuickPanel(tmp, origin.ID);
                InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
                if (activeUpdated)
                {
                    _activeItemManager.SwitchActiveItem(origin.CurrentItem);
                }
            }
        } else
        // Перенос из быстрого доступа
        {
            // в сумку
            if (!origin.QuickSlot)
            {
                if (_activeItemManager.Active == dropped.CurrentItem)
                {
                    activeUpdated = true;
                }
                if (_bagItems.Count < _capacity)
                {
                    _bagItems.Add(dropped.CurrentItem);
                    InventoryUI.Instance.AddItemToBag(dropped.CurrentItem);
                    _quickPanelItems.Remove(dropped.CurrentItem);
                    dropped.InitSlot(null);
                    InventoryUI.Instance.UpdateCapacity(_bagItems.Count, _capacity);
                } else
                {
                    InventoryUI.Instance.DisplayNoSpaceHint();
                }
                if (activeUpdated)
                {
                    _activeItemManager.SwitchActiveItem(null);
                }


            } else
            {
                // перенос между слотами быстрого доступа
                if (origin.Empty)
                {
                    activeUpdated = (_activeItemManager.Active == dropped.CurrentItem);
                    InventoryUI.Instance.AddItemToQuickPanel(dropped.CurrentItem, origin.ID);
                    if (activeUpdated)
                    {
                        _activeItemManager.SwitchActiveItem(null);
                    }
                    dropped.InitSlot(null);
                }
                else
                {
                    activeUpdated = (_activeItemManager.Active == dropped.CurrentItem || _activeItemManager.Active == origin.CurrentItem);
                    PickableItem tmp = dropped.CurrentItem;
                    //dropped.InitSlot(null);
                    InventoryUI.Instance.AddItemToQuickPanel(origin.CurrentItem, dropped.ID);
                    InventoryUI.Instance.AddItemToQuickPanel(tmp, origin.ID);
                    if (activeUpdated)
                    {
                        _activeItemManager.SwitchActiveItem(origin.CurrentItem);
                    }
                }
            }
            
        }

        InventoryUI.Instance.OnItemDrag(false);


    }
   
    public void SetActiveItem(PickableItem item)
    {
        _activeItemManager.SwitchActiveItem(item);
    }
    public List<PickableItem> GetItems()
    {
        List<PickableItem> allItem = new List<PickableItem>();
        foreach (var item in _bagItems)
        {
            allItem.Add(item);
        }
        foreach (var item in _quickPanelItems)
        {
            allItem.Add(item);
        }
        return allItem;
    }

    public bool CheckSpace(bool isUsable)
    {
        bool haveEmptySlot = false;
        if (isUsable)
        {
            haveEmptySlot = _quickPanelItems.Count < _quickSlotsCapacity;
        }
        haveEmptySlot = _bagItems.Count < _capacity;

        if (!haveEmptySlot)
        {
            InventoryUI.Instance.DisplayNoSpaceHint();
        }

        return haveEmptySlot;
    }


    public List<PickableItem> GetInventoryList()
    {
        return _bagItems.Concat(_quickPanelItems).ToList();
    }

    public void SetLoadedInventory(List<string> items = null)
    {
        List<PickableItem> itemPrefabs = new();
        if (items == null)
        {
            ResetInventory();
            items = SaveManager.Instance.LoadLobbyItems();
            //SetStartItems(_starterPack.GetStartItems());
        }
        foreach (var id in items)
        {
            PickableItem item = ItemsManager.Instance.GetItem(id);
            if (item != null)
            {
                itemPrefabs.Add(item);
            }
                
        }
        SetStartItems(itemPrefabs.AsReadOnly());
    }
}
