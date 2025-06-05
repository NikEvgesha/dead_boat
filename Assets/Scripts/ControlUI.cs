using System;
using UnityEngine;

[Serializable]
public struct TouchControls
{
    public OnScreenButton jumpButton;
    public OnScreenButton sprintButton;
    public OnScreenButton pickUpButton;
    public OnScreenButton putToInventoryButton;
    public OnScreenButton attachButton;
    public OnScreenButton reloadButton;
    public OnScreenButton attackButton;
    public OnScreenButton useButton;
    public OnScreenButton rotateXButton;
    public OnScreenButton rotateYButton;


    public OnScreenJoystick moveJoystick;
    public CameraTouchController cameraTouchController;
}

[Serializable]
public struct DesktopHints
{
    public GameObject common;
    public GameObject pickUp;
    public GameObject putToInventory;
    public GameObject attach;
    public GameObject rotate;
}
public class ControlUI : MonoBehaviour
{
    private static ControlUI _instance;
    public static ControlUI Instance => _instance;
    [SerializeField]
    private GameObject _mobileUI;
    [SerializeField]
    private GameObject _desktopUI;

    [SerializeField] private TouchControls _touchControls;
    [SerializeField] private DesktopHints _descktopHints;
    private bool _isQuitting;
    private bool _isMobile;
    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
    public void UseMobileSetup(bool isMobile)
    {
        _isMobile = isMobile;
        _mobileUI.SetActive(isMobile);
        _desktopUI.SetActive(!isMobile);
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*    public void SwitchPlatformControls(bool onPlatform)
        {
            _touchControls.jumpButton.gameObject.SetActive(onPlatform);
        }*/

    public TouchControls GetTouchControls()
    {
        return _touchControls;
    }


    public void ShowPickUpButton(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
        {
            _touchControls.pickUpButton.gameObject.SetActive(visible);

            if (Input.GetMouseButtonDown(0) && visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    PickableItem clickable = hit.collider.GetComponent<PickableItem>();
                    if (clickable != null)
                    {
                        PlayerInput.Instance.ForcePickUp = true;
                    }
                }
            }
        }
        else
            _descktopHints.pickUp.SetActive(visible);
    }

    public void ShowPutToInventoryButton(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
            _touchControls.putToInventoryButton.gameObject.SetActive(visible);
        else
            _descktopHints.putToInventory.SetActive(visible);
    }

    public void OnItemPickUp(bool picked)
    {
        if (_isQuitting)
            return;
        _touchControls.putToInventoryButton.gameObject.SetActive(!picked);
    }

    public void ShowAttachButton(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
            _touchControls.attachButton.gameObject.SetActive(visible);
        else
            _descktopHints.attach.SetActive(visible);
    }
    
    public void ShowAttackButton(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
            _touchControls.attackButton.gameObject.SetActive(visible);
        //else
            //_descktopHints.attack.SetActive(visible);
    }

    public void ShowReloadButton(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
            _touchControls.reloadButton.gameObject.SetActive(visible);
        //else
            //_descktopHints.reload.SetActive(visible);
    }

    public void ShowUseButton(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
            _touchControls.useButton.gameObject.SetActive(visible);
        //else
        //_descktopHints.reload.SetActive(visible);
    }

    public void ShowRotateButtons(bool visible)
    {
        if (_isQuitting)
            return;
        if (_isMobile)
        {
            _touchControls.rotateXButton.gameObject.SetActive(visible);
            _touchControls.rotateYButton.gameObject.SetActive(visible);
        } else
        {
            _descktopHints.rotate.gameObject.SetActive(visible);
        }
    }
}
