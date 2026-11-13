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
    [SerializeField] private TriggerBossFight _soloBossOverride;
    private bool spawned;

    private void Start()
    {
        if (!_boardController)
            _boardController = FindAnyObjectByType<BoardController>();
        _boardController.EndGame += SpawnBoss;

    }
    private void OnDestroy() { if (_boardController) _boardController.EndGame -= SpawnBoss; }
    private void SpawnBoss()
    {
        if (spawned) return;
        var template = !DeadBoat.Online.SharedRunContext.Active && _soloBossOverride ? _soloBossOverride : _boss;
        if (!template) return;
        spawned = true;
        Vector3 spawnPos = _boardController.transform.position;
        var encounter = Instantiate(template, spawnPos, template.transform.rotation, transform);
        if (DeadBoat.Online.SharedRunContext.GenerationActive)
        {
            int index = 0;
            foreach (var enemy in encounter.GetComponentsInChildren<EnemyCore>(true))
                enemy.gameObject.AddComponent<DeadBoat.Online.WorldSpawnIdentity>().Key = "boss:part:" + index++;
        }
        encounter.StartBossFight();
    }

}
