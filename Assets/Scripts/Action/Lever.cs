using UnityEngine;

public class Lever : MonoBehaviour
{
    [SerializeField] public ActivateObject _activeSubject;
    [SerializeField] public Animator _animator;

    public void Use()
    {
        _activeSubject.Activate();
        _animator.SetTrigger("Start");
    }
}
