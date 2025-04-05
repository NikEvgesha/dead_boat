using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieController : MonoBehaviour
{
    [Header("Настройки цели и активации")]
    [Tooltip("Цель (например, игрок), к которой будет двигаться зомби")]
    public Transform target;

    [Tooltip("Расстояние, на котором зомби впервые замечают игрока и активируются")]
    public float detectionDistance = 30f;

    [Tooltip("Расстояние, при превышении которого зомби прекращают преследование игрока")]
    public float chaseDistance = 50f;

    [Header("Настройки атаки")]
    [Tooltip("Расстояние, на котором зомби начинает атаку")]
    public float attackRange = 2f;
    [Tooltip("Время между атаками (секунды)")]
    public float attackCooldown = 1.5f;

    [Header("Параметры уровня моба")]
    [Tooltip("Уровень моба (от 1 до 10)")]
    [Range(1, 10)]
    public int mobLevel = 1;
    [Tooltip("Минимальная скорость (при уровне 1)")]
    public float minSpeed = 2f;
    [Tooltip("Максимальная скорость (при уровне 10)")]
    public float maxSpeed = 6f;
    [Tooltip("Минимальное количество HP (при уровне 1)")]
    public int minHP = 50;
    [Tooltip("Максимальное количество HP (при уровне 10)")]
    public int maxHP = 200;

    [Tooltip("HP bar")]
    [SerializeField] private Scrollbar _hpBar;

    private NavMeshAgent agent;
    private Animator animator;
    private float lastAttackTime;
    [SerializeField] private int currentHP;
    [SerializeField] private int _currentMaxHP;
    private bool _isDie;
    private SimpleRagdoll _simpleRagdoll;
    private bool _useRagdoll;

    private PlayerStatsManager _player;
    private PickableItem _pickableItem;
    void Awake()
    {
        _pickableItem = GetComponentInChildren<PickableItem>();
        _pickableItem.enabled = false;
        _simpleRagdoll = GetComponent<SimpleRagdoll>();
        _useRagdoll = _simpleRagdoll != null;
        _player = FindAnyObjectByType<PlayerStatsManager>();
        target = _player.transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (_hpBar == null)
        {
            _hpBar = GetComponentInChildren<Scrollbar>();
        }
    }

    void Start()
    {
        // Ограничиваем уровень от 1 до 10 и вычисляем параметр t (от 0 до 1)
        mobLevel = Mathf.Clamp(mobLevel, 1, 10);
        float t = (mobLevel - 1f) / 9f;
        agent.speed = Mathf.Lerp(minSpeed, maxSpeed, t);
        _currentMaxHP = Mathf.RoundToInt(Mathf.Lerp(minHP, maxHP, t));
        currentHP = _currentMaxHP;
        lastAttackTime = -attackCooldown;
        _hpBar.gameObject.SetActive(false);
        // На старте, если игрок далеко (больше detectionDistance), отключаем NavMeshAgent
        if (Vector3.Distance(transform.position, target.position) > detectionDistance)
        {
            agent.enabled = false;
        }
    }

    void Update()
    {
        if (_isDie)
            return;
        if (target == null)
            return;

        float distance = Vector3.Distance(transform.position, target.position);

        // Если игрок находится слишком далеко (за пределами chaseDistance), отключаем агент и прекращаем обработку
        if (distance > chaseDistance)
        {
            if (agent.enabled)
            {
                agent.enabled = false;
                animator.SetFloat("Speed", 0f);
            }
            return;
        }
        else
        {
            // Если агент отключен, а игрок уже достаточно близко (в пределах chaseDistance), включаем его
            if (!agent.enabled)
            {
                // Если игрок уже в зоне обнаружения, включаем агент
                if (distance <= detectionDistance)
                {
                    agent.enabled = true;
                }
                else
                {
                    // Если игрок находится между detectionDistance и chaseDistance, можно решить включать агент тоже,
                    // чтобы зомби не теряли цель, если уже преследуют. Здесь можно настроить поведение по желанию.
                    agent.enabled = true;
                }
            }
        }

        if (!agent.enabled)
            return;

        // Обновляем направление движения
        agent.SetDestination(target.position);

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            agent.isStopped = false;
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    /// <summary>
    /// Запускает анимацию атаки (и может наносить урон цели).
    /// </summary>
    void Attack()
    {
        //TakeDamage(10);
        Debug.Log("Зомби атакует!");
        animator.SetTrigger("Attack");
        // Здесь можно добавить дополнительную логику атаки (например, уменьшение HP цели).
    }

    /// <summary>
    /// Получает урон и проверяет, если HP <= 0, то уничтожает зомби.
    /// </summary>
    public void TakeDamage(int damage)
    {

        if (_isDie)
            return;

        Debug.Log("Зомби получает урон!");
        currentHP -= damage;
        if (!_hpBar.gameObject.activeSelf)
        {
            _hpBar.gameObject.SetActive(true);
        }
        _hpBar.size = (float)currentHP / _currentMaxHP;
        //Debug.Log(_hpBar.size);
        if (currentHP <= 0)
        {
            Die();
        }

    }

    void Die()
    {
        _hpBar.gameObject.SetActive(false);
        _isDie = true;
        Debug.Log("Зомби погибает.");
        if (_useRagdoll)
        {
            _simpleRagdoll.EnableRagdoll();
            _pickableItem.enabled = true;
            _pickableItem.transform.SetParent(null);
            //gameObject.SetActive(false);
        }
        // Здесь можно запустить анимацию смерти, отключить агента и т.д.
        Destroy(gameObject);
    }
}
