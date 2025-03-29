using UnityEngine;
using System.Collections.Generic;

public class BuildingController : MonoBehaviour
{
    [Header("Настройки магазина")]
    [Tooltip("Объект магазина, который включается или отключается")]
    [SerializeField] private GameObject _shopObject;

    [Tooltip("Список префабов предметов для магазина")]
    [SerializeField] private List<GameObject> _shopItemsPrefabs;

    [Tooltip("Точка спавна предметов в магазине")]
    [SerializeField] private Transform _shopItemsSpawnPoint;

    [Tooltip("Максимальное количество предметов, которое может появиться в магазине")]
    [SerializeField] private int _maxShopItems = 5;

    private bool _shopActive;

    /// <summary>
    /// Включает или отключает магазин, а при включении инициирует его наполнение.
    /// </summary>
    /// <param name="active">true - активировать магазин, false - деактивировать</param>
    public void SetShopActive(bool active)
    {
        _shopActive = active;
        if (_shopObject != null)
        {
            _shopObject.SetActive(active);
        }

        if (active)
        {
            InitializeShop();
        }
    }

    /// <summary>
    /// Инициализирует магазин, спавняя случайное количество предметов из заданного списка.
    /// </summary>
    private void InitializeShop()
    {
        // Очищаем старые предметы, если они есть
        foreach (Transform child in _shopItemsSpawnPoint)
        {
            Destroy(child.gameObject);
        }

        // Определяем случайное количество предметов (от 1 до _maxShopItems)
        int itemsCount = Random.Range(1, _maxShopItems + 1);
        for (int i = 0; i < itemsCount; i++)
        {
            int randomIndex = Random.Range(0, _shopItemsPrefabs.Count);
            GameObject itemPrefab = _shopItemsPrefabs[randomIndex];
            Instantiate(itemPrefab, _shopItemsSpawnPoint.position, Quaternion.identity, _shopItemsSpawnPoint);
        }
    }
}
