// EnemyCore.cs
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Базовый ядро всех врагов: управление HP, получением урона и смертью.
/// Не содержит уровней и специфики атак.
/// </summary>
public abstract class EnemyCore : MonoBehaviour
{
    [Header("UI и звук")]
    [SerializeField] protected EnemyType enemyType;
    [SerializeField] protected Scrollbar hpBar;
    [SerializeField] protected AudioClip audioDie;
    [SerializeField] protected AudioClip audioDamage;
    [SerializeField] protected AudioClip audioHit;
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioSource audioSourceEnemy;

    public event Action Death;

    protected int currentHP;
    protected int _maxHP;
    protected bool isDead;

    protected virtual void Awake()
    {
        if (hpBar == null)
            hpBar = GetComponentInChildren<Scrollbar>();
    }

    /// <summary>
    /// Устанавливает максимальный HP и текущий HP.
    /// </summary>
    public virtual void SetHP(int maxHP)
    {
        _maxHP = maxHP;
        currentHP = maxHP;
        if (hpBar)
        {
            hpBar.size = 1f;
            hpBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Получение урона.
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        DamagePopup.Create(transform, damage);
        PlayDamageSound();

        currentHP -= damage;
        if (hpBar && !hpBar.gameObject.activeSelf)
            hpBar.gameObject.SetActive(true);
        if (hpBar)
            hpBar.size = (float)currentHP / _maxHP;

        if (currentHP <= 0)
            Die();
    }

    protected virtual void PlayDamageSound()
    {
        if (audioSourceEnemy && audioDamage)
            audioSourceEnemy.PlayOneShot(audioDamage);
    }

    /// <summary>
    /// Смерть врага.
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;
        if (hpBar)
            hpBar.gameObject.SetActive(false);

        PlayDeathSound();
        GameEvents.OnEnemyKilled?.Invoke(enemyType);
        Death?.Invoke();
    }

    protected virtual void PlayDeathSound()
    {
        if (audioSourceEnemy && audioDie)
            audioSourceEnemy.PlayOneShot(audioDie);
    }
}
