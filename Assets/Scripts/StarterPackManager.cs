using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class StarterPackManager : MonoBehaviour
{
    [SerializeField] private List<PickableItem> _starterPackItems;
    [SerializeField] private ProfessionCatalog _professionCatalog;
    [SerializeField] private bool _includeProfessionStarterItems = true;

    private void Start()
    {
        //StartCoroutine(AddStarterPack());
    }


    public ReadOnlyCollection<PickableItem> GetStartItems()
    {
        ProfessionService.ConfigureCatalog(_professionCatalog);
        return ProfessionService.BuildStarterPack(_starterPackItems, _includeProfessionStarterItems);
    }
}
