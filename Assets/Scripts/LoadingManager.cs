using MirraGames.SDK;
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
        MirraSDK.WaitForProviders(static () => {
            LoadingManager.Instance.StartGame();
            // Методы SDK не должны вызывать вылет или NullReferenceException,
            // делегат будет вызван только когда все провайдеры имеют статус IsInitialized.
        });

    }
    private void StartGame()
    {
        
        bool haveSave = SaveManager.Instance.LoadGameProgress().Item1 >= 0;
        if (SaveManager.Instance.IsNewPlayer || haveSave)
        {
            _location = Location.Game;
            if (!haveSave)
                GameLoader.Instance.LoadNextScene(_gameScene, true);
            else
            {
                int lvlId = SaveManager.Instance.LoadLevelId();
                if (lvlId >= 0)
                    GameLoader.Instance.LoadNextScene(LevelManager.Instance.GetLevel(lvlId).Scene, true);
                else
                {
                    _location = Location.Lobby;
                    GameLoader.Instance.LoadNextScene(_lobbyScene, true);
                }    
                    
            }
           
            SaveManager.Instance.SetSave(true);
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

    public void LoadLocation(Location location, string sceneName = null)
    {
        if (location == Location.Game)
        {
            GameLoader.Instance.LoadNextScene(sceneName != null ? sceneName : _gameScene, true);
            _location = Location.Game;
        }
        else if (location == Location.Lobby)
        {
            GameLoader.Instance.LoadNextScene(_lobbyScene, true);
            _location = Location.Lobby;
        }
    }

}
