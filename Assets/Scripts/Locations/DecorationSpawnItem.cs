using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DecorationSpawnItem
{
    public GameObject prefab;  // Префаб декорации
    public float chance;       // Шанс (вес) появления этой декорации
}

[CreateAssetMenu(fileName = "DecorationSpawnCollection", menuName = "Spawning/DecorationSpawnCollection")]
public class DecorationSpawnCollection : ScriptableObject
{
    public List<DecorationSpawnItem> spawnItems;

    // Выбирает префаб с учётом указанных весов
    public GameObject GetRandomSpawnPrefab()
    {
        float totalWeight = 0f;
        foreach (var item in spawnItems)
        {
            totalWeight += item.chance;
        }

        float randomValue = Random.Range(0f, totalWeight);
        foreach (var item in spawnItems)
        {
            if (randomValue < item.chance)
                return item.prefab;
            randomValue -= item.chance;
        }
        return null;
    }
}
