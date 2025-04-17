using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleWeapon : MonoBehaviour
{
    [SerializeField] private UsableItem _usableItem;
    [SerializeField] private Animator _animator;
    [SerializeField] private BoxCollider _damageArea;
    [SerializeField] private int _damage;
    [SerializeField] private AudioClip _audioHit;
    [SerializeField] private AudioClip _audioSwing;
    [SerializeField] private AudioSource _audioSource;

    private List<ZombieController> _zombies = new List<ZombieController>();
    private bool _use;
    private bool _active;

    private void OnTriggerEnter(Collider other)
    {
        if (!_usableItem.IsActive)
            return;
        ZombieController zombie = other.GetComponentInParent<ZombieController>();
        if (_use && zombie)
        {
            if (_zombies.Contains(zombie))
                return;

            _zombies.Add(zombie);
            zombie.TakeDamage(_damage);

            if (_audioSource)
                if (_audioHit)
                    _audioSource.PlayOneShot(_audioHit);
        }
    }
    private void OnEnable()
    {
        _usableItem.Active += SetActiveUse;
        //SetActiveUse(_usableItem.IsActive);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        _use = false;
        _active = false;
        _damageArea.enabled = false;
        _zombies.Clear();
        _usableItem.Active -= SetActiveUse;
        ControlUI.Instance.ShowAttackButton(_active);
    }

    private void Start()
    {
        SetActiveUse(_usableItem.IsActive);
    }
    private void UseUpdate()
    {
        if (!_use)
        {
            PlayRandomAttack();
            _use = !_use;
        }
    }
    public void PlayRandomAttack()
    {
        // Генерируем случайное число 1, 2 или 3
        _damageArea.enabled = true;
        int randomAttack = Random.Range(1, 4);
        _animator.SetInteger("AttackIndex", randomAttack);
        _animator.SetTrigger("DoAttack");
        if(_audioSource)
            if(_audioSwing)
                _audioSource.PlayOneShot(_audioSwing);
        StartCoroutine(EndAnimation());
    }
    
    private IEnumerator EndAnimation()
    {

        yield return new WaitForSeconds(1);

        _damageArea.enabled = false;
        _use = false;
        _zombies.Clear();

        yield return null;
    }
    private void SetActiveUse(bool active)
    {
        if (_active == active)
            return;

        _active = active;

        ControlUI.Instance.ShowAttackButton(_active);

        _damageArea.enabled = false;

        if (_active)
            _usableItem.Use += UseUpdate;
        else 
            _usableItem.Use -= UseUpdate;

    }
}
