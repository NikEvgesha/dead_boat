using UnityEngine;

public class LightStick : MonoBehaviour
{
    [SerializeField] private GameObject _light;
    [SerializeField] private bool _isOn;

    private void Awake()
    {
        _light.SetActive(_isOn);
    }
    public void ActivateLight(bool isActivate)
    {
        _isOn = isActivate;
        _light.SetActive(isActivate);
    }
}
