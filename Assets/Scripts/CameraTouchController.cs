using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class CameraTouchController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField] private RectTransform _touchArea;
    [SerializeField] private float _dragSpeed = 0.1f;
    [SerializeField] private float _deadZone = 2f;

    private bool _isDragging;
    private Vector2 _startPos;
    private Vector2 _input;

    public void OnPointerDown(PointerEventData eventData)
    {
        _startPos = eventData.position;
        _isDragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector2 currentPos = eventData.position;
        Vector2 delta = currentPos - _startPos;

        if (delta.magnitude < _deadZone)
        {
            delta = Vector2.zero;
        }

        _input = new Vector2(delta.x, delta.y) * _dragSpeed;
        _startPos = currentPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        _input = Vector2.zero;
    }

    public Vector2 GetRotationInput()
    {
        return _input;
    }
}
