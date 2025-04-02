using UnityEngine;

public class LendController : MonoBehaviour
{
    [System.Serializable]
    public class LocationGroundSegments
    {
        [Tooltip("Куски земли для этой локации")]
        public Transform[] groundSegments;

        [Tooltip("Флаг: является ли эта локация последней (после неё ничего не будет)")]
        public bool isFinalLocation;

        // Индекс для циклического перемещения сегментов именно этой локации
        [HideInInspector] public int currentSegmentIndex = 0;
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
            player = FindObjectOfType<BoardController>()?.transform;
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

        // Если локация не финальная – перемещаем сегменты циклически
        if (!activeLocation.isFinalLocation)
        {
            if (player.position.z > activeSegments[activeLocation.currentSegmentIndex].position.z + segmentLength)
            {
                // При перемещении увеличиваем позицию конца пути
                lastSegmentZ += segmentLength;
                Vector3 newPos = activeSegments[activeLocation.currentSegmentIndex].position;
                newPos.z = lastSegmentZ;
                activeSegments[activeLocation.currentSegmentIndex].position = newPos;
                activeLocation.currentSegmentIndex = (activeLocation.currentSegmentIndex + 1) % activeSegments.Length;
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
        locations[currentLocationIndex].currentSegmentIndex = 0;
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
