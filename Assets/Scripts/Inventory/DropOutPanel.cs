using UnityEngine;
using UnityEngine.EventSystems;

public class DropOutPanel : MonoBehaviour, IDropHandler
{

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot slot = eventData.pointerDrag.GetComponent<InventorySlot>();
        if (slot != null && slot.CurrentItem != null)
            slot.SetNewParent(null);
    }
}
