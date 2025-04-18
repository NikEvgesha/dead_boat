using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } private set { } }


    [SerializeField] private PlayerStatsManager _player;
    [SerializeField] private string _lobbySceneName = "Lobby";
    public PlayerStatsManager Player { get { return _player; } }

    public bool isEndGame = false;

    [Header("Дистанция всей игры")]
    public float PlayDistance = 100000f;

    private bool _pause;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            isEndGame = false;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        isEndGame = true;
    }
    private void Start()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatsManager>();
        }
        TutorialManager.Instance.StartTutorial();
    }

    public void EndGame(bool lobby)
    {
        isEndGame = true;
        string scenenName = lobby ? _lobbySceneName : SceneManager.GetActiveScene().name;
        SetPause(false);
        GameLoader.Instance.LoadNextScene(scenenName, true);
    }

    public void SetPause(bool paused, bool controlAudio = true)
    {
        _pause = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (controlAudio)
            AudioListener.pause = paused;
    }

}
