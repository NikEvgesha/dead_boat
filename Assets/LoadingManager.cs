using System;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class LoadingManager : MonoBehaviour
{
    [SerializeField] private string _lobbyScene;
    [SerializeField] private string _gameScene;

    private Location _location = Location.None;
    public Location CurrentLocation;

    public Action<Location> LocationChanged;

    private static LoadingManager _instance;
    public static LoadingManager Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("LoadingManager уже существует! Удаляем дубликат.");
            Destroy(gameObject);
        }
    }
    void Start()
    {
        GameLoader.Instance.OnSceneLoaded += OnSceneLoaded;
        if (SaveManager.Instance.IsNewPlayer)
        {
            GameLoader.Instance.LoadNextScene(_gameScene, true);
            _location = Location.Game;
        }  
        else
        {
            GameLoader.Instance.LoadNextScene(_lobbyScene, true);
            _location = Location.Lobby;
        }
    }


    private void OnDisable()
    {
        GameLoader.Instance.OnSceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded()
    {
        CurrentLocation = _location;
        LocationChanged?.Invoke(CurrentLocation);
    }

    public void LoadLocation(Location location)
    {
        if (location == Location.Game)
        {
            GameLoader.Instance.LoadNextScene(_gameScene, true);
            _location = Location.Game;
        }
        else if (location == Location.Lobby)
        {
            GameLoader.Instance.LoadNextScene(_lobbyScene, true);
            _location = Location.Lobby;
        }
    }

}
