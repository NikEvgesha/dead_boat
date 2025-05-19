using System.Collections;
using UnityEngine;
using UnityEditor;

enum TentacleAnimation
{
    Start,
    Active,
    Passive,
    Dead,
    Attack
}

public class TentaclePart : EnemyCore
{
    [Header("ўупальце")]
    [SerializeField] private int _tentacleHP = 200;
    [SerializeField] private TentacleBoss _mainBody;
    [SerializeField] private Animator _animator;
    private bool _dead = false;
    private bool _active = false;
    private void Start()
    {
        SetHP(_tentacleHP);
    }
    public void StartUseTentakl()
    {
        StartAnimation(TentacleAnimation.Start);
    }
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        if (currentHP <= 0)
            Dead();
    }
    private void Dead()
    {
        _active = false;
        _dead = !_dead;
        _mainBody.DeadTentacle(this);
        StartAnimation(TentacleAnimation.Dead);
    }
    public void SetActive()
    {
        _active = true;
        StartAnimation(TentacleAnimation.Active);
        StartCoroutine(AttackBoard());
    }
    private void DisableTentacle()
    {
        _mainBody.ChangeTentacle(this);
        StartAnimation(TentacleAnimation.Passive);
    }
    private void StartAnimation(TentacleAnimation animation)
    {
        _animator.SetTrigger(animation.ToString());
        
    }

    private IEnumerator AttackBoard()
    {
        int waitTime = Random.Range(10,30);
        yield return new WaitForSeconds(waitTime);
        if (_active)
        {
            _active = false;

            StartAnimation(TentacleAnimation.Attack);

            yield return new WaitForSeconds(1);

            DisableTentacle();
        }
    }
    public float GetStartHP()
    {
        return _tentacleHP;
    }
}
