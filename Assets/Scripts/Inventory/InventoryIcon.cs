using UnityEngine;

public class InventoryIcon : MonoBehaviour
{
    private PickableItem _item;
    private InventorySlot _parent;
    public InventorySlot ParentSlot {
        get { return _parent; }
        set { _parent = value; }

    }
    public PickableItem Item {
        get { return _item; }
        set { _item = value; }

    }


    /*    private ItemData _itemData;
        private Image _image;
        private Transform _newParent;
        private Transform _currentParent;
        private PickableItem _item;

        public PickableItem Item { get { return _item; } }

        public Transform CurrentParent { get { return _currentParent; } }

        public void Init(PickableItem item)
        {
            _item = item;
            _image = GetComponent<Image>();
            _itemData = item.Data;
            _image.sprite = _itemData.IMG;
            _currentParent = transform.parent;
        }

        public void SetNewParent(Transform newParent) 
        {
            _newParent = newParent;
            _currentParent = _newParent;
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            _image.raycastTarget = false;
            _newParent = transform.parent;
            transform.SetParent(InventoryUI.Instance.gameObject.transform, true);
            //InventoryUI.Instance.OnItemDrag(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _image.raycastTarget = true;
            if (_newParent == null)
            {
                DropOut();
            } else
            {
                UpdateParent();
            }
            //InventoryUI.Instance.OnItemDrag(false);
        }

        public void UpdateParent()
        {
            transform.SetParent(_newParent, true);
            transform.localPosition = Vector3.zero;
        }

        public void DropOut()
        {
            Inventory.Instance.DropOutItem(_item);
            Destroy(gameObject);
        }*/
}
