using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TentacleBoss : TriggerBossFight
{
    //[SerializeField] private Animator _animator;
    [SerializeField] private List<TentaclePart> _tentacle = new List<TentaclePart>();
    private float _currentHP;
    private float _startHP = 0.1f;
    public override void StartBossFight()
    {
        base.StartBossFight();
        foreach (var item in _tentacle)
        {
            item.StartUseTentakl();
            _startHP += item.GetStartHP();
        }
        _currentHP = _startHP;
        ChangeTentacle(null);
        //_animator.SetTrigger(_triggerStartAnimation);
    }
    public void ChangeTentacle(TentaclePart old)
    {
        int r = Random.Range(0,_tentacle.Count);

        if (_tentacle[r] == old)
            r++;
        if(r >= _tentacle.Count)
            r = 0;
        
        _tentacle[r].SetActive();
    }
    public void DeadTentacle(TentaclePart old)
    {
        _tentacle.Remove(old);
        if (_tentacle.Count == 0)
        {
            BossDead();
            return;
        }
        ChangeTentacle(old);
    }
    private void BossDead()
    {
        //_animator.SetTrigger(_triggerDeadAnimation);
        DeadBoss();
    }
    private IEnumerator DeadBoss()
    {
        yield return new WaitForSeconds(1);
        EndGameUIManager.EndGame(EndGameState.Win);
    }
}
