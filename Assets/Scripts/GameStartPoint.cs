using UnityEngine;
using DeadBoat.Online;

public class GameStartPoint : MonoBehaviour
{
    private const int StartGameCanvasSortingOrder = 100;

    [SerializeField] private GameObject _canvas;
    [SerializeField] private Transform _insidePoint;
    [SerializeField] private Transform _outsidePoint;

    private Transform _player;
    private Canvas _canvasComponent;
    private LobbyDepartureUI _departureUI;
    private LobbyOnlineBootstrap _online;
    private bool _onlinePortalOpened;

    private void Awake()
    {
        ApplyCanvasSorting();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement.Instance.Teleport(_insidePoint);
            _online = FindFirstObjectByType<LobbyOnlineBootstrap>();
            if (_online != null && _online.IsOnline && !_online.IsInDepartureRoom)
            {
                _onlinePortalOpened = true;
                _departureUI = GetComponent<LobbyDepartureUI>();
                if (_departureUI == null)
                    _departureUI = gameObject.AddComponent<LobbyDepartureUI>();
                _departureUI.Open(this, _online);
            }
            else
            {
                _onlinePortalOpened = false;
                ShowCanvas();
            }
            ControlManager.Instance.CursorActive = true;
        }
    }

    public void Cancel()
    {
        if (_onlinePortalOpened && _online != null && _online.IsConnecting)
            return;

        if (_onlinePortalOpened)
        {
            _departureUI?.Close();
            _onlinePortalOpened = false;
            if (_online != null && _online.Mode == LobbyOnlineMode.Online && !_online.IdleDisconnected)
                _ = _online.ReturnToVisualLobbyAsync();
        }
        PlayerMovement.Instance.Teleport(_outsidePoint);
        if (_canvas != null)
            _canvas.SetActive(false);
        ControlManager.Instance.ForceGameplayCursor();
    }

    public async void ShowSoloMapFromDeparture()
    {
        if (!_onlinePortalOpened || _online == null || _online.IsConnecting)
            return;

        await _online.LeaveDepartureForSoloSelectionAsync();
        if (this == null)
            return;
        _departureUI?.Close();
        ShowCanvas();
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
