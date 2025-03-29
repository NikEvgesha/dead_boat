using UnityEngine;

[CreateAssetMenu(menuName = "SpawnSettings/MeteorsSpawnSettings")]
public class MeteorsSpawnSettings : ScriptableObject
{
    [Header("Настройки метеоров")]
    [Tooltip("Минимальное количество метеоров в сегменте")]
    public int MinMeteorCount = 1;

    [Tooltip("Максимальное количество метеоров в сегменте")]
    public int MaxMeteorCount = 3;

    [Tooltip("Шанс спавна метеоров в сегменте (0-1)")]
    [Range(0f, 1f)]
    public float SpawnChance = 1f;

    [Tooltip("Диапазон смещения по оси X для метеоров")]
    public Vector2 XRange = new Vector2(-100f, 100f);

    [Tooltip("Диапазон высоты (оси Y) для метеоров")]
    public Vector2 YRange = new Vector2(50f, 200f);
}
