using UnityEngine;

public class FireballBoss : MonoBehaviour
{
    [SerializeField] private float _flyTime = 1;
    [SerializeField] private float _damage;
    [SerializeField] private float _hitRadius = 10;
    [SerializeField] private LayerMask _hitLayers;
    private Vector3 _selfPosition;
    private Vector3 _targetPosition;
    private bool _haveTarget;
    private float _time = 0;
    public void SetTarget(Vector3 target)
    {
        _selfPosition = this.transform.position;
        _targetPosition = target;
        _haveTarget = true;
    }
    private void Update()
    {
        if (!_haveTarget)
            return;
        if (_time < _flyTime)
        {
            _time += Time.deltaTime;
            this.transform.position = Vector3.Lerp(_selfPosition, _targetPosition, _time/_flyTime);
            return;
        }
        //_haveTarget = false;
        ActivateDamageBox();
    }
    private void ActivateDamageBox()
    {
        // ÷ентр сферы Ч позици€ объекта
        Collider[] hits = Physics.OverlapSphere(this.transform.position, _hitRadius, _hitLayers);
        foreach (var hit in hits)
        {
            var health = hit.GetComponent<PlayerStatsManager>();
            if (health != null)
                health.TakeDamage((int)_damage);
        }
        Destroy(this.gameObject);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, _hitRadius);
    }
}
