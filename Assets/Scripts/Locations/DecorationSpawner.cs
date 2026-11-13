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

    [SerializeField, Min(0f)] private float _cityExclusionRadius;
    private CitySpawner citySpawner;
    [SerializeField, Min(0f)] private float _decorationSpacing;

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
    public float simulateTravelDistance = 600f;

    private float _stopSpawnDistance = 100000f;

    private List<GameObject> spawnedDecorations = new List<GameObject>();
    private List<float> _zPositions = new List<float>();

    void Start()
    {

        if (boardController == null)
            boardController = FindAnyObjectByType<BoardController>();
        if (boardController == null)
            Debug.LogError("boardController не найден");
        if (player == null)
            player = boardController?.transform;
        if (_cityExclusionRadius > 0f)
            citySpawner = FindAnyObjectByType<CitySpawner>();
        // Запускаем корутину для симуляции начального спавна
        StartCoroutine(SimulateInitialSpawns());
        StartCoroutine(RemovalDistanceUpdate());
    }

    IEnumerator SimulateInitialSpawns()
    {
        while (!boardController.StartSpawn)
            yield return null;

        _stopSpawnDistance = GameManager.Instance.PlayDistance - (2 * spawnAreaOffset.y + spawnAreaSize.y);
        // Определяем стартовую точку как текущую позицию минус simulateTravelDistance
        float simulatedStartDistance = boardController.SaveDistanceTraveled - simulateTravelDistance;
        // Начинаем спавнить с первого интервала после simulatedStartDistance
        float currentSimulatedDistance = simulatedStartDistance + spawnInterval;
        // Создаем объекты так, как будто мы уже проехали от simulatedStartDistance до текущей позиции
        while (currentSimulatedDistance <= boardController.SaveDistanceTraveled && currentSimulatedDistance < _stopSpawnDistance-FixCoordinate.Instance.BoardAddPos)
        {

            SpawnDecoration(currentSimulatedDistance);
            currentSimulatedDistance += spawnInterval;
            // Разбиваем создание объектов по кадрам
            yield return null;
        }
        // Устанавливаем порог следующего спавна для будущего
        nextSpawnDistance = boardController.SaveDistanceTraveled + spawnInterval + FixCoordinate.Instance.BoardAddPos;
    }

    void Update()
    {
        // Если фактическое пройденное расстояние достигло порога спавна, создаем декорацию
        if (boardController.TotalDistanceTraveled >= nextSpawnDistance && nextSpawnDistance < _stopSpawnDistance)
        {
            SpawnDecoration(nextSpawnDistance- FixCoordinate.Instance.BoardAddPos);
            nextSpawnDistance += spawnInterval;
        }

        
    }
    private IEnumerator RemovalDistanceUpdate()
    {
        while (enabled)
        {
            // Удаляем декорации, которые находятся позади игрока более чем на removalDistance
            for (int i = spawnedDecorations.Count - 1; i >= 0; i--)
            {
                if (spawnedDecorations[i] == null)
                {
                    spawnedDecorations.RemoveAt(i);
                    _zPositions.RemoveAt(i);
                    continue;
                }
                if (_zPositions[i]  < boardController.TotalDistanceTraveled - removalDistance)
                {
                    Destroy(spawnedDecorations[i]);
                    spawnedDecorations.RemoveAt(i);
                    _zPositions.RemoveAt(i);
                }
            }
            yield return new WaitForSeconds(1);
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

        float routeDistance = spawnPoint2D.y + FixCoordinate.Instance.BoardAddPos;
        if (_cityExclusionRadius > 0f &&
            (worldAvoidArea.Contains(spawnPoint2D) || !citySpawner ||
                citySpawner.IsWithinCityClearance(routeDistance, _cityExclusionRadius)))
            return;

        if (_decorationSpacing > 0f && IsTooCloseToDecoration(spawnPoint2D)) return;

        GameObject decorationPrefab = decorationCollection.GetRandomSpawnPrefab();
        if (decorationPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(spawnPoint2D.x, 0f, spawnPoint2D.y);
            if (_decorationSpacing > 0f && OverlapsOversizedDecoration(decorationPrefab, spawnPosition)) return;
            if (OverlapsVisibleDecoration(decorationPrefab, spawnPosition)) return;
            GameObject decoration = Instantiate(decorationPrefab, spawnPosition, Quaternion.identity);
            spawnedDecorations.Add(decoration);
            _zPositions.Add(routeDistance);
            decoration.transform.SetParent(this.gameObject.transform);
        }
        else
        {
            Debug.LogWarning("Не удалось выбрать префаб декорации из коллекции!");
        }
    }

    private bool OverlapsVisibleDecoration(GameObject prefab, Vector3 position)
    {
        if (!TryGetVisibleBounds(prefab, out var candidate)) return false;
        candidate.center += position - prefab.transform.position;
        foreach (var zone in FindObjectsByType<ZoneCleaner>(FindObjectsSortMode.None))
            if (zone.isActiveAndEnabled && zone.ProtectsDecoration(prefab, candidate)) return true;

        foreach (var placed in spawnedDecorations)
            if (placed && placed.activeInHierarchy && TryGetVisibleBounds(placed, out var occupied) &&
                candidate.Intersects(occupied)) return true;
        return false;
    }

    internal void ClearDecorations(ZoneCleaner zone, Bounds protectedBounds)
    {
        for (int i = spawnedDecorations.Count - 1; i >= 0; i--)
        {
            var decoration = spawnedDecorations[i];
            if (!decoration || !zone.IncludesDecoration(decoration) ||
                !TryGetVisibleBounds(decoration, out var visibleBounds) || !protectedBounds.Intersects(visibleBounds)) continue;

            Destroy(decoration);
            spawnedDecorations.RemoveAt(i);
            _zPositions.RemoveAt(i);
        }
    }

    private static bool TryGetVisibleBounds(GameObject decoration, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var renderer in decoration.GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.enabled) continue;
            if (found) bounds.Encapsulate(renderer.bounds);
            else bounds = renderer.bounds;
            found = true;
        }
        return found;
    }

    private bool OverlapsOversizedDecoration(GameObject prefab, Vector3 position)
    {
        if (!prefab.TryGetComponent<BoxCollider>(out var box)) return false;
        var candidate = GetFootprint(box, position);
        bool oversized = candidate.width > _decorationSpacing || candidate.height > _decorationSpacing;

        foreach (var decoration in spawnedDecorations)
        {
            if (!decoration || !decoration.TryGetComponent<BoxCollider>(out var otherBox)) continue;

            var occupied = GetFootprint(otherBox, decoration.transform.position);
            if ((oversized || occupied.width > _decorationSpacing || occupied.height > _decorationSpacing) &&
                candidate.Overlaps(occupied))
                return true;
        }
        return false;
    }

    // Decorations are spawned with identity rotation. Their root box contains the rotated visuals.
    private static Rect GetFootprint(BoxCollider box, Vector3 position)
    {
        var scale = box.transform.lossyScale;
        var size = Vector3.Scale(box.size, scale);
        var min = position + Vector3.Scale(box.center, scale) - size * .5f;
        return new Rect(min.x, min.z, size.x, size.z);
    }


    private bool IsTooCloseToDecoration(Vector2 position)
    {
        float minSquaredDistance = _decorationSpacing * _decorationSpacing;
        foreach (var decoration in spawnedDecorations)
        {
            if (!decoration) continue;
            var placed = decoration.transform.position;
            if ((new Vector2(placed.x, placed.z) - position).sqrMagnitude < minSquaredDistance)
                return true;
        }
        return false;
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
        float boardAddPos = FixCoordinate.Instance != null ? FixCoordinate.Instance.BoardAddPos : 0f;
        float boardZ = (boardController != null) ? boardController.TotalDistanceTraveled - boardAddPos : 0f;

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
