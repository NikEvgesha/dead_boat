using UnityEngine;
using UnityEngine.EventSystems;

public class BagDropPanel : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot slot = eventData.pointerDrag.GetComponent<InventorySlot>();
        if (slot != null && slot.CurrentItem != null && slot.QuickSlot)
        {
            InventorySlot origin = InventoryUI.Instance.GetEmptyBagSlot();
            if (origin != null)
            {
                Inventory.Instance.TrySwitch(slot, origin);
            }
        }

            
    }
}
    
