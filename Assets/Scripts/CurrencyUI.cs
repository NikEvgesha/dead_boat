using UnityEngine;
using UnityEngine.UI;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private Text _currencyAmount;
    [SerializeField] private CurrencyType _type;

    private void Start()
    {
        CurrencyManager.Instance.CurrencyChanged += OnCurrencyChanged;
        OnCurrencyChanged(_type, CurrencyManager.Instance.GetBalance(_type));
    }

    private void OnEnable()
    {
        if (CurrencyManager.Instance)
        {
            CurrencyManager.Instance.CurrencyChanged += OnCurrencyChanged;
            OnCurrencyChanged(_type, CurrencyManager.Instance.GetBalance(_type));
        }
    }

    private void OnDisable()
    {
        CurrencyManager.Instance.CurrencyChanged -= OnCurrencyChanged;
    }


    private void OnCurrencyChanged(CurrencyType type, int newAmount)
    {
        if (type == _type)
        {
            _currencyAmount.text = newAmount.ToString();
        }
    }
}
