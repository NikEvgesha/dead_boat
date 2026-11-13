using UnityEngine;
using System.Collections.Generic;

public class CitySpawner : MonoBehaviour
{
    [Header("Ќастройки спавна городов")]
    [Tooltip("—сылка на компонент BoardController, отслеживающий пройденное рассто€ние")]
    public BoardController boardController;

    [Tooltip("ѕрефаб города дл€ спавна")]
    public GameObject cityPrefab;

    [Tooltip("Ѕазовое рассто€ние между спавнами городов (в метрах)")]
    public float baseSpawnDistance = 3333.3333f;

    [Tooltip("ѕри каждом спавне рассто€ние до следующего города увеличиваетс€ на это значение (в метрах)")]
    public float additionalDistanceIncrement = 33.333332f;

    [Header("Ѕуфер спавна и удалени€")]
    [Tooltip("√ород будет заспавнен, когда поезд приблизитс€ к точке спавна на это рассто€ние (в метрах)")]
    public float spawnBuffer = 500f;

    [Tooltip("√ород будет удалЄн, если окажетс€ позади поезда на это рассто€ние (в метрах)")]
    public float removalDistance = 500f;

    [Tooltip(" оличество городов")]
    [SerializeField]
    private int _citys = 9;

    private float nextSpawnDistance;
    private int spawnCount = 0;
    private List<GameObject> spawnedCities = new List<GameObject>();

    public bool streamCities = true;
    public DeadBoat.Online.WorldCityStreaming Streaming { get; private set; }
    private void LateUpdate() => Streaming?.Tick(spawnedCities, streamCities);
    public int Cities => _citys;

    public bool IsWithinCityClearance(float routeDistance, float clearance)
    {
        if (Mathf.Abs(routeDistance) <= clearance) return true;

        // Decorations can be generated ahead of the city instances.
        float cityDistance = baseSpawnDistance;
        for (int index = 1; index < _citys && cityDistance <= routeDistance + clearance; index++)
        {
            if (Mathf.Abs(routeDistance - cityDistance) <= clearance) return true;
            cityDistance += CitySpacing(index + 1);
        }
        return false;
    }

    private float CitySpacing(int count) => baseSpawnDistance + count * additionalDistanceIncrement;

    private void Awake()
    {
        InitBoardController();

        float playDistance = GameManager.Instance.PlayDistance;
        _citys = 1;
        while (playDistance > baseSpawnDistance)
        {
            playDistance = playDistance - baseSpawnDistance + (additionalDistanceIncrement * _citys);
            _citys++;
        }
        // —павним стартовый город сразу в (0, 0, 0)
        //SpawnCityAt(new Vector3(0f, 0f, 0f));
        spawnCount = 1;  // стартовый город учтен

        // ”станавливаем порог спавна дл€ следующего города
        nextSpawnDistance = baseSpawnDistance;
    }

    private void InitBoardController()
    {
        if (boardController == null)
            boardController = FindAnyObjectByType<BoardController>();
        if (boardController == null)
            Debug.LogError("boardController не найден");
    }

/*    void Start()
    {
        float playDistance = GameManager.Instance.PlayDistance;
        _citys = 0;
        while (playDistance> baseSpawnDistance)
        {
            playDistance = playDistance - baseSpawnDistance + (additionalDistanceIncrement * _citys);
            _citys++;
        }
        // —павним стартовый город сразу в (0, 0, 0)
        //SpawnCityAt(new Vector3(0f, 0f, 0f));
        spawnCount = 1;  // стартовый город учтен

        // ”станавливаем порог спавна дл€ следующего города
        nextSpawnDistance = baseSpawnDistance;
    }*/

    void Update()
    {
        if (DeadBoat.Online.SharedRunContext.Active && !DeadBoat.Online.SharedRunContext.Playing) return;
        float front = DeadBoat.Online.SharedRunContext.Playing ? DeadBoat.Online.SharedRunContext.State.WorldFront : boardController.TotalDistanceTraveled;
        float rear = DeadBoat.Online.SharedRunContext.Playing ? DeadBoat.Online.SharedRunContext.State.WorldRear : boardController.TotalDistanceTraveled - removalDistance;
        // —павним новый город, если:
        // 1. ќбщее число городов меньше _citys (включа€ стартовый)
        // 2. ѕоезд приблизилс€ к точке спавна на spawnBuffer метров
        if (spawnCount < _citys && front + spawnBuffer >= nextSpawnDistance)
        {
            SpawnCity();
            spawnCount++;
            // –ассто€ние до следующего спавна увеличиваетс€: базовый интервал + дополнительное увеличение
            nextSpawnDistance += CitySpacing(spawnCount);
        }

        // ”дал€ем города, которые наход€тс€ позади поезда более, чем на removalDistance
        for (int i = spawnedCities.Count - 1; i >= 0; i--)
        {
            if (spawnedCities[i] == null)
            {
                spawnedCities.RemoveAt(i);
                continue;
            }
            if (spawnedCities[i].transform.position.z + FixCoordinate.Instance.BoardAddPos < rear)
            {
                Destroy(spawnedCities[i]);
                spawnedCities.RemoveAt(i);
            }
        }
    }

    void SpawnCity()
    {
        // √ород по€вл€етс€ вдоль оси Z с фиксированными координатами X и Y.
        Vector3 spawnPosition = new Vector3(0f, 0f, nextSpawnDistance - FixCoordinate.Instance.BoardAddPos);
        SpawnCityAt(spawnPosition);
    }

    void SpawnCityAt(Vector3 spawnPosition)
    {
        GameObject city = Instantiate(cityPrefab, spawnPosition, Quaternion.identity);
        spawnedCities.Add(city);
        city.transform.SetParent(transform);
        if (DeadBoat.Online.SharedRunContext.Playing)
        {
            Streaming ??= new DeadBoat.Online.WorldCityStreaming(this);
            city.AddComponent<DeadBoat.Online.WorldCityRegion>().Initialize(this, cityPrefab, nextSpawnDistance);
            return;
        }
        city.AddComponent<DeadBoat.Online.WorldGenerationAnchor>().Key = "city:" + Mathf.RoundToInt(nextSpawnDistance);
        int index = 0;
        foreach (var content in city.GetComponentsInChildren<LocationContentSpawner>(true))
            content.InitializeStableGeneration("city:" + Mathf.RoundToInt(nextSpawnDistance) + ":" + index++);
    }
}
