using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private Scrollbar _musicVolume;
    [SerializeField] private Scrollbar _soundVolume;

    private void OnEnable()
    {
        _musicVolume.value = SoundManager.Instance.MusicVolume;
        _soundVolume.value = SoundManager.Instance.SoundVolume;
    }
}
