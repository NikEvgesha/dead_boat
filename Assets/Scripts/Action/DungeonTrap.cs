using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonTrap : MonoBehaviour
{
    [SerializeField] List<LightStick> _torchs;
    [SerializeField] float _waitOn = 0.5f;

    [Header("Enemies")]
    [Tooltip("Типы зомби")]
    [SerializeField] private ZombieController _enemyTypes;
    [Tooltip("Сколько зомби спавнить за одну итерацию")]
    [SerializeField] private int _spawnCount = 10;
    [Tooltip("Пауза между итерациями спавна (сек)")]
    [SerializeField] private float _cooldown = 1f;

    [SerializeField] private Transform _spawnPos;

    private bool _dungeonActivate;
    private bool _trapActivate;
    private int _configuredSpawnCount = -1;

    public ZombieController ScriptedEnemyPrefab => _enemyTypes;
    public int ScriptedSpawnCount => Mathf.Max(0, _configuredSpawnCount >= 0 ? _configuredSpawnCount : _spawnCount);
    public Transform ScriptedSpawnPosition => _spawnPos;
    public float ScriptedCooldown => Mathf.Max(0f, _cooldown);
    public float ScriptedFirstDelay => _torchs != null && _torchs.Count > 1 ? Mathf.Max(0f, _waitOn) : 0f;
    public int ScriptedLevel
    {
        get
        {
            var content = GetComponentInParent<LocationContentSpawner>(true);
            return content != null ? content.ResolveScriptedEnemyLevel() : 1;
        }
    }

    private void Awake() => _configuredSpawnCount = _spawnCount;
    private void Start() => DeadBoat.Online.SharedWorldActionsRuntime.Register(this);
    private void OnDestroy() => DeadBoat.Online.SharedWorldActionsRuntime.Remove(this);

    // Accepted state only. Shared authority owns wave construction and its cursor.
    public void SharedApply(int flags, bool restoring)
    {
        _dungeonActivate = (flags & 1) != 0;
        _trapActivate = (flags & 2) != 0;
        if ((!_dungeonActivate && !_trapActivate) || _torchs == null) return;
        foreach (var torch in _torchs)
            if (torch != null) torch.ActivateLight(!_trapActivate);
    }

    public void _InDungeons()
    {
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 1)) return;
        if (_dungeonActivate) return;
        _dungeonActivate = true;
        StartCoroutine(DungeonsAcivate());
    }
    public void _InTrap()
    {
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 2)) return;
        if (_trapActivate) return;
        _trapActivate = true;
        StartCoroutine(TrupActivate());
    }
    private IEnumerator TrupActivate()
    {
        if (_torchs.Count > 0)
        {
            int onTorch = 0;
            foreach (var torch in _torchs) 
            {
                torch.ActivateLight(false);
                onTorch++;
                if (onTorch == 2)
                {
                    yield return new WaitForSeconds(_waitOn);
                }
            }
        }
        while (_spawnCount > 0)
        {
            _spawnCount--;
            SpawnEnemies();
            yield return new WaitForSeconds(_cooldown);
        }
        yield break;
    }
    private IEnumerator DungeonsAcivate()
    {
        if (_torchs.Count > 0)
        {
            int onTorch = 0;
            foreach (var torch in _torchs)
            {
                torch.ActivateLight(true);
                onTorch++;
                if (onTorch == 2)
                {
                    yield return new WaitForSeconds(_waitOn);
                }
            }
        }
        yield break;
    }

    private void SpawnEnemies()
    {
        ZombieController zomby = Instantiate(_enemyTypes, _spawnPos.position, _enemyTypes.transform.rotation, transform);
        zomby.InitializeLevel(1);
    }
}
