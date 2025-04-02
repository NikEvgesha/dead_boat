using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemStore : MonoBehaviour
{
    [SerializeField] private Transform _pointsParent;
    [SerializeField] private StoreItemsCollection _collection;

    private List<PickableItem> _availableItems;

    public Action<bool> PlayerEnter;

    private void Start()
    {
        _availableItems = new List<PickableItem>(_collection.Items);
        foreach (StorePoint point in _pointsParent.GetComponentsInChildren<StorePoint>())
        {
            if (point.StaticItem)
            {
                point.InitPoint(this);
            }
            else if (!point.StaticItem && _availableItems.Count > 0)
            {
                int random_idx = UnityEngine.Random.Range(0, _availableItems.Count);
                point.InitPoint(this, _availableItems[random_idx]);
                _availableItems.RemoveAt(random_idx);
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerEnter?.Invoke(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerEnter?.Invoke(false);
        }
    }




}
