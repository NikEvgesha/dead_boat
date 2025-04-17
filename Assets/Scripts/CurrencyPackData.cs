using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyPack", menuName = "ScriptableObject/CurrencyPackData")]
public class CurrencyPackData : ScriptableObject
{
    [SerializeField] private ItemData _data;
    [SerializeField] private int _amount;
    [SerializeField] private CurrencyType _currencyType;

    public ItemData Data => _data;
    public int Amount => _amount;
    public CurrencyType CurrencyType => _currencyType;
}