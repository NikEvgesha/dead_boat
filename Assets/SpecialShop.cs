using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct SpecialShopItem
{
    public PickableItem item;
    public int price;
    public CurrencyType currencyType;
}

public class SpecialShop : MonoBehaviour
{
    [SerializeField] private List<SpecialShopItem> _items;

    [SerializeField] private Canvas _infoCanvas;
    [SerializeField] private Canvas _shopCanvas;
    [SerializeField] private BuyTouchHandler _touchPanel;
    [SerializeField] private Transform _buyPoint;
    [SerializeField] private Image _openProgress;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private SpecialShopSlot _slotPrefab;

    private bool _active;
    private float _progress;
    private bool _openInProgress;

    public Action BuyItem;


    private void OnEnable()
    {

    }

    private void Start()
    {
        InitSlots();
    }


    public void InitSlots()
    {
        foreach (SpecialShopItem item in _items)
        {
            SpecialShopSlot slot = _grid.SpawnObject<SpecialShopSlot>(_slotPrefab.gameObject);
            slot.Init(item);
        }
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
            _shopCanvas.gameObject.SetActive(true);
        }
        _progress = 0;
        _openProgress.fillAmount = _progress;
        _openInProgress = false;
    }
}
