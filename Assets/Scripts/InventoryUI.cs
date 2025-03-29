using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private DynamicGridSpawner _mainInventoryGrid;
    [SerializeField] private DynamicGridSpawner _qickPanelGrid;
    [SerializeField] private InventorySlot _slotPrefab; // ItemSlot
    [SerializeField] private Text _capacityText;
    [SerializeField] private GameObject _dropOutPanel;

    private static InventoryUI _instance;
    public static InventoryUI Instance { get { return _instance; } }

    private List<InventorySlot> _mainSlots = new();
    private List<InventorySlot> _quickSlots = new();

    private int _mainCapacity;
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

    public void Open()
    {
        _panel.SetActive(true);
        ControlManager.Instance.CursorActive = true;
    }

    public void Close()
    {
        _panel.SetActive(false);
        ControlManager.Instance.CursorActive = false;
    }

    public void SpawnSlots(int mainCapacity, int quickCapacity)
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
    }


    public void OnItemDrag(bool dragging)
    {
        _dropOutPanel.SetActive(dragging);
    }


    public void AddItem(PickableItem item)
    {
        bool added = false;

        for (int i = 0; i < _quickPanelCapacity; i++)
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
        }

    }


    public void UpdateCapacity(int occupied, int total)
    {
        _capacityText.text = occupied + "/" + total;
    }


}
