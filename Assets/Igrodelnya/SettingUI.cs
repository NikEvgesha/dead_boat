using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private Scrollbar _musicVolume;
    [SerializeField] private Scrollbar _soundVolume;
    [SerializeField] private GameObject _panel;
    private bool _isOpen;
    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        ControlManager.Instance.CursorActive = _isOpen;
        _panel.SetActive(_isOpen);
        GameManager.Instance.SetPause(_isOpen, false);
    }
    private void Start()
    {
        if (SoundManager.Instance.IsReady)
        {
            SetValues();
        } else
        {
            SoundManager.Instance.Ready += SetValues;
        }
            
    }

    private void OnDisable()
    {
        SoundManager.Instance.Ready -= SetValues;
    }

    private void SetValues()
    {
        _musicVolume.value = SoundManager.Instance.MusicVolume;
        _soundVolume.value = SoundManager.Instance.SoundVolume;
    }
}
