using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorePoint : MonoBehaviour
{
    [SerializeField] private PickableItem _itemPrefab;
    [SerializeField] private Transform _buyPoint;
    [SerializeField] private Transform _sellPoint;

    [SerializeField] private GameObject _BuyInfoCanvas;
    [SerializeField] private GameObject _PriceCanvas;
    [SerializeField] private BuyTouchHandler _buyTouchPanel;

    [SerializeField] private Text _price;
    [SerializeField] private Text _name;
    [SerializeField] private Image _buyProgress;
    [SerializeField] private Image _currencyIcon;
    [SerializeField] private Image _noMoney;
    private bool _isNoMoney;


    [SerializeField] private bool _staticItem;

    //[SerializeField] private AudioSource _audioSource;

    private bool _active;
    private float _progress;
    private bool _buyInProgress;
    private StoreItem _storeItem;

    private int price;
    private CurrencyType currencyType;

    public bool StaticItem => _staticItem;

    private ItemStore _store;

    public Action BuyItem;

    private void Start()
    {
        _sellPoint = _sellPoint == null ? _buyPoint : _sellPoint;
    }
    public void InitPoint(bool inLobby, ItemStore store, PickableItem prefab = null)
    {
        if (prefab != null)
            _itemPrefab = prefab;
        if (_itemPrefab.TryGetComponent<StoreItem>(out _storeItem))
        {
            List<Vector3> offset = _itemPrefab.GetSellPoint();
            if (offset.Count == 0)
            {
                Instantiate(_itemPrefab.GetModel(), _sellPoint);
            }
            else
            {
                GameObject model = Instantiate(_itemPrefab.GetModel(), _sellPoint);
                model.transform.localPosition = offset[0];
                Quaternion deltaRotation = Quaternion.Euler(offset[1]);
                model.transform.localRotation = deltaRotation;
                model.transform.localScale = offset[2];
            }
            if (inLobby)
            {
                currencyType = CurrencyType.Gems;
                price = _storeItem.GemPrice;
            } else
            {
                currencyType = CurrencyType.Coins;
                price = _storeItem.CoinPrice;
            }
            _price.text = price.ToString();
            _currencyIcon.sprite = CurrencyManager.Instance.GetCurrencyIcon(currencyType);
                
            _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(_itemPrefab.Data.name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());

            _BuyInfoCanvas.SetActive(false);
            _PriceCanvas.SetActive(false);
            _store = store;
            _store.PlayerEnter += SwitchPriceVisibility;
            _buyTouchPanel = _BuyInfoCanvas.GetComponentInChildren<BuyTouchHandler>();
            _buyTouchPanel.PointerDown += TryBuy;
        }
    }

    private void OnDisable()
    {
        if (_store != null)
            _store.PlayerEnter -= SwitchPriceVisibility;
        if (_buyTouchPanel != null)
            _buyTouchPanel.PointerDown -= TryBuy;
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
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _active = false;
            _BuyInfoCanvas.SetActive(false);
            StopAllCoroutines();
            _progress = 0;
            _buyProgress.fillAmount = _progress;
            _buyInProgress = false;
        }
    }


    private void Update()
    {
        if (!_active  || _buyInProgress) return;

        if (PlayerInput.Instance.Interaction)
        {
            TryBuy();
        }
        if (_isNoMoney && !PlayerInput.Instance.InteractionHold)
        {
            _isNoMoney = false;
            _noMoney.gameObject.SetActive(_isNoMoney);
        }
    }

    private void TryBuy()
    {
        bool enoughMoney = CurrencyManager.Instance.CheckEnoughCurrency(currencyType, price);
        _itemPrefab.CheckTags();
        bool isAmmo = _itemPrefab.HaveTag(ItemTag.Ammo);
        bool isUsable = _itemPrefab.Usable;
        if (enoughMoney && (isAmmo || Inventory.Instance.CheckSpace(isUsable)))
        {
            _buyInProgress = true;
            _progress = 0;
            StartCoroutine(BuyProcess());
        }
        else if (!enoughMoney)
        {
            _isNoMoney = true;
            _noMoney.gameObject.SetActive(_isNoMoney);
        }
    }


    private IEnumerator BuyProcess()
    {
        while ((_buyTouchPanel.Hold || PlayerInput.Instance.InteractionHold) && _progress < 1f)
        {
            _progress += Time.deltaTime;
            _buyProgress.fillAmount = _progress;
            yield return null;
        }

        if (_progress >= 1f)
        {
            CurrencyManager.Instance.RemoveCurrency(currencyType, price);
            PickableItem item = Instantiate(_itemPrefab, _buyPoint);
            if (LoadingManager.Instance.CurrentLocation == Location.Lobby)
            {
                Inventory.Instance.AddItem(item);
                if (!item.HaveTag(ItemTag.Ammo))
                    SaveManager.Instance.SaveLobbyItem(item.Data.Name);
                    
            }
            BuyItem?.Invoke();
            //if (_audioSource)
                //_audioSource.Play();
        }
        _progress = 0;
        _buyProgress.fillAmount = _progress;
        _buyInProgress = false; 

    }
}
