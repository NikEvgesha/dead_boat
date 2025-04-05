using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerStatsManager : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _staminaConsumptionRate = 1f;

    private float _stamina;
    private float _health;

    private Dictionary<PlayerStat, float> _stats;
    private Dictionary<PlayerStat, float> _statsMax;

    public Action NoStamina;
    public Action NoHealth;
    public Action<PlayerStat, float, float> StatChanged;

    public float Stamina
    {
        get
        {
            return _stamina;
        }
        private set
        {
            _stamina = Mathf.Clamp(value, 0, _maxStamina);

            if (_stamina == 0)
            {
                NoStamina?.Invoke();
            }
            StatChanged?.Invoke(PlayerStat.Stamina, _stamina, _maxStamina);

        }
    }

    public float Health
    {
        get
        {
            return _health / _maxHealth;
        }
        private set
        {
            _health = Mathf.Clamp(value, 0, _maxHealth);

            if (_health == 0)
            {
                NoHealth?.Invoke();
            }
            StatChanged?.Invoke(PlayerStat.Health, _health, _maxHealth);
        }
    }


    private void Awake()
    {
        _stats = new()
        {
            { PlayerStat.Health, Health},
            { PlayerStat.Stamina, Stamina}
        };

        _statsMax = new()
        {
            { PlayerStat.Health, _maxHealth},
            { PlayerStat.Stamina, _maxStamina}
        };
        _stamina = _maxStamina;
        _health = _maxHealth;
    }

    private void FixedUpdate()
    {
        if (PlayerInput.Instance.Sprint)
        {
            if (Stamina > 0)
                Stamina -= _staminaConsumptionRate * Time.fixedDeltaTime;
        } else
        {
            if (Stamina < _maxStamina)
                Stamina += _staminaConsumptionRate * Time.fixedDeltaTime;
        }
    }

    public float GetStat(PlayerStat stat)
    {
        return _stats[stat];
    }

    public float GetStatMax(PlayerStat stat)
    {
        return _stats[stat];
    }
    public void TakeDamage( int damage)
    {
        Debug.Log("Нанесли :" + damage);
        Debug.Log("Сейчас Health :" + Health);
        Debug.Log("Сейчас _health :" + _health);

        Health = _health - damage;

        Debug.Log("Потом Health :" + Health);
        Debug.Log("Потом _health :" + _health);
    }


}
