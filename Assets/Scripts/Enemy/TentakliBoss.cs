using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TentakliBoss : TriggerBossFight
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float _startHP;
    [SerializeField] private List<Tentakl> _tentakli = new List<Tentakl>();
    private float _currentHP;
    private string _triggerStartAnimation = "Start";
    private string _triggerDeadAnimation = "Dead";
    public override void StartBossFight()
    {
        base.StartBossFight();
        _animator.SetTrigger(_triggerStartAnimation);
    }
    public void ChangeTentacl(Tentakl old)
    {
        int r = Random.Range(0,_tentakli.Count);

        if (_tentakli[r] == old)
            r++;
        if(r >= _tentakli.Count)
            r = 0;
        
        _tentakli[r].SetActive();
    }
    public void DeadTentacl(Tentakl old)
    {
        _tentakli.Remove(old);
        if (_tentakli.Count == 0)
        {
            BossDead();
            return;
        }
        ChangeTentacl(old);
    }
    private void BossDead()
    {
        _animator.SetTrigger(_triggerDeadAnimation);
        DeadBoss();
    }
    private IEnumerator DeadBoss()
    {
        yield return new WaitForSeconds(1);
        EndGameUIManager.EndGame(EndGameState.Win);
    }
}
