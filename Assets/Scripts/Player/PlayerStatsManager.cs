using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct Experience
{
    public int CurrentLevel;
    public List<int> LevelsExpToUp;
    public int ExpToUp
    {
        get 
        {
            return LevelsExpToUp[Mathf.Min(CurrentLevel, LevelsExpToUp.Count - 1)];
        }
    }
    private int _exp;
    public int Exp
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
                NewLevel?.Invoke();
            }
            else
            {
                _exp = value;
            }
            SaveManager.Instance.SavePlayerExperience(_exp, CurrentLevel);
            ChangeExp?.Invoke(_exp);
        }
    }
    public int ResExp
    {
        set
        {
            if (value != 0) return;
            CurrentLevel = value;
            _exp = value;
            SaveManager.Instance.SavePlayerExperience(_exp, CurrentLevel);
            ChangeExp?.Invoke(value);
            ChangeLevel?.Invoke(value);
        }
    }
    [HideInInspector] public UnityEvent<int> ChangeExp;
   [HideInInspector] public UnityEvent<int> ChangeLevel;
    [HideInInspector] public UnityEvent NewLevel;

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
    public bool InGame = true;
    private string _animatorTrigger = "Damage";
    private float _stamina;
    private float _health;
    private bool _isDead;

    private Dictionary<PlayerStat, float> _stats;
    private Dictionary<PlayerStat, float> _statsMax;

    public Action NoStamina;
    public Action NoHealth;
    public Action<PlayerStat, float, float> StatChanged;
    public float MaxHealth
    {
        get {
            float health = _maxHealth;
            if (LevelStatManager.Instance)
                health += LevelStatManager.Instance.Stats.HP;
            return ProfessionService.ApplyMaxHealth(health);
        }
    }
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
            _health = Mathf.Clamp(value, 0, MaxHealth);

            if (_health == 0)
            {
                NoHealth?.Invoke();
            }
            StatChanged?.Invoke(PlayerStat.Health, _health, MaxHealth);
            SaveManager.Instance.SavePlayerHealth(_health);

        }
    }
    public void AddExp(int exp)
    {
        if (LevelStatManager.Instance)
            exp = (int)(exp * LevelStatManager.Instance.Stats.MultExp);
        exp = ProfessionService.ApplyExperienceGain(exp);
        _experience.Exp += exp;
    }
    public Experience Experience()
    {
        return _experience;
    }
    public void ChangeExp(int exp)
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
            { PlayerStat.Health, MaxHealth},
            { PlayerStat.Stamina, _maxStamina}
        };
        _experience.ChangeExp.AddListener(ChangeExp);
        _stamina = _maxStamina;
        _health = MaxHealth; 
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
        if(_isDead || !InGame) 
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
        Health = MaxHealth;
        _isDead = false;
    }
    public void LoadHealth(float hp)
    {
        if(hp != 0) 
            Health = hp;
    }
    public void LoadExp(int exp, int Level)
    {
        _experience.CurrentLevel = Level;
        _experience.Exp = exp;
    }
    public void ResetExp()
    {
        _experience.ResExp = 0;
    }
}
