using System;
using UnityEngine;

[Serializable]
public struct TouchControls
{
    public OnScreenButton upButton;
    public OnScreenButton downButton;
    public OnScreenButton jumpButton;
    public OnScreenButton pickUpButton;
    public OnScreenButton putToInventoryButton;
    public OnScreenJoystick moveJoystick;
    public CameraTouchController cameraTouchController;
}
public class ControlUI : MonoBehaviour
{
    [SerializeField]
    private GameObject _mobileUI;
    [SerializeField]
    private GameObject _desktopUI;

    [SerializeField] private TouchControls _touchControls;

    private bool _isMobile;

    public void UseMobileSetup(bool isMobile)
    {
        _isMobile = isMobile;
        _mobileUI.SetActive(isMobile);
        _desktopUI.SetActive(!isMobile);
    }
    

    public void SwitchPlatformControls(bool onPlatform)
    {
        _touchControls.upButton.gameObject.SetActive(!onPlatform);
        _touchControls.downButton.gameObject.SetActive(!onPlatform);
        _touchControls.jumpButton.gameObject.SetActive(onPlatform);
    }

    public TouchControls GetTouchControls()
    {
        return _touchControls;
    }


    public void ShowPickUpButton(bool visible)
    {
        _touchControls.pickUpButton.gameObject.SetActive(visible);
        _touchControls.putToInventoryButton.gameObject.SetActive(visible);
    }

}
