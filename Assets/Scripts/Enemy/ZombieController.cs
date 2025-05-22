// ZombieController.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ZombieController : LevelledEnemy
{
    [Header("Таргетинг и дистанции")]
    public Transform target;
    public float detectionDistance = 30f;
    public float chaseDistance = 50f;

    [Header("Атака")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 5;

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
        if (pickable) pickable.enabled = false;
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
        agent.SetDestination(target.position);
        HandleAttack(dist);
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
        if (dist <= attackRange)
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
        player.TakeDamage(attackDamage);
        if (audioSource && audioHit)
            audioSource.PlayOneShot(audioHit);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        animator.SetTrigger("Hurt");
    }

    protected override void Die()
    {
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
}