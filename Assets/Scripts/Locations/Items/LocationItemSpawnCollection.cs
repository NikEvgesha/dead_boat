using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocationItemSpawnCollection", menuName = "Spawning/Location Item Spawn Collection")]
public class LocationItemSpawnCollection : ScriptableObject
{
    [Tooltip("Spawn entries for this location")]
    public List<LocationItemSpawnEntry> spawnEntries;

    public PickableItem GetRandomItem(ItemType requiredType, ItemSize requiredSize)
    {
        if (spawnEntries == null || spawnEntries.Count == 0)
            return null;
        List<LocationItemSpawnEntry> validEntries = new List<LocationItemSpawnEntry>();
        foreach (LocationItemSpawnEntry entry in spawnEntries)
        {
            bool typeMatches = requiredType == ItemType.Any || entry.itemType == requiredType || entry.itemType == ItemType.Any;
            bool sizeMatches = requiredSize == ItemSize.Any || entry.itemSize == requiredSize || entry.itemSize == ItemSize.Any;
            if (typeMatches && sizeMatches)
                validEntries.Add(entry);
        }

        if (validEntries.Count == 0)
            return null;

        float totalChance = 0f;
        List<float> effectiveChances = new List<float>(validEntries.Count);

        foreach (LocationItemSpawnEntry entry in validEntries)
        {
            float chance = GetEffectiveChance(entry);
            effectiveChances.Add(chance);
            totalChance += chance;
        }

        if (totalChance <= 0f)
            return null;

        float randomValue = Random.Range(0f, totalChance);
        for (int i = 0; i < validEntries.Count; i++)
        {
            if (randomValue < effectiveChances[i])
                return validEntries[i].itemPrefab;

            randomValue -= effectiveChances[i];
        }

        return validEntries[validEntries.Count - 1].itemPrefab;
    }

    private static float GetEffectiveChance(LocationItemSpawnEntry entry)
    {
        if (entry == null || entry.itemPrefab == null)
            return 0f;

        float chance = Mathf.Max(0f, entry.chance);

        if (entry.itemPrefab.TryGetComponent<EggCollectibleItem>(out _))
            return EggSpawnRuntimeState.GetEffectiveChance(chance);

        return ProfessionService.ApplyLocationItemSpawnChance(chance);
    }
}
