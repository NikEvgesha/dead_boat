using UnityEngine;

public class Lever : MonoBehaviour
{
    [SerializeField] public ActivateObject _activeSubject;
    [SerializeField] public Animator _animator;
    [SerializeField] private UseButtonShow _useButtonShow;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_activeSubject == null)
            _activeSubject = GetComponentInParent<ActivateObject>();

        if (_useButtonShow == null)
            _useButtonShow = GetComponentInChildren<UseButtonShow>(true);

        if (_useButtonShow != null)
            _useButtonShow.UseButtonShowBool = true;
    }

    public void Use()
    {
        if (_activeSubject != null)
            _activeSubject.Activate();
        else
            Debug.LogWarning($"Lever '{name}' has no active subject assigned.", this);

        if (_animator != null)
            _animator.SetTrigger("Start");
    }
}
