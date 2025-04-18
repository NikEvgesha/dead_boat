using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BankStorePoint : MonoBehaviour
{
    [SerializeField] private Transform _sellPoint;
    [SerializeField] private CurrencyPackData _packData;
    [SerializeField] private GameObject _buyInfoCanvas;

    [SerializeField] private Text _name;
    [SerializeField] private Image _buyProgress;
    [SerializeField] private BuyTouchHandler _buyTouchPanel;

    private float _progress;
    private bool _buyInProgress;

    private bool _active;

    private void Start()
    {
        if (_packData == null) return;

        Instantiate(_packData.Model, _sellPoint);

        _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(_packData.Data.Name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _buyInfoCanvas.gameObject.SetActive(true);
            _active = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _active = false;
            _buyInfoCanvas.SetActive(false);
            StopAllCoroutines();
            _progress = 0;
            _buyProgress.fillAmount = _progress;
            _buyInProgress = false;
        }
    }

    private void Update()
    {
        if (!_active) return;

        if (PlayerInput.Instance.Interaction)
        {
            TryBuy();
        }
    }


    private void TryBuy()
    {
        _buyInProgress = true;
        _progress = 0;
        StartCoroutine(BuyProcess());
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
            GemsShop.Instance.TryBuy(_packData);
        }
        _progress = 0;
        _buyProgress.fillAmount = _progress;
        _buyInProgress = false;

    }
}
