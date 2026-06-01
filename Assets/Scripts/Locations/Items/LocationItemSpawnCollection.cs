using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocationItemSpawnCollection", menuName = "Spawning/Location Item Spawn Collection")]
public class LocationItemSpawnCollection : ScriptableObject
{
    private const float EggSpawnChanceMultiplier = 2.5f;

    [Tooltip("Spawn entries for this location")]
    public List<LocationItemSpawnEntry> spawnEntries;

    public PickableItem GetRandomItem(ItemType requiredType, ItemSize requiredSize)
    {
        if (spawnEntries == null || spawnEntries.Count == 0)
            return null;
        List<LocationItemSpawnEntry> validEntries = new List<LocationItemSpawnEntry>();
        foreach (LocationItemSpawnEntry entry in spawnEntries)
        {
            if (EntryMatches(entry, requiredType, requiredSize))
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

    public bool TryGetRandomEggItem(ItemType requiredType, ItemSize requiredSize, out PickableItem itemPrefab)
    {
        itemPrefab = null;

        if (spawnEntries == null || spawnEntries.Count == 0)
            return false;

        List<LocationItemSpawnEntry> validEntries = new List<LocationItemSpawnEntry>();
        foreach (LocationItemSpawnEntry entry in spawnEntries)
        {
            if (EntryMatches(entry, requiredType, requiredSize) && IsEggEntry(entry))
                validEntries.Add(entry);
        }

        if (validEntries.Count == 0)
            return false;

        float totalChance = 0f;
        foreach (LocationItemSpawnEntry entry in validEntries)
            totalChance += Mathf.Max(0f, entry.chance);

        if (totalChance <= 0f)
            return false;

        float randomValue = Random.Range(0f, totalChance);
        for (int i = 0; i < validEntries.Count; i++)
        {
            float chance = Mathf.Max(0f, validEntries[i].chance);
            if (randomValue < chance)
            {
                itemPrefab = validEntries[i].itemPrefab;
                return itemPrefab != null;
            }

            randomValue -= chance;
        }

        itemPrefab = validEntries[validEntries.Count - 1].itemPrefab;
        return itemPrefab != null;
    }

    public bool HasEggItem(ItemType requiredType, ItemSize requiredSize)
    {
        if (spawnEntries == null || spawnEntries.Count == 0)
            return false;

        foreach (LocationItemSpawnEntry entry in spawnEntries)
        {
            if (EntryMatches(entry, requiredType, requiredSize) && IsEggEntry(entry))
                return true;
        }

        return false;
    }

    public static bool IsEggPrefab(PickableItem itemPrefab)
    {
        return itemPrefab != null && itemPrefab.TryGetComponent<EggCollectibleItem>(out _);
    }

    private static float GetEffectiveChance(LocationItemSpawnEntry entry)
    {
        if (entry == null || entry.itemPrefab == null)
            return 0f;

        float chance = Mathf.Max(0f, entry.chance);

        if (IsEggEntry(entry))
            return EggSpawnRuntimeState.GetEffectiveChance(chance * EggSpawnChanceMultiplier);

        return ProfessionService.ApplyLocationItemSpawnChance(chance);
    }

    private static bool EntryMatches(LocationItemSpawnEntry entry, ItemType requiredType, ItemSize requiredSize)
    {
        if (entry == null)
            return false;

        bool typeMatches = requiredType == ItemType.Any || entry.itemType == requiredType || entry.itemType == ItemType.Any;
        bool sizeMatches = requiredSize == ItemSize.Any || entry.itemSize == requiredSize || entry.itemSize == ItemSize.Any;
        return typeMatches && sizeMatches;
    }

    private static bool IsEggEntry(LocationItemSpawnEntry entry)
    {
        return entry != null && IsEggPrefab(entry.itemPrefab);
    }
}
