using System.Collections;
using UnityEngine;

public class RangedWeaponController : MonoBehaviour
{
    public enum WeaponType { Pistol, Rifle, Shotgun }

    [Header("Настройки оружия")]
    [SerializeField] private UsableItem _usableItem;
    [SerializeField] private WeaponType weaponType = WeaponType.Pistol;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private Camera fpsCam;
    [SerializeField] private Transform muzzleTransform;  // Точка появления пули (дула)
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private Animator _animator;         // Аниматор для оружия
    [SerializeField] private LayerMask hitLayers;


    [Header("Настройки дробовика")]
    [SerializeField] private int pelletCount = 10;
    [SerializeField] private float spreadAngle = 5f;

    [Header("Настройки патронов")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int _currentAmmo = 10 ;
    [SerializeField] private int _reserveAmmo = 30;
    [SerializeField] private float reloadTime = 2f;
    private bool _isReloading = false;

    [Header("Трейл пули")]
    [SerializeField] private GameObject bulletTrailPrefab;
    [SerializeField] private float bulletSpeed = 100f; // Скорость "перемещения" трейла

    public int CurrentAmmo 
    { 
        get { return _currentAmmo; }
        set 
        { 
            _currentAmmo = value;
            AmmoUI.ChangeAmmo(_currentAmmo, _reserveAmmo);
        }
    }
    public int ReserveAmmo
    {
        get { return _reserveAmmo; }
        set
        {
            _reserveAmmo = value;
            AmmoUI.ChangeAmmo(_currentAmmo, _reserveAmmo);
        }
    }

    private bool _isShooting = false;
    private bool _active = false;
    private void Start()
    {
        fpsCam = Camera.main;
    }
    private void OnEnable()
    {
        _usableItem.Active += SetActiveUse;
        SetActiveUse(_usableItem.IsActive);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isShooting = false;
        _active = false;
        AmmoUI.UseGun(_active);
        _usableItem.Active -= SetActiveUse;
        _usableItem.Use -= UseUpdate;
    }

    private void Update()
    {
        // Дополнительная проверка для перезарядки (клавиша R)
        if (PlayerInput.Instance.Reload && !_isReloading && _currentAmmo < maxAmmo && _reserveAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }

    // Управление активацией оружия через _usableItem
    private void SetActiveUse(bool active)
    {
        if (_active == active)
            return;

        _active = active;
        AmmoUI.UseGun(_active);
        AmmoUI.ChangeAmmo(_currentAmmo, _reserveAmmo);
        if (_active)
            _usableItem.Use += UseUpdate;
        else
            _usableItem.Use -= UseUpdate;
    }

    // Метод, вызываемый по событию Use
    private void UseUpdate()
    {
        if (_isReloading)
            return;

        if (_currentAmmo <= 0)
        {
            Debug.Log("Патронов нет! Перезарядитесь.");
            StartCoroutine(Reload());
            return;
        }

        if (!_isShooting)
        {
            // Вызываем анимацию выстрела
            if (_animator != null)
                _animator.SetTrigger("DoAttack");

            Fire();
            _isShooting = true;
            CurrentAmmo--; // Расходуем один патрон на выстрел (даже для дробовика)
            StartCoroutine(ResetShooting());
        }
    }

    // Сброс блокировки выстрела через fireRate
    private IEnumerator ResetShooting()
    {
        yield return new WaitForSeconds(fireRate);
        _isShooting = false;
    }

    // Перезарядка оружия
    private IEnumerator Reload()
    {
        _isReloading = true;

        if (_animator != null)
            _animator.SetTrigger("Reload");

        Debug.Log("Перезарядка...");
        yield return new WaitForSeconds(reloadTime);

        int neededAmmo = maxAmmo - _currentAmmo;
        int ammoToReload = (_reserveAmmo >= neededAmmo) ? neededAmmo : _reserveAmmo;
        CurrentAmmo += ammoToReload;
        ReserveAmmo -= ammoToReload;
        _isReloading = false;
        Debug.Log("Перезарядка завершена. Текущие патроны: " + _currentAmmo);
    }

    // Основной метод выстрела
    private void Fire()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

        // Если оружие - дробовик, выполняем несколько raycast'ов с разбросом
        if (weaponType == WeaponType.Shotgun)
        {
            for (int i = 0; i < pelletCount; i++)
            {
                Quaternion spread = Quaternion.Euler(
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle),
                    0
                );
                Vector3 direction = spread * fpsCam.transform.forward;
                ShootRay(direction);
            }
        }
        else // Для пистолета и винтовки – одиночный луч
        {
            ShootRay(fpsCam.transform.forward);
        }
    }

    // Метод проверки попадания с помощью raycast и создания трейла пули
    private void ShootRay(Vector3 direction)
    {
        Vector3 start = muzzleTransform.position;
        Vector3 endPoint;
        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range, hitLayers))
        {
            Debug.Log("Попадание: " + hit.transform.name);

            ZombieController targetHealth = hit.transform.GetComponentInParent<ZombieController>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage((int)damage);
            }
            endPoint = hit.point;

            if (impactEffect != null)
            {
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGO, 2f);
            }
        }
        else
        {
            endPoint = fpsCam.transform.position + direction * range;
        }

        // Создание визуального трейла пули
        if (bulletTrailPrefab != null)
            StartCoroutine(SpawnTrail(start, endPoint));
    }

    // Сопрограмма для анимации трейла пули
    private IEnumerator SpawnTrail(Vector3 start, Vector3 end)
    {
        GameObject trail = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        LineRenderer lr = trail.GetComponent<LineRenderer>();
        if (lr == null)
            yield break;
        lr.SetPosition(0, start);
        lr.SetPosition(1, start);

        float distance = Vector3.Distance(start, end);
        float travelTime = distance / bulletSpeed;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / travelTime;
            Vector3 currentPos = Vector3.Lerp(start, end, t);
            lr.SetPosition(1, currentPos);
            yield return null;
        }
        lr.SetPosition(1, end);
        Destroy(trail, 0.1f);
    }
}
