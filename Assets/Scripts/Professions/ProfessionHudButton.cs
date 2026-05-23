using UnityEngine;
using UnityEngine.UI;

public sealed class ProfessionHudButton : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private Text _badgeText;
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.T;

    private void Awake()
    {
        if (_visualRoot == null)
            _visualRoot = gameObject;

        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        if (_button != null)
            _button.onClick.AddListener(OpenProfessionPanel);

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();

        if (Input.GetKeyDown(_keyboardShortcut))
            ToggleProfessionPanel();
    }

    public void OpenProfessionPanel()
    {
        ProfessionSelectionPanel panel = ResolvePanel();
        if (panel == null)
            return;

        panel.Open();
        Refresh();
    }

    private void ToggleProfessionPanel()
    {
        ProfessionSelectionPanel panel = ResolvePanel();
        if (panel == null)
            return;

        if (panel.IsOpen)
            panel.CloseFromButton();
        else
            panel.Open();

        Refresh();
    }

    private void Refresh()
    {
        bool visible = ShouldShow();

        if (_visualRoot != null && _visualRoot.activeSelf != visible)
            _visualRoot.SetActive(visible);

        if (_button != null)
            _button.interactable = visible;

        if (_badgeRoot != null)
            _badgeRoot.SetActive(false);

        if (_badgeText != null)
            _badgeText.text = string.Empty;
    }

    private bool ShouldShow()
    {
        return ProfessionService.GetTotalProfessionCount() > 0;
    }

    private static ProfessionSelectionPanel ResolvePanel()
    {
        return ProfessionSelectionPanel.Instance ?? ProfessionTemporaryUIBootstrap.EnsurePanel();
    }
}
