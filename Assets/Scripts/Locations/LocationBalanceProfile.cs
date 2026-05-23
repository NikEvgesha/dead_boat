using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LocationSceneBalance
{
    public string sceneName;
    public LocationItemSpawnCollection itemCollectionOverride;
    [Min(0.1f)] public float enemyLevelMultiplier = 1f;
    public int enemyLevelOffset;
    [Min(0)] public int maxLocationEnemySpawns;
    [Range(0f, 1f)] public float locationEnemySpawnChance = 1f;
    [Min(0f)] public float nightSpawnCountMultiplier = 1f;
    [Min(0.1f)] public float nightSpawnCooldownMultiplier = 1f;

    public int ResolveEnemyLevel(int sourceLevel)
    {
        int level = Mathf.RoundToInt(Mathf.Max(1, sourceLevel) * enemyLevelMultiplier) + enemyLevelOffset;
        return Mathf.Clamp(level, 1, 10);
    }

    public int ResolveLocationEnemyLimit(int spawnPointCount)
    {
        if (maxLocationEnemySpawns <= 0)
            return Mathf.Max(0, spawnPointCount);

        return Mathf.Min(Mathf.Max(0, spawnPointCount), maxLocationEnemySpawns);
    }

    public int ResolveNightSpawnCount(int baseSpawnCount)
    {
        if (baseSpawnCount <= 0 || nightSpawnCountMultiplier <= 0f)
            return 0;

        float scaledCount = baseSpawnCount * nightSpawnCountMultiplier;
        int result = Mathf.FloorToInt(scaledCount);
        float fraction = scaledCount - result;

        if (fraction > 0f && UnityEngine.Random.value < fraction)
            result++;

        return Mathf.Max(0, result);
    }

    public float ResolveNightSpawnCooldown(float baseCooldown)
    {
        return Mathf.Max(0.1f, baseCooldown * nightSpawnCooldownMultiplier);
    }
}

[CreateAssetMenu(fileName = "LocationBalanceProfile", menuName = "Spawning/Location Balance Profile")]
public class LocationBalanceProfile : ScriptableObject
{
    [SerializeField] private LocationSceneBalance _defaultBalance = new();
    [SerializeField] private List<LocationSceneBalance> _sceneBalances = new();

    public LocationSceneBalance DefaultBalance => _defaultBalance;
    public IReadOnlyList<LocationSceneBalance> SceneBalances => _sceneBalances;

    public LocationSceneBalance GetCurrentSceneBalance()
    {
        return GetBalanceForScene(SceneManager.GetActiveScene().name);
    }

    public LocationSceneBalance GetBalanceForScene(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName) && _sceneBalances != null)
        {
            for (int i = 0; i < _sceneBalances.Count; i++)
            {
                LocationSceneBalance balance = _sceneBalances[i];
                if (balance == null)
                    continue;

                if (string.Equals(balance.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                    return balance;
            }
        }

        return _defaultBalance;
    }

    public LocationItemSpawnCollection ResolveItemCollection(LocationItemSpawnCollection fallback)
    {
        LocationSceneBalance balance = GetCurrentSceneBalance();
        return balance != null && balance.itemCollectionOverride != null
            ? balance.itemCollectionOverride
            : fallback;
    }
}