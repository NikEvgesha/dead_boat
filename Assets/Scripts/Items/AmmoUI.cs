using System;
using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{

    private static AmmoUI _instance;
    public static AmmoUI Instance => _instance;

    [SerializeField] private Text _ammo;
    public static Action<int, int> ChangeAmmo;
    public static Action<bool> UseGun;
    private string _noAmmoTrigger = "NoAmmo";

    private void Awake()
    {
        ActivateAmmo(false);
        ChangeAmmo += NewAmmo;
        UseGun += ActivateAmmo;

        if (_instance == null)
            _instance = this;
        else
            Destroy(gameObject);
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
    public void NoAmmo()
    {
        _ammo.GetComponent<Animator>().SetTrigger(_noAmmoTrigger);
    }
}
