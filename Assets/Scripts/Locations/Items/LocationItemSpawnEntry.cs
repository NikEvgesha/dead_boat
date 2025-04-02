using UnityEngine;

[System.Serializable]
public class LocationItemSpawnEntry
{
    [Tooltip("Префаб предмета (компонент PickableItem)")]
    public PickableItem itemPrefab;

    [Tooltip("Шанс появления данного варианта")]
    public float chance = 1f;

    [Tooltip("Тип предмета")]
    public ItemType itemType = ItemType.Any;

    [Tooltip("Размер предмета")]
    public ItemSize itemSize = ItemSize.Any;
}
