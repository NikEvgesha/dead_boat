using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } private set { } }


    [SerializeField] private PlayerStatsManager _player;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private int _reward;
    [SerializeField] private GameObject _startQuest;
    public PlayerStatsManager Player { get { return _player; } }

    public bool isEndGame = false;

    public Action GameStart;
    public Action<int> GameResume;

    [Header("Дистанция всей игры")]
    public float PlayDistance = 100000f;

    private bool _pause;
    private PlayerAmmoManager _ammoManager;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            isEndGame = false;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        isEndGame = true;
    }
    private void Start()
    {
        if (_player == null)
        {
            _player = PlayerManager.Instance.GetComponent<PlayerStatsManager>();
            //PlayerMovement.Instance.Teleport(_playerSpawnPoint);
        }
        _ammoManager = _player.GetComponent<PlayerAmmoManager>();

        (int, List<string>) loadedData = SaveManager.Instance.LoadGameProgress();

        Debug.Log(String.Format("LOADED DATA: {0},  {1} items", loadedData.Item1, loadedData.Item2.Count));

        //Inventory.Instance.ResetInventory();
        if (loadedData.Item1 >= 0)
        {
            Inventory.Instance.SetLoadedInventory(loadedData.Item2);
            GameResume?.Invoke(loadedData.Item1);
        } else
        {
            string scene = SceneManager.GetActiveScene().name;
            Inventory.Instance.SetLoadedInventory();
            SaveManager.Instance.SaveGameProgress(0, Inventory.Instance.GetInventoryList(), LevelManager.Instance.GetLevelId(scene));
        }
        _ammoManager.Save();
        GameStart?.Invoke();

        SaveManager.Instance.ResetLobbyItems();

        //StartQuest();
    }
    private void StartQuest()
    {
        if (_startQuest != null)
            QuestManager.Instance.StartQuest();
        else
            Debug.LogWarning("Не найден prefab Quest_Kill5Rats_Prefab");
    }

    public void EndGame(bool lobby)
    {
        PlayerManager.Instance.gameObject.transform.SetParent(null);
        DontDestroyOnLoad(PlayerManager.Instance.gameObject);
        isEndGame = true;
        Location location = lobby ? Location.Lobby : Location.Game;
        PauseManager.Instance.SetPause(false);
        PlayerManager.Instance.transform.parent = null;
        Inventory.Instance.ResetInventory();
        CurrencyManager.Instance.Reset();
        PlayerStatsManager.Instance.Revive();
        PlayerInput.Instance.SitTrain(false);
        //SaveManager.Instance.ResetAttachedItems();
        SaveManager.Instance.SaveGameProgress(-1, Inventory.Instance.GetItems());
        SaveManager.Instance.SaveBoardItem(new List<PickableItem>());
        SaveManager.Instance.SaveFuel(0);
        SaveManager.Instance.SaveQuestProgress();
        _ammoManager.ResetAmmo();
        LoadingManager.Instance.LoadLocation(location);
    }


    public void AddReward()
    {
        CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, _reward);
    }
}
