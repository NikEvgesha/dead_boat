using UnityEngine;

[CreateAssetMenu(menuName = "SpawnSettings/IslandsSpawnSettings")]
public class IslandsSpawnSettings : ScriptableObject
{
    [Header("Настройки островов")]
    [Tooltip("Минимальное количество островов в сегменте")]
    public int MinIslandCount = 3;

    [Tooltip("Максимальное количество островов в сегменте")]
    public int MaxIslandCount = 5;

    [Tooltip("Шанс спавна островов в сегменте (0-1)")]
    [Range(0f, 1f)]
    public float SpawnChance = 1f;

    [Tooltip("Диапазон смещения по оси X для островов")]
    public Vector2 XRange = new Vector2(-50f, 50f);
}
