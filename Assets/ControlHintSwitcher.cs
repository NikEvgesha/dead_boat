using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlHintSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _desktopHint;
    [SerializeField] private GameObject _mobileHint;


    private void Start()
    {
        bool isMobile = ControlManager.Instance.UseTouchControl;
        if (_desktopHint != null)
        {
            _desktopHint.SetActive(!isMobile);
        }
        if (_mobileHint != null)
        {
            _mobileHint.SetActive(isMobile);
        }
    }
}
