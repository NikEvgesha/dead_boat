using UnityEngine;

public class ActiveItemManager : MonoBehaviour
{

    [SerializeField] private Transform _handPoint;

    private PickableItem _activeItem;
    public PickableItem Active => _activeItem;



    public void SwitchActiveItem(PickableItem newItem)
    {
        if (newItem == _activeItem) return;
        if (_activeItem != null)
        {
            _activeItem.transform.SetParent(Inventory.Instance.transform);
            _activeItem.gameObject.SetActive(false);
            _activeItem.SetKinematic(false);
            GetActiveUsable().SetActiveItem(false);
        }
        _activeItem = newItem;
        if (newItem != null)
        {
            _activeItem.gameObject.SetActive(true);
            _activeItem.transform.SetParent(_handPoint);
            _activeItem.transform.localPosition = Vector3.zero;
            _activeItem.transform.localRotation = new Quaternion();
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


