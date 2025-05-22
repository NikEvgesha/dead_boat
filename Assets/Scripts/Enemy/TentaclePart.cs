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
    [Header("Щупальце")]
    [SerializeField] private int _tentacleHP = 200;
    [SerializeField] private int _damage = 10;
    [SerializeField] private TentacleBoss _mainBody;
    [SerializeField] private Animator _animator;
    [SerializeField] private Outline _outline;

    [SerializeField] private Vector3 _hitBoxSize = new Vector3(5, 5, 5);
    [SerializeField] private Transform _pointHit;
    [SerializeField] private LayerMask _hitLayers;

    private bool _dead = false;
    public bool Active 
    {
        get { return _active; } 
        set 
        {
            _active = value;
            _outline.enabled = value;
            hpBar.gameObject.SetActive(value);
        } 
    }
    private bool _active;
    private void Start()
    {
        SetHP(_tentacleHP);
    }
    public void StartUseTentakl()
    {
        Active = false;
        StartAnimation(TentacleAnimation.Start);
    }
    public override void TakeDamage(int damage)
    {
        if (!Active)
            return;
        base.TakeDamage(damage);
        _mainBody.TakeDamage(damage);
    }
    protected override void Die()
    {
        base.Die();
        Active = false;
        _dead = !_dead;
        _mainBody.DeadTentacle(this);
        StartAnimation(TentacleAnimation.Dead);
    }
    public void SetActive()
    {
        Active = true;
        StartAnimation(TentacleAnimation.Active);
        StartCoroutine(AttackBoard());
    }
    private void DisableTentacle()
    {
        StartAnimation(TentacleAnimation.Passive);
        _mainBody.ChangeTentacle(this);
    }
    private void StartAnimation(TentacleAnimation animation)
    {
        _animator.SetTrigger(animation.ToString());
        
    }
    private IEnumerator AttackBoard()
    {
        int waitTime = Random.Range(5,10);
        yield return new WaitForSeconds(waitTime);
        if (Active)
        {
            Active = false;

            StartAnimation(TentacleAnimation.Attack);
            ActivateDamageBox();
            yield return new WaitForSeconds(1);

            DisableTentacle();
        }
    }
    public float GetStartHP()
    {
        return _tentacleHP;
    }

    private void ActivateDamageBox()
    {
        // Центр куба — позиция объекта
        Collider[] hits = Physics.OverlapBox(_pointHit.position, _hitBoxSize * 0.5f, _pointHit.rotation, _hitLayers);
        foreach (var hit in hits)
        {
            var health = hit.GetComponent<PlayerStatsManager>();
            if (health != null)
                health.TakeDamage(_damage);
        }
    }

    // Визуализация зоны в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(_pointHit.position, _pointHit.rotation, _hitBoxSize);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
