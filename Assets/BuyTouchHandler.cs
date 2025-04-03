using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuyTouchHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action PointerDown;
    public bool Hold { get; private set; }
    public void OnPointerDown(PointerEventData eventData)
    {
        Hold = true;
        PointerDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Hold = false;
    }
}
