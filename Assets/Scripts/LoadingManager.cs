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
        // Explicit exit from the isolated first-run prototype boots normal services before Lobby.
        if (DeadBoat.FirstRun.FirstRunPrototype.ConsumeLobbyReturn())
        {
            _location = Location.Lobby;
            GameLoader.Instance.LoadNextScene(_lobbyScene, true, false);
            return;
        }
        if (DeadBoat.FirstRun.FirstRunSession.TryBoot(SaveManager.Instance, DeadBoat.Online.SharedRunRecovery.HasPending))
        {
            _location = Location.Game;
            GameLoader.Instance.LoadNextScene(DeadBoat.FirstRun.FirstRunSession.SceneName, true, false);
            return;
        }
        bool haveSave = SaveManager.Instance.LoadGameProgress().Item1 >= 0;
        if (DeadBoat.Online.SharedRunRecovery.HasPending)
        {
            _ = DeadBoat.Online.SharedRunRecovery.Instance;
            _location = Location.Lobby;
            GameLoader.Instance.LoadNextScene(_lobbyScene, true);
            return;
        }
        DeadBoat.Online.SoloRunBranch.RestoreStartup();
        haveSave = SaveManager.Instance.LoadGameProgress().Item1 >= 0;
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
        if (GameLoader.Instance != null)
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
        if (DeadBoat.FirstRun.FirstRunSession.Active && location == Location.Game && sceneName != DeadBoat.FirstRun.FirstRunSession.SceneName)
        {
            LoadLocation(Location.Lobby, null, false);
            return;
        }
        if (location == Location.Lobby && DeadBoat.FirstRun.FirstRunSession.Active && GameManager.Instance != null && !GameManager.Instance.isEndGame)
        {
            GameManager.Instance.EndGame(true, false);
            return;
        }
        if (location == Location.Lobby && DeadBoat.FirstRun.FirstRunSession.Active)
        {
            DeadBoat.FirstRun.FirstRunSession.Close(false);
            DeadBoat.FirstRun.FirstRunSession.End();
        }
        if (location == Location.Game && sceneName == DeadBoat.FirstRun.FirstRunSession.SceneName && !DeadBoat.FirstRun.FirstRunSession.Active)
        {
#if UNITY_EDITOR
            bool isolatedPreview = SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave && !DeadBoat.FirstRun.FirstRunProfile.IsTemporary;
#else
            bool isolatedPreview = false;
#endif
            if (!isolatedPreview) { LoadLocation(Location.Lobby, null, false); return; }
        }
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
            DeploymentType deployment = MirraSDK.Platform.Deployment;
            PlatformType platform = MirraSDK.Platform.Current;
            return deployment != DeploymentType.Editor &&
                   platform != PlatformType.Editor &&
                   platform != PlatformType.Localhost &&
                   platform != PlatformType.Unknown &&
                   platform != PlatformType.Playgama &&
                   platform != PlatformType.PlaygamaBridge;
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
