using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } private set { } }


    [SerializeField] private PlayerStatsManager _player;
    [SerializeField] private string _lobbySceneName = "SampleScene";
    public PlayerStatsManager Player { get { return _player; } }

    public bool isEndGame = false;

    [Header("Дистанция всей игры")]
    public float PlayDistance = 100000f;


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

    private void Start()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatsManager>();
        }
    }

    public void EndGame(bool lobby)
    {
        isEndGame = true;
        string scenenName = lobby ? _lobbySceneName : SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scenenName);
    }
    
}
