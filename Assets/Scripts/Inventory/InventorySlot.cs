using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class InventorySlot : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler
{
/*    [SerializeField] private Transform _container;
    [SerializeField] private InventoryItem _itemIconPrefab;*/


    [SerializeField] protected Image _icon;

    [SerializeField] private Text _name;
    [SerializeField] private Text _tag;
    [SerializeField] private Text _price;

    [SerializeField] private GameObject _activeMarker;

    [SerializeField] private InventoryIcon _container;
    [SerializeField] private Transform _slot;
    [SerializeField] private Image _interactionIcon;


    private Transform _newParent;
    private Transform _currentParent;
    private bool _isQuickSlot;
    private bool _isOverInteractionPoint;
    private Transform _lastInteractionPoint;
    private bool _isDragging;
    protected PickableItem _item;
    public PickableItem CurrentItem { get { return _item; } }
    public bool Empty => (_item == null);
    public bool QuickSlot { get; set; }
    public bool Active { get; set; }
    public int ID { get; set; }


    public virtual void InitSlot(PickableItem item)
    {
        _slot = _slot == null ? transform : _slot;
        if (item == null)
        {
            UpdateParent();
            //_icon.enabled = false;
            _item = null;
            _container.gameObject.SetActive(false);
            if (!QuickSlot || !InventoryUI.Instance.IsOpen)
                gameObject.SetActive(false);
            return;
        }

        //_icon.enabled = true;
        _interactionIcon.gameObject.SetActive(false);
        _item = item;
        _item.SetSlot(this);
        _container.gameObject.SetActive(true);
        _icon.sprite = item.Data.IMG;
        _container.ParentSlot = this;
        _currentParent = _slot;

        if (!QuickSlot)
        {
            _name.text = LocalizationManager.Instance.LocalizationData.GetTranslation(item.Data.Name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());
            if (item.gameObject.TryGetComponent<SellableItem>(out SellableItem sell))
            {
                _price.text = sell.GetReward().ToString() + "$"; // use coin icon instead
            }
            else
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
            ActivateElements(true);
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
    private void ActivateElements(bool activate)
    {
        if (_name != null)
        {
            _name.gameObject.SetActive(activate);
            _tag.gameObject.SetActive(activate);
            _price.gameObject.SetActive(activate);
        }
    }

    public void SetNewParent(Transform newParent)
    {
        _newParent = newParent;
        _currentParent = _newParent;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        _icon.raycastTarget = false;
        ActivateElements(false);

        _newParent = transform;
        _container.transform.SetParent(InventoryUI.Instance.gameObject.transform, true);
        InventoryUI.Instance.OnItemDrag(true);
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _container.transform.position = Input.mousePosition;


        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            InteractionAreaIndicator indicator = hit.collider.GetComponent<InteractionAreaIndicator>();

            if (indicator != null && (
                (indicator.Type == InteractionArea.Fuel && _item.IsFuel) || 
                (indicator.Type == InteractionArea.Sell && _item.IsSellable) ||
                (indicator.Type == InteractionArea.Reward && _item.IsRewarded)))
            {
                _isOverInteractionPoint = true;
                _interactionIcon.gameObject.SetActive(true);
                _interactionIcon.sprite = indicator.GetIcon();
                _lastInteractionPoint = indicator.GetInteractionPoint();
            }
            else
            {
                _isOverInteractionPoint = false;
                _interactionIcon.gameObject.SetActive(false);
            }
        }
        else
        {
            _isOverInteractionPoint = false;
            _interactionIcon.gameObject.SetActive(false);
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _icon.raycastTarget = true;
        ActivateElements(true);
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
        _container.transform.SetParent(_slot, true);
        _container.transform.localPosition = Vector3.zero;
    }

    public void DropOut()
    {
        if (_isOverInteractionPoint)
            Inventory.Instance.DropOutItem(_item, this, _lastInteractionPoint);
        else
            Inventory.Instance.DropOutItem(_item, this);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (!QuickSlot)
            DropOut();
        else //if (ControlManager.Instance.UseTouchControl)
        {
            Inventory.Instance.SetActiveItem(_item);
            //SwitchActive(true);
        }
    }


    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot slot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (slot == null || (!slot.Empty && !slot.CurrentItem.Usable) || slot._item == this._item) return;

        Inventory.Instance.TrySwitch(slot, this);

    }


    public void SwitchActive(bool active)
    {
        if (!QuickSlot) return;

        Active = active;
        _activeMarker.SetActive(active);
    }
    public void UsebleActiveItem()
    {
        PickableItem item = _item;
        UpdateParent();
        DropOut();
        Destroy(item.gameObject);
    }
}
