using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class InventorySlot : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
/*    [SerializeField] private Transform _container;
    [SerializeField] private InventoryItem _itemIconPrefab;*/


    [SerializeField] protected Image _icon;

    [SerializeField] private Text _name;
    [SerializeField] private Text _tag;
    [SerializeField] private Text _price;

    [SerializeField] private InventoryIcon _container;


    private Transform _newParent;
    private Transform _currentParent;

    protected PickableItem _item;
    public PickableItem CurrentItem { get { return _item; } }
    public bool Empty => (_item == null);


    public virtual void InitSlot(PickableItem item)
    {
        if (item == null)
        {
            //_icon.enabled = false;
            _item = null;
            _container.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        //_icon.enabled = true;
        _item = item;
        _container.gameObject.SetActive(true);
        _icon.sprite = item.Data.IMG;
        _container.ParentSlot = this;
        _currentParent = transform;

        if (_name != null)
        {
            _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(item.Data.Name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());

        }

        if (_price != null)
        {
            if (item.gameObject.TryGetComponent<SellableItem>(out SellableItem sell))
            {
                _price.text = sell.Cost.ToString() + "$"; // use coin icon instead
            }
            else
            {
                _price.text = "";
            }
        }
        if (_tag != null)
        {
            if (item.gameObject.GetComponent<FuelItem>() != null)
            {
                _tag.text = LocalizationManager.Instance.LocalizationData.GetTranslation(ItemTag.Fuel.ToString(), LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Tag.ToString());
            }
            else
            {
                _tag.text = "";
            }
        }

    }

    /*    public void OnDrop(PointerEventData eventData)
        {
            InventoryItem itemIcon = eventData.pointerDrag.GetComponent<InventoryItem>();
            if (_container.childCount == 0)
            {
                itemIcon.SetNewParent(_container);
            } else
            {
                InventoryItem currentChild = _container.GetChild(0).GetComponent<InventoryItem>();
                currentChild.SetNewParent(itemIcon.CurrentParent);
                currentChild.UpdateParent();
                itemIcon.SetNewParent(_container);
            }
        }*/


    public void SetNewParent(Transform newParent)
    {
        _newParent = newParent;
        _currentParent = _newParent;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        _icon.raycastTarget = false;
        if (_name != null)
        {
            _name.gameObject.SetActive(false);
            _tag.gameObject.SetActive(false);
            _price.gameObject.SetActive(false);
        }


        _newParent = transform;
        _container.transform.SetParent(InventoryUI.Instance.gameObject.transform, true);
        InventoryUI.Instance.OnItemDrag(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _container.transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _icon.raycastTarget = true;
        if (_name != null)
        {
            _name.gameObject.SetActive(true);
            _tag.gameObject.SetActive(true);
            _price.gameObject.SetActive(true);
        }
        if (_newParent == null)
        {
            UpdateParent();
            DropOut();
        }
        else
        {
            UpdateParent();
        }
        InventoryUI.Instance.OnItemDrag(false);
    }

    public void UpdateParent()
    {
        _container.transform.SetParent(transform, true);
        _container.transform.localPosition = Vector3.zero;
    }

    public void DropOut()
    {
        Inventory.Instance.DropOutItem(_item, this);
        //Destroy(gameObject);
    }


}
