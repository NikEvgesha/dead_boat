using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UIElements;

public class LocationSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    public BoardController _boardController;              // Ссылка на компонент BoardController с TotalDistanceTraveled (лодка)
    public PlayerMovement _player;                             // Ссылка на Transform игрока
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
    private int generationIndex;
    private bool ownsRuntimeCollection;
    private float lastSpawnZ;                            // Z-координата последнего созданного объекта
    private List<LocationContentSpawner> spawnedLocations = new List<LocationContentSpawner>();

    public static Action Change;

    void Start()
    {
        // Если не назначена, пытаемся найти на сцене
        if (_boardController == null)
            _boardController = FindObjectOfType<BoardController>();
        if (_boardController == null)
            Debug.LogError("BoardController не найден");

        if (_player == null)
            _player = FindObjectOfType<PlayerMovement>(); ;
        if (_player == null)
            Debug.LogError("Player (игрок) не назначен и не найден по тэгу 'Player'");

        // Определяем максимальную дистанцию спавна (по лодке)
        _stopSpawnDistance = GameManager.Instance.PlayDistance - spawnThreshold;

        // Инициализируем lastSpawnZ положением игрока по Z, чтобы спавн шел от него
        lastSpawnZ = DeadBoat.Online.SharedRunContext.Active ? 0f : _player.zPositionFix;
        if (DeadBoat.Online.SharedRunContext.Active && locationCollection != null)
        {
            locationCollection = Instantiate(locationCollection);
            ownsRuntimeCollection = true;
        }
    }

    private void OnDestroy()
    {
        if (ownsRuntimeCollection && locationCollection != null) Destroy(locationCollection);
    }

    void Update()
    {
        // 1) Спавн новых локаций, когда игрок продвинулся вперед
        var shared = DeadBoat.Online.SharedRunContext.State;
        float front = DeadBoat.Online.SharedRunContext.Playing ? shared.WorldFront : _player.zPositionFix;
        float rear = DeadBoat.Online.SharedRunContext.Playing ? shared.WorldRear : _boardController.TotalDistanceTraveled - removalDistance;
        if (DeadBoat.Online.SharedRunContext.Active && !DeadBoat.Online.SharedRunContext.Playing) return;
        if (front + spawnThreshold > lastSpawnZ && lastSpawnZ < _stopSpawnDistance)
        {
            SpawnLocation();
        }

        // 2) Удаляем уже созданные локации по положению лодки
        for (int i = spawnedLocations.Count - 1; i >= 0; i--)
        {
            // Если этот объект позади лодки больше, чем removalDistance, уничтожаем
            if (spawnedLocations[i].ZPosition < rear)
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
        var random = DeadBoat.Online.SharedRunContext.Active
            ? DeadBoat.Online.SharedRunContext.Random("layout", generationIndex) : null;
        float distance = random != null ? random.Range(minDistance, maxDistance)
            : UnityEngine.Random.Range(minDistance, maxDistance);
        lastSpawnZ += distance;
        int currentIndex = generationIndex++;

        // Фиксируем позицию спавна: spawnX, spawnY и динамический Z = lastSpawnZ
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, lastSpawnZ - FixCoordinate.Instance.PlayerAddPos);

        // Берём случайный префаб из коллекции
        if (locationCollection != null)
        {
            LocationContentSpawner chosenPrefab = locationCollection.GetRandomSpawnPrefab(lastSpawnZ, random);
            if (chosenPrefab != null)
            {
                LocationContentSpawner spawnedObj = Instantiate(chosenPrefab, spawnPosition, Quaternion.identity);
                spawnedObj.ZPosition = spawnPosition.z + FixCoordinate.Instance.BoardAddPos;
                spawnedLocations.Add(spawnedObj);
                spawnedObj.transform.SetParent(this.transform);
                spawnedObj.InitializeGeneration(currentIndex);
                spawnedObj.SetLevel(DeadBoat.Online.SharedRunContext.Active
                    ? _boardController.GetLevelAtDistance(lastSpawnZ) : _boardController.GetLevel());
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
