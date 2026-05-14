using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EggInventoryHudButton : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private Text _badgeText;
    [SerializeField] private RectTransform _animateRoot;
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.G;
    [SerializeField] private float _punchScale = 1.18f;
    [SerializeField] private float _punchDuration = 0.18f;

    private EggHatchingManager _manager;
    private Coroutine _punchRoutine;
    private Vector3 _baseScale = Vector3.one;
    private int _newEggsCount;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_animateRoot == null)
            _animateRoot = transform as RectTransform;

        if (_animateRoot != null)
            _baseScale = _animateRoot.localScale;

        if (_button != null)
            _button.onClick.AddListener(OpenInventory);
    }

    private void OnEnable()
    {
        TryBindManager();
        Refresh();
    }

    private void OnDisable()
    {
        if (_manager != null)
        {
            _manager.StateChanged -= Refresh;
            _manager.EggsCollected -= HandleEggsCollected;
        }
    }

    private void Update()
    {
        if (_manager == null)
        {
            TryBindManager();
            Refresh();
        }

        if (_newEggsCount > 0 && EggNestSelectionPanel.Instance != null && EggNestSelectionPanel.Instance.IsOpen)
        {
            _newEggsCount = 0;
            Refresh();
        }

        if (Input.GetKeyDown(_keyboardShortcut))
            ToggleInventory();
    }

    public void _OpenInventory()
    {
        OpenInventory();
    }

    public void Refresh()
    {
        bool discovered = _manager != null && _manager.HasDiscoveredEggs();
        if (_visualRoot != null)
            _visualRoot.SetActive(discovered);

        if (_button != null)
            _button.interactable = discovered;

        if (_badgeRoot != null)
            _badgeRoot.SetActive(discovered && _newEggsCount > 0);

        if (_badgeText != null)
            _badgeText.text = _newEggsCount.ToString();
    }

    private void OpenInventory()
    {
        if (_manager == null || !_manager.HasDiscoveredEggs())
            return;

        _newEggsCount = 0;
        Refresh();
        EggNestSelectionPanel.Instance?.OpenInventory();
    }

    private void ToggleInventory()
    {
        EggNestSelectionPanel panel = EggNestSelectionPanel.Instance;
        if (panel != null && panel.IsInventoryViewOnly)
        {
            panel.CloseFromShortcut();
            return;
        }

        OpenInventory();
    }

    private void HandleEggsCollected(int amount)
    {
        _newEggsCount += Mathf.Max(1, amount);
        Refresh();
        PlayPunch();
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
        {
            _manager.StateChanged -= Refresh;
            _manager.EggsCollected -= HandleEggsCollected;
        }

        _manager = manager;
        _manager.StateChanged += Refresh;
        _manager.EggsCollected += HandleEggsCollected;
    }

    private void PlayPunch()
    {
        if (_animateRoot == null || !isActiveAndEnabled)
            return;

        if (_punchRoutine != null)
            StopCoroutine(_punchRoutine);

        _punchRoutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        float halfDuration = Mathf.Max(0.01f, _punchDuration * 0.5f);
        Vector3 targetScale = _baseScale * Mathf.Max(1f, _punchScale);

        for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            _animateRoot.localScale = Vector3.Lerp(_baseScale, targetScale, t / halfDuration);
            yield return null;
        }

        for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            _animateRoot.localScale = Vector3.Lerp(targetScale, _baseScale, t / halfDuration);
            yield return null;
        }

        _animateRoot.localScale = _baseScale;
        _punchRoutine = null;
    }
}
