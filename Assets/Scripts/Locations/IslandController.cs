using UnityEngine;
using System.Collections.Generic;

public class IslandController : MonoBehaviour
{
    [Header("Точки спавна лута")]
    [SerializeField] private Transform[] _lootSpawnPoints;

    [Header("Точки спавна монстров")]
    [SerializeField] private Transform[] _monsterSpawnPoints;

    [Header("Настройки лута")]
    [SerializeField] private List<LootItem> _lootItems;

    [Header("Настройки монстров")]
    [SerializeField] private List<MonsterItem> _monsterItems;
    [SerializeField] private int _minMonstersPerPoint = 0;
    [SerializeField] private int _maxMonstersPerPoint = 3;

    void Start()
    {
        SpawnLoot();
        SpawnMonsters();
    }

    /// <summary>
    /// Спавнит лут на всех заданных точках спавна.
    /// </summary>
    private void SpawnLoot()
    {
        if (_lootSpawnPoints == null || _lootSpawnPoints.Length == 0)
        {
            Debug.LogWarning("Не заданы точки спавна лута для острова.");
            return;
        }

        foreach (Transform spawnPoint in _lootSpawnPoints)
        {
            GameObject lootPrefab = ChooseLootPrefab();
            if (lootPrefab != null)
            {
                Instantiate(lootPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            }
        }
    }

    /// <summary>
    /// Спавнит монстров на всех заданных точках спавна.
    /// Для каждой точки генерируется случайное количество монстров.
    /// </summary>
    private void SpawnMonsters()
    {
        if (_monsterSpawnPoints == null || _monsterSpawnPoints.Length == 0)
        {
            Debug.LogWarning("Не заданы точки спавна монстров для острова.");
            return;
        }

        foreach (Transform spawnPoint in _monsterSpawnPoints)
        {
            int monsterCount = Random.Range(_minMonstersPerPoint, _maxMonstersPerPoint + 1);
            for (int i = 0; i < monsterCount; i++)
            {
                GameObject monsterPrefab = ChooseMonsterPrefab();
                if (monsterPrefab != null)
                {
                    // Добавляем небольшое случайное смещение, чтобы монстры не накладывались друг на друга
                    Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                    Instantiate(monsterPrefab, spawnPoint.position + randomOffset, spawnPoint.rotation, transform);
                }
            }
        }
    }

    /// <summary>
    /// Выбирает префаб лута из списка, используя весовые коэффициенты.
    /// </summary>
    /// <returns>Выбранный префаб лута</returns>
    private GameObject ChooseLootPrefab()
    {
        if (_lootItems == null || _lootItems.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var item in _lootItems)
        {
            totalWeight += item.Weight;
        }
        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        foreach (var item in _lootItems)
        {
            currentSum += item.Weight;
            if (randomValue <= currentSum)
            {
                return item.Prefab;
            }
        }
        return _lootItems[_lootItems.Count - 1].Prefab;
    }

    /// <summary>
    /// Выбирает префаб монстра из списка, используя весовые коэффициенты.
    /// </summary>
    /// <returns>Выбранный префаб монстра</returns>
    private GameObject ChooseMonsterPrefab()
    {
        if (_monsterItems == null || _monsterItems.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var item in _monsterItems)
        {
            totalWeight += item.Weight;
        }
        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        foreach (var item in _monsterItems)
        {
            currentSum += item.Weight;
            if (randomValue <= currentSum)
            {
                return item.Prefab;
            }
        }
        return _monsterItems[_monsterItems.Count - 1].Prefab;
    }
}

/// <summary>
/// Класс для описания предмета лута с весом (шансом появления)
/// </summary>
[System.Serializable]
public class LootItem
{
    [Tooltip("Префаб предмета лута")]
    public GameObject Prefab;

    [Tooltip("Вес (шанс) появления лута")]
    public float Weight = 1f;
}

/// <summary>
/// Класс для описания монстра с весом (шансом появления)
/// </summary>
[System.Serializable]
public class MonsterItem
{
    [Tooltip("Префаб монстра")]
    public GameObject Prefab;

    [Tooltip("Вес (шанс) появления монстра")]
    public float Weight = 1f;
}
