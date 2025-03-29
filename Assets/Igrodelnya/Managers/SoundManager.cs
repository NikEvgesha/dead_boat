using System;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    private const float minVolume = -80f;
    private const float maxVolume = 0f;

    [SerializeField] private AudioMixerGroup _mixer;
    [SerializeField] private AnimationCurve _curve;
    [SerializeField] private AudioSource _music;
    [SerializeField] private AudioSource _uiClick;
    [SerializeField] private AudioClip _lose;
    [SerializeField] private AudioClip _win;

    private static SoundManager _instance;
    private bool _soundON;
    private float _soundVolume;
    private float _musicVolume;
    private string _soundName = "SoundVolume";
    private string _musicName = "MusicVolume";

    
    public static SoundManager Instance { get { return _instance; } }
    public bool isSoundOn { get { return _soundON; } }
    public float SoundVolume
    {
        get { return _soundVolume; }
        set
        {
            _soundVolume = value;
            _mixer.audioMixer.SetFloat(_soundName, Mathf.Lerp(minVolume, maxVolume, _curve.Evaluate(_soundVolume)));
            //VolumeChange?.Invoke(_volume);
            SaveManager.Instance.SaveSoundVolume(value);
        }
    }
    public float MusicVolume
    {
        get { return _musicVolume; }
        set
        {
            _musicVolume = value;
            _mixer.audioMixer.SetFloat(_musicName, Mathf.Lerp(minVolume, maxVolume, _curve.Evaluate(_musicVolume)));
            //VolumeChange?.Invoke(_volume);
            SaveManager.Instance.SaveMusicVolume(value);
        }
    }
    

    public Action<float> VolumeChange;

    // volume change


    private void Awake()
    {
        if (Instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        _music.Play();
    }
    private void Start()
    {
        float[] volume = SaveManager.Instance.GetVolume();
        MusicVolume = volume[0];
        SoundVolume = volume[1];
        //GameManager.Instance.GameLose += GameLose;
        //GameManager.Instance.GameWin += GameWon;
    }
    private void StartPlayMusic()
    {
        _music.Play();
    }
    private void GameLose()
    {
        _music.loop = false;
        _music.clip = _lose;
        _music.Play();
    }
    private void GameWon(float none)
    {
        _music.loop = false;
        _music.clip = _win;
        _music.Play();
    }
    public void PlayUIClick() 
    {
        _uiClick.Play();
    }
}
