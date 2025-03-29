using System;
using UnityEngine;

public class Settings : MonoBehaviour
{

    public static Settings instance;
    public Action<float> ChangeMouseSensitivity;
    public Action<float> ChangeVolume;
    //public Action<bool> ControllJostick;
    //[SerializeField] private Toggle _joystickToggle;
    [SerializeField] private GameObject _UIWindow;
    [SerializeField] private GameObject _exitButton;

    //private bool _isOpen;


    /*    private void OnEnable()
        {
            UIInputHandler.Instance.SettingaOpenAction += ToggleUIOpen;
            _isOpen = false;
        }

        private void OnDisable()
        {
            UIInputHandler.Instance.SettingaOpenAction -= ToggleUIOpen;
        }*/

    private void OnEnable()
    {
        //GameManager.Instance.LevelInProgress += OnLevelSwitch;
    }

    private void OnDisable()
    {
        //GameManager.Instance.LevelInProgress -= OnLevelSwitch;
    }

    private void OnLevelSwitch(bool inProgress)
    {
        if (_exitButton != null)
            _exitButton.gameObject.SetActive(inProgress);
    }


    // �������� ������ � ����� - ������ ��������
    private void Awake()
    {

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void Sensitivity(float sens)
    {
        ChangeMouseSensitivity?.Invoke(sens);
    }

    public void SoundVolume(float volume)
    {
        SoundManager.Instance.SoundVolume = volume;
    }
    public void MusicVolume(float volume)
    {
        SoundManager.Instance.MusicVolume = volume;
    }

/*    private void ToggleUIOpen()
    {
        _isOpen = !_isOpen;
        _UIWindow.SetActive(_isOpen);
        if (ControlManager.Instance && ControlManager.Instance.UseCursor != _isOpen)
        {
            ControlManager.Instance.UnlockMouse();
        }
    }*/

}
