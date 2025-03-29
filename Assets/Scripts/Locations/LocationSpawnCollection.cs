using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocationSpawnCollection", menuName = "Spawning/LocationSpawnCollection")]
public class LocationSpawnCollection : ScriptableObject
{
    // Список вариантов спавна с указанными шансами
    public List<LocationSpawnItem> spawnItems;

    // Метод для выбора случайного префаба с учетом весов
    public GameObject GetRandomSpawnPrefab()
    {
        // Вычисляем сумму всех шансов
        float totalWeight = 0f;
        foreach (var item in spawnItems)
        {
            totalWeight += item.chance;
        }

        // Получаем случайное значение от 0 до totalWeight
        float randomValue = Random.Range(0f, totalWeight);

        // Проходим по списку и возвращаем префаб, когда случайное значение попадает в диапазон данного элемента
        foreach (var item in spawnItems)
        {
            if (randomValue < item.chance)
                return item.prefab;
            randomValue -= item.chance;
        }
        return null; // На всякий случай, если что-то пошло не так
    }
}
