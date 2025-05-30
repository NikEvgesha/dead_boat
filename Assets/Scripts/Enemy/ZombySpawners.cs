using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Трансформ игрока, вокруг которого спавнить мобов")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private BoardController _boardController;
    [Header("Spawn Radius")]
    [Tooltip("Минимальное расстояние от игрока")]
    [SerializeField] private float minSpawnDistance = 5f;
    [Tooltip("Максимальное расстояние от игрока")]
    [SerializeField] private float maxSpawnDistance = 20f;

    [Header("Enemies")]
    [Tooltip("Типы зомби")]
    [SerializeField] private List<ZombieController> enemyTypes = new List<ZombieController>();
    [Tooltip("Сколько зомби спавнить за одну итерацию")]
    [SerializeField] private int spawnCount = 3;
    [Tooltip("Пауза между итерациями спавна (сек)")]
    [SerializeField] private float cooldown = 10f;

    private int nextEnemyType = 0;
    private bool isNight = false;
    private bool corutineStart = false;

    private void Start()
    {
        if (!playerTransform)
            playerTransform = PlayerStatsManager.Instance.transform;

        if (!_boardController)
            _boardController = FindAnyObjectByType<BoardController>();

        if (!corutineStart)
        {
            corutineStart = true;
            DayTime.instanse.DayNightCycle += OnDayNightCycle;
            StartCoroutine(SpawnRoutine());
        }
        
    }
    private void OnEnable()
    {
        if (DayTime.instanse)
        {
            corutineStart = true;
            DayTime.instanse.DayNightCycle += OnDayNightCycle;
            StartCoroutine(SpawnRoutine());
        }
    }

    private void OnDisable()
    {

        corutineStart = false;
        StopAllCoroutines();
        DayTime.instanse.DayNightCycle -= OnDayNightCycle;
    }

    private void OnDayNightCycle()
    {
        isNight = DayTime.instanse.IsNight();
        if (isNight)
        {
            // переключаем тип зомби каждый раз, когда наступает ночь
            nextEnemyType = (nextEnemyType + 1) % enemyTypes.Count;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (gameObject.activeSelf)
        {
            if (isNight)
                SpawnEnemies();

            yield return new WaitForSeconds(cooldown);
        }
    }

    private void SpawnEnemies()
    {
        if (playerTransform == null || enemyTypes.Count == 0)
            return;
        if (_boardController.TotalDistanceTraveled >= GameManager.Instance.PlayDistance - 500)
            return;
        
        var prefab = enemyTypes[nextEnemyType];
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomPositionInAnnulus();
            ZombieController zomby = Instantiate(prefab, spawnPos, prefab.transform.rotation, transform);
            zomby.InitializeLevel(_boardController.GetLevel());
        }
    }

    private Vector3 GetRandomPositionInAnnulus()
    {
        // случайный угол
        float angle = Random.Range(0f, Mathf.PI * 2f);
        // случайная дистанция между min и max
        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
        // смещение по XZ плоскости
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
        return playerTransform.position + offset;
    }

    // Рисуем в редакторе две окружности вокруг игрока
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // минимальная дистанция — жёлтая
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);

        // максимальная — красная
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);
    }
}
