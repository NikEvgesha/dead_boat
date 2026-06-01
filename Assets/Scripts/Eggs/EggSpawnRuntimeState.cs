using UnityEngine;

public static class EggSpawnRuntimeState
{
    private static int _collectedInRun;
    private static int _spawnedInRun;
    private static float _decreasePerCollected = 0.12f;
    private static float _minMultiplier = 0.15f;

    public static int CollectedInRun => _collectedInRun;
    public static bool ShouldGuaranteeFirstEggSpawn => _collectedInRun == 0 && _spawnedInRun == 0;

    public static void Configure(float decreasePerCollected, float minMultiplier)
    {
        _decreasePerCollected = Mathf.Clamp01(decreasePerCollected);
        _minMultiplier = Mathf.Clamp(minMultiplier, 0.01f, 1f);
    }

    public static void ResetRun()
    {
        _collectedInRun = 0;
        _spawnedInRun = 0;
    }

    public static void OnEggCollected()
    {
        _collectedInRun++;
    }

    public static void OnEggSpawned()
    {
        _spawnedInRun++;
    }

    public static float GetCurrentMultiplier()
    {
        float multiplier = 1f - (_collectedInRun * _decreasePerCollected);
        return Mathf.Max(_minMultiplier, multiplier);
    }

    public static float GetEffectiveChance(float baseChance)
    {
        return Mathf.Max(0f, baseChance) * GetCurrentMultiplier();
    }
}
