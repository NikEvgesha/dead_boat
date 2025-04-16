using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MoneyBag : MonoBehaviour
{
    [SerializeField] private float _minScale = 3f;
    [SerializeField] private float _maxScale = 20f;
    [SerializeField] private int _maxScaleMoney;
    [SerializeField] private Text _amountText;
    [SerializeField] private Image _fillImg;
    [SerializeField] private BuyTouchHandler _collectTouchPanel;
    [SerializeField] private Transform _bag;
    private int _money;
    private float _scale;

    private bool _active;
    private float _progress;
    private bool _collectInProgress;

    public Action TakeBag;
    public int Money
    {
        get
        {
            return _money;
        }
        set
        {
            _money = value;
            _scale = Mathf.Lerp(_minScale, _maxScale, (float)value / _maxScaleMoney);
            _bag.localScale = Vector3.one * _scale;
            _amountText.text = _money.ToString();
        }
    }

    private void OnEnable()
    {
        _collectTouchPanel.PointerDown += TryCollect;
    }

    private void OnDisable()
    {
        _collectTouchPanel.PointerDown -= TryCollect;
    }

    private void Update()
    {
        if (!_active || _collectInProgress) return;

        if (PlayerInput.Instance.Interaction)
        {
            TryCollect();
        }
    }

    private void TryCollect()
    {
        _collectInProgress = true;
        _progress = 0;
        StartCoroutine(BuyProcess());
    }


    private IEnumerator BuyProcess()
    {
        while ((_collectTouchPanel.Hold || PlayerInput.Instance.InteractionHold) && _progress < 1f)
        {
            _progress += Time.deltaTime;
            _fillImg.fillAmount = _progress;
            yield return null;
        }

        if (_progress >= 1f)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, _money);
        }
        _progress = 0;
        _fillImg.fillAmount = _progress;
        _collectInProgress = false;
        TakeBag?.Invoke();
        Destroy(gameObject);

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _collectTouchPanel.gameObject.SetActive(true);
            _active = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _active = false;
            _collectTouchPanel.gameObject.SetActive(false);
            StopAllCoroutines();
        }
    }

}
