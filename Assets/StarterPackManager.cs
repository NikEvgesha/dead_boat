using System.Collections.Generic;
using UnityEngine;

public class StarterPackManager : MonoBehaviour
{
    [SerializeField] private List<PickableItem> _starterPackItems;

    private void Start()
    {
        foreach (var item in _starterPackItems)
        {
            PickableItem itemObj = Instantiate(item, null);
            itemObj.PutToInventory();
        }
    }

/*    public ReadOnlyCollection<PickableItem> GetStartItems()
    {
        return _starterPackItems.AsReadOnly();
    }*/
}
