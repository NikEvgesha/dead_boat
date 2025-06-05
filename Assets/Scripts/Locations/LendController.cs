using UnityEngine;
using System.Linq;

public class LendController : MonoBehaviour
{
    [System.Serializable]
    public class LocationGroundSegments
    {
        [Tooltip("Куски земли для этой локации")]
        public Transform[] groundSegments;

        [Tooltip("Флаг: является ли эта локация последней (после неё ничего не будет)")]
        public bool isFinalLocation;

        // Пока не нужен: индексы для циклического передвижения
        // [HideInInspector] public int currentSegmentIndex = 0;
    }

    [Header("Настройки локаций")]
    [Tooltip("Список групп сегментов для разных локаций")]
    public LocationGroundSegments[] locations;

    [Tooltip("Длина каждого куска земли (по оси Z)")]
    public float segmentLength = 100f;

    [Tooltip("Ссылка на трансформ игрока или лодки")]
    public Transform player;

    [Header("Настройки перехода")]
    [Tooltip("Расстояние, после которого происходит смена локации")]
    public float transitionDistance = 300f;

    private float _stopSpawnDistance = 100000f;

    // Текущий индекс локации
    private int currentLocationIndex = 0;

    // Порог для следующего перехода
    private float nextTransitionDistance;

    // Переменная для хранения последней позиции по оси Z уже выстроенного пути
    private float lastSegmentZ;

    private void Awake()
    {
        InitBoardController();
    }

    private void Start()
    {
        _stopSpawnDistance = GameManager.Instance.PlayDistance;
        transitionDistance = _stopSpawnDistance - segmentLength;
        nextTransitionDistance = transitionDistance; // первый порог перехода
        InitializeLocations();
    }

    // Инициализация локаций: включаем только первую, остальные отключаются
    private void InitializeLocations()
    {
        for (int i = 0; i < locations.Length; i++)
        {
            bool active = (i == currentLocationIndex);
            foreach (Transform segment in locations[i].groundSegments)
            {
                segment.gameObject.SetActive(active);
            }
        }
        // Вычисляем начальное значение lastSegmentZ из активных сегментов первой локации
        lastSegmentZ = CalculateLastSegmentZ();
    }

    // Находит максимальную позицию Z среди активных сегментов
    private float CalculateLastSegmentZ()
    {
        float maxZ = float.MinValue;
        foreach (var location in locations)
        {
            foreach (var segment in location.groundSegments)
            {
                if (segment.gameObject.activeSelf && segment.position.z > maxZ)
                    maxZ = segment.position.z;
            }
        }
        return maxZ;
    }

    // Если player не задан, ищем компонент BoardController
    private void InitBoardController()
    {
        if (player == null)
            player = FindObjectOfType<PlayerStatsManager>()?.transform;
        if (player == null)
            Debug.LogError("BoardController не найден");
    }

    void Update()
    {
        // Переход на новую локацию, если игрок прошёл порог и следующая локация существует
        if (player.position.z > nextTransitionDistance && currentLocationIndex < locations.Length - 1)
        {
            currentLocationIndex++;
            // Включаем сегменты новой локации
            foreach (Transform segment in locations[currentLocationIndex].groundSegments)
            {
                segment.gameObject.SetActive(true);
            }
            nextTransitionDistance += transitionDistance;
            // Располагаем сегменты новой локации так, чтобы они продолжали путь предыдущих
            PositionNewLocationSegments();
        }

        var activeLocation = locations[currentLocationIndex];
        Transform[] activeSegments = activeLocation.groundSegments;

        // Если локация не финальная – перемещаем сегменты циклически (вперед и назад)
        if (!activeLocation.isFinalLocation)
        {
            // Ищем самый «низкий» (минимальный Z) и самый «верхний» (максимальный Z) сегмент
            Transform lowestSeg = activeSegments[0];
            Transform highestSeg = activeSegments[0];
            foreach (Transform seg in activeSegments)
            {
                if (seg.position.z < lowestSeg.position.z)
                    lowestSeg = seg;
                if (seg.position.z > highestSeg.position.z)
                    highestSeg = seg;
            }

            // Движемся вперёд: если игрок прошёл нижний сегмент дальше, чем на length
            if (player.position.z > lowestSeg.position.z + segmentLength)
            {
                float newZ = highestSeg.position.z + segmentLength;
                Vector3 newPos = lowestSeg.position;
                newPos.z = newZ;
                lowestSeg.position = newPos;

                // Обновляем lastSegmentZ (максимальное Z) при необходимости
                if (newZ > lastSegmentZ)
                    lastSegmentZ = newZ;
            }
            // Движемся назад: если игрок ушёл ниже нижнего сегмента более, чем на length
            else if (player.position.z < lowestSeg.position.z - 10)
            {
                float newZ = lowestSeg.position.z - segmentLength;
                Vector3 newPos = highestSeg.position;
                newPos.z = newZ;
                highestSeg.position = newPos;

                // При движении назад логика lastSegmentZ не так критична,
                // но, если нужно, можно вычислять минимальный Z аналогично lastSegmentZ
            }
        }
        else // Финальная локация – не перемещаем сегменты циклически
        {
            float finalEndZ = GetFinalEndPosition(activeSegments);
            if (player.position.z > finalEndZ)
            {
                // Можно вызвать событие завершения уровня, если требуется
                // OnLevelComplete();
            }
        }
    }

    // Позиционирует сегменты новой локации так, чтобы они шли после уже выстроенного пути
    private void PositionNewLocationSegments()
    {
        float startZ = lastSegmentZ + segmentLength;
        Transform[] newSegments = locations[currentLocationIndex].groundSegments;
        for (int i = 0; i < newSegments.Length; i++)
        {
            Vector3 pos = newSegments[i].position;
            pos.z = startZ + i * segmentLength;
            newSegments[i].position = pos;
        }
        // Обновляем значение конца пути до конца новой локации
        lastSegmentZ = startZ + (newSegments.Length - 1) * segmentLength;
        //currentSegmentIndex = 0; // больше не нужен
    }

    // Определяет конечную позицию финальной локации (самый дальний сегмент + segmentLength)
    private float GetFinalEndPosition(Transform[] segments)
    {
        float maxZ = float.MinValue;
        foreach (Transform segment in segments)
        {
            if (segment.position.z > maxZ)
                maxZ = segment.position.z;
        }
        return maxZ + segmentLength;
    }
}
