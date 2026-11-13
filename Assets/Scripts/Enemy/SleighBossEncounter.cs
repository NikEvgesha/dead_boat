using System.Collections;
using UnityEngine;

namespace DeadBoat.Events.NewYear
{
    [RequireComponent(typeof(SleighBossController))]
    public sealed class SleighBossEncounter : TriggerBossFight
    {
        private SleighBossController controller;
        private bool started;
        public override void StartBossFight()
        {
            if (started || DeadBoat.Online.SharedRunContext.Active) return;
            started = true;
            if (!_board) _board = FindAnyObjectByType<BoardController>();
            controller = GetComponent<SleighBossController>();
            transform.position = (_board ? _board.transform.position : transform.position) + new Vector3(0,16,20);
            controller.Defeated += OnDefeated;
            controller.BeginFinalEncounter();
        }
        private void OnDefeated()
        {
            controller.Defeated -= OnDefeated;
            GameEvents.OnEnemyKilled?.Invoke(EnemyType.Boss);
            StartCoroutine(Victory());
        }
        private IEnumerator Victory()
        {
            yield return new WaitForSeconds(1);
            EndGameUIManager.EndGame(EndGameState.Win);
        }
        private void OnDestroy() { if(controller) controller.Defeated -= OnDefeated; }
    }
}
