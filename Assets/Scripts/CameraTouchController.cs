using UnityEngine;
using UnityEngine.EventSystems;

public class CameraTouchController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler { 
    [SerializeField] private RectTransform _touchArea;
    [SerializeField] private float _dragSpeed = 0.1f;
    [SerializeField] private float _deadZone = 2f;

    private bool _isDragging;
    private Vector2 _startPos;
    private Vector2 _input;

    private void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;
            if (touch.phase == TouchPhase.Began)
            {
                StartDrag(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && _isDragging)
            {
                Drag(touch.position);
            } else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
                EndDrag();
            }
        }
    }

    public void StartDrag(Vector3 startPos)
    {
        _startPos = startPos;
        _isDragging = true;
    }

    public void Drag(Vector3 pointerPos)
    {
        if (!_isDragging) return;

        Vector2 currentPos = pointerPos;
        Vector2 delta = currentPos - _startPos;

        if (delta.magnitude < _deadZone)
        {
            delta = Vector2.zero;
        }

        _input = new Vector2(delta.x, delta.y) * _dragSpeed;
        _startPos = currentPos;
    }

    public void EndDrag()
    {
        _isDragging = false;
        _input = Vector2.zero;
    }



    public void OnPointerDown(PointerEventData eventData)
    {
        // Create a list to store raycast results
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        RaycastResult? res = null;
        // Check each result
        foreach (var result in results)
        {
            // If we hit any UI element that's not part of world space canvas
            if (result.gameObject.GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
            {
                res = result;
                break;
            }
        }

        if (res.HasValue)
        {
            BuyTouchHandler handler = res.Value.gameObject.GetComponentInParent<BuyTouchHandler>();
            if (handler)
            {
                handler.OnPointerDown(eventData);
            }
        } else
        {
            StartDrag(eventData.position);
        }

            
    }

    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Create a list to store raycast results
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        RaycastResult? res = null;
        // Check each result
        foreach (var result in results)
        {
            // If we hit any UI element that's not part of world space canvas
            if (result.gameObject.GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
            {
                res = result;
                break;
            }
        }

        if (res.HasValue)
        {
            BuyTouchHandler handler = res.Value.gameObject.GetComponentInParent<BuyTouchHandler>();
            if (handler)
            {
                handler.OnPointerUp(eventData);
            }
        }
        else
        {
            EndDrag();
        }
    }

    public Vector2 GetRotationInput()
    {
        return _input;
    }


}
