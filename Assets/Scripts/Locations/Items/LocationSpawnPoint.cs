using UnityEngine;

[System.Serializable]
public class LocationSpawnPoint
{
    [Tooltip("Точка, где может появиться предмет")]
    public Transform spawnTransform;

    [Tooltip("Place the bottom of the spawned visual at this point instead of its pivot.")]
    public bool alignVisualBase;

    [Tooltip("Optional maximum visual width/depth at this slot; zero preserves prefab size.")]
    [Min(0f)] public float maxVisualFootprint;

    [Tooltip("Тип предмета, разрешённый для этой точки (или Any для любого типа)")]
    public ItemType allowedType = ItemType.Any;

    [Tooltip("Размер предмета, разрешённый для этой точки (или Any для любого размера)")]
    public ItemSize allowedSize = ItemSize.Any;
}
