using UnityEngine;

public class Lever : MonoBehaviour
{
    [SerializeField] public ActivateObject _activeSubject;
    [SerializeField] public Animator _animator;
    [SerializeField] private UseButtonShow _useButtonShow;
    private bool _sharedActivated;

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

    private void Start() => DeadBoat.Online.SharedWorldActionsRuntime.Register(this);
    private void OnDestroy() => DeadBoat.Online.SharedWorldActionsRuntime.Remove(this);

    public void SharedApply(bool restoring)
    {
        if (_sharedActivated && !restoring) return;
        _sharedActivated = true;
        if (!restoring && _useButtonShow != null) _useButtonShow.PlayAcceptedFeedback();
        if (_activeSubject != null) _activeSubject.Activate();
        DeadBoat.Online.SharedWorldActionAnimation.Apply(_animator, restoring);
        if (_useButtonShow != null) _useButtonShow.gameObject.SetActive(false);
    }

    public void Use()
    {
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 1)) return;
        if (_activeSubject != null)
            _activeSubject.Activate();
        else
            Debug.LogWarning($"Lever '{name}' has no active subject assigned.", this);

        if (_animator != null)
            _animator.SetTrigger("Start");
    }
}
