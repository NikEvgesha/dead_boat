using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private BoardController _boardController;

    [Header("Enemies")]
    [Tooltip("Тип Босса")]
    [SerializeField] private TriggerBossFight _boss;

    private void Start()
    {
        if (!_boardController)
            _boardController = FindAnyObjectByType<BoardController>();
        _boardController.EndGame += SpawnBoss;

    }
    private void SpawnBoss()
    {
        Vector3 spawnPos = _boardController.transform.position;
        _boss = Instantiate(_boss, spawnPos, _boss.transform.rotation, transform);
        _boss.StartBossFight();
    }

}
