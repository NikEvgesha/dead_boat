using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour
{

    [SerializeField] private GameObject _loadingImage;
    private string _currentSceneName;
    private static GameLoader _instance;
    private AsyncOperation _asyncOperation;

    private bool _startLoadingFinished = false;

    public static GameLoader Instance { get { return _instance; } }
    public Action OnSceneLoaded;
    public Action<string> OnLoadFailed;


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("LocalizationManager уже существует! Удаляем дубликат.");
            Destroy(gameObject);
        }
        
        

    }


    public void StartAfterSDK()
    {
        //_currentSceneName = _gameOptions.LobbySceneName;
        //SceneManager.LoadScene(_currentSceneName);
    }

    public void LoadNextScene(string SceneName, bool asyncMode,bool withAds = true)
    {
        _currentSceneName = SceneName;

        if (asyncMode)
        {
            ShowLoadingScreen();
            if (_startLoadingFinished && withAds)
                AdsManager.Instance.ShowInterstitialAd();
            else
                _startLoadingFinished = true;

            StartCoroutine("SceneLoad", _currentSceneName);
            //AdsManager.Instance.ShowInterstitialAd();
        } else
        {
            SceneManager.LoadScene(_currentSceneName);
        }

        
    }


    public void ShowLoadingScreen()
    {
        if (_loadingImage == null)
            return;

        Canvas canvas = _loadingImage.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 200);
        }

        _loadingImage.SetActive(true);
        LoadingProgressBarUI.Instance?.Progress(0f);
    }

    public void HideLoadingScreen()
    {
        if (_loadingImage != null)
            _loadingImage.SetActive(false);
    }

    private IEnumerator SceneLoad(string sceneName)
    {
        // Render the loading screen before starting the costly scene operation on WebGL.
        yield return null;

        double loadStarted = Time.realtimeSinceStartupAsDouble;
        Debug.Log("[Scene load] Begin " + sceneName);
        Exception loadError = null;
        try
        {
            _asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (Exception exception)
        {
            loadError = exception;
        }

        if (loadError != null)
            Debug.LogException(loadError);

        if (loadError != null || _asyncOperation == null)
        {
            HideLoadingScreen();
            OnLoadFailed?.Invoke("Could not open this level. Please try again.");
            yield break;
        }
        while (!_asyncOperation.isDone)
        {
            LoadingProgressBarUI.Instance?.Progress(_asyncOperation.progress);
            yield return true;
        }
        LoadingProgressBarUI.Instance?.Progress(1f);
        Debug.Log("[Scene load] Activated " + sceneName + "; timeScale=" + Time.timeScale);
        BoardController board = FindAnyObjectByType<BoardController>();
        if (board != null)
        {
            double nextReport = Time.realtimeSinceStartupAsDouble + 10;
            while (board != null && !board.StartGame)
            {
                if (Time.realtimeSinceStartupAsDouble >= nextReport)
                {
                    nextReport += 10;
                    Debug.LogWarning("[Scene load] Waiting for boat: " + sceneName + "; timeScale=" + Time.timeScale + "; elapsed=" + (Time.realtimeSinceStartupAsDouble - loadStarted).ToString("F1"));
                }
                yield return null;
            }
        }

        if (!PauseManager.Instance.IsInitialize)
            PauseManager.Instance.StartInitialize();

        HideLoadingScreen();
        //AdsManager.Instance.ShowInterstitialAd();
        Debug.Log("[Scene load] Ready " + sceneName);
        OnSceneLoaded?.Invoke();
    }


}
