using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class EggAdRewardHudButton : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _statusText;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private Text _badgeText;
    [SerializeField] private RectTransform _animateRoot;
    [SerializeField] private string _rewardId = "EggAdReward";
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.None;
    [SerializeField] private float _pulseSpeed = 4f;

    private EggHatchingManager _manager;
    private bool _adInProgress;
    private bool _isSubscribedToManager;
    private float _pulse;
    private Color _baseIconColor = Color.white;

    private void Awake()
    {
        if (_visualRoot == null)
            _visualRoot = gameObject;

        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        if (_animateRoot == null)
            _animateRoot = _button != null ? _button.GetComponent<RectTransform>() : transform as RectTransform;

        if (_iconImage != null)
            _baseIconColor = _iconImage.color;

        if (_button != null)
            _button.onClick.AddListener(ClaimReward);

        TryBindManager();
        Refresh();
    }

    private void OnEnable()
    {
        TryBindManager();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }

    private void Update()
    {
        if (_manager == null)
            TryBindManager();

        Refresh();
        AnimateReadyState();

        if (_keyboardShortcut != KeyCode.None && Input.GetKeyDown(_keyboardShortcut))
            ClaimReward();
    }

    private void ClaimReward()
    {
        if (!CanClaim())
            return;

        if (AdsManager.Instance == null)
            return;

        _adInProgress = true;
        Refresh();

        AdsManager.Instance.ShowRewardedAd(
            _rewardId,
            success =>
            {
                _adInProgress = false;

                if (success)
                    _manager?.TryGrantAdRewardEgg();

                Refresh();
            });
    }

    private void Refresh()
    {
        bool visible = ShouldShow();
        bool ready = visible && _manager != null && _manager.CanClaimAdRewardEgg();
        int remainingSeconds = visible && _manager != null ? _manager.GetAdRewardRemainingSeconds() : 0;

        if (_visualRoot != null && _visualRoot.activeSelf != visible)
            _visualRoot.SetActive(visible);

        if (_button != null)
            _button.interactable = visible && ready && !_adInProgress;

        if (_badgeRoot != null)
            _badgeRoot.SetActive(visible && ready);

        if (_badgeText != null)
            _badgeText.text = string.Empty;

        if (_statusText != null)
            _statusText.text = ready ? "6H" : FormatRemaining(remainingSeconds);

        if (_iconImage != null)
        {
            Color color = _baseIconColor;
            color.a = ready ? _baseIconColor.a : Mathf.Min(_baseIconColor.a, 0.45f);
            _iconImage.color = color;
        }
    }

    private bool ShouldShow()
    {
        if (LoadingManager.Instance == null)
            return true;

        return LoadingManager.Instance.CurrentLocation == Location.Lobby;
    }

    private bool CanClaim()
    {
        return ShouldShow() && !_adInProgress && _manager != null && _manager.CanClaimAdRewardEgg();
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
            UnsubscribeFromManager();

        _manager = manager;
        SubscribeToManager();
    }

    private void SubscribeToManager()
    {
        if (_manager == null || _isSubscribedToManager)
            return;

        _manager.StateChanged += Refresh;
        _isSubscribedToManager = true;
    }

    private void UnsubscribeFromManager()
    {
        if (_manager == null || !_isSubscribedToManager)
            return;

        _manager.StateChanged -= Refresh;
        _isSubscribedToManager = false;
    }

    private void AnimateReadyState()
    {
        if (_animateRoot == null)
            return;

        if (!CanClaim())
        {
            _pulse = 0f;
            _animateRoot.localScale = Vector3.one;
            return;
        }

        _pulse += Time.unscaledDeltaTime * Mathf.Max(0.1f, _pulseSpeed);
        float scale = 1f + Mathf.Sin(_pulse) * 0.035f;
        _animateRoot.localScale = new Vector3(scale, scale, 1f);
    }

    private static string FormatRemaining(int remainingSeconds)
    {
        if (remainingSeconds <= 0)
            return "6H";

        TimeSpan time = TimeSpan.FromSeconds(remainingSeconds);
        if (time.TotalHours >= 1d)
            return $"{(int)time.TotalHours}:{time.Minutes:00}";

        return $"{time.Minutes:00}:{time.Seconds:00}";
    }
}
