using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

enum TentaklAnimation
{
    Start,
    Active,
    Passive,
    Dead,
    Attack
}

public class Tentakl : MonoBehaviour
{
    [SerializeField] private TentakliBoss _mainBody;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _startHP;
    [SerializeField] private List<Tentakl> _tentakli;
    private bool _dead = false;
    private bool _active = false;
    private float _currentHP;
    public Action<float> Damage;
    private void Start()
    {
        _currentHP = _startHP;
    }
    public void StartUseTentakl()
    {
        StartAnimation(TentaklAnimation.Start);
    }
    public void TakeDamage(int damage)
    {
        if (_dead)
            return;
        _currentHP -= damage;
        if (_currentHP <= 0)
            Dead();
    }
    private void Dead()
    {
        _active = false;
        _dead = !_dead;
        _mainBody.DeadTentacl(this);
        StartAnimation(TentaklAnimation.Dead);
    }
    public void SetActive()
    {
        _active = true;
        StartAnimation(TentaklAnimation.Active);
        AttackBoard();
    }
    private void DisableTentackl()
    {
        _mainBody.ChangeTentacl(this);
        StartAnimation(TentaklAnimation.Passive);
    }
    private void StartAnimation(TentaklAnimation animation)
    {
        _animator.SetTrigger(animation.ToString());
    }

    private IEnumerator AttackBoard()
    {
        yield return new WaitForSeconds(1);
        if (_active)
        {
            _active = false;

            StartAnimation(TentaklAnimation.Attack);

            yield return new WaitForSeconds(1);

            DisableTentackl();
        }
            

    }
}
