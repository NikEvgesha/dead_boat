
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FuelAdRewardPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Image _fillImg;
    [SerializeField] private BuyTouchHandler _touchPanel;
    //[SerializeField] private FuelItem _rewardItem;
    [SerializeField] private float _fuelReward;
    [SerializeField] private AudioSource _audioSource;

    private BoardController _board;
    private bool _isOpen;
    private float _activationProgress;
    private bool _activationInProgress;

    public void Start()
    {
        _board = GetComponentInParent<BoardController>();
        _board.NoFuel += ShowPanel;
        _touchPanel.PointerDown += StartActivation;
    }

    private void OnDisable()
    {
        _board.NoFuel -= ShowPanel;
        _touchPanel.PointerDown -= StartActivation;
    }

    private void ShowPanel(bool noFuel)
    {
        _isOpen = noFuel;
        _panel.SetActive(noFuel);
    }

    private void Update()
    {
        if (!_isOpen || _activationInProgress) return;

        if (!_board.PlayerOnSeat && _isOpen)
        {
            ShowPanel(false);
            return;
        }

        if (_isOpen && _board.PlayerOnSeat && PlayerInput.Instance.InteractionHold)
        {
            StartActivation();
        }
    }

    private void StartActivation() {
        _activationProgress = 0;
        _activationInProgress = true;
        StartCoroutine(Activation());
    }


    private IEnumerator Activation()
    {
        while ((_touchPanel.Hold || PlayerInput.Instance.InteractionHold) && _activationProgress < 1f)
        {
            _activationProgress += Time.deltaTime;
            _fillImg.fillAmount = _activationProgress;
            yield return null;
        }

        if (_activationProgress >= 1f)
        {
            AdsManager.Instance.ShowRewardedAd(
                    "buyFuel",
                    (success) =>
                    {
                        if (success)
                        {
                            _board.AddFuel(_fuelReward);
                            _audioSource.Play();
                        }
                            
                    });
        }
        _activationProgress = 0;
        _fillImg.fillAmount = _activationProgress;
        _activationInProgress = false;
    }


}
