using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StorePoint : MonoBehaviour
{
    [SerializeField] private PickableItem _itemPrefab;
    [SerializeField] private Transform _buyPoint;
    
    [SerializeField] private GameObject _BuyInfoCanvas;
    [SerializeField] private GameObject _PriceCanvas;

    [SerializeField] private Text _price;
    [SerializeField] private Text _name;
    [SerializeField] private Image _buyProgress;
    [SerializeField] private GameObject _hintDesctop;
    [SerializeField] private GameObject _hintTouch;

    [SerializeField] private bool _staticItem;

    private PlayerInput _input;
    private bool _active;
    private float _progress;
    private bool _buyInProgress;
    private StoreItem _storeItem;

    public bool StaticItem => _staticItem;

    private ItemStore _store;


    public void InitPoint(ItemStore store, PickableItem prefab = null)
    {
        if (prefab != null)
            _itemPrefab = prefab;
        if (_itemPrefab.TryGetComponent<StoreItem>(out _storeItem))
        {
            Instantiate(_itemPrefab.GetModel(), transform);
            _price.text = _storeItem.price.ToString() + "$";
            _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(_itemPrefab.Data.name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());
            _hintTouch.SetActive(ControlManager.Instance.UseTouchControl);
            _hintDesctop.SetActive(!ControlManager.Instance.UseTouchControl);

            _BuyInfoCanvas.SetActive(false);
            _PriceCanvas.SetActive(false);
            _store = store;
            _store.PlayerEnter += SwitchPriceVisibility;
        }
    }

    private void OnDisable()
    {
        if (_store != null)
            _store.PlayerEnter -= SwitchPriceVisibility;
    }


    public void SwitchPriceVisibility(bool visible)
    {
        _PriceCanvas.SetActive(visible);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _BuyInfoCanvas.SetActive(true);
            _active = true;
            if (_input == null)
                _input = other.gameObject.GetComponent<PlayerInput>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _active = false;
            _BuyInfoCanvas.SetActive(false);
            StopAllCoroutines();
        }
    }


    private void Update()
    {
        if (!_active || !_input || _buyInProgress) return;

        if (_input.Interaction)
        {
            if (CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Coins, _storeItem.price))
            {
                _buyInProgress = true;
                _progress = 0;
                StartCoroutine(BuyProcess());
            }
        }
    }


    private IEnumerator BuyProcess()
    {
        while (_input.InteractionHold && _progress < 1f)
        {
            _progress += Time.deltaTime;
            _buyProgress.fillAmount = _progress;
            yield return null;
        }

        if (_progress >= 1f)
        {
            CurrencyManager.Instance.RemoveCurrency(CurrencyType.Coins, _storeItem.price);
            Instantiate(_itemPrefab, _buyPoint);
        }
        _progress = 0;
        _buyProgress.fillAmount = _progress;
        _buyInProgress = false;

    }
}
