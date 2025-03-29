using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class InventorySlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Transform _container;
    [SerializeField] private InventoryItem _itemIconPrefab;

    //private InventoryItemIcon

    //public ItemData CurrentItem { get { return _itemData; } }
   

    public bool Empty { get { return (_container.childCount == 0); } }


    public void InitSlot(PickableItem item)
    {
        InventoryItem icon = Instantiate(_itemIconPrefab, _container);
        icon.Init(item);
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItem itemIcon = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (_container.childCount == 0)
        {
            itemIcon.SetNewParent(_container);
        } else
        {
            InventoryItem currentChild = _container.GetChild(0).GetComponent<InventoryItem>();
            currentChild.SetNewParent(itemIcon.CurrentParent);
            currentChild.UpdateParent();
            itemIcon.SetNewParent(_container);
        }
    }


}
