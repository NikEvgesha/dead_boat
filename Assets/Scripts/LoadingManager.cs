using MirraGames.SDK;
using MirraGames.SDK.Common;
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
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("LoadingManager already exists. Removing duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameLoader.Instance.OnSceneLoaded += OnSceneLoaded;
        MirraSDK.WaitForProviders(static () => {
            LoadingManager.Instance.StartGame();
        });
    }

    private void StartGame()
    {
        GameLoader.Instance.StartAfterSDK();
        bool haveSave = SaveManager.Instance.LoadGameProgress().Item1 >= 0;
        if (SaveManager.Instance.IsNewPlayer || haveSave)
        {
            _location = Location.Game;
            if (!haveSave)
                GameLoader.Instance.LoadNextScene(_gameScene, true);
            else
            {
                int lvlId = SaveManager.Instance.LoadLevelId();
                if (LevelManager.Instance != null && LevelManager.Instance.TryGetLevel(lvlId, out LevelData level))
                    GameLoader.Instance.LoadNextScene(level.Scene, true);
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
        TryGameplayStart();
    }

    public void LoadLocation(Location location, string sceneName = null, bool withAds = true)
    {
        TryGameplayStop();

        if (location == Location.Game)
        {
            GameLoader.Instance.LoadNextScene(sceneName != null ? sceneName : _gameScene, true, withAds);
            _location = Location.Game;
        }
        else if (location == Location.Lobby)
        {
            GameLoader.Instance.LoadNextScene(_lobbyScene, true, withAds);
            _location = Location.Lobby;
        }
    }

    private bool IsGameplayAnalyticsEnabled()
    {
        if (!MirraSDK.IsInitialized)
            return false;

        try
        {
            PlatformType platform = MirraSDK.Platform.Current;
            return platform != PlatformType.Playgama && platform != PlatformType.PlaygamaBridge;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LoadingManager: gameplay analytics disabled ({exception.Message})");
            return false;
        }
    }

    private void TryGameplayStart()
    {
        if (!IsGameplayAnalyticsEnabled()) return;

        try
        {
            MirraSDK.Analytics.GameplayStart();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LoadingManager: failed to send GameplayStart ({exception.Message})");
        }
    }

    private void TryGameplayStop()
    {
        if (!IsGameplayAnalyticsEnabled()) return;

        try
        {
            MirraSDK.Analytics.GameplayStop();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LoadingManager: failed to send GameplayStop ({exception.Message})");
        }
    }
}
