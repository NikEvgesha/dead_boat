using System;
using UnityEngine;

public class SellPoint : MonoBehaviour
{
    [SerializeField] private StoreType _type;
    [SerializeField] private MoneyBag _moneyBagPrefab;
    [SerializeField] private Transform _moneyBagPoint;


    private MoneyBag _moneyBag;

    public Action<StoreType> SellItem;
    // TODO: spawn money bag


    private void OnTriggerEnter(Collider other)
    {

        if (other.TryGetComponent<PickableItem>(out PickableItem item))
        {
            bool selled = false;
            int cost = 0;
            switch (_type)
            {
                case StoreType.Items:
                    if (other.TryGetComponent<SellableItem>(out SellableItem sellable))
                    {
                        selled = true;
                        cost = sellable.GetReward();
                    }
                        
                    break;
                case StoreType.Enemies:
                    if (other.TryGetComponent<EnemyReward>(out EnemyReward rewarded))
                    {
                        selled = true;
                        cost = rewarded.GetReward();
                    }
                    break;
            }
            if (!selled) return;
            if (_moneyBag == null)
            {
                _moneyBag = Instantiate(_moneyBagPrefab, _moneyBagPoint);
                _moneyBag.Money = cost;
            }
            else
            {
                _moneyBag.Money += cost;
            }
            SellItem?.Invoke(_type);
            Destroy(other.gameObject);

            
        }
    }

}
