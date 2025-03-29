using UnityEngine;
using System.Collections.Generic;

public class WorldManager : MonoBehaviour
{
    [Header("Ссылки на объекты")]
    [SerializeField] private TrainController _trainController;
    [SerializeField] private GameObject _stationPrefab;
    [SerializeField] private GameObject[] _islandPrefabs;
    [SerializeField] private GameObject _meteorPrefab;

    [Header("Настройки спавна (ScriptableObject)")]
    [SerializeField] private StationSpawnSettings _stationSpawnSettings;
    [SerializeField] private IslandsSpawnSettings _islandsSpawnSettings;
    [SerializeField] private MeteorsSpawnSettings _meteorsSpawnSettings;

    // Список для отслеживания созданных объектов мира
    private List<GameObject> _spawnedWorldObjects = new List<GameObject>();

    // Дистанция, до которой уже сгенерирован мир
    private float _nextSpawnDistance = 0f;

    void Start()
    {
        // Если TrainController не задан через инспектор, ищем его
        if (_trainController == null)
        {
            _trainController = FindObjectOfType<TrainController>();
        }
        if (_trainController == null)
        {
            Debug.LogError("TrainController не найден на сцене!");
            return;
        }

        // Генерируем первую станцию (например, для обучения)
        SpawnStation(_nextSpawnDistance);
        _nextSpawnDistance += _stationSpawnSettings.StationLength;

        // Предзагружаем сегменты вперед от поезда согласно настроенным параметрам спавна
        while (_nextSpawnDistance < _trainController.TotalDistanceTraveled + _stationSpawnSettings.SpawnDistanceAhead)
        {
            SpawnNextSegment();
        }
    }

    void Update()
    {
        // Генерируем новые сегменты, если поезд продвигается
        while (_nextSpawnDistance < _trainController.TotalDistanceTraveled + _stationSpawnSettings.SpawnDistanceAhead)
        {
            SpawnNextSegment();
        }

        // Удаляем объекты, оказавшиеся далеко позади поезда
        for (int i = _spawnedWorldObjects.Count - 1; i >= 0; i--)
        {
            if (_spawnedWorldObjects[i].transform.position.z < _trainController.TotalDistanceTraveled - _stationSpawnSettings.DespawnDistanceBehind)
            {
                Destroy(_spawnedWorldObjects[i]);
                _spawnedWorldObjects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Генерирует следующий сегмент мира: станция, острова и метеоры.
    /// Используются шансы спавна из настроек.
    /// </summary>
    private void SpawnNextSegment()
    {
        // Спавним станцию, если шанс позволяет
        if (Random.value <= _stationSpawnSettings.SpawnChance)
        {
            SpawnStation(_nextSpawnDistance);
        }

        // Спавним острова, если шанс позволяет
        if (Random.value <= _islandsSpawnSettings.SpawnChance)
        {
            SpawnIslandsBetweenStations(_nextSpawnDistance - _stationSpawnSettings.StationLength, _nextSpawnDistance);
        }

        // Спавним метеоры, если шанс позволяет
        if (Random.value <= _meteorsSpawnSettings.SpawnChance)
        {
            SpawnMeteors(_nextSpawnDistance - _stationSpawnSettings.StationLength, _nextSpawnDistance);
        }

        // Обновляем следующую дистанцию
        _nextSpawnDistance += _stationSpawnSettings.StationLength;
    }

    /// <summary>
    /// Создает станцию на заданной дистанции (по оси Z)
    /// </summary>
    /// <param name="distance">Дистанция для спавна станции</param>
    private void SpawnStation(float distance)
    {
        Vector3 spawnPos = new Vector3(0f, 0f, distance);
        GameObject station = Instantiate(_stationPrefab, spawnPos, Quaternion.identity);
        _spawnedWorldObjects.Add(station);
    }

    /// <summary>
    /// Создает острова в заданном сегменте между станциями
    /// </summary>
    /// <param name="startDistance">Начало сегмента</param>
    /// <param name="endDistance">Конец сегмента</param>
    private void SpawnIslandsBetweenStations(float startDistance, float endDistance)
    {
        // Определяем количество островов в сегменте
        int islandCount = Random.Range(_islandsSpawnSettings.MinIslandCount, _islandsSpawnSettings.MaxIslandCount + 1);

        for (int i = 0; i < islandCount; i++)
        {
            float z = Random.Range(startDistance, endDistance);
            float x = Random.Range(_islandsSpawnSettings.XRange.x, _islandsSpawnSettings.XRange.y);
            Vector3 islandPos = new Vector3(x, 0f, z);

            int islandIndex = Random.Range(0, _islandPrefabs.Length);
            GameObject island = Instantiate(_islandPrefabs[islandIndex], islandPos, Quaternion.identity);
            _spawnedWorldObjects.Add(island);
        }
    }

    /// <summary>
    /// Создает метеоры в заданном сегменте
    /// </summary>
    /// <param name="startDistance">Начало сегмента</param>
    /// <param name="endDistance">Конец сегмента</param>
    private void SpawnMeteors(float startDistance, float endDistance)
    {
        int meteorCount = Random.Range(_meteorsSpawnSettings.MinMeteorCount, _meteorsSpawnSettings.MaxMeteorCount + 1);

        for (int i = 0; i < meteorCount; i++)
        {
            float z = Random.Range(startDistance, endDistance);
            float x = Random.Range(_meteorsSpawnSettings.XRange.x, _meteorsSpawnSettings.XRange.y);
            float y = Random.Range(_meteorsSpawnSettings.YRange.x, _meteorsSpawnSettings.YRange.y);
            Vector3 meteorPos = new Vector3(x, y, z);

            GameObject meteor = Instantiate(_meteorPrefab, meteorPos, Quaternion.identity);
            _spawnedWorldObjects.Add(meteor);
        }
    }
}
