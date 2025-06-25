using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimerAd : MonoBehaviour
{
    [SerializeField] private float _adsInterval;
    [SerializeField] private float _noDamageInterval;
    [SerializeField] private GameObject _adPanel;
    [SerializeField] private Text _secondsRemainText;
    private PlayerStatsManager _playerStats;
    private bool _timerReady;
    private bool _canShowAd = true;
    private IEnumerator _damageTimer;
    private void Start()
    {
        _playerStats = PlayerManager.Instance.StatsManager;
        _playerStats.StatChanged += OnPlayerStatChange;
        StartCoroutine(AdTimer());
    }

    private void OnDisable()
    {
        _playerStats.StatChanged -= OnPlayerStatChange;
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
            while (!_canShowAd || PlayerInput.Instance.InteractionHold)
            {
                yield return new WaitForSeconds(1);
            }

            int secondsRemain = 3;
            _adPanel.SetActive(true);
            while (secondsRemain > 0)
            {
                _secondsRemainText.text = secondsRemain.ToString();
                yield return new WaitForSeconds(1);
                secondsRemain--;
            }

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
