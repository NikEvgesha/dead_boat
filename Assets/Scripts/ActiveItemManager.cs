using UnityEngine;

public class ActiveItemManager : MonoBehaviour
{

    [SerializeField] private Transform _handPoint;

    private PickableItem _activeItem;
    public PickableItem Active => _activeItem;

    // OnDestroy must forget the reference before switching: reparenting an object
    // that Unity is already destroying is invalid.
    public void ForgetDestroyedItem(PickableItem item)
    {
        if (ReferenceEquals(_activeItem, item)) _activeItem = null;
    }



    public void SwitchActiveItem(PickableItem newItem)
    {
        if (newItem != null) newItem.CancelStowAnimation();
        if (newItem == _activeItem) return;
        if (_activeItem != null)
        {
            Vector3 worldScale = DeadBoat.Online.SharedItemScale.Capture(_activeItem);
            _activeItem.transform.SetParent(Inventory.Instance.transform);
            DeadBoat.Online.SharedItemScale.Apply(_activeItem.transform, worldScale);
            _activeItem.gameObject.SetActive(false);
            _activeItem.SetKinematic(false);
            GetActiveUsable().SetActiveItem(false);
        }
        _activeItem = newItem;
        if (newItem != null)
        {
            Vector3 worldScale = DeadBoat.Online.SharedItemScale.Capture(newItem);
            _activeItem.gameObject.SetActive(true);
            _activeItem.transform.SetParent(_handPoint);
            _activeItem.transform.localPosition = Vector3.zero;
            _activeItem.transform.localRotation = new Quaternion();
            DeadBoat.Online.SharedItemScale.Apply(_activeItem.transform, worldScale);
            _activeItem.SetKinematic(true);
        }
        InventoryUI.Instance.SetActiveItem(_activeItem);
        if (_activeItem != null)
            GetActiveUsable().SetActiveItem(true);
    }
    private void Update()
    {
        if (ControlManager.Instance.CursorActive) return;
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll < 0)
            {
                SwitchActiveItem(InventoryUI.Instance.GetNextUsable(_activeItem));
            }
            else
            {
                SwitchActiveItem(InventoryUI.Instance.GetPrevUsable(_activeItem));
            }
        }
    }


    public ActivateItem GetActiveUsable()
    {
        if (_activeItem != null && _activeItem.TryGetComponent<ActivateItem>(out ActivateItem usable))
        {
            return usable;
        }
        return null;
    }


}


