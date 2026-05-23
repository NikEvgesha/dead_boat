using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    private const string DefaultBalanceProfileResourcePath = "LocationBalanceProfile";

    [Header("Player Reference")]
    [Tooltip("Player transform used as the center for roaming enemy spawns")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private BoardController _boardController;

    [Header("Spawn Radius")]
    [Tooltip("Minimum distance from the player")]
    [SerializeField] private float minSpawnDistance = 5f;
    [Tooltip("Maximum distance from the player")]
    [SerializeField] private float maxSpawnDistance = 20f;

    [Header("Enemies")]
    [Tooltip("Zombie prefabs")]
    [SerializeField] private List<ZombieController> enemyTypes = new List<ZombieController>();
    [Tooltip("Enemies spawned per iteration")]
    [SerializeField] private int spawnCount = 3;
    [Tooltip("Delay between spawn iterations in seconds")]
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private LocationBalanceProfile _balanceProfile;

    private int nextEnemyType = 0;
    private bool isNight = false;
    private bool corutineStart = false;

    private void Start()
    {
        EnsureBalanceProfile();

        if (!playerTransform)
            playerTransform = PlayerStatsManager.Instance.transform;

        if (!_boardController)
            _boardController = FindAnyObjectByType<BoardController>();

        if (!corutineStart)
        {
            corutineStart = true;
            DayTime.instanse.DayNightCycle += OnDayNightCycle;
            StartCoroutine(SpawnRoutine());
        }
    }

    private void OnEnable()
    {
        EnsureBalanceProfile();

        if (DayTime.instanse)
        {
            corutineStart = true;
            DayTime.instanse.DayNightCycle += OnDayNightCycle;
            StartCoroutine(SpawnRoutine());
        }
    }

    private void OnDisable()
    {
        corutineStart = false;
        StopAllCoroutines();
        if (DayTime.instanse != null)
            DayTime.instanse.DayNightCycle -= OnDayNightCycle;
    }

    private void OnDayNightCycle()
    {
        isNight = DayTime.instanse.IsNight();
        if (isNight && enemyTypes.Count > 0)
            nextEnemyType = (nextEnemyType + 1) % enemyTypes.Count;
    }

    private IEnumerator SpawnRoutine()
    {
        while (gameObject.activeSelf)
        {
            if (isNight)
                SpawnEnemies();

            yield return new WaitForSeconds(GetEffectiveCooldown());
        }
    }

    private void SpawnEnemies()
    {
        if (playerTransform == null || enemyTypes.Count == 0 || _boardController == null)
            return;

        if (_boardController.TotalDistanceTraveled >= GameManager.Instance.PlayDistance - 500)
            return;

        ZombieController prefab = enemyTypes[nextEnemyType];
        if (prefab == null)
            return;

        int effectiveSpawnCount = GetEffectiveSpawnCount();
        int enemyLevel = GetEffectiveEnemyLevel();
        for (int i = 0; i < effectiveSpawnCount; i++)
        {
            Vector3 spawnPos = GetRandomPositionInAnnulus();
            ZombieController zombie = Instantiate(prefab, spawnPos, prefab.transform.rotation, transform);
            zombie.InitializeLevel(enemyLevel);
        }
    }

    private int GetEffectiveSpawnCount()
    {
        LocationSceneBalance balance = GetSceneBalance();
        return balance != null ? balance.ResolveNightSpawnCount(spawnCount) : spawnCount;
    }

    private float GetEffectiveCooldown()
    {
        LocationSceneBalance balance = GetSceneBalance();
        return balance != null ? balance.ResolveNightSpawnCooldown(cooldown) : cooldown;
    }

    private int GetEffectiveEnemyLevel()
    {
        int sourceLevel = _boardController != null ? _boardController.GetLevel() : 1;
        LocationSceneBalance balance = GetSceneBalance();
        return balance != null ? balance.ResolveEnemyLevel(sourceLevel) : sourceLevel;
    }

    private LocationSceneBalance GetSceneBalance()
    {
        EnsureBalanceProfile();
        return _balanceProfile != null ? _balanceProfile.GetCurrentSceneBalance() : null;
    }

    private void EnsureBalanceProfile()
    {
        if (_balanceProfile != null)
            return;

        _balanceProfile = Resources.Load<LocationBalanceProfile>(DefaultBalanceProfileResourcePath);
    }

    private Vector3 GetRandomPositionInAnnulus()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
        return playerTransform.position + offset;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);
    }
}
