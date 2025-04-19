using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool _pause;
    public bool IsPaused => _pause;

    private static PauseManager _instance;
    public static PauseManager Instance { get { return _instance; } private set { } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetPause(bool paused, bool controlAudio = true)
    {
        _pause = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (controlAudio)
            AudioListener.pause = paused;
    }
}