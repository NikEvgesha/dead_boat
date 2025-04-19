using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    [SerializeField] private InventorySlot _InventorySlotPrefab;
    [SerializeField] private InventorySlot _QuickSlotPrefab;

    [SerializeField] private Transform _inventorySlotsParent;
    [SerializeField] private Transform _quickSlotsParent;

    [SerializeField] private Text _capacityText;
    [SerializeField] private Text _capacityButtonText;
    [SerializeField] private GameObject _dropOutPanel;
    [SerializeField] private ScrollRect _scrollRect;

    private static InventoryUI _instance;
    public static InventoryUI Instance { get { return _instance; } }

    private List<InventorySlot> _mainSlots = new();
    private List<InventorySlot> _quickSlots = new();

    private int _quickPanelCapacity;
    private int _bagCapacity;
    private bool _isOpen;
    private InventorySlot _activeSlot;
    private bool _initialized;

    public bool IsOpen => _isOpen;

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
        if (!_initialized)
        {
            _quickPanelCapacity = Inventory.Instance.QuickPanelCapacity;
            _bagCapacity = Inventory.Instance.BagCapacity;
            for (int i = 0; i < _quickPanelCapacity; i++)
            {
                InventorySlot slot = Instantiate(_QuickSlotPrefab, _quickSlotsParent);
                slot.QuickSlot = true;
                _quickSlots.Add(slot);
                slot.InitSlot(null);
                slot.ID = i;
            }


            for (int i = 0; i < _bagCapacity; i++)
            {
                InventorySlot slot = Instantiate(_InventorySlotPrefab, _inventorySlotsParent);
                slot.QuickSlot = false;
                _mainSlots.Add(slot);
                slot.InitSlot(null);
                slot.ID = i;
            }
            _initialized = true;
        }
        
    }

    private void Update()
    {
        if (PlayerInput.Instance.Inventory)
        {
            ToggleOpen();
        }
    }


    public void ToggleOpen()
    {
        if (!_isOpen)
        {
            _panel.SetActive(true);
            _isOpen = true;
            ControlManager.Instance.CursorActive = true;
            for (int i = 0; i < _quickPanelCapacity; i++)
            {
                _quickSlots[i].gameObject.SetActive(true);
            }
        } else
        {
            if (!ControlManager.Instance.UseTouchControl)
                ControlManager.Instance.CursorActive = false;
            for (int i = 0; i < _quickPanelCapacity; i++)
            {
                if (_quickSlots[i].Empty)
                    _quickSlots[i].gameObject.SetActive(false);
            }
            _panel.SetActive(false);
            _isOpen = false;
        }
    }


    public void OnItemDrag(bool dragging)
    {
        _dropOutPanel.SetActive(dragging);
    }


    public void AddItemToBag(PickableItem item, int id = -1)
    {

        if (id == -1)
        {
            for (int i = 0; i < _bagCapacity; i++)
            {
                if (_mainSlots[i].Empty)
                {
                    _mainSlots[i].gameObject.SetActive(true);
                    _mainSlots[i].InitSlot(item);
                    return;
                }
            }
        }
        else
        {
            if (id < _bagCapacity)
            {
                _mainSlots[id].gameObject.SetActive(true);
                _mainSlots[id].InitSlot(item);
            }
        }

    }


    public void AddItemToQuickPanel(PickableItem item, int id = -1)
    {
        if (id == -1)
        {
            for (int i = 0; i < _quickPanelCapacity; i++)
            {
                if (_quickSlots[i].Empty)
                {
                    _quickSlots[i].gameObject.SetActive(true);
                    _quickSlots[i].InitSlot(item);
                    return;
                }
            }
        } else
        {
            if (id < _quickPanelCapacity)
            {
                _quickSlots[id].gameObject.SetActive(true);
                _quickSlots[id].InitSlot(item);
            }
        }
        
    }


    public void UpdateCapacity(int occupied, int total)
    {
        _capacityText.text = occupied + "/" + total;
        _capacityButtonText.text = _capacityText.text;
    }


    public InventorySlot GetEmptyBagSlot()
    {
        for (int i = 0; i < _bagCapacity; i++)
        {
            if (_mainSlots[i].Empty)
            {
                return _mainSlots[i];
            }
        }
        return null;
    }

/*    private void Update()
    {
        if (Inventory.Instance.ActiveItem == null) return;

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int id = _activeItemID;
            if (scroll < 0)
            {

                SwitchActiveItem(_quickPanelItems[(_activeItemID + 1) % _quickPanelItems.Count]);
            }
            else
            {
                int newId = (_activeItemID - 1) >= 0 ? _activeItemID - 1 : _quickPanelItems.Count - 1;
                SwitchActiveItem(_quickPanelItems[newId]);
            }
            }
        }
    }
*/


    public PickableItem GetNextUsable(PickableItem current)
    {
        PickableItem next = null;
        bool after = false;
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            if (_quickSlots[i].Empty) continue;
            if (next == null)
            {
                next = _quickSlots[i].CurrentItem;
            }
            if (after)
            {
                return _quickSlots[i].CurrentItem;
            }

            if (_quickSlots[i].CurrentItem == current)
                after = true;
        }
        return next;
    }


    public PickableItem GetPrevUsable(PickableItem current)
    {
        PickableItem prev = null;
        bool before = false;
        for (int i = _quickPanelCapacity - 1; i >= 0; i--)
        {
            if (_quickSlots[i].Empty) continue;
            if (prev == null)
            {
                prev = _quickSlots[i].CurrentItem;
            }
            if (before)
            {
                return _quickSlots[i].CurrentItem;
            }

            if (_quickSlots[i].CurrentItem == current)
                before = true;
        }
        return prev;
    }


    public void SetActiveItem(PickableItem item)
    {
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            _quickSlots[i].SwitchActive(item != null && _quickSlots[i].CurrentItem == item);
        }
    }


    public void Reset()
    {
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            _quickSlots[i].InitSlot(null);
        }

        for (int i = 0; i < _bagCapacity; i++)
        {
            _mainSlots[i].InitSlot(null);
        }
    }

}
