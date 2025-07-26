// ZombieController.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ZombieController : LevelledEnemy
{
    [SerializeField] private float Exp = 10;
    [Header("Таргетинг и дистанции")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionDistance = 30f;
    [SerializeField] private float chaseDistance = 50f;

    [Header("Атака")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 5;
    [SerializeField] private float _hitRadius;
    [SerializeField] private LayerMask _hitLayers;
    [SerializeField] private Vector3 _PointHit;

    private NavMeshAgent agent;
    private Animator animator;
    private float lastAttack;
    private SimpleRagdoll ragdoll;
    private PickableItem pickable;
    private PlayerStatsManager player;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ragdoll = GetComponent<SimpleRagdoll>();
        pickable = GetComponentInChildren<PickableItem>();
        if (pickable)
        {
            pickable.enabled = false;
            pickable.tag = Tag.Zomby.ToString();
        }
        player = FindObjectOfType<PlayerStatsManager>();
        target = player?.transform;
    }

    void Start()
    {
        InitializeLevel(mobLevel);
        if (Vector3.Distance(transform.position, target.position) > detectionDistance)
            agent.enabled = false;
        else
            EnsureOnNavMesh();
    }

    void Update()
    {
        if (isDead || target == null) return;
        float dist = Vector3.Distance(transform.position, target.position);
        if (!CheckSee(dist)) return;
        Vector3 navTarget = GetNavMeshTarget(target.position);
        agent.SetDestination(navTarget);
        HandleAttack(dist);
    }
    Vector3 GetNavMeshTarget(Vector3 worldPos)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(worldPos, out hit, 2f, NavMesh.AllAreas))
            return hit.position;
        return worldPos;
    }
    bool CanHitPlayer()
    {
        RaycastHit rayHit;
        // Делим расчёт на две части: 
        // 1) точка старта чуть выше центра (например, уровень груди или головы)
        float heightOffset = 1.5f;
        Vector3 origin = transform.position + Vector3.up * heightOffset;
        // 2) направленный вектор на игрока
        Vector3 dirToPlayer = (target.position + Vector3.up * heightOffset - origin).normalized;
        // Проводим луч в направлении игрока на расстояние attackRange
        if (Physics.Raycast(origin, dirToPlayer, out rayHit, attackRange))
        {
            // Если первый попавшийся коллайдер — сам игрок (или его менеджер статистик)
            return rayHit.transform == target
                || rayHit.transform.GetComponentInParent<PlayerStatsManager>() != null;
        }
        return false;
    }

    bool CheckSee(float dist)
    {
        if (dist > chaseDistance)
        {
            if (agent.enabled)
            {
                agent.enabled = false;
                animator.SetFloat("Speed", 0);
            }
            return false;
        }
        if (!agent.enabled)
            EnsureOnNavMesh();
        return true;
    }

    void HandleAttack(float dist)
    {
        if (dist <= attackRange && CanHitPlayer())
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0);
            if (Time.time - lastAttack >= attackCooldown)
            {
                Attack();
                lastAttack = Time.time;
            }
        }
        else
        {
            agent.isStopped = false;
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void Attack()
    {
        transform.LookAt(player.transform);
        animator.SetTrigger("Attack");
        Collider[] hits = Physics.OverlapSphere(this.transform.position + (Vector3.forward * _PointHit.x) + (Vector3.up * _PointHit.y), _hitRadius, _hitLayers);
        foreach (var hit in hits)
        {
            var health = hit.GetComponent<PlayerStatsManager>();
            if (health != null)
                health.TakeDamage(attackDamage);
            if (audioSource && audioHit)
                audioSource.PlayOneShot(audioHit);
        }
        //player.TakeDamage(attackDamage);
        /*
        if (audioSource && audioHit)
            audioSource.PlayOneShot(audioHit);*/
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        animator.SetTrigger("Hurt");
    }

    protected override void Die()
    {
        player.AddExp(Exp);
        if (ragdoll)
        {
            ragdoll.EnableRagdoll();
            if (pickable)
            {
                pickable.enabled = true;
                pickable.transform.SetParent(null);
                pickable.tag = Tag.Item.ToString();
            }
        }
        base.Die();
        Destroy(gameObject);
    }

    bool EnsureOnNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 50f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true;
            return true;
        }
        Debug.LogWarning($"{name} не нашёл NavMesh");
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position+(Vector3.forward*_PointHit.x) + (Vector3.up * _PointHit.z), _hitRadius);
    }
}