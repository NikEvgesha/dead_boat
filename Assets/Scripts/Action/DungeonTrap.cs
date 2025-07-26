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

    public void _InDungeons()
    {
        if (_dungeonActivate) return;
        _dungeonActivate = true;
        StartCoroutine(DungeonsAcivate());
    }
    public void _InTrap()
    {
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
