using System.Collections;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private bool _isActivate;
    [SerializeField] private Portal _nextPortal;
    [SerializeField] private GameObject _partical;
    [SerializeField] private SpriteRenderer _portalSprite;

    private bool _initialStateCaptured;
    private bool _initiallyActive;
    public Portal NextPortal => _nextPortal;
    public bool InitiallyActive
    {
        get { CaptureInitialState(); return _initiallyActive; }
    }

    private void Awake() => CaptureInitialState();
    private void CaptureInitialState()
    {
        if (_initialStateCaptured) return;
        _initiallyActive = _isActivate;
        _initialStateCaptured = true;
    }

    private void Activate(bool isActivate)
    {
        _isActivate = isActivate;
        if (_partical != null) _partical.SetActive(isActivate);
        Color color = isActivate ? Color.white : Color.black;
        if (_portalSprite != null) _portalSprite.color = color;
    }

    private void Start()
    {
        CaptureInitialState();
        Activate(_isActivate);
        DeadBoat.Online.SharedWorldActionsRuntime.Register(this);
    }
    private void OnDestroy() => DeadBoat.Online.SharedWorldActionsRuntime.Remove(this);

    public void SharedApply(bool active)
    {
        CaptureInitialState();
        // Canonical state replaces any old local rearm, including one begun before binding.
        StopAllCoroutines();
        Activate(active);
    }
    public void _ActivateTeleport(float time)
    {
        if (DeadBoat.Online.SharedWorldActionsRuntime.Request(this, 1, time)) return;
        StartCoroutine(StartActivateTeleport(time));
    }
    public IEnumerator StartActivateTeleport(float time)
    {
        yield return new WaitForSeconds(time);
        Activate(true);
    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (!player) return;
        if (DeadBoat.Online.SharedWorldActionsRuntime.Travel(this, player)) return;
        if (!_isActivate || _nextPortal == null) return;

        Activate(false);

        player.Teleport(_nextPortal.transform);
        _nextPortal._ActivateTeleport(1);
    }
}
