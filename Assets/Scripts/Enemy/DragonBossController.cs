using UnityEngine;

/// <summary>
/// Контроллер босса Дракон: кружит вокруг заданного центра,
/// случайно выбирает атаку (стрельба или пикирование) с кулдауном,
/// стреляет огнём и выполняет атаку-пикирование через центр плавным движением.
/// Добавлен глобальный множитель скорости для быстрого изменения темпа.
/// </summary>
public class DragonBossController : BossControllerBase
{
    public enum State { Orbiting, Shooting, Diving }

    [Header("Параметры Дракона")]
    [SerializeField] private int _starsHP = 2000;

    [Header("Orbit Parameters")]
    public Transform orbitCenter;
    public float orbitRadius = 10f;
    public float orbitAngularSpeed = 45f;
    public float orbitDuration = 5f;
    public float orbitHeight = 5f;

    [Header("Attack Parameters")]
    public FireballBoss fireProjectile;
    public float shootDuration = 3f;
    public float timeBetweenShots = 0.5f;

    [Header("Dive Attack Parameters")]
    public float diveHeightOffset = -5f;
    public float diveSpeed = 20f;
    public float diveDamage = 20f;

    [Header("Cooldowns")]
    public float shootCooldown = 5f;
    public float diveCooldown = 8f;

    [Header("Modifiers")]
    public float speedMultiplier = 1f;
    public float MaxSpeedMultiplier = 2f;

    private State currentState;
    private float stateTimer;
    private float lastShotTime;
    private int orbitDirection;
    private float currentAngle;
    private Vector3 shootingPosition;

    // Для плавного дайва
    private Vector3 diveStartPos, diveMidPos, diveEndPos, deadPos;
    private float diveTimer, diveTime1, diveTime2;

    // Трекеры для кулдаунов
    private float lastAttackShootTime = -Mathf.Infinity;
    private float lastAttackDiveTime = -Mathf.Infinity;

    public System.Action<float> AddDamage;
    public System.Action Dead;
    private float oldHP;
    protected override void Awake()
    {
        base.Awake();
        orbitCenter = FindAnyObjectByType<BoardController>().transform;
    }
    private void Start()
    {
        SetHP(_starsHP);
        InitializeBoss();
        if (orbitCenter == null)
            Debug.LogError("[DragonBossController] Не задан orbitCenter для орбиты босса!");
        EnterState(State.Orbiting);
    }

    public override void InitializeBoss() { }

    private void Update()
    {
        if (orbitCenter == null) return;
        if (isDead)
        {
            GoToDiePoint();
            return;
        }
        // Ускоряем внутренние таймеры
        if (currentHP != oldHP)
        {
            speedMultiplier = Mathf.Lerp(MaxSpeedMultiplier, speedMultiplier, currentHP*1f / _starsHP);
            oldHP = currentHP;
        }

        stateTimer += Time.deltaTime * speedMultiplier;
        switch (currentState)
        {
            case State.Orbiting:
                UpdateOrbit();
                if (stateTimer >= orbitDuration)
                    SelectNextAttack();
                break;
            case State.Shooting:
                UpdateShooting();
                if (stateTimer >= shootDuration)
                    EnterState(State.Orbiting);
                break;
            case State.Diving:
                UpdateDiving();
                break;
        }
    }

    private void SelectNextAttack()
    {
        bool canShoot = Time.time - lastAttackShootTime >= shootCooldown;
        bool canDive = Time.time - lastAttackDiveTime >= diveCooldown;
        // Выбираем доступную атаку
        if (canShoot && canDive)
        {
            if (Random.value < 0.5f)
                EnterState(State.Shooting);
            else
                EnterState(State.Diving);
        }
        else if (canShoot)
            EnterState(State.Shooting);
        else if (canDive)
            EnterState(State.Diving);
        else
            // Если обе на кулдауне, повторно орбитим
            EnterState(State.Orbiting);
    }

    private void EnterState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;

        Vector3 center = orbitCenter.position + Vector3.up * orbitHeight;

        if (newState == State.Orbiting)
        {
            orbitDirection = (Random.value < 0.5f) ? 1 : -1;
            Vector3 offset = transform.position - center;
            currentAngle = Mathf.Atan2(offset.z, offset.x);
        }
        else if (newState == State.Shooting)
        {
            lastShotTime = -Mathf.Infinity;
            shootingPosition = transform.position;
            lastAttackShootTime = Time.time;
        }
        else if (newState == State.Diving)
        {
            lastAttackDiveTime = Time.time;
            // Настройка точек и времени дайва
            diveStartPos = transform.position;
            diveMidPos = orbitCenter.position + Vector3.up * diveHeightOffset;
            float oppositeAngle = currentAngle + Mathf.PI * orbitDirection;
            Vector3 centerOrbit = orbitCenter.position + Vector3.up * orbitHeight;
            diveEndPos = centerOrbit + new Vector3(Mathf.Cos(oppositeAngle), 0f, Mathf.Sin(oppositeAngle)) * orbitRadius;

            float dist1 = Vector3.Distance(diveStartPos, diveMidPos);
            float dist2 = Vector3.Distance(diveMidPos, diveEndPos);
            diveTime1 = dist1 / (diveSpeed * speedMultiplier);
            diveTime2 = dist2 / (diveSpeed * speedMultiplier);
            diveTimer = 0f;
        }
    }

    private void UpdateOrbit()
    {
        currentAngle += orbitDirection * orbitAngularSpeed * speedMultiplier * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 center = orbitCenter.position + Vector3.up * orbitHeight;
        Vector3 newPos = center + new Vector3(Mathf.Cos(currentAngle), 0f, Mathf.Sin(currentAngle)) * orbitRadius;
        transform.position = newPos;
        Vector3 tangent = new Vector3(-Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle)) * orbitDirection;
        transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
    }

    private void UpdateShooting()
    {
        transform.position = shootingPosition;
        Vector3 lookTarget = player.position;// + Vector3.up * orbitHeight;
        transform.LookAt(lookTarget);

        if (stateTimer - lastShotTime >= timeBetweenShots)
        {
            Vector3 spawnPos = transform.position + transform.forward * 2f;
            FireballBoss fireballBoss = Instantiate(fireProjectile, spawnPos, transform.rotation);
            fireballBoss.SetTarget(lookTarget);
            lastShotTime = stateTimer;
        }
    }

    private void UpdateDiving()
    {
        diveTimer += Time.deltaTime;
        if (diveTimer < diveTime1)
        {
            float t = diveTimer / diveTime1;
            transform.position = Vector3.Lerp(diveStartPos, diveMidPos, t);
            transform.LookAt(diveMidPos);
        }
        else if (diveTimer < diveTime1 + diveTime2)
        {
            float t = (diveTimer - diveTime1) / diveTime2;
            transform.position = Vector3.Lerp(diveMidPos, diveEndPos, t);
            transform.LookAt(diveEndPos);
        }
        else
        {
            currentAngle += Mathf.PI * orbitDirection;
            EnterState(State.Orbiting);
        }
    }
    private void GoToDiePoint()
    {

        diveTimer += Time.deltaTime;
        if (diveTimer < diveTime1)
        {
            float t = diveTimer / diveTime1;
            transform.position = Vector3.Lerp(diveStartPos, deadPos, t);
            transform.LookAt(deadPos);
            return;
        }
        gameObject.SetActive(false);
        Dead?.Invoke();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (currentState == State.Diving && other.CompareTag("Player"))
        {
            var hp = other.GetComponent<PlayerStatsManager>();
            if (hp != null)
                hp.TakeDamage((int)diveDamage);
        }
    }

    public float GetStartHP()
    {
        return _starsHP;
    }
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        AddDamage?.Invoke(damage);
    }
    protected override void Die()
    {
        diveStartPos = transform.position;
        deadPos = new Vector3(transform.position.x, transform.position.y - 20, transform.position.z);
        diveTimer = 0;
        base.Die();
        //_mainBody.DeadTentacle(this);
        //StartAnimation(TentacleAnimation.Dead);
    }
}
