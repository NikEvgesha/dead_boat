[System.Serializable]
public class LocationSpawnItem
{
    public LocationContentSpawner prefab; // Префаб объекта
    public float chance;      // Вес (шанс) появления данного объекта

    public bool HaveMinSpawnDistence;
    public float MinSpawnDistence;

    public bool HaveMaxSpawnDistence;
    public float MaxSpawnDistence;
}
