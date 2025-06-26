using System;
using UnityEngine;

public class SellPoint : MonoBehaviour
{
    [SerializeField] private StoreType _type;
    [SerializeField] private MoneyBag _moneyBagPrefab;
    [SerializeField] private Transform _moneyBagPoint;

    [SerializeField] private AudioSource _audioSource;

    private MoneyBag _moneyBag;
    private PickableItem _lastItem;

    public Action<StoreType> SellItem;
    // TODO: spawn money bag


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PickableItem>(out PickableItem item))
        {
            if (item == _lastItem) return;
            _lastItem = item;
            bool selled = false;
            int cost = 0;
            switch (_type)
            {
                case StoreType.Items:
                    if (other.TryGetComponent<SellableItem>(out SellableItem sellable))
                    {
                        ItemTag tag = item.Tags.Contains(ItemTag.Valuable) ? ItemTag.Valuable : ItemTag.Trash;
                        GameEvents.OnItemCollected?.Invoke(tag);
                        selled = true;
                        cost = sellable.GetReward();
                    }
                        
                    break;
                case StoreType.Enemies:
                    if (other.TryGetComponent<EnemyReward>(out EnemyReward rewarded))
                    {
                        GameEvents.OnItemCollected?.Invoke(ItemTag.Reward);
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
            if (_audioSource)
                _audioSource.Play();

        }
    }

}
