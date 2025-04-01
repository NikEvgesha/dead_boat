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

    private static InventoryUI _instance;
    public static InventoryUI Instance { get { return _instance; } }

    private List<InventorySlot> _mainSlots = new();
    private List<InventorySlot> _quickSlots = new();

    private int _quickPanelCapacity;

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
        _quickPanelCapacity = Inventory.Instance.QuickPanelCapacity;
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            InventorySlot slot = Instantiate(_QuickSlotPrefab, _quickSlotsParent);
            _quickSlots.Add(slot);
            slot.InitSlot(null);
            slot.gameObject.SetActive(false);
        }
    }

    public void Open()
    {
        _panel.SetActive(true);
        ControlManager.Instance.CursorActive = true;
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            _quickSlots[i].gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        ControlManager.Instance.CursorActive = false;
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            if (_quickSlots[i].Empty)
                _quickSlots[i].gameObject.SetActive(false);
        }
        _panel.SetActive(false);
    }

/*    public void SpawnSlots(int mainCapacity, int quickCapacity)
    {
        _mainCapacity = mainCapacity;
        _quickPanelCapacity = quickCapacity;
        for (int i = 0; i < mainCapacity; i++)
        {
            InventorySlot slot = _mainInventoryGrid.SpawnObject<InventorySlot>(_slotPrefab.gameObject);
            _mainSlots.Add(slot);
        }

        for (int i = 0; i < quickCapacity; i++)
        {
            InventorySlot slot = _qickPanelGrid.SpawnObject<InventorySlot>(_slotPrefab.gameObject);
            _quickSlots.Add(slot);
        }
    }*/


    public void OnItemDrag(bool dragging)
    {
        //_dropOutPanel.SetActive(dragging);
    }


    public void AddItemToBag(PickableItem item)
    {
        InventorySlot slot = Instantiate(_InventorySlotPrefab, _inventorySlotsParent);
        _mainSlots.Add(slot);
        slot.InitSlot(item);




        /*        for (int i = 0; i < _quickPanelCapacity; i++)
                {
                    if (_quickSlots[i].Empty)
                    {
                        _quickSlots[i].InitSlot(item);
                        added = true;
                        break;
                    }
                }

                if (added) return;

                for (int i = 0; i < _mainCapacity; i++)
                {
                    if (_mainSlots[i].Empty)
                    {
                        _mainSlots[i].InitSlot(item);
                        added = true;
                        break;
                    }
                }*/

    }


    public void AddItemToQuickPanel(PickableItem item)
    {
        //bool added = false;
        Debug.Log("adding");
        for (int i = 0; i < _quickPanelCapacity; i++)
        {
            if (_quickSlots[i].Empty)
            {
                Debug.Log("added");
                _quickSlots[i].gameObject.SetActive(true);
                _quickSlots[i].InitSlot(item);
                return;
            }
        }
    }


    public void UpdateCapacity(int occupied, int total)
    {
        _capacityText.text = occupied + "/" + total;
        _capacityButtonText.text = _capacityText.text;
    }


}
