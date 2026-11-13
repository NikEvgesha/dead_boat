using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckItemOnLocation : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private List<ItemData> ItemCheck;

    private PickableItem _lastItem;
    private IReadOnlyList<ItemData> _requiredItems;
    private int _sharedAcceptedMask;

    public IReadOnlyList<ItemData> RequiredItems
    {
        get { CaptureRequiredItems(); return _requiredItems; }
    }

    public UnityEvent<PickableItem> PutItem;
    // TODO: spawn money bag
    private void Awake() => CaptureRequiredItems();
    private void Start()
    {
        CaptureRequiredItems();
        DeadBoat.Online.SharedWorldActionsRuntime.Register(this);
    }
    private void OnDestroy() => DeadBoat.Online.SharedWorldActionsRuntime.Remove(this);

    private void CaptureRequiredItems()
    {
        if (_requiredItems != null) return;
        _requiredItems = ItemCheck != null
            ? new List<ItemData>(ItemCheck).AsReadOnly()
            : Array.Empty<ItemData>();
    }

    public void SharedApply(int acceptedMask, PickableItem item, bool restoring)
    {
        // Keep the authored requirements intact. Canonical consumption is committed by the runtime.
        int count = Mathf.Min(RequiredItems.Count, 32);
        for (int slot = 0; slot < count; slot++)
        {
            int bit = 1 << slot;
            if ((acceptedMask & bit) == 0 || (_sharedAcceptedMask & bit) != 0) continue;
            // Mark before invoking callbacks, which can re-enter this adapter or disable its object.
            _sharedAcceptedMask |= bit;
            PutItem?.Invoke(item);
            if (!restoring && _audioSource != null) _audioSource.Play();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // The shared runtime throttles retries while an authority-side location is being restored.
        if (!DeadBoat.Online.SharedRunContext.Active || !other.TryGetComponent<PickableItem>(out var item)) return;
        if (item.Status == ItemStatus.InInventory || DeadBoat.Online.SharedRunRecovery.Restoring ||
            DeadBoat.Online.SharedRecoveryInventory.Applying) return;
        DeadBoat.Online.SharedWorldActionsRuntime.Offer(this, item);
    }

    private void OnTriggerEnter(Collider other)
    {
        CaptureRequiredItems();

        if (other.TryGetComponent<PickableItem>(out PickableItem item))
        {
            if (item.Status == ItemStatus.InInventory || DeadBoat.Online.SharedRunRecovery.Restoring ||
                DeadBoat.Online.SharedRecoveryInventory.Applying) return;
            if (DeadBoat.Online.SharedWorldActionsRuntime.Offer(this, item)) return;
            if (ItemCheck == null || ItemCheck.Count <= 0) return;
            if (item == _lastItem) return;
            _lastItem = item;
            ItemData useDate = null;
            foreach (var data in ItemCheck)
            {
                if (_lastItem.Data.Name == data.Name)
                {
                    useDate = data;
                    PutItem?.Invoke(_lastItem);
                    Destroy(other.gameObject);
                    if (_audioSource)
                        _audioSource.Play();
                    break;
                }
            }
            _lastItem = null;
            if (useDate) ItemCheck.Remove(useDate);
        }
    }

}
