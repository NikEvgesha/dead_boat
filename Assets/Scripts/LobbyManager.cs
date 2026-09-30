using System.Collections.Generic;
using System.Collections;
using DeadBoat.Online;
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
        PlayerMovement.Instance.Teleport(_playerSpawnPoint);
        Inventory.Instance.SetLoadedInventory(null, false);
        // Entering the lobby (including an online connection failure) must not
        // discard an unfinished solo run. GameManager.EndGame clears it explicitly.
        //PlayerManager.Instance.gameObject.transform.position = _playerSpawnPoint.position;
    }


    public void StartGame()
    {
        StartCoroutine(StartSoloGame());
    }

    private IEnumerator StartSoloGame()
    {
        LobbyOnlineBootstrap online = FindFirstObjectByType<LobbyOnlineBootstrap>();
        if (online != null)
        {
            var disconnect = online.DisconnectForSoloRunAsync();
            while (!disconnect.IsCompleted)
                yield return null;
            if (disconnect.IsFaulted)
                Debug.LogException(disconnect.Exception);
        }

        ControlManager.Instance.ForceGameplayCursor();
        // The player explicitly starts a new solo run here. Merely entering
        // the lobby must leave any unfinished saved run intact.
        SaveManager.Instance.SaveGameProgress(-1, new List<PickableItem>());
        LoadingManager.Instance.LoadLocation(Location.Game);
    }
}
