using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RailPoolManager : MonoBehaviour
{
    [Header("Настройки рельс")]
    public GameObject railPrefab;
    public int poolSize = 20;
    public float railLength = 10f;
    public int railsBackward = 3; // число сегментов позади поезда
    public int railsForward = 10; // число сегментов впереди поезда (для ориентира)

    private List<GameObject> railPool = new List<GameObject>();
    private TrainController trainController;

    void Start()
    {
        trainController = GetComponent<TrainController>();
        if (trainController == null)
        {
            Debug.LogError("Компонент TrainController не найден на поезде!");
            return;
        }

        // Создание пула рельс
        for (int i = 0; i < poolSize; i++)
        {
            GameObject railSegment = Instantiate(railPrefab, transform.position, Quaternion.identity);
            railPool.Add(railSegment);
        }

        // Располагаем сегменты рельс вдоль оси X так, чтобы railsBackward сегментов было позади поезда
        float startX = transform.position.x - railsBackward * railLength;
        for (int i = 0; i < poolSize; i++)
        {
            Vector3 pos = new Vector3(startX + i * railLength, transform.position.y, transform.position.z);
            railPool[i].transform.position = pos;
        }

        StartCoroutine(RailUpdateCoroutine());
    }

    private IEnumerator RailUpdateCoroutine()
    {
        while (true)
        {
            float trainX = transform.position.x;
            // Если первый сегмент рельс находится слишком далеко позади поезда, перемещаем его вперед
            while (railPool[0].transform.position.x + railLength < trainX - railsBackward * railLength)
            {
                GameObject firstRail = railPool[0];
                // Новая позиция определяется по последнему сегменту в пуле
                float newX = railPool[railPool.Count - 1].transform.position.x + railLength;
                firstRail.transform.position = new Vector3(newX, firstRail.transform.position.y, firstRail.transform.position.z);

                // Перемещаем сегмент из начала списка в конец
                railPool.RemoveAt(0);
                railPool.Add(firstRail);
            }
            yield return new WaitForSeconds(0.05f); // Проверяем каждые 0.05 секунды
        }
    }
}
