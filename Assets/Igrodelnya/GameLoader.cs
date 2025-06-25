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

    public static GameLoader Instance { get { return _instance; } }
    public Action OnSceneLoaded;


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("LocalizationManager уже существует! Удаляем дубликат.");
            Destroy(gameObject);
        }
        
        

    }


    /*    private void Start()
        {
            _currentSceneName = _gameOptions.LobbySceneName;
            SceneManager.LoadScene(_currentSceneName);
        }*/

    public void LoadNextScene(string SceneName, bool asyncMode)
    {
        _currentSceneName = SceneName;

        if (asyncMode)
        {
            AdsManager.Instance.ShowInterstitialAd();
            _loadingImage.SetActive(true);
            StartCoroutine("SceneLoad", _currentSceneName);
            //AdsManager.Instance.ShowInterstitialAd();
        } else
        {
            SceneManager.LoadScene(_currentSceneName);
        }

        
    }


    private IEnumerator SceneLoad(string sceneName)
    {
        float loadingProgress;
        _asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        while (_asyncOperation.progress < 0.95f)
        {
            loadingProgress = Mathf.Clamp01(_asyncOperation.progress / 0.95f);
            yield return true;
        }
        BoardController board = FindAnyObjectByType<BoardController>();
        if (board != null)
        {
            while (!board.StartGame)
            {
                yield return null;
            }
        }

        if (!PauseManager.Instance.IsInitialize)
            PauseManager.Instance.StartInitialize();

        _loadingImage.SetActive(false);
        //AdsManager.Instance.ShowInterstitialAd();
        OnSceneLoaded?.Invoke();
    }


}
