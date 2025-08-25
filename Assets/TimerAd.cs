using MirraGames.SDK;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimerAd : MonoBehaviour
{
    [SerializeField] private float _noDamageInterval;
    [SerializeField] private GameObject _adPanel;
    [SerializeField] private Text _secondsRemainText;
    [SerializeField] private BoardController _boardController;

    private int _adsInterval = 60;
    private PlayerStatsManager _playerStats;
    private bool _timerReady;
    private bool _canShowAd = true;
    private IEnumerator _damageTimer;
    private IEnumerator _adTimer;
    private void Start()
    {
        if (!PurchasesManager.Instance.PurchasesAvailable())
        {
            this.enabled = false;
            return;
        }


        _playerStats = PlayerManager.Instance.StatsManager;
        _playerStats.StatChanged += OnPlayerStatChange;
        LoadingManager.Instance.LocationChanged += OnLocationChanged;
        _adsInterval = MirraSDK.Flags.GetInt("AdsInterval", _adsInterval);
        if (MirraSDK.Platform.Current == MirraGames.SDK.Common.PlatformType.Y8)
            _adsInterval = 121;

        _adTimer = AdTimer();
        StartCoroutine(_adTimer);
    }

    private void OnDisable()
    {
        if (_playerStats != null)
            _playerStats.StatChanged -= OnPlayerStatChange;
        if (_boardController != null)
            _boardController.EndGame -= OnEndGame;
        LoadingManager.Instance.LocationChanged -= OnLocationChanged;
    }


    private IEnumerator BoardAwait()
    {
        while (GameManager.Instance == null || GameManager.Instance.Board == null)
        {
            yield return null;
        }
        _boardController = GameManager.Instance.Board;
        _boardController.EndGame += OnEndGame;
    }


    private void OnEndGame()
    {
        _canShowAd = false;
        StopAllCoroutines();
    }

    private void OnLocationChanged(Location location)
    {
        if (_boardController != null)
        {
            _boardController.EndGame -= OnEndGame;
            _boardController = null;
        }
        if (location == Location.Game)
        {
            StartCoroutine(BoardAwait());
        }
        StopAllCoroutines();
        _canShowAd = true;
        _adTimer = AdTimer();
        StartCoroutine(_adTimer);

    }

    private void OnPlayerStatChange(PlayerStat stat, float current, float max)
    {
        if (stat != PlayerStat.Health) return;
        _canShowAd = false;
        if (_damageTimer != null)
            StopCoroutine(_damageTimer);
        _damageTimer = DamageTimer();
        StartCoroutine(_damageTimer);
    }

    private IEnumerator AdTimer()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(_adsInterval);
            while (!_canShowAd || PlayerInput.Instance.InteractionHold || !MirraSDK.Ads.IsInterstitialReady)
            {
                yield return new WaitForSeconds(1);
            }

            int secondsRemain = 2;
            _adPanel.SetActive(true);
            PauseManager.Instance.SetPause(true, true);
            while (secondsRemain > 0)
            {
                _secondsRemainText.text = secondsRemain.ToString();
                yield return new WaitForSecondsRealtime(1);
                secondsRemain--;
            }
            PauseManager.Instance.SetPause(false);
            AdsManager.Instance.ShowInterstitialAd();
            _adPanel.SetActive(false);
        }
    }

    private IEnumerator DamageTimer()
    {
        yield return new WaitForSeconds(_noDamageInterval);
        _canShowAd = true;
    }

}
