using UnityEngine;

public class ActivateObject : MonoBehaviour
{
    [SerializeField] Animator animator;
    private bool _sharedActivated;
    private void Start() => DeadBoat.Online.SharedWorldActionsRuntime.Register(this);
    private void OnDestroy() => DeadBoat.Online.SharedWorldActionsRuntime.Remove(this);

    public void SharedApply(bool restoring)
    {
        if (_sharedActivated && !restoring) return;
        _sharedActivated = true;
        DeadBoat.Online.SharedWorldActionAnimation.Apply(animator, restoring);
    }

   public void Activate()
    {
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 1)) return;
        animator.SetTrigger("Start");
    }
}
