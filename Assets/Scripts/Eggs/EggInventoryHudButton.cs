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
    [SerializeField] private Vector2 _gameAnchor = new Vector2(0.05052081f, 0.535f);
    [SerializeField] private Vector2 _gameSize = new Vector2(92f, 92f);

    private EggHatchingManager _manager;
    private Coroutine _punchRoutine;
    private Vector3 _baseScale = Vector3.one;
    private int _newEggsCount;
    private RectTransform _rectTransform;
    private RectSnapshot _defaultRect;
    private bool _hasDefaultRect;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_animateRoot == null)
            _animateRoot = transform as RectTransform;

        _rectTransform = transform as RectTransform;
        CaptureDefaultRect();

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
            TryBindManager();

        Refresh();

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
        bool discovered = ShouldShow();
        ApplyLocationPosition();

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
        if (!ShouldShow())
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

    private bool ShouldShow()
    {
        if (!HasDiscoveredEggs())
            return false;

        if (LoadingManager.Instance == null)
            return true;

        Location currentLocation = LoadingManager.Instance.CurrentLocation;
        if (currentLocation == Location.Game)
            return EggSpawnRuntimeState.CollectedInRun > 0;

        return currentLocation == Location.Lobby;
    }

    private bool HasDiscoveredEggs()
    {
        if (_manager != null && _manager.HasDiscoveredEggs())
            return true;

        EggFeatureState state = EggFeatureStorage.Load();
        return state != null && state.hasDiscoveredEggs;
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
        if (manager == null && ShouldCreateRuntimeManager())
            manager = CreateRuntimeManager();

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

    private bool ShouldCreateRuntimeManager()
    {
        if (LoadingManager.Instance == null)
            return false;

        return LoadingManager.Instance.CurrentLocation == Location.Game &&
               EggSpawnRuntimeState.CollectedInRun > 0;
    }

    private EggHatchingManager CreateRuntimeManager()
    {
        GameObject root = new GameObject("RuntimeEggHatchingManager");
        root.transform.SetParent(transform, false);
        return root.AddComponent<EggHatchingManager>();
    }

    private void CaptureDefaultRect()
    {
        if (_rectTransform == null || _hasDefaultRect)
            return;

        _defaultRect = new RectSnapshot(_rectTransform);
        _hasDefaultRect = true;
    }

    private void ApplyLocationPosition()
    {
        if (_rectTransform == null || !_hasDefaultRect || LoadingManager.Instance == null)
            return;

        if (LoadingManager.Instance.CurrentLocation == Location.Game)
        {
            _rectTransform.anchorMin = _gameAnchor;
            _rectTransform.anchorMax = _gameAnchor;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.sizeDelta = GetGameSize();
            return;
        }

        _defaultRect.ApplyTo(_rectTransform);
    }

    private Vector2 GetGameSize()
    {
        if (_gameSize.x > 1f && _gameSize.y > 1f)
            return _gameSize;

        return _defaultRect.Size.x > 1f && _defaultRect.Size.y > 1f
            ? _defaultRect.Size
            : new Vector2(92f, 92f);
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

    private readonly struct RectSnapshot
    {
        private readonly Vector2 _anchorMin;
        private readonly Vector2 _anchorMax;
        private readonly Vector2 _anchoredPosition;
        private readonly Vector2 _sizeDelta;
        private readonly Vector2 _size;
        private readonly Vector2 _pivot;

        public RectSnapshot(RectTransform rect)
        {
            _anchorMin = rect.anchorMin;
            _anchorMax = rect.anchorMax;
            _anchoredPosition = rect.anchoredPosition;
            _sizeDelta = rect.sizeDelta;
            _size = rect.rect.size;
            _pivot = rect.pivot;
        }

        public Vector2 Size => _size;

        public void ApplyTo(RectTransform rect)
        {
            rect.anchorMin = _anchorMin;
            rect.anchorMax = _anchorMax;
            rect.anchoredPosition = _anchoredPosition;
            rect.sizeDelta = _sizeDelta;
            rect.pivot = _pivot;
        }
    }
}
