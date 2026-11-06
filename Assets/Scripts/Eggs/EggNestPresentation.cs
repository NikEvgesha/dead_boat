using UnityEngine;

// Visual-only: no storage writes, colliders, lights or particle systems.
[DisallowMultipleComponent]
public sealed class EggNestPresentation : MonoBehaviour
{
    public Renderer statusRing;
    public GameObject readyMarker;
    private EggNestPoint nest;
    private EggHatchingManager manager;
    private MaterialPropertyBlock block;
    private float nextRefresh;
    public int DisplayedState { get; private set; } = -1;
    private void OnEnable() { nest = GetComponent<EggNestPoint>(); Refresh(); }
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1; Refresh();
    }
    private void OnDisable() { if (manager) manager.StateChanged -= Refresh; manager = null; }
    public void Refresh()
    {
        if (manager != EggHatchingManager.Instance)
        {
            if (manager) manager.StateChanged -= Refresh;
            manager = EggHatchingManager.Instance;
            if (manager) manager.StateChanged += Refresh;
        }
        var state = manager && nest ? manager.GetNestState(nest.NestId) : null;
        ApplyState(state == null ? 0 : manager.GetRemainingSeconds(nest.NestId) > 0 ? 1 : 2);
    }
    public void ApplyState(int state)
    {
        if (DisplayedState == state) return;
        DisplayedState = state;
        if (statusRing)
        {
            statusRing.enabled = state > 0;
            block ??= new MaterialPropertyBlock();
            block.SetColor("_Color", state == 2 ? new Color(.25f, .85f, .46f) : new Color(1, .65f, .14f));
            statusRing.SetPropertyBlock(block);
        }
        if (readyMarker) readyMarker.SetActive(state == 2);
    }
}
