using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocationItemSpawnCollection", menuName = "Spawning/Location Item Spawn Collection")]
public class LocationItemSpawnCollection : ScriptableObject
{
    [Tooltip("Список вариантов спавна предметов для локации")]
    public List<LocationItemSpawnEntry> spawnEntries;

    /// <summary>
    /// Возвращает случайный префаб предмета (PickableItem) с учётом весов, подходящий под заданные фильтры.
    /// </summary>
    /// <param name="requiredType">Требуемый тип (или Any)</param>
    /// <param name="requiredSize">Требуемый размер (или Any)</param>
    /// <returns>Выбранный префаб (PickableItem) или null, если вариантов нет</returns>
    public PickableItem GetRandomItem(ItemType requiredType, ItemSize requiredSize)
    {
        // Фильтруем варианты по типу и размеру.
        List<LocationItemSpawnEntry> validEntries = new List<LocationItemSpawnEntry>();
        foreach (var entry in spawnEntries)
        {
            bool typeMatches = (requiredType == ItemType.Any || entry.itemType == requiredType || entry.itemType == ItemType.Any);
            bool sizeMatches = (requiredSize == ItemSize.Any || entry.itemSize == requiredSize || entry.itemSize == ItemSize.Any);
            if (typeMatches && sizeMatches)
            {
                validEntries.Add(entry);
            }
        }
        if (validEntries.Count == 0)
            return null;

        // Вычисляем сумму шансов.
        float totalChance = 0f;
        foreach (var entry in validEntries)
        {
            totalChance += entry.chance;
        }
        float randomValue = Random.Range(0f, totalChance);
        foreach (var entry in validEntries)
        {
            if (randomValue < entry.chance)
                return entry.itemPrefab;
            randomValue -= entry.chance;
        }
        return validEntries[validEntries.Count - 1].itemPrefab;
    }
}
