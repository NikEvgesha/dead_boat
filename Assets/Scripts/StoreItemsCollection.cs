using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoreItemsCollection", menuName = "Spawning/Store Items Collection")]
public class StoreItemsCollection : ScriptableObject
{
    [SerializeField] private List<PickableItem> _items;

    public List<PickableItem> Items => _items;
}

