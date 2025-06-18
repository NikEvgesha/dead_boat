using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AmmoData
{
    public WeaponType Type;
    public int StartingCount;
}

public class PlayerAmmoManager : MonoBehaviour
{
    public static PlayerAmmoManager Instance;

    // Задаём стартовый запас патронов через инспектор
    [SerializeField]
    private List<AmmoData> _startingAmmo;

    // Основной словарь: для каждого WeaponType — свой счётчик патронов
    private Dictionary<WeaponType, int> _ammo;
    public Action NewAmmo;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Инициализируем словарь
        _ammo = new Dictionary<WeaponType, int>();
        // Заполняем начальными значениями
        
    }

    private void Start()
    {
        _ammo[WeaponType.Pistol] = SaveManager.Instance.LoadAmmo(WeaponType.Pistol);
        _ammo[WeaponType.Rifle] = SaveManager.Instance.LoadAmmo(WeaponType.Rifle);
        _ammo[WeaponType.Shotgun] = SaveManager.Instance.LoadAmmo(WeaponType.Shotgun);

        foreach (var entry in _startingAmmo)
        {
            if (_ammo[entry.Type] == 0)
                _ammo[entry.Type] = entry.StartingCount;
        }
    }

    /// <summary>
    /// Получить текущее количество патронов под конкретное оружие.
    /// </summary>
    public int GetAmmo(WeaponType weapon)
    {
        return _ammo.TryGetValue(weapon, out int count) ? count : 0;
    }

    /// <summary>
    /// Добавить патроны.
    /// </summary>
    public void AddAmmo(WeaponType weapon, int amount)
    {
        if (amount <= 0) return;
        if (!_ammo.ContainsKey(weapon))
            _ammo[weapon] = 0;
        _ammo[weapon] += amount;
        NewAmmo?.Invoke();
        Save();
        /*if (LoadingManager.Instance.CurrentLocation == Location.Lobby)
        {
            Save();
        }*/
    }

    /// <summary>
    /// Потратить патроны. Вернёт true, если достаточно, и вычтет их.
    /// Иначе вернёт false, оставив счётчик без изменений.
    /// </summary>
    public bool UseAmmo(WeaponType weapon, int amount)
    {
        if (amount <= 0) return false;
        if (GetAmmo(weapon) >= amount)
        {
            _ammo[weapon] -= amount;

            Save();
            return true;
        }
        return false;
    }


    public void Save() {
        foreach (WeaponType type in _ammo.Keys)
        {
            SaveManager.Instance.SaveAmmo(type, _ammo[type]);
        }
        
    }

    public void ResetAmmo() {
        foreach (var entry in _startingAmmo)
        {
            _ammo[entry.Type] = entry.StartingCount;
        }
        Save();
    }
}
