using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckItemOnLocation : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private List<ItemData> ItemCheck;

    private PickableItem _lastItem;

    public UnityEvent<PickableItem> PutItem;
    // TODO: spawn money bag


    private void OnTriggerEnter(Collider other)
    {
        if (ItemCheck.Count <= 0) return;

        if (other.TryGetComponent<PickableItem>(out PickableItem item))
        {
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
