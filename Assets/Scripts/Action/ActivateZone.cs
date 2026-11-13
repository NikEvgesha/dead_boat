using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ActivateZone : MonoBehaviour
{
    public UnityEvent PlayerOnTriggerEnter;
    public UnityEvent PlayerOnTriggerExit;

    private int _sharedFlags;
    private void Start() => DeadBoat.Online.SharedWorldActionsRuntime.Register(this);
    private void OnDestroy() => DeadBoat.Online.SharedWorldActionsRuntime.Remove(this);

    public void SharedApply(int flags, bool restoring)
    {
        int current = flags & 3;
        int added = current & ~_sharedFlags;
        _sharedFlags = current;
        if ((added & 1) != 0) SharedReplayEvent(1);
        if ((added & 2) != 0) SharedReplayEvent(2);
    }

    public void SharedReplayEvent(int command)
    {
        if (command == 1) PlayerOnTriggerEnter?.Invoke();
        else if (command == 2) PlayerOnTriggerExit?.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 1)) return;
        PlayerOnTriggerEnter?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {

        if (!other.CompareTag("Player")) return;
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 2)) return;
        PlayerOnTriggerExit?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        // The first enter can coincide with a page/master readiness gap.
        // Known accepted flags are suppressed locally; pending requests are bounded.
        if (DeadBoat.Online.SharedRunContext.Active && other.CompareTag("Player"))
            DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 1);
    }
}
