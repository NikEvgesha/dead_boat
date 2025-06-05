using UnityEngine;
using System.Collections.Generic;
using System;

public class LocationSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    public BoardController boardController;              // Ссылка на компонент BoardController с TotalDistanceTraveled (лодка)
    public Transform player;                             // Ссылка на Transform игрока
    public LocationSpawnCollection locationCollection;   // ScriptableObject с вариантами объектов для спавна
    public float minDistance = 100f;                     // Минимальное расстояние до следующего объекта
    public float maxDistance = 200f;                     // Максимальное расстояние до следующего объекта
    public float spawnThreshold = 20f;                   // Насколько впереди игрок должен быть для спавна

    [Header("Настройки позиции")]
    public float spawnX = 10f;                           // Фиксированная позиция по X (например, берег)
    public float spawnY = 0f;                            // Фиксированная позиция по Y (уровень земли)
    // Z рассчитывается динамически

    [Header("Оптимизация")]
    public float removalDistance = 200f;                 // Удалять, если позади лодки больше этого

    private float _stopSpawnDistance = 100000f;          // Границы, пока лодка не доедет до конца уровня
    private float lastSpawnZ;                            // Z-координата последнего созданного объекта
    private List<LocationContentSpawner> spawnedLocations = new List<LocationContentSpawner>();

    public static Action Change;

    void Start()
    {
        // Если не назначена, пытаемся найти на сцене
        if (boardController == null)
            boardController = FindObjectOfType<BoardController>();
        if (boardController == null)
            Debug.LogError("BoardController не найден");

        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError("Player (игрок) не назначен и не найден по тэгу 'Player'");

        // Определяем максимальную дистанцию спавна (по лодке)
        _stopSpawnDistance = GameManager.Instance.PlayDistance - spawnThreshold;

        // Инициализируем lastSpawnZ положением игрока по Z, чтобы спавн шел от него
        lastSpawnZ = player.position.z;
    }

    void Update()
    {
        // 1) Спавн новых локаций, когда игрок продвинулся вперед
        if (player.position.z + spawnThreshold > lastSpawnZ && lastSpawnZ < _stopSpawnDistance)
        {
            SpawnLocation();
        }

        // 2) Удаляем уже созданные локации по положению лодки
        for (int i = spawnedLocations.Count - 1; i >= 0; i--)
        {
            // Если этот объект позади лодки больше, чем removalDistance, уничтожаем
            if (spawnedLocations[i].transform.position.z < boardController.TotalDistanceTraveled - removalDistance)
            {
                Destroy(spawnedLocations[i].gameObject);
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

        // Фиксируем позицию спавна: spawnX, spawnY и динамический Z = lastSpawnZ
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, lastSpawnZ);

        // Берём случайный префаб из коллекции
        if (locationCollection != null)
        {
            LocationContentSpawner chosenPrefab = locationCollection.GetRandomSpawnPrefab(lastSpawnZ);
            if (chosenPrefab != null)
            {
                LocationContentSpawner spawnedObj = Instantiate(chosenPrefab, spawnPosition, Quaternion.identity);
                spawnedLocations.Add(spawnedObj);
                spawnedObj.transform.SetParent(this.transform);
                spawnedObj.SetLevel(boardController.GetLevel());
            }
            else
            {
                Debug.LogWarning("Не удалось получить префаб из коллекции!");
            }
        }
        else
        {
            Debug.LogWarning("Location Collection не назначена или пуста!");
        }
    }
}
