using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct Experience
{
    public int CurrentLevel;
    public List<float> LevelsExpToUp;
    public float ExpToUp
    {
        get 
        {
            return LevelsExpToUp[Mathf.Min(CurrentLevel, LevelsExpToUp.Count - 1)];
        }
    }
    private float _exp;
    public float Exp
    {
        get
        {
            return _exp;
        }
        set
        {
            if (value - _exp < 0) return;

            if (value - _exp == 0)
            {
                ChangeExp?.Invoke(value);
                ChangeLevel?.Invoke(CurrentLevel); 
                return;
            }
            if (ExpToUp <= value)
            {
                _exp = value - ExpToUp;
                CurrentLevel++;
                ChangeLevel?.Invoke(CurrentLevel);
            }
            else
            {
                _exp = value;
            }
            SaveManager.Instance.SavePlayerExperience(_exp, CurrentLevel);
            ChangeExp?.Invoke(value);
        }
    }
   [HideInInspector] public UnityEvent<float> ChangeExp;
   [HideInInspector] public UnityEvent<int> ChangeLevel;

}

[RequireComponent(typeof(PlayerMovement))]
public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance;

    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private Experience _experience = new();
    [SerializeField] private float _staminaConsumptionRate = 1f;
    [SerializeField] private float _staminaRestoreRate = 5f;
    [SerializeField] private Animator _animator;
    private string _animatorTrigger = "Damage";
    private float _stamina;
    private float _health;
    private bool _isDead;

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
            //_stamina = Mathf.Clamp(value, 0, _maxStamina);
            _stamina = _maxStamina;
            if (_stamina == 0)
            {
                NoStamina?.Invoke();
            }
            //StatChanged?.Invoke(PlayerStat.Stamina, _stamina, _maxStamina);

        }
    }

    public float Health
    {
        get
        {
            return _health;
        }
        private set
        {
            _health = Mathf.Clamp(value, 0, _maxHealth);

            if (_health == 0)
            {
                NoHealth?.Invoke();
            }
            StatChanged?.Invoke(PlayerStat.Health, _health, _maxHealth);
            SaveManager.Instance.SavePlayerHealth(_health);

        }
    }
    public void AddExp(float exp)
    {
        _experience.Exp += exp;
    }
    public Experience Experience()
    {
        return _experience;
    }
    public void ChangeExp(float exp)
    {
        StatChanged?.Invoke(PlayerStat.Exp, exp, _experience.ExpToUp);
    }
         
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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
        _experience.ChangeExp.AddListener(ChangeExp);
        _stamina = _maxStamina;
        _health = _maxHealth; 
    }
    private void Start()
    {
        LoadHealth(SaveManager.Instance.LoadPlayerHealth());
        LoadExp(SaveManager.Instance.LoadPlayerExperience().Item1, SaveManager.Instance.LoadPlayerExperience().Item2);
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
                Stamina += _staminaRestoreRate * Time.fixedDeltaTime;
        }
    }

    public float GetStat(PlayerStat stat)
    {
        return _stats[stat];
    }

    public float GetStatMax(PlayerStat stat)
    {
        return _statsMax[stat];
    }
    public void TakeDamage( int damage)
    {
        if(_isDead) 
            return;

        Health = _health - damage;
        _animator.SetTrigger(_animatorTrigger);
        if (_health <= 0)
            Dead();
    }
    public void AddHealth(int heal)
    {
        if (_isDead)
            return;
        Health = _health + heal;
    }
    public void Dead()
    {
        EndGameUIManager.EndGame(EndGameState.Faint);
        _isDead = true;
    }
    public void Revive()
    {
        Health = _maxHealth;
        _isDead = false;
    }
    public void LoadHealth(float hp)
    {
        if(hp != 0) 
            Health = hp;
    }
    public void LoadExp(float exp, int Level)
    {
        _experience.CurrentLevel = Level;
        _experience.Exp = exp;
    }
}
