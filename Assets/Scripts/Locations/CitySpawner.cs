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
    public float baseSpawnDistance = 10000f;

    [Tooltip("ѕри каждом спавне рассто€ние до следующего города увеличиваетс€ на это значение (в метрах)")]
    public float additionalDistanceIncrement = 100f;

    [Header("Ѕуфер спавна и удалени€")]
    [Tooltip("√ород будет заспавнен, когда поезд приблизитс€ к точке спавна на это рассто€ние (в метрах)")]
    public float spawnBuffer = 500f;

    [Tooltip("√ород будет удалЄн, если окажетс€ позади поезда на это рассто€ние (в метрах)")]
    public float removalDistance = 500f;

    private float nextSpawnDistance;
    private int spawnCount = 0;
    private List<GameObject> spawnedCities = new List<GameObject>();

    void Start()
    {
        // ѕервый город спавнитс€ при достижении базового рассто€ни€
        nextSpawnDistance = baseSpawnDistance;
    }

    void Update()
    {
        // ≈сли поезд приблизилс€ к точке спавна на spawnBuffer метров, создаЄм город
        if (boardController.TotalDistanceTraveled + spawnBuffer >= nextSpawnDistance)
        {
            SpawnCity();
            spawnCount++;
            // –ассто€ние до следующего спавна увеличиваетс€: базовый интервал + дополнительное увеличение
            nextSpawnDistance += baseSpawnDistance + spawnCount * additionalDistanceIncrement;
        }

        // ”дал€ем города, которые наход€тс€ позади поезда более, чем на removalDistance
        for (int i = spawnedCities.Count - 1; i >= 0; i--)
        {
            if (spawnedCities[i].transform.position.z < boardController.TotalDistanceTraveled - removalDistance)
            {
                Destroy(spawnedCities[i]);
                spawnedCities.RemoveAt(i);
            }
        }
    }

    void SpawnCity()
    {
        // ¬ данном примере город по€вл€етс€ вдоль оси Z,
        // а координаты X и Y зафиксированы (их можно настроить по требовани€м игры)
        Vector3 spawnPosition = new Vector3(0f, 0f, nextSpawnDistance);
        GameObject city = Instantiate(cityPrefab, spawnPosition, Quaternion.identity);
        spawnedCities.Add(city);
    }
}
