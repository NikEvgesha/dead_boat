using UnityEngine;
using System.Collections.Generic;

public class LocationContentSpawner : MonoBehaviour
{
    [Header("Предметы")]
    [Tooltip("ScriptableObject со списком предметов для спавна на локации")]
    public LocationItemSpawnCollection itemCollection;

    [Tooltip("Список точек, где могут появляться собираемые предметы")]
    public List<LocationSpawnPoint> itemSpawnPoints;

    [Header("Враги")]
    [Tooltip("Типы врагов, защищающего локацию")]
    public List<ZombieController> _enemyPrefabs;

    [Tooltip("Список точек для спавна врагов")]
    public List<Transform> enemySpawnPoints;

    public float ZPosition;

    private int _level = 1;
    /*[Tooltip("Вероятность спавна врагов (от 0 до 1)")]
    [Range(0f, 1f)]
    public float enemySpawnChance = 1f;*/

    void Start()
    {
        SpawnItems();
        SpawnEnemies();
    }

    /// <summary>
    /// Спавнит предметы по каждой заданной точке с учётом фильтров (тип и размер).
    /// </summary>
    void SpawnItems()
    {
        foreach (var spawnPoint in itemSpawnPoints)
        {
            // Получаем префаб, подходящий под условия точки
            PickableItem itemPrefab = itemCollection.GetRandomItem(spawnPoint.allowedType, spawnPoint.allowedSize);
            if (itemPrefab != null)
            {
                // Инстанцируем GameObject компонента PickableItem
                Instantiate(itemPrefab.gameObject, spawnPoint.spawnTransform.position, spawnPoint.spawnTransform.rotation, transform);
            }
        }
    }

    /// <summary>
    /// Спавнит врагов в заданных точках с учётом заданной вероятности.
    /// </summary>
    void SpawnEnemies()
    {
        ZombieController enemyPrefab;
        foreach (var enemyPoint in enemySpawnPoints)
        {
            enemyPrefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Count)];
            enemyPrefab = Instantiate(enemyPrefab, enemyPoint.position, enemyPoint.rotation, transform);
            enemyPrefab.InitializeLevel(_level);
        }
    }
    public void SetLevel(int level)
    {
        _level = level;
    }
}
