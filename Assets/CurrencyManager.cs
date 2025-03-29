using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    [SerializeField] private Sprite _gemsIcon;
    [SerializeField] private Sprite _coinIcon;

    private static CurrencyManager _instance;

    private Dictionary<CurrencyType, int> _balance = new() 
    {
        { CurrencyType.Coins, 0},
        { CurrencyType.Gems, 0}
    };

    private Dictionary<CurrencyType, Sprite> _currencyIcons;

    public int Gems { get { return _balance[CurrencyType.Gems]; } }
    public int Coins { get { return _balance[CurrencyType.Coins]; } }

    public Action<CurrencyType, int> CurrencyChanged;
    public static CurrencyManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _currencyIcons = new Dictionary<CurrencyType, Sprite> {
            {CurrencyType.Gems, _gemsIcon},
            {CurrencyType.Coins, _coinIcon},
        };
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {
        // load ?
/*
        _balance[CurrencyType.Coins] = 0;
        _balance[CurrencyType.Gems] = 0;*/
    }


    public void AddCurrency(CurrencyType type, int amount)
    {
        _balance[type] += amount;
        CurrencyChanged?.Invoke(type, _balance[type]);
    }

    public bool RemoveCurrency(CurrencyType type, int amount)
    {
        if (_balance[type] >= amount)
        {
            _balance[type] -= amount;
            CurrencyChanged?.Invoke(type, _balance[type]);
            return true;
        }
        return false;
    }

    public int GetBalance(CurrencyType type)
    {
        return _balance[type];
    }
}
