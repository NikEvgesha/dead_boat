using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DecorationSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на компонент, отслеживающий общее пройденное расстояние (например, BoardController)")]
    public BoardController boardController;

    [Tooltip("Трансформ игрока (или поезда) – используется как опорная точка")]
    public Transform player;

    [Tooltip("Коллекция декораций для спавна")]
    public DecorationSpawnCollection decorationCollection;

    [Header("Spawn Timing")]
    [Tooltip("Интервал спавна декораций (по пройденному расстоянию)")]
    public float spawnInterval = 100f;
    private float nextSpawnDistance;

    [Header("Spawn Area (относительно игрока)")]
    [Tooltip("Смещение (x, z) от позиции игрока для области спавна")]
    public Vector2 spawnAreaOffset = new Vector2(-50f, -50f);
    [Tooltip("Размер области спавна (ширина по x и высота по z)")]
    public Vector2 spawnAreaSize = new Vector2(100f, 100f);

    [Header("Avoid Area (относительно игрока)")]
    [Tooltip("Смещение (x, z) от позиции игрока для зоны, где спавн запрещён")]
    public Vector2 avoidAreaOffset = new Vector2(-5f, -50f);
    [Tooltip("Размер avoid-области (ширина по x и высота по z)")]
    public Vector2 avoidAreaSize = new Vector2(10f, 100f);

    [Header("Removal")]
    [Tooltip("Расстояние позади игрока, после которого декорация удаляется")]
    public float removalDistance = 500f;

    [Header("Simulation")]
    [Tooltip("Расстояние, которое симулируется как уже проеханное (назад от текущей позиции), для начального спавна")]
    public float simulateTravelDistance = 500f;

    private float _stopSpawnDistance = 100000f;

    private List<GameObject> spawnedDecorations = new List<GameObject>();

    void Start()
    {
        _stopSpawnDistance = GameManager.Instance.PlayDistance - (2*spawnAreaOffset.y + spawnAreaSize.y);
        // Запускаем корутину для симуляции начального спавна
        StartCoroutine(SimulateInitialSpawns());
    }

    IEnumerator SimulateInitialSpawns()
    {
        // Определяем стартовую точку как текущую позицию минус simulateTravelDistance
        float simulatedStartDistance = boardController.TotalDistanceTraveled - simulateTravelDistance;
        // Начинаем спавнить с первого интервала после simulatedStartDistance
        float currentSimulatedDistance = simulatedStartDistance + spawnInterval;

        // Создаем объекты так, как будто мы уже проехали от simulatedStartDistance до текущей позиции
        while (currentSimulatedDistance <= boardController.TotalDistanceTraveled)
        {
            SpawnDecoration(currentSimulatedDistance);
            currentSimulatedDistance += spawnInterval;
            // Разбиваем создание объектов по кадрам
            yield return null;
        }
        // Устанавливаем порог следующего спавна для будущего
        nextSpawnDistance = boardController.TotalDistanceTraveled + spawnInterval;
    }

    void Update()
    {
        // Если фактическое пройденное расстояние достигло порога спавна, создаем декорацию
        if (boardController.TotalDistanceTraveled >= nextSpawnDistance && nextSpawnDistance < _stopSpawnDistance)
        {
            SpawnDecoration(nextSpawnDistance);
            nextSpawnDistance += spawnInterval;
        }

        // Удаляем декорации, которые находятся позади игрока более чем на removalDistance
        for (int i = spawnedDecorations.Count - 1; i >= 0; i--)
        {
            if (spawnedDecorations[i] == null)
            {
                spawnedDecorations.RemoveAt(i);
                continue;
            }
            if (spawnedDecorations[i].transform.position.z < boardController.TotalDistanceTraveled - removalDistance)
            {
                Destroy(spawnedDecorations[i]);
                spawnedDecorations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Создает декорацию, рассчитывая зону спавна относительно переданной базовой координаты (baseZ).
    /// </summary>
    /// <param name="baseZ">Базовая координата Z для расчета зоны спавна.</param>
    void SpawnDecoration(float baseZ)
    {
        float playerX = player.position.x;
        // Рассчитываем мировую область спавна с использованием baseZ вместо текущего boardController.TotalDistanceTraveled.
        Rect worldSpawnArea = new Rect(
            playerX + spawnAreaOffset.x,
            baseZ + spawnAreaOffset.y,
            spawnAreaSize.x,
            spawnAreaSize.y
        );
        // Рассчитываем мировую область, где спавн запрещён.
        Rect worldAvoidArea = new Rect(
            playerX + avoidAreaOffset.x,
            baseZ + avoidAreaOffset.y,
            avoidAreaSize.x,
            avoidAreaSize.y
        );

        // Выбираем случайную точку в области спавна, исключая область avoid.
        Vector2 spawnPoint2D = GetRandomPointInAreaExcluding(worldSpawnArea, worldAvoidArea);

        GameObject decorationPrefab = decorationCollection.GetRandomSpawnPrefab();
        if (decorationPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(spawnPoint2D.x, 0f, spawnPoint2D.y);
            GameObject decoration = Instantiate(decorationPrefab, spawnPosition, Quaternion.identity);
            spawnedDecorations.Add(decoration);
            decoration.transform.SetParent(this.gameObject.transform);
        }
        else
        {
            Debug.LogWarning("Не удалось выбрать префаб декорации из коллекции!");
        }
    }

    /// <summary>
    /// Возвращает случайную точку внутри заданной области, исключая точки из excludeArea.
    /// </summary>
    Vector2 GetRandomPointInAreaExcluding(Rect area, Rect excludeArea)
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(area.xMin, area.xMax);
            float randomZ = Random.Range(area.yMin, area.yMax); // здесь y Rect используется как z
            Vector2 point = new Vector2(randomX, randomZ);
            if (!excludeArea.Contains(point))
            {
                return point;
            }
        }
        return new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
    }

    // Метод для визуализации областей в режиме редактора
    private void OnDrawGizmos()
    {
        Vector3 playerPos = (player != null) ? player.position : Vector3.zero;
        float boardZ = (boardController != null) ? boardController.TotalDistanceTraveled : 0f;

        // Отрисовка области спавна, исходя из текущего boardZ
        Vector2 spawnOrigin = new Vector2(playerPos.x + spawnAreaOffset.x, boardZ + spawnAreaOffset.y);
        Vector2 spawnSize = spawnAreaSize;
        Vector3 spawnBL = new Vector3(spawnOrigin.x, 0f, spawnOrigin.y);
        Vector3 spawnBR = new Vector3(spawnOrigin.x + spawnSize.x, 0f, spawnOrigin.y);
        Vector3 spawnTR = new Vector3(spawnOrigin.x + spawnSize.x, 0f, spawnOrigin.y + spawnSize.y);
        Vector3 spawnTL = new Vector3(spawnOrigin.x, 0f, spawnOrigin.y + spawnSize.y);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(spawnBL, spawnBR);
        Gizmos.DrawLine(spawnBR, spawnTR);
        Gizmos.DrawLine(spawnTR, spawnTL);
        Gizmos.DrawLine(spawnTL, spawnBL);

        // Отрисовка зоны avoid
        Vector2 avoidOrigin = new Vector2(playerPos.x + avoidAreaOffset.x, boardZ + avoidAreaOffset.y);
        Vector2 avoidSize = avoidAreaSize;
        Vector3 avoidBL = new Vector3(avoidOrigin.x, 0f, avoidOrigin.y);
        Vector3 avoidBR = new Vector3(avoidOrigin.x + avoidSize.x, 0f, avoidOrigin.y);
        Vector3 avoidTR = new Vector3(avoidOrigin.x + avoidSize.x, 0f, avoidOrigin.y + avoidSize.y);
        Vector3 avoidTL = new Vector3(avoidOrigin.x, 0f, avoidOrigin.y + avoidSize.y);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(avoidBL, avoidBR);
        Gizmos.DrawLine(avoidBR, avoidTR);
        Gizmos.DrawLine(avoidTR, avoidTL);
        Gizmos.DrawLine(avoidTL, avoidBL);
    }
}
