using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private string _sceneName;


    private static LobbyManager _instance;
    public static LobbyManager Instance { get { return _instance; } private set { } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void StartGame()
    {
        GameLoader.Instance.LoadNextScene(_sceneName, true);
    }
}
