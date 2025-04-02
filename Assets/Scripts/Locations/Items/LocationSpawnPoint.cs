using UnityEngine;

[System.Serializable]
public class LocationSpawnPoint
{
    [Tooltip("Точка, где может появиться предмет")]
    public Transform spawnTransform;

    [Tooltip("Тип предмета, разрешённый для этой точки (или Any для любого типа)")]
    public ItemType allowedType = ItemType.Any;

    [Tooltip("Размер предмета, разрешённый для этой точки (или Any для любого размера)")]
    public ItemSize allowedSize = ItemSize.Any;
}
