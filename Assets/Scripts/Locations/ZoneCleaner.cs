using UnityEngine;

public class ZoneCleaner : MonoBehaviour
{
    [Tooltip("Половинные размеры области проверки (half extents). Например, если зона 500x500, halfExtents = (250, 1, 250)")]
    public Vector3 halfExtents = new Vector3(250f, 1f, 250f);

    [Tooltip("Маска слоя, включающая объекты с тегами Decoration и Location")]
    public LayerMask checkLayer;

    [Tooltip("Список запрещённых тегов, которые необходимо удалить")]
    public string[] forbiddenTags = { "Decoration", "Location" };

    [SerializeField]
    private int _startClearTime = 1; // время между проверками при старте

    [SerializeField]
    private int _checkCount = 5;     // сколько раз выполнить очистку

    [SerializeField]
    private Transform _centerPoint;  // если задан, используется как центр зоны очистки

    [SerializeField]
    private Transform _StartPoint;   // если заданы, используется перегруженная версия CleanZone (на основе 2-х точек)

    [SerializeField]
    private Transform _EndPoint;

    private bool _usePoint = false;
    private bool _useCenterPoint = false;
    private int _currentCheckCount;
    private float _ClearTime;

    private void Start()
    {
        _ClearTime = _startClearTime;
        _useCenterPoint = _centerPoint != null;
        _usePoint = (_StartPoint != null && _EndPoint != null);

        if (_usePoint)
        {
            // Если заданы две точки, вызываем перегруженную версию CleanZone
            CleanZone(_StartPoint.position, _EndPoint.position);
        }
        else
        {
            // Если задан центр, используем его, иначе свой transform.position
            Vector3 center = _useCenterPoint ? _centerPoint.position : transform.position;
            CleanZone(center);
        }
    }

    private void Update()
    {
        if (_currentCheckCount >= _checkCount)
            return;

        _ClearTime -= Time.deltaTime;
        if (_ClearTime <= 0)
        {
            _currentCheckCount++;

            if (_usePoint)
            {
                CleanZone(_StartPoint.position, _EndPoint.position);
            }
            else
            {
                CleanZone(transform.position);
            }
            _ClearTime = _startClearTime;
        }
    }

    /// <summary>
    /// Выполняет физический запрос (OverlapBox) в зоне с заданным центром (используя стандартные halfExtents)
    /// и удаляет объекты с запрещёнными тегами.
    /// </summary>
    /// <param name="center">Центр проверки</param>
    public void CleanZone(Vector3 center)
    {
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, checkLayer);
        foreach (Collider col in colliders)
        {
            foreach (string tag in forbiddenTags)
            {
                if (col.CompareTag(tag))
                {
                    //Debug.Log($"Объект с тегом {col.tag} обнаружен в зоне (центр) и будет удалён.");
                    Destroy(col.gameObject);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Перегруженная версия CleanZone, которая вычисляет зону очистки на основе двух точек.
    /// Центр вычисляется как средняя точка между point1 и point2, а half extents – как половина разницы по осям X и Z.
    /// Y оставляем из halfExtents.
    /// </summary>
    /// <param name="point1">Первая точка</param>
    /// <param name="point2">Вторая точка</param>
    public void CleanZone(Vector3 point1, Vector3 point2)
    {
        Vector3 center = (point1 + point2) / 2f;
        Vector3 extents = new Vector3(Mathf.Abs(point1.x - point2.x) / 2f, halfExtents.y, Mathf.Abs(point1.z - point2.z) / 2f);
        Collider[] colliders = Physics.OverlapBox(center, extents, Quaternion.identity, checkLayer);
        foreach (Collider col in colliders)
        {
            foreach (string tag in forbiddenTags)
            {
                if (col.CompareTag(tag))
                {
                    //Debug.Log($"Объект с тегом {col.tag} обнаружен в зоне (2 точки) и будет удалён.");
                    Destroy(col.gameObject);
                    break;
                }
            }
        }
    }

    // Для визуализации в редакторе
    private void OnDrawGizmosSelected()
    {
        _usePoint = (_StartPoint != null && _EndPoint != null);

        if (_usePoint)
        {
            // Рисуем зону, определяемую двумя точками
            Vector3 p1 = _StartPoint.position;
            Vector3 p2 = _EndPoint.position;
            Vector3 zoneCenter = (p1 + p2) / 2f;
            Vector3 extents = new Vector3(Mathf.Abs(p1.x - p2.x) / 2f, halfExtents.y, Mathf.Abs(p1.z - p2.z) / 2f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(zoneCenter, extents * 2);
        } 
        else
        {
            Gizmos.color = Color.red;
            // Если задан _centerPoint, рисуем зону вокруг него, иначе вокруг transform.position
            Vector3 center = (_centerPoint != null) ? _centerPoint.position : transform.position;
            Gizmos.DrawWireCube(center, halfExtents * 2);
        }
    }
}
