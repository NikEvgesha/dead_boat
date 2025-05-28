using UnityEngine;

/// <summary>
/// Контроллер босса Дракон: кружит вокруг заданного центра, останавливается для атаки и снова уходит в полёт.
/// </summary>
public class DragonBossController : BossControllerBase
{
    public enum State { Orbiting, Shooting }

    [Header("Параметры Дракона")]
    [SerializeField] private int _starsHP = 2000;

    [Header("Orbit Parameters")]
    public Transform orbitCenter;                  // Точка, вокруг которой летит дракон
    public float orbitRadius = 10f;               // Радиус орбиты
    public float orbitAngularSpeed = 45f;          // Скорость вращения в градусах/с
    public float orbitDuration = 5f;               // Время орбиты перед атакой
    public float orbitHeight = 5f;                 // Высота полёта относительно центра

    [Header("Attack Parameters")]
    public GameObject fireProjectile;              // Префаб огненного выстрела
    public float shootDuration = 3f;               // Время стрельбы
    public float timeBetweenShots = 0.5f;          // Интервал между выстрелами

    private State currentState;
    private float stateTimer;
    private float lastShotTime;
    private int orbitDirection;
    private float currentAngle;
    private Vector3 shootingPosition;

    void Start()
    {
        SetHP(_starsHP);
        InitializeBoss();
        if (orbitCenter == null)
            Debug.LogError("[DragonBossController] Не задан orbitCenter для орбиты босса!");
        EnterState(State.Orbiting);
    }

    public override void InitializeBoss() { }

    void Update()
    {
        if (isDead || orbitCenter == null) return;
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Orbiting:
                UpdateOrbit();
                if (stateTimer >= orbitDuration)
                    EnterState(State.Shooting);
                break;

            case State.Shooting:
                UpdateShooting();
                if (stateTimer >= shootDuration)
                    EnterState(State.Orbiting);
                break;
        }
    }

    private void EnterState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;

        if (newState == State.Orbiting)
        {
            // Выбираем направление и рассчитываем начальный угол относительно orbitCenter
            orbitDirection = (Random.value < 0.5f) ? 1 : -1;
            Vector3 center = orbitCenter.position + Vector3.up * orbitHeight;
            Vector3 offset = transform.position - center;
            currentAngle = Mathf.Atan2(offset.z, offset.x);
        }
        else if (newState == State.Shooting)
        {
            lastShotTime = -Mathf.Infinity;
            shootingPosition = transform.position;
        }
    }

    private void UpdateOrbit()
    {
        // Движение по окружности вокруг orbitCenter
        currentAngle += orbitDirection * orbitAngularSpeed * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 center = orbitCenter.position + Vector3.up * orbitHeight;
        Vector3 newPos = center + new Vector3(Mathf.Cos(currentAngle), 0f, Mathf.Sin(currentAngle)) * orbitRadius;
        transform.position = newPos;

        // Поворот по направлению движения
        Vector3 tangentDir = new Vector3(-Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle)) * orbitDirection;
        transform.rotation = Quaternion.LookRotation(tangentDir, Vector3.up);
    }

    private void UpdateShooting()
    {
        // Фиксируем позицию и разворачиваемся к игроку
        transform.position = shootingPosition;
        Vector3 lookTarget = player.position + Vector3.up * orbitHeight;
        transform.LookAt(lookTarget);

        if (stateTimer - lastShotTime >= timeBetweenShots)
        {
            Vector3 spawnPos = transform.position + transform.forward * 2f;
            Instantiate(fireProjectile, spawnPos, transform.rotation);
            lastShotTime = stateTimer;
        }
    }
}
