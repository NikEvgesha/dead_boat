using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DecorationSpawnItem
{
    public GameObject prefab;  // Префаб декорации
    public float chance;       // Шанс (вес) появления этой декорации
}

[CreateAssetMenu(fileName = "DecorationSpawnCollection", menuName = "Spawning/DecorationSpawnCollection")]
public class DecorationSpawnCollection : ScriptableObject
{
    // Основной список, который используется в игре
    public List<DecorationSpawnItem> spawnItems = new List<DecorationSpawnItem>();

#if UNITY_EDITOR
    // Временный список для заполнения в редакторе (не включается в билд)
    [SerializeField]
    private List<GameObject> temporarySpawnItems = new List<GameObject>();
    [SerializeField]
    private float _defaultChance = 1f;
    // Метод вызывается в редакторе при изменении значений в инспекторе
    private void OnValidate()
    {
        if (temporarySpawnItems.Count == 0)
            return;
        int indexItem = 0;
        foreach (GameObject item in temporarySpawnItems) 
        {
            if (indexItem >= spawnItems.Count)
            {
                spawnItems.Add(CreateNewItem(indexItem));
            } 
            else
            {
                spawnItems[indexItem].prefab = item;
            }
            indexItem++;
        }
    }
    private DecorationSpawnItem CreateNewItem(int indexItem)
    {
        DecorationSpawnItem item = new DecorationSpawnItem();
        item.prefab = temporarySpawnItems[indexItem];
        item.chance = _defaultChance;
        return item;
    }
#endif

    // Метод выбора префаба с учётом указанных весов
    public GameObject GetRandomSpawnPrefab()
    {
        float totalWeight = 0f;
        foreach (var item in spawnItems)
        {
            totalWeight += item.chance;
        }

        float randomValue = Random.Range(0f, totalWeight);
        foreach (var item in spawnItems)
        {
            if (randomValue < item.chance)
                return item.prefab;
            randomValue -= item.chance;
        }
        return null;
    }
}
