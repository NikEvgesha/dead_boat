using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UseButtonShow : MonoBehaviour
{

    [SerializeField] private Canvas _infoCanvas;
    [SerializeField] private BuyTouchHandler _touchPanel;
    [SerializeField] private Image _openProgress;
    [SerializeField] private AudioSource _source;

    private bool _active;
    private float _progress;
    private bool _openInProgress;

    public UnityEvent Activate;

    private void Start()
    {
        _touchPanel.PointerDown += OpenShop;
    }

    private void OnDisable()
    {
        _touchPanel.PointerDown -= OpenShop;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _infoCanvas.gameObject.SetActive(true);
            _active = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _active = false;
            _infoCanvas.gameObject.SetActive(false);
            StopAllCoroutines();
            _progress = 0;
            _openProgress.fillAmount = _progress;
            _openInProgress = false;

        }
    }


    private void Update()
    {
        if (!_active || _openInProgress) return;

        if (PlayerInput.Instance.Interaction)
        {
            OpenShop();
        }
    }


    private void OpenShop()
    {
        _openInProgress = true;
        _progress = 0;
        StartCoroutine(OpenProcess());
    }


    private IEnumerator OpenProcess()
    {
        while ((_touchPanel.Hold || PlayerInput.Instance.InteractionHold) && _progress < 1f)
        {
            _progress += Time.deltaTime;
            _openProgress.fillAmount = _progress;
            yield return null;
        }

        if (_progress >= 1f)
        {
            _source.Play();
            Activate?.Invoke();
        }
        _progress = 0;
        _openProgress.fillAmount = _progress;
        _openInProgress = false;
    }
}
