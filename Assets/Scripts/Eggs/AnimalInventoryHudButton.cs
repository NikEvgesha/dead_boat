using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnimalInventoryHudButton : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private Text _badgeText;
    [SerializeField] private RectTransform _animateRoot;
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.O;
    [SerializeField] private float _punchScale = 1.18f;
    [SerializeField] private float _punchDuration = 0.18f;

    private EggHatchingManager _manager;
    private Coroutine _punchRoutine;
    private Vector3 _baseScale = Vector3.one;
    private int _newAnimalsCount;

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
            _manager.AnimalsCollected -= HandleAnimalsCollected;
        }
    }

    private void Update()
    {
        if (_manager == null)
            TryBindManager();

        Refresh();

        if (_newAnimalsCount > 0 && AnimalInventoryPanel.Instance != null && AnimalInventoryPanel.Instance.IsOpen)
        {
            _newAnimalsCount = 0;
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
        bool discovered = ShouldShow();

        if (_visualRoot != null)
            _visualRoot.SetActive(discovered);

        if (_button != null)
            _button.interactable = discovered;

        if (_badgeRoot != null)
            _badgeRoot.SetActive(discovered && _newAnimalsCount > 0);

        if (_badgeText != null)
            _badgeText.text = _newAnimalsCount.ToString();
    }

    private void OpenInventory()
    {
        if (!ShouldShow())
            return;

        _newAnimalsCount = 0;
        Refresh();
        AnimalInventoryPanel.Instance?.OpenInventory();
    }

    private void ToggleInventory()
    {
        AnimalInventoryPanel panel = AnimalInventoryPanel.Instance;
        if (panel != null && panel.IsOpen)
        {
            panel.CloseFromShortcut();
            return;
        }

        OpenInventory();
    }

    private bool ShouldShow()
    {
        if (_manager == null || !_manager.HasHatchedAnimal())
            return false;

        if (LoadingManager.Instance == null)
            return true;

        return LoadingManager.Instance.CurrentLocation == Location.Lobby;
    }

    private void HandleAnimalsCollected(int amount)
    {
        _newAnimalsCount += Mathf.Max(1, amount);
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
            _manager.AnimalsCollected -= HandleAnimalsCollected;
        }

        _manager = manager;
        _manager.StateChanged += Refresh;
        _manager.AnimalsCollected += HandleAnimalsCollected;
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
