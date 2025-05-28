using System.Collections.Generic;
using UnityEngine;

public class ItemsManager : MonoBehaviour
{
    private static ItemsManager _instance;
    public static ItemsManager Instance { get { return _instance; } private set { } }
    [SerializeField] private StoreItemsCollection collection;

    private Dictionary<string, PickableItem> items;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        items = new();
        foreach (PickableItem item in collection.Items)
        {
            items.TryAdd(item.Data.Name, item);
        }
    }


    public PickableItem GetItem(string id)
    {
        if (items.ContainsKey(id))
        {
            return items[id];
        }
        return null;
    }

}
