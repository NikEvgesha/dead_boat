using System.Collections;
using UnityEngine;

public class DamageArea : MonoBehaviour
{
    [SerializeField] private int _damage = 10; 
    private PlayerStatsManager _playerIn;
    private bool _isDamage;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerStatsManager>(out _playerIn))
        {
            StartCoroutine(DamageCoroutine());
        }
    }
    private IEnumerator DamageCoroutine()
    {
        yield return new WaitForSeconds(1);
        while (_playerIn && !_isDamage)
        {
            _isDamage = true;
            _playerIn.TakeDamage(_damage);
            yield return new WaitForSeconds(1);
            _isDamage = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        _playerIn = null;
    }

}
