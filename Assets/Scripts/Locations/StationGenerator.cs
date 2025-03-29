using UnityEngine;
using System.Collections.Generic;

public class StationGenerator : MonoBehaviour
{
    [Header("Настройки генерации зданий")]
    [Tooltip("Точки для размещения зданий")]
    [SerializeField] private Transform[] _buildingSpawnPoints;

    [Tooltip("Префабы зданий")]
    [SerializeField] private GameObject[] _buildingPrefabs;

    [Tooltip("Флаг: станция содержит магазины (применяется для промежуточных станций)")]
    [SerializeField] private bool _hasShops = true;

    [Tooltip("Вероятность наличия магазина в здании (если применимо)")]
    [SerializeField] private float _shopProbability = 0.3f;

    void Start()
    {
        GenerateStation();
    }

    /// <summary>
    /// Генерирует станцию: перемешивает точки спавна и в каждой из них создаёт здание.
    /// При наличии магазинов случайно включает их для зданий с соответствующим контроллером.
    /// </summary>
    private void GenerateStation()
    {
        // Получаем список точек и перемешиваем их (чтобы здания располагались в случайном порядке)
        List<Transform> spawnPoints = new List<Transform>(_buildingSpawnPoints);
        ShuffleList(spawnPoints);

        foreach (Transform spawnPoint in spawnPoints)
        {
            // Выбираем случайный префаб здания
            int prefabIndex = Random.Range(0, _buildingPrefabs.Length);
            GameObject buildingPrefab = _buildingPrefabs[prefabIndex];

            // Создаём здание на позиции точки спавна, делая его дочерним объектом станции
            GameObject building = Instantiate(buildingPrefab, spawnPoint.position, spawnPoint.rotation, transform);

            // Если станция должна иметь магазины, проверяем наличие BuildingController в здании
            if (_hasShops)
            {
                BuildingController buildingController = building.GetComponent<BuildingController>();
                if (buildingController != null)
                {
                    // С вероятностью _shopProbability включаем магазин
                    bool hasShop = Random.value < _shopProbability;
                    buildingController.SetShopActive(hasShop);
                }
            }
        }
    }

    /// <summary>
    /// Реализация алгоритма перемешивания списка (Fisher-Yates shuffle)
    /// </summary>
    /// <typeparam name="T">Тип элементов списка</typeparam>
    /// <param name="list">Список для перемешивания</param>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
