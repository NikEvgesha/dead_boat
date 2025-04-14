using UnityEngine;

public class PlayerItemPickUp : MonoBehaviour
{
    [SerializeField] private Transform _itemJoint;
    [SerializeField] private Transform _cameraObj;
    [SerializeField] private float _pickUpDistance = 2f;
    [SerializeField] private LayerMask _layerMask;

    private PickableItem _raycastHitItem;
    private PickableItem _grabbedItem;
    private ControlUI _controlUI;

    bool _hitted = false;

    private void Start()
    {
        _controlUI = FindAnyObjectByType<ControlUI>();
    }


    private void Update()
    {
        //if (ControlManager.Instance.CursorActive) return;
        if (_grabbedItem && !_grabbedItem.Grabbed)
        {
            DropItem();
            return;
        }

        if (!_grabbedItem)
        {
            CheckRaycast();
        }

        if (PlayerInput.Instance.PickUp)
        {
            if (_grabbedItem != null)
            {
                DropItem();
            } else
            {
                TryPickupObject();
            } 
        }

        if (PlayerInput.Instance.Interaction)
        {
            TryPutToInventory();
        }

        if (PlayerInput.Instance.Attach)
        {
            TryAttach();
        }
        
    }

    public bool PickUpIsUse()
    {
        bool ray = _hitted && !_raycastHitItem.Attached;
        return ray || _grabbedItem;
    }
    private void CheckRaycast()
    {
        bool hitted = false;
        if (Physics.Raycast(_cameraObj.position, _cameraObj.forward, out RaycastHit hit, _pickUpDistance, _layerMask))
        {
            PickableItem item = hit.transform.GetComponent<PickableItem>();
            if (item == null)
                item = hit.transform.GetComponentInParent<PickableItem>();

            if (item)
            {
                if (item.enabled)
                {
                    if (_raycastHitItem != null && _raycastHitItem != item)
                    {
                        _raycastHitItem.OnFocus(false);
                    }
                    _raycastHitItem = item;
                    item.OnFocus(true);

                    hitted = true;
                }
            } 
        }
        if (!hitted)
        {
            if (_raycastHitItem != null)
            {
                _raycastHitItem.OnFocus(false);
            }
            _raycastHitItem = null;
        }
        if (_hitted != hitted)
        {
            //_controlUI.ShowPickUpButton(hitted);
            _hitted = hitted;
        }
            
    }

    private bool TryPickupObject()
    {
        if (_raycastHitItem != null && !_raycastHitItem.Attached)
        {
            _grabbedItem = _raycastHitItem;
            _grabbedItem.PickUp(_itemJoint);
            _controlUI.OnItemPickUp(true);
            return true;
        }
        return false;
    }


    private void TryPutToInventory()
    {
        if (_grabbedItem == null && _raycastHitItem != null)
        {
            _raycastHitItem.PutToInventory();
            _raycastHitItem.OnFocus(false);
            _raycastHitItem = null;
        }
    }


    private void DropItem()
    {
        _grabbedItem.Drop();
        _grabbedItem = null;
        _controlUI.OnItemPickUp(false);
    }


    private void TryAttach()
    {
        if (_grabbedItem != null)
        {
            if (_grabbedItem.TrySetAttach(true))
            {
                _grabbedItem = null;
            }
        } else if (_raycastHitItem != null)
        {
            _raycastHitItem.TrySetAttach(!_raycastHitItem.Attached);
        }
    }

}
