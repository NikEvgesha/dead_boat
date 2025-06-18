using UnityEngine;
using UnityEngine.UI;

public class PlayerStatBar : MonoBehaviour
{
    [SerializeField] private PlayerStat _stat;
    [SerializeField] private Image _bar;
    [SerializeField] private GameObject _barObj;

    private bool _barActive = false;

    private PlayerStatsManager _player;


    private void Start()
    {
        _player = PlayerManager.Instance.StatsManager;
        _player.StatChanged += OnStatChange;
        _player.AddHealth(0);
    }

    private void OnDisable()
    {
        _player.StatChanged -= OnStatChange;
    }

    private void OnStatChange(PlayerStat stat, float amount, float max)
    {
        if (stat != _stat) return;

        if (amount >= max && _barActive)
        {
            _barObj.SetActive(false);
            _barActive = false;
            return;
        }


        if (amount < max && !_barActive)
        {
            _barObj.SetActive(true);
            _barActive = true;
        }

        _bar.fillAmount = amount/max;

        

    }


}
