using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(UnityEngine.UI.Outline))]
public class EggSelectionSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float _hoverScale = 1.04f;

    private RectTransform _rectTransform;
    private UnityEngine.UI.Outline _outline;
    private Vector3 _baseScale;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _outline = GetComponent<UnityEngine.UI.Outline>();
        _baseScale = _rectTransform.localScale;
        ApplyNormalState();
    }

    private void OnDisable()
    {
        ApplyNormalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale * Mathf.Max(1f, _hoverScale);

        if (_outline != null)
            _outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyNormalState();
    }

    private void ApplyNormalState()
    {
        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale == Vector3.zero ? Vector3.one : _baseScale;

        if (_outline == null)
            return;

        _outline.enabled = false;
    }
}
