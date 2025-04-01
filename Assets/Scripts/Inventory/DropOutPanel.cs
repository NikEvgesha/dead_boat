using UnityEngine;
using UnityEngine.EventSystems;

public class DropOutPanel : MonoBehaviour, IDropHandler
{

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("drop: " + eventData.pointerDrag.name);
        InventorySlot item = eventData.pointerDrag.GetComponent<InventorySlot>();
        if (item != null)
            item.SetNewParent(null);
    }
}
