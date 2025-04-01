using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainInventorySlot : InventorySlot
{
  /*  [SerializeField] private Text _name;
    [SerializeField] private Text _tag;
    [SerializeField] private Text _price;


    public override void InitSlot(PickableItem item)
    {
        base.InitSlot(item);

        _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(item.Data.Name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());
        
        if (item.gameObject.TryGetComponent<SellableItem>(out SellableItem sell))
        {
            _price.text = sell.Cost.ToString() + "$"; // use coin icon instead
        } else
        {
            _price.text = "";
        }

        if (item.gameObject.GetComponent<FuelItem>() != null)
        {
            _tag.text = LocalizationManager.Instance.LocalizationData.GetTranslation(ItemTag.Fuel.ToString(), LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Tag.ToString());
        }
        else
        {
            _tag.text = "";
        }

    }*/
}