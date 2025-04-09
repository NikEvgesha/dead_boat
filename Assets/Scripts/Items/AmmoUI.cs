using System;
using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{

    [SerializeField] private Text _ammo;
    public static Action<int, int> ChangeAmmo;
    public static Action<bool> UseGun;

    private void Awake()
    {
        ActivateAmmo(false);
        ChangeAmmo += NewAmmo;
        UseGun += ActivateAmmo;
    }
    private void OnDestroy()
    {
        ChangeAmmo -= NewAmmo;
        UseGun -= ActivateAmmo;
    }
    private void NewAmmo(int inGun, int inStore)
    {
        _ammo.text = $"{inGun}/{inStore}";
    }
    private void ActivateAmmo(bool use)
    {
        _ammo.gameObject.SetActive(use);
    }
}
