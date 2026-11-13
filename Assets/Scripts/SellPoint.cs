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
    public MoneyBag CheckpointBag => _moneyBag;
    public StoreType SaleType => _type;
    private void Update() => DeadBoat.Online.SharedSalesRuntime.Tick(this);
    private void OnDestroy() => DeadBoat.Online.SharedSalesRuntime.Remove(this);
    public void ApplySharedBalance(int coins, int revision = 0)
    {
        if (coins <= 0)
        {
            if (_moneyBag != null) Destroy(_moneyBag.gameObject);
            _moneyBag = null;
            return;
        }
        RestoreCheckpointBag(coins);
        _moneyBag.BindSharedSale(this, revision);
    }
    public void NotifySharedSale(ItemTag tag)
    {
        GameEvents.OnItemCollected?.Invoke(tag);
        SellItem?.Invoke(_type);
        if (_audioSource != null) _audioSource.Play();
    }
    public void RestoreCheckpointBag(int coins)
    {
        if (coins <= 0) return;
        if (_moneyBag == null) _moneyBag = Instantiate(_moneyBagPrefab, _moneyBagPoint);
        if (_moneyBag.Money != coins) _moneyBag.Money = coins;
        _moneyBag.SalePoint = this;
    }

    public Action<StoreType> SellItem;
    // TODO: spawn money bag

    private void OnTriggerStay(Collider other)
    {
        // Independent city restoration can register the authority's sale point
        // after the first trigger event. The shared path throttles/revalidates
        // retries; the original offline sale remains an enter-only operation.
        if (DeadBoat.Online.SharedRunContext.Active && other.TryGetComponent<PickableItem>(out var item))
            DeadBoat.Online.SharedSalesRuntime.Sell(this, item);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PickableItem>(out PickableItem item))
        {
            if (DeadBoat.Online.SharedSalesRuntime.Sell(this, item)) return;
            if (item.Status == ItemStatus.InInventory || DeadBoat.Online.SharedRunRecovery.Restoring ||
                DeadBoat.Online.SharedRecoveryInventory.Applying) return;
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
            _moneyBag.SalePoint = this;
            if (DeadBoat.Online.SoloRunBranch.Active)
                DeadBoat.Online.SoloRunBranch.RememberSale(DeadBoat.Online.SharedSalesRuntime.Key(this), _moneyBag.Money);
            DeadBoat.FirstRun.FirstRunCheckpointWorld.MarkConsumed(item);
            SellItem?.Invoke(_type);
            Destroy(other.gameObject);
            if (_audioSource)
                _audioSource.Play();

        }
    }

}
