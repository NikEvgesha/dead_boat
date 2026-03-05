using UnityEngine;

[DisallowMultipleComponent]
public class ProfessionNpcPoint : MonoBehaviour
{
    [SerializeField] private Canvas _infoCanvas;
    [SerializeField] private ProfessionSelectionPanel _selectionPanel;
    [SerializeField] private BuyTouchHandler _touchOpenButton;
    [SerializeField] private bool _closePanelOnTriggerExit = false;

    private bool _active;

    private void Start()
    {
        if (_infoCanvas != null)
            _infoCanvas.gameObject.SetActive(false);

        if (_touchOpenButton != null)
            _touchOpenButton.PointerDown += TryOpenPanel;
    }

    private void OnDisable()
    {
        if (_touchOpenButton != null)
            _touchOpenButton.PointerDown -= TryOpenPanel;
    }

    private void Update()
    {
        if (!_active || PlayerInput.Instance == null)
            return;

        if (PlayerInput.Instance.Interaction)
            TryOpenPanel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _active = true;
        if (_infoCanvas != null)
            _infoCanvas.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _active = false;
        if (_infoCanvas != null)
            _infoCanvas.gameObject.SetActive(false);

        if (_closePanelOnTriggerExit)
        {
            ProfessionSelectionPanel panel = ResolvePanel();
            if (panel != null && panel.IsOpen)
                panel.CloseFromExternal();
        }
    }

    private void TryOpenPanel()
    {
        if (!_active)
            return;

        ProfessionSelectionPanel panel = ResolvePanel();
        if (panel == null)
            return;

        panel.Open();
    }

    private ProfessionSelectionPanel ResolvePanel()
    {
        if (_selectionPanel != null)
            return _selectionPanel;

        return ProfessionSelectionPanel.Instance;
    }
}
