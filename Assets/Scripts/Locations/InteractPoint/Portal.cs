using System.Collections;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private bool _isActivate;
    [SerializeField] private Portal _nextPortal;
    [SerializeField] private GameObject _partical;
    [SerializeField] private SpriteRenderer _portalSprite;

    private void Activate(bool isActivate)
    {
        _isActivate = isActivate;
        _partical.SetActive(isActivate);
        Color color = isActivate ? Color.white : Color.black;
        _portalSprite.color = color;
    }

    private void Start()
    {
        _partical.SetActive(_isActivate);
    }
    public void _ActivateTeleport(float time)
    {
        StartCoroutine(StartActivateTeleport(time));
    }
    public IEnumerator StartActivateTeleport(float time)
    {
        yield return new WaitForSeconds(time);
        Activate(true);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!_isActivate) { return; }
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (!player) return;

        Activate(false);

        player.Teleport(_nextPortal.transform);
        _nextPortal._ActivateTeleport(1);
    }
}
