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
    private Coroutine _collectionRoutine;

    public Action TakeBag;
    public bool Collected { get; private set; }
    public SellPoint SalePoint { get; set; }
    public int SharedRevision { get; private set; }
    public void BindSharedSale(SellPoint point, int revision)
    {
        SalePoint = point;
        if (SharedRevision == revision) return;
        SharedRevision = revision;
        Collected = false;
    }
    public void ConfirmSharedCollection(int revision)
    {
        if (Collected || SharedRevision != revision) return;
        Collected = true;
        TakeBag?.Invoke();
    }
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
        CancelCollection();
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
        if (_collectInProgress || Collected || DeadBoat.Online.SharedItemInteraction.Blocked) return;
        _collectInProgress = true;
        _progress = 0;
        var routine = StartCoroutine(BuyProcess());
        _collectionRoutine = _collectInProgress ? routine : null;
    }

    public void CancelCollection()
    {
        var routine = _collectionRoutine;
        _collectionRoutine = null;
        if (routine != null) StopCoroutine(routine);
        ResetCollectionState();
    }

    private void ResetCollectionState()
    {
        _collectionRoutine = null;
        _progress = 0;
        if (_fillImg != null) _fillImg.fillAmount = 0;
        _collectInProgress = false;
    }

    private IEnumerator BuyProcess()
    {
        try
        {
            while (!DeadBoat.Online.SharedItemInteraction.Blocked &&
                (_collectTouchPanel.Hold || PlayerInput.Instance.InteractionHold) && _progress < 1f)
            {
                _progress += Time.deltaTime;
                _fillImg.fillAmount = _progress;
                yield return null;
            }

            if (_progress >= 1f && !DeadBoat.Online.SharedItemInteraction.Blocked &&
                !DeadBoat.Online.SharedSalesRuntime.Collect(this))
            {
                Collected = true;
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, _money);
                TakeBag?.Invoke();
                Destroy(gameObject);
            }
        }
        finally
        {
            ResetCollectionState();
        }
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
            CancelCollection();
        }
    }

}
