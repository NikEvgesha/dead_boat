using UnityEngine;
using System.Collections.Generic;
using System;

public class LocationSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    public BoardController boardController;           // Ссылка на компонент BoardController с TotalDistanceTraveled
    public LocationSpawnCollection locationCollection;  // Ссылка на ScriptableObject с вариантами объектов для спавна
    public float minDistance = 100f;                    // Минимальное расстояние до следующего объекта
    public float maxDistance = 200f;                    // Максимальное расстояние до следующего объекта
    public float spawnThreshold = 20f;                  // Порог спавна впереди (например, поезда)

    [Header("Настройки позиции")]
    public float spawnX = 10f;                          // Фиксированная позиция по X (например, берег)
    public float spawnY = 0f;                           // Фиксированная позиция по Y (уровень земли)
    // Координата Z рассчитывается динамически

    [Header("Оптимизация")]
    public float removalDistance = 200f;                // Расстояние позади поезда, после которого объект удаляется

    private float _stopSpawnDistance = 100000f;

    private float lastSpawnZ;                           // Координата Z последнего созданного объекта
    private List<LocationContentSpawner> spawnedLocations = new List<LocationContentSpawner>();

    public static Action Change;

    void Start()
    {
        if (boardController == null)
            boardController = FindObjectOfType<BoardController>();
        if (boardController == null)
            Debug.LogError("BoardController не найден");

        _stopSpawnDistance = GameManager.Instance.PlayDistance - spawnThreshold;
        // Инициализируем lastSpawnZ значением текущего пройденного расстояния,
        // чтобы объекты спавнились впереди поезда
        lastSpawnZ = boardController.TotalDistanceTraveled;
    }

    void Update()
    {
        // Если поезд приближается к точке спавна нового объекта, создаём его
        if (boardController.TotalDistanceTraveled + spawnThreshold > lastSpawnZ && lastSpawnZ < _stopSpawnDistance )
        {
            SpawnLocation();
        }

        // Оптимизация: удаляем объекты, которые находятся далеко позади поезда
        for (int i = spawnedLocations.Count - 1; i >= 0; i--)
        {
            if (spawnedLocations[i].transform.position.z < boardController.TotalDistanceTraveled - removalDistance)
            {
                Destroy(spawnedLocations[i]);
                spawnedLocations.RemoveAt(i);
            }
        }
    }

    void SpawnLocation()
    {
        Change?.Invoke();
        // Вычисляем случайное расстояние до следующего объекта и обновляем lastSpawnZ
        float distance = UnityEngine.Random.Range(minDistance, maxDistance);
        lastSpawnZ += distance;

        // Рассчитываем позицию спавна: фиксированные spawnX, spawnY, динамический Z
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, lastSpawnZ);

        // Выбираем случайный префаб из коллекции с учетом шансов
        if (locationCollection != null) //&& locationCollection.spawnItems.Count > 0)
        {
            LocationContentSpawner chosenPrefab = locationCollection.GetRandomSpawnPrefab(lastSpawnZ);
            if (chosenPrefab != null)
            {
                LocationContentSpawner spawnedObj = Instantiate(chosenPrefab, spawnPosition, Quaternion.identity);
                spawnedLocations.Add(spawnedObj);
                spawnedObj.transform.SetParent(this.gameObject.transform);
                spawnedObj.SetLevel(boardController.GetLevel());
            }
            else
            {
                Debug.LogWarning("Не удалось получить префаб из коллекции!");
            }
        }
        else
        {
            Debug.LogWarning("Location Collection не назначена или не содержит элементов!");
        }
    }
}
