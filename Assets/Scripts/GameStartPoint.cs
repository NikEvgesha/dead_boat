using UnityEngine;

public class GameStartPoint : MonoBehaviour
{
    private const int StartGameCanvasSortingOrder = 100;

    [SerializeField] private GameObject _canvas;
    [SerializeField] private Transform _insidePoint;
    [SerializeField] private Transform _outsidePoint;

    private Transform _player;
    private Canvas _canvasComponent;

    private void Awake()
    {
        ApplyCanvasSorting();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement.Instance.Teleport(_insidePoint);
            ShowCanvas();
            ControlManager.Instance.CursorActive = true;
        }
    }

    public void Cancel()
    {
        PlayerMovement.Instance.Teleport(_outsidePoint);
        _canvas.SetActive(false);
        ControlManager.Instance.ForceGameplayCursor();
    }

    private void ShowCanvas()
    {
        if (_canvas == null)
            return;

        _canvas.SetActive(true);
        ApplyCanvasSorting();
        Canvas.ForceUpdateCanvases();
    }

    private void ApplyCanvasSorting()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = StartGameCanvasSortingOrder;
        canvas.transform.SetAsLastSibling();
    }

    private Canvas ResolveCanvas()
    {
        if (_canvasComponent != null)
            return _canvasComponent;

        if (_canvas == null)
            return null;

        _canvasComponent = _canvas.GetComponent<Canvas>();
        return _canvasComponent;
    }
}
