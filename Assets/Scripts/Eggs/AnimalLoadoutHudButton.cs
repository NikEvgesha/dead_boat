using UnityEngine;
using UnityEngine.UI;

public sealed class AnimalLoadoutHudButton : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private Text _badgeText;
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.Y;

    private EggHatchingManager _manager;

    private void Awake()
    {
        if (_visualRoot == null)
            _visualRoot = gameObject;

        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        if (_button != null)
            _button.onClick.AddListener(OpenLoadout);

        TryBindManager();
        Refresh();
    }

    private void OnEnable()
    {
        TryBindManager();

        if (_manager != null)
            _manager.StateChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (_manager != null)
            _manager.StateChanged -= Refresh;
    }

    private void Update()
    {
        if (_manager == null)
        {
            TryBindManager();
            Refresh();
        }

        if (Input.GetKeyDown(_keyboardShortcut))
            ToggleLoadout();
    }

    public void OpenLoadout()
    {
        if (!ShouldShow())
            return;

        AnimalLoadoutPanel.Instance?.Open();
    }

    private void ToggleLoadout()
    {
        if (!ShouldShow())
            return;

        AnimalLoadoutPanel panel = AnimalLoadoutPanel.Instance;
        if (panel == null)
            return;

        if (panel.IsOpen)
            panel.CloseFromShortcut();
        else
            panel.Open();
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
        if (_manager == null || !_manager.HasHatchedAnimal())
            return false;

        if (LoadingManager.Instance == null)
            return true;

        return LoadingManager.Instance.CurrentLocation == Location.Lobby;
    }

    private void TryBindManager()
    {
        if (_manager != null)
            return;

        _manager = EggHatchingManager.Instance;
    }
}
