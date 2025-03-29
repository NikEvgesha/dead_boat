using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropOutPanel : MonoBehaviour, IDropHandler
{

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItem itemIcon = eventData.pointerDrag.GetComponent<InventoryItem>();
        itemIcon.SetNewParent(null);
    }
}
