// LevelledEnemy.cs
using UnityEngine;

/// <summary>
/// Враг с поддержкой уровней: задаёт скорость и HP по уровню.
/// </summary>
public abstract class LevelledEnemy : EnemyCore
{
    [Header("Параметры уровня")]
    [Range(1, 10)] public int mobLevel = 1;
    public float minSpeed = 2f;
    public float maxSpeed = 6f;
    public int minHP = 50;
    public int maxHP = 200;
    protected float speed;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Инициализация параметров по уровню.
    /// </summary>
    public virtual void InitializeLevel(int level)
    {
        mobLevel = Mathf.Clamp(level, 1, 10);
        float t = (mobLevel - 1f) / 9f;
        speed = Mathf.Lerp(minSpeed, maxSpeed, t);
        int hp = Mathf.RoundToInt(Mathf.Lerp(minHP, maxHP, t));
        SetHP(hp);
    }
}
