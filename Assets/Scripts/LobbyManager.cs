using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private string _sceneName;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private PlayerManager _playerPrefab;
    [SerializeField] private GameObject _loadingCollection;

    private static LobbyManager _instance;
    public static LobbyManager Instance { get { return _instance; } private set { } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (!PlayerManager.Instance)
            {
                Instantiate(_loadingCollection);
            }
                

        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SaveManager.Instance.SaveGameProgress(-1, Inventory.Instance.GetItems());
        PlayerMovement.Instance.Teleport(_playerSpawnPoint);
        Inventory.Instance.SetLoadedInventory();
        //SaveManager.Instance.SaveGameProgress(-1, Inventory.Instance.GetItems());
        //PlayerManager.Instance.gameObject.transform.position = _playerSpawnPoint.position;
    }


    public void StartGame()
    {
        ControlManager.Instance.CursorActive = false;
        LoadingManager.Instance.LoadLocation(Location.Game);
    }
}
