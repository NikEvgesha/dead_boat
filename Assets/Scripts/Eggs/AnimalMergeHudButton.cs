using UnityEngine;
using UnityEngine.UI;

public sealed class AnimalMergeHudButton : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private Text _badgeText;
    [SerializeField] private RectTransform _animateRoot;
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.U;

    private EggHatchingManager _manager;
    private float _pulse;

    private void Awake()
    {
        if (_visualRoot == null)
            _visualRoot = gameObject;

        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        if (_animateRoot == null)
            _animateRoot = _button != null ? _button.GetComponent<RectTransform>() : transform as RectTransform;

        if (_button != null)
            _button.onClick.AddListener(OpenMerge);

        TryBindManager();
        Refresh();
    }

    private void OnEnable()
    {
        TryBindManager();

        if (_manager != null)
            _manager.StateChanged += HandleStateChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (_manager != null)
            _manager.StateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (_manager == null)
        {
            TryBindManager();
            Refresh();
        }

        if (Input.GetKeyDown(_keyboardShortcut))
            ToggleMerge();

        Refresh();
        AnimateReadyBadge();
    }

    public void OpenMerge()
    {
        if (!ShouldShow())
            return;

        AnimalMergeSelectionPanel.Instance?.Open();
    }

    private void ToggleMerge()
    {
        if (!ShouldShow())
            return;

        AnimalMergeSelectionPanel panel = AnimalMergeSelectionPanel.Instance;
        if (panel == null)
            return;

        if (panel.IsOpen)
            panel.CloseFromShortcut();
        else
            panel.Open();
    }

    private void TryBindManager()
    {
        if (_manager != null)
            return;

        _manager = EggHatchingManager.Instance;
    }

    private void HandleStateChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool visible = ShouldShow();
        if (_visualRoot != null && _visualRoot.activeSelf != visible)
            _visualRoot.SetActive(visible);

        if (_button != null)
            _button.interactable = visible;

        AnimalMergeState merge = visible && _manager != null ? _manager.GetAnimalMergeState() : null;
        bool ready = merge != null && _manager.IsAnimalMergeReady();
        int candidateCount = visible && merge == null && _manager != null ? _manager.GetAnimalMergeCandidateCount() : 0;
        bool showBadge = ready || candidateCount > 0;

        if (_badgeRoot != null)
            _badgeRoot.SetActive(showBadge);

        if (_badgeText != null)
            _badgeText.text = ready ? "!" : candidateCount.ToString();
    }

    private bool ShouldShow()
    {
        if (_manager == null || !_manager.HasUnlockedAnimalMerge())
            return false;

        if (LoadingManager.Instance == null)
            return true;

        return LoadingManager.Instance.CurrentLocation == Location.Lobby;
    }

    private void AnimateReadyBadge()
    {
        if (_animateRoot == null || _badgeRoot == null || !_badgeRoot.activeInHierarchy)
        {
            _pulse = 0f;
            if (_animateRoot != null)
                _animateRoot.localScale = Vector3.one;
            return;
        }

        _pulse += Time.unscaledDeltaTime * 4f;
        float scale = 1f + Mathf.Sin(_pulse) * 0.035f;
        _animateRoot.localScale = new Vector3(scale, scale, 1f);
    }
}
