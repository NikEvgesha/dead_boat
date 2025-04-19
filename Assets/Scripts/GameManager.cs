using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } private set { } }


    [SerializeField] private PlayerStatsManager _player;
    [SerializeField] private string _lobbySceneName = "Lobby";
    [SerializeField] private Transform _playerSpawnPoint;
    public PlayerStatsManager Player { get { return _player; } }

    public bool isEndGame = false;

    public Action GameStart;

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
            _player = PlayerManager.Instance.GetComponent<PlayerStatsManager>();
            PlayerMovement.Instance.Teleport(_playerSpawnPoint);
        }
        GameStart?.Invoke();
    }

    public void EndGame(bool lobby)
    {
        isEndGame = true;
        Location location = lobby ? Location.Lobby : Location.Game;
        PauseManager.Instance.SetPause(false);
        LoadingManager.Instance.LoadLocation(location);
        DontDestroyOnLoad(PlayerManager.Instance.gameObject);
        Inventory.Instance.ResetInventory();
        CurrencyManager.Instance.Reset();
        PlayerStatsManager.Instance.Revive();
    }
}
