
// BossControllerBase.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Абстрактный базовый класс для всех боссов.
/// Содержит общие ссылки: NavMeshAgent, Animator и игрока.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public abstract class BossControllerBase : EnemyCore
{
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform player;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = FindObjectOfType<PlayerStatsManager>()?.transform;
    }

    /// <summary>
    /// Инициализация параметров босса (HP, фазы и т.д.).
    /// </summary>
    public abstract void InitializeBoss();
}