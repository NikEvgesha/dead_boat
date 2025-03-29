using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour
{

    //[SerializeField] private GameOptions _gameOptions;
    private string _currentSceneName;
    private static GameLoader _instance;
    private AsyncOperation _asyncOperation;

    public static GameLoader Instance { get { return _instance; } }


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
            StartCoroutine("SceneLoad", _currentSceneName);
        } else
        {
            SceneManager.LoadScene(_currentSceneName);
        }

        
    }


    private IEnumerator SceneLoad(string sceneName)
    {
        float loadingProgress;
        _asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        while (_asyncOperation.progress < 0.9f)
        {
            loadingProgress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);
            yield return true;
        }
    }


}
