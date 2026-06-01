using System.Collections.Generic;
using UnityEngine;

public class LocationContentSpawner : MonoBehaviour
{
    private const string DefaultBalanceProfileResourcePath = "LocationBalanceProfile";

    [Header("Items")]
    [Tooltip("ScriptableObject with location item spawn entries")]
    public LocationItemSpawnCollection itemCollection;

    [Tooltip("Optional scene balance profile. If empty, Assets/Resources/LocationBalanceProfile is used.")]
    [SerializeField] private LocationBalanceProfile _balanceProfile;

    [Tooltip("Points where collectable items can appear")]
    public List<LocationSpawnPoint> itemSpawnPoints;

    [Header("Enemies")]
    [Tooltip("Enemy prefabs that can defend this location")]
    public List<ZombieController> _enemyPrefabs;

    [Tooltip("Points where enemies can appear")]
    public List<Transform> enemySpawnPoints;

    public float ZPosition;

    private int _level = 1;

    private void Start()
    {
        LocationSceneBalance balance = ResolveBalance();
        SpawnItems(ResolveItemCollection());
        SpawnEnemies(balance);
    }

    private void SpawnItems(LocationItemSpawnCollection activeItemCollection)
    {
        if (activeItemCollection == null || itemSpawnPoints == null)
            return;

        int guaranteedEggSpawnPointIndex = FindGuaranteedEggSpawnPointIndex(activeItemCollection);

        for (int i = 0; i < itemSpawnPoints.Count; i++)
        {
            LocationSpawnPoint spawnPoint = itemSpawnPoints[i];
            if (spawnPoint == null || spawnPoint.spawnTransform == null)
                continue;

            PickableItem itemPrefab = null;
            bool forceEgg = i == guaranteedEggSpawnPointIndex;
            if (forceEgg)
                activeItemCollection.TryGetRandomEggItem(spawnPoint.allowedType, spawnPoint.allowedSize, out itemPrefab);

            itemPrefab ??= activeItemCollection.GetRandomItem(spawnPoint.allowedType, spawnPoint.allowedSize);
            if (itemPrefab == null)
                continue;

            Instantiate(itemPrefab.gameObject, spawnPoint.spawnTransform.position, spawnPoint.spawnTransform.rotation, transform);

            if (LocationItemSpawnCollection.IsEggPrefab(itemPrefab))
                EggSpawnRuntimeState.OnEggSpawned();
        }
    }

    private int FindGuaranteedEggSpawnPointIndex(LocationItemSpawnCollection activeItemCollection)
    {
        if (!EggSpawnRuntimeState.ShouldGuaranteeFirstEggSpawn)
            return -1;

        if (activeItemCollection == null || itemSpawnPoints == null || itemSpawnPoints.Count == 0)
            return -1;

        List<int> candidateIndexes = new List<int>();
        for (int i = 0; i < itemSpawnPoints.Count; i++)
        {
            LocationSpawnPoint spawnPoint = itemSpawnPoints[i];
            if (spawnPoint == null || spawnPoint.spawnTransform == null)
                continue;

            if (activeItemCollection.HasEggItem(spawnPoint.allowedType, spawnPoint.allowedSize))
                candidateIndexes.Add(i);
        }

        if (candidateIndexes.Count == 0)
            return -1;

        return candidateIndexes[Random.Range(0, candidateIndexes.Count)];
    }

    private void SpawnEnemies(LocationSceneBalance balance)
    {
        if (_enemyPrefabs == null || _enemyPrefabs.Count == 0 || enemySpawnPoints == null || enemySpawnPoints.Count == 0)
            return;

        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < enemySpawnPoints.Count; i++)
        {
            if (enemySpawnPoints[i] != null)
                spawnPoints.Add(enemySpawnPoints[i]);
        }

        Shuffle(spawnPoints);

        int enemyLevel = balance != null ? balance.ResolveEnemyLevel(_level) : _level;
        int maxSpawns = balance != null ? balance.ResolveLocationEnemyLimit(spawnPoints.Count) : spawnPoints.Count;
        float spawnChance = balance != null ? balance.locationEnemySpawnChance : 1f;
        int spawnedCount = 0;

        for (int i = 0; i < spawnPoints.Count && spawnedCount < maxSpawns; i++)
        {
            if (Random.value > spawnChance)
                continue;

            ZombieController enemyPrefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Count)];
            if (enemyPrefab == null)
                continue;

            Transform enemyPoint = spawnPoints[i];
            ZombieController enemy = Instantiate(enemyPrefab, enemyPoint.position, enemyPoint.rotation, transform);
            enemy.InitializeLevel(enemyLevel);
            spawnedCount++;
        }
    }

    public void SetLevel(int level)
    {
        _level = level;
    }

    private LocationItemSpawnCollection ResolveItemCollection()
    {
        EnsureBalanceProfile();
        return _balanceProfile != null ? _balanceProfile.ResolveItemCollection(itemCollection) : itemCollection;
    }

    private LocationSceneBalance ResolveBalance()
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

    private static void Shuffle<T>(List<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
