using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

public class RailSpawner : MonoBehaviour
{
    [Tooltip("Префаб рельс, который будет клонироваться")]
    public GameObject railPrefab;

    [Tooltip("Желаемое количество рельс")]
    public int railCount = 0;

    [Tooltip("Направление, в котором будут спавниться рельсы")]
    public Vector3 spawnDirection = Vector3.right;

    [Tooltip("Расстояние между рельсами")]
    public float spawnDistance = 2.0f;

    // Список для хранения созданных рельс
    private List<GameObject> spawnedRails = new List<GameObject>();

    // Сохраним предыдущее значение, чтобы понять, что изменилось
    [SerializeField]
    private int previousRailCount = 0;

    private void OnValidate()
    {
        if (railCount != previousRailCount)
        {
            previousRailCount = railCount;
#if UNITY_EDITOR
            // Отложенный вызов метода обновления до завершения OnValidate
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    UpdateRails();
                }
            };
#else
            UpdateRails();
#endif
        }
    }

    void UpdateRails()
    {
        // Удаляем старые рельсы
        for (int i = spawnedRails.Count - 1; i >= 0; i--)
        {
            if (spawnedRails[i] != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(spawnedRails[i]);
#else
                Destroy(spawnedRails[i]);
#endif
            }
        }
        spawnedRails.Clear();

        // Нормализуем направление, чтобы корректно учитывать spawnDistance
        Vector3 direction = spawnDirection.normalized;

        // Создаем новое количество рельс
        for (int i = 0; i < railCount; i++)
        {
            // Расчет позиции с учетом выбранного направления и расстояния
            Vector3 position = transform.position + direction * spawnDistance * i;
            GameObject rail;
#if UNITY_EDITOR
            rail = (GameObject)PrefabUtility.InstantiatePrefab(railPrefab, transform);
#else
            rail = Instantiate(railPrefab, transform);
#endif
            rail.transform.position = position;
            spawnedRails.Add(rail);
        }
    }
}
