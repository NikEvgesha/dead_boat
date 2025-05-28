using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class StarterPackManager : MonoBehaviour
{
    [SerializeField] private List<PickableItem> _starterPackItems;

    private void Start()
    {
        //StartCoroutine(AddStarterPack());
    }


    public ReadOnlyCollection<PickableItem> GetStartItems()
    {
        return _starterPackItems.AsReadOnly();
    }
}
