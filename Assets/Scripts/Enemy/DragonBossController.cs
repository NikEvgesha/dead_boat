
// DragonBossController.cs
using UnityEngine;

/// <summary>
/// Контроллер босса Дракон: летает и стреляет.
/// </summary>
public class DragonBossController : BossControllerBase
{
    [Header("Параметры полёта")] public float flySpeed = 5f;
    [Header("Атака огнём")] public GameObject fireProjectile;
    public float shootInterval = 2f;

    private float lastShoot;

    void Start()
    {
        SetHP(2000);
        InitializeBoss();
    }

    public override void InitializeBoss()
    {
        // Любая дополнительная инициализация фаз дракона
    }

    void Update()
    {
        if (isDead) return;
        FlyAround();
        TryShoot();
    }

    void FlyAround()
    {
        // Простейший паттерн полёта вокруг игрока
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * flySpeed * Time.deltaTime;
        transform.LookAt(player);
    }

    void TryShoot()
    {
        if (Time.time - lastShoot > shootInterval)
        {
            Instantiate(fireProjectile, transform.position + transform.forward * 2f, transform.rotation);
            lastShoot = Time.time;
        }
    }
}
