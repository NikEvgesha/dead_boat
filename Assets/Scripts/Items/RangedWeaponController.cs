using System.Collections;
using UnityEngine;

public class RangedWeaponController : MonoBehaviour
{

    [Header("Настройки оружия")]
    [SerializeField] private ActivateItem _usableItem;
    [SerializeField] private WeaponType weaponType = WeaponType.Pistol;
    [SerializeField] private float _damage = 10f;
    public float Damage
    {
        get
        {
            float damage = _damage;
            if (LevelStatManager.Instance)
                damage += LevelStatManager.Instance.Stats.RangeDamage;
            return ProfessionService.ApplyRangedDamage(damage);
        }
    }
    [SerializeField] private float range = 100f;
    [SerializeField] private float _attackSpeed = 0.5f;
    public float AttackSpeed
    {
        get
        {
            float attackSpeed = _attackSpeed;
            if (LevelStatManager.Instance)
                attackSpeed *= LevelStatManager.Instance.Stats.RangeAttackSpeed;
            return ProfessionService.ApplyRangedAttackSpeed(attackSpeed);
        }
    }
    [SerializeField] private Camera fpsCam;
    [SerializeField] private Transform muzzleTransform;  // Точка появления пули (дула)
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private Animator _animator;         // Аниматор для оружия
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private AudioClip _audioReload;
    [SerializeField] private AudioClip _audioShoot;
    [SerializeField] private AudioSource _audioSource;


    [Header("Настройки дробовика")]
    [SerializeField] private int pelletCount = 10;
    [SerializeField] private float spreadAngle = 5f;

    [Header("Настройки патронов")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int _currentAmmo = 10 ;
    //private int _reserveAmmo = 30;
    [SerializeField] private float _reloadTime = 2f;
    public float ReloadSpeed
    {
        get
        {
            float reloadSpeed = _reloadTime;
            if (LevelStatManager.Instance)
                reloadSpeed *= LevelStatManager.Instance.Stats.RangeReloadSpeed;
            return ProfessionService.ApplyRangedReloadSpeed(reloadSpeed);
        }
    }

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
            AmmoUI.ChangeAmmo?.Invoke(_currentAmmo, ReserveAmmo);
        }
    }
    public int ReserveAmmo
    {
        get { return PlayerAmmoManager.Instance.GetAmmo(weaponType); }
        set
        {
            int change = PlayerAmmoManager.Instance.GetAmmo(weaponType) - value;
            if (change < 0)
                PlayerAmmoManager.Instance.AddAmmo(weaponType,change);

            if (change > 0)
                PlayerAmmoManager.Instance.UseAmmo(weaponType, change);

            AmmoUI.ChangeAmmo?.Invoke(_currentAmmo, PlayerAmmoManager.Instance.GetAmmo(weaponType));
        }
    }

    private bool _isShooting = false;
    private bool _active = false;
    private void Start()
    {
        fpsCam = Camera.main;
        SetActiveUse(_usableItem.IsActive);
    }
    private void OnEnable()
    {
        _usableItem.Active += SetActiveUse;
        //SetActiveUse(_usableItem.IsActive);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isShooting = false;
        _active = false;
        _isReloading = false;
        AmmoUI.UseGun?.Invoke(_active);
        _usableItem.Active -= SetActiveUse;
        _usableItem.Use -= UseUpdate;
        //ControlUI.Instance.ShowAttackButton(_active);
        //ControlUI.Instance.ShowReloadButton(_active);
    }

    private void Update()
    {
        // Дополнительная проверка для перезарядки (клавиша R)
        if (PlayerInput.Instance.Reload && !_isReloading && _currentAmmo < maxAmmo && ReserveAmmo > 0)
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

        ControlUI.Instance.ShowAttackButton(_active);
        ControlUI.Instance.ShowReloadButton(_active);
        AmmoUI.UseGun?.Invoke(_active);
        AmmoUI.ChangeAmmo?.Invoke(_currentAmmo, ReserveAmmo);

        if (_active)
        {
            PlayerAmmoManager.Instance.NewAmmo += NewAmmo;
            _usableItem.Use += UseUpdate;
        }
        else
        {
            PlayerAmmoManager.Instance.NewAmmo -= NewAmmo;
            _usableItem.Use -= UseUpdate;
        }
    }
    private void NewAmmo()
    {
        ReserveAmmo = ReserveAmmo;
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
            {
                _animator.speed = AttackSpeed / _attackSpeed;
                _animator.SetTrigger("DoAttack");
            }
            
            Fire();
            _isShooting = true;
            CurrentAmmo--; // Расходуем один патрон на выстрел (даже для дробовика)
            StartCoroutine(ResetShooting());
        }
    }

    // Сброс блокировки выстрела через fireRate
    private IEnumerator ResetShooting()
    {
        yield return new WaitForSeconds(_attackSpeed/AttackSpeed);
        _isShooting = false;
    }

    // Перезарядка оружия
    private IEnumerator Reload()
    {
        if (ReserveAmmo!=0)
        {
            _isReloading = true;

            if (_animator != null)
            {
                _animator.speed = ReloadSpeed/_reloadTime;
                _animator.SetTrigger("Reload");
            }
            if (_audioSource)
                if (_audioReload)
                    _audioSource.PlayOneShot(_audioReload);
            Debug.Log("Перезарядка...");
            yield return new WaitForSeconds(_reloadTime/ReloadSpeed);

            int neededAmmo = maxAmmo - _currentAmmo;
            int ammoToReload = (ReserveAmmo >= neededAmmo) ? neededAmmo : ReserveAmmo;
            CurrentAmmo += ammoToReload;
            ReserveAmmo -= ammoToReload;
            _isReloading = false;
            Debug.Log("Перезарядка завершена. Текущие патроны: " + _currentAmmo);
        } 
        else
        {
            _isReloading = true;
            AmmoUI.Instance.NoAmmo();
            Debug.Log("Добавить анимацию отсутствия патрон");
            yield return new WaitForSeconds(1);
            _isReloading = false;
        }
    }

    // Основной метод выстрела
    private void Fire()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        if (_audioSource)
            if(_audioShoot)
                _audioSource.PlayOneShot(_audioShoot);
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

            EnemyCore targetHealth = hit.transform.GetComponentInParent<EnemyCore>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage((int)Damage);
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
    public WeaponType GetWeaponType()
    {
        return weaponType;
    }
    public bool TryAddAmmo(int ammo, WeaponType type)
    {
        // Сюда больше не приходят
        if (type == weaponType)
        {
            ReserveAmmo += ammo;
            return true;
        }
        return false;
    }
}
