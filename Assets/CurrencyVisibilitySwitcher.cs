using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyVisibilitySwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _gems;


    private void Start()
    {
        CurrencyManager.Instance.ShowGems += SwitchVisibility;
    }

    private void OnDisable()
    {
        CurrencyManager.Instance.ShowGems -= SwitchVisibility;
    }

    private void SwitchVisibility(bool visible)
    {
        _gems.SetActive(visible);
    }
}
