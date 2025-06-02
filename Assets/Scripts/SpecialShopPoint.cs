using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class SpecialShopPoint : MonoBehaviour
{

    [SerializeField] private Canvas _infoCanvas;
    [SerializeField] private BoatShop _shop;
    [SerializeField] private BuyTouchHandler _touchPanel;
    [SerializeField] private Transform _buyPoint;
    [SerializeField] private Image _openProgress;
    [SerializeField] private AudioSource _source;

    private bool _active;
    private float _progress;
    private bool _openInProgress;

    public Action BuyItem;

    private void Start()
    {
        _shop.ItemPurchased += OnItemPurchase;
        _touchPanel.PointerDown += OpenShop;
    }

    private void OnDisable()
    {
        _shop.ItemPurchased -= OnItemPurchase;
        _touchPanel.PointerDown -= OpenShop;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _infoCanvas.gameObject.SetActive(true);
            _active = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {

            if (_shop.Opened)
                _shop.ToggleOpen();

            _active = false;
            _infoCanvas.gameObject.SetActive(false);
            StopAllCoroutines();
            _progress = 0;
            _openProgress.fillAmount = _progress;
            _openInProgress = false;

        }
    }


    private void Update()
    {
        if (!_active || _openInProgress) return;

        if (PlayerInput.Instance.Interaction)
        {
            OpenShop();
        }
    }


    private void OpenShop()
    {
            _openInProgress = true;
            _progress = 0;
            StartCoroutine(OpenProcess());
    }


    private IEnumerator OpenProcess()
    {
        while ((_touchPanel.Hold || PlayerInput.Instance.InteractionHold) && _progress < 1f)
        {
            _progress += Time.deltaTime;
            _openProgress.fillAmount = _progress;
            yield return null;
        }

        if (_progress >= 1f)
        {
            _source.Play();
            _shop.ToggleOpen();
        }
        _progress = 0;
        _openProgress.fillAmount = _progress;
        _openInProgress = false;
    }


    public void OnItemPurchase(PickableItem item)
    {
        Instantiate(item, _buyPoint);
    }

}
