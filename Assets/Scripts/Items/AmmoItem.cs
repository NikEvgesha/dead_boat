using System.Collections.Generic;
using UnityEngine;

public class AmmoItem : MonoBehaviour
{
    [SerializeField] private int AmmoCount;
    [SerializeField] private WeaponType AmmoType;
    public event System.Action Collected;
    public bool AddAmmo()
    {
        PlayerAmmoManager.Instance.AddAmmo(AmmoType, AmmoCount);
        Collected?.Invoke();
        return true;
    }

        public bool AddAmmo(string old)
    {
        List<PickableItem> items = Inventory.Instance.GetItems();
        foreach (var item in items)
        {
            RangedWeaponController weaponController = item.GetComponentInChildren<RangedWeaponController>();
            if (weaponController)
            {
                if (weaponController.TryAddAmmo(AmmoCount, AmmoType))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
