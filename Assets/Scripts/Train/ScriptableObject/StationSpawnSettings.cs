using UnityEngine;

[CreateAssetMenu(menuName = "SpawnSettings/StationSpawnSettings")]
public class StationSpawnSettings : ScriptableObject
{
    [Header("Настройки станции")]
    [Tooltip("Дистанция вперед от поезда, до которой генерируются станции")]
    public float SpawnDistanceAhead = 2000f;

    [Tooltip("Дистанция позади поезда, после которой объекты удаляются")]
    public float DespawnDistanceBehind = 500f;

    [Tooltip("Расстояние между станциями")]
    public float StationLength = 20000f;

    [Tooltip("Шанс спавна станции (0-1), обычно 1")]
    [Range(0f, 1f)]
    public float SpawnChance = 1f;
}
