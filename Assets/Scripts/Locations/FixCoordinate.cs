using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FixCoordinate : MonoBehaviour
{
    public static FixCoordinate Instance;
    [SerializeField] private GameObject _startLocation;
    private float _playerAddPos;
    private float _boardAddPos;
    private List<GameObject> _gameObjects = new List<GameObject>();
    private GameObject _player;

    public float PlayerAddPos
    {
        get { return _playerAddPos; }
        set 
        {
            _playerAddPos = value;
            SaveManager.Instance.SavePlayerFixPos(PlayerAddPos);
        }
    }
    public float BoardAddPos 
    { 
        get { return _boardAddPos; } 
        set 
        {
            _boardAddPos = value;
            SaveManager.Instance.SaveBoardFixPos(BoardAddPos);
        } 
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }
        _playerAddPos = SaveManager.Instance.LoadPlayerFixPos();
        _boardAddPos = SaveManager.Instance.LoadBoardFixPos();
        if (_playerAddPos > 0 || _boardAddPos > 0)
        {
            if (_startLocation)
                _startLocation.SetActive(false);
        }
    }
    public void FixPosition()
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go == this.gameObject) continue;
            _gameObjects.Add(go);
            // если у объекта нет родителя и он не игрок
            if (go.transform.parent == null)
                go.transform.SetParent(this.transform, true);
        }
        if (PlayerManager.Instance.gameObject.scene.name == "DontDestroyOnLoad")
        {
            _player = PlayerManager.Instance.gameObject;
            _player.transform.SetParent(this.transform, true);
        }
        PlayerAddPos += PlayerManager.Instance.gameObject.transform.position.z;
        BoardAddPos += BoardController.Instance.gameObject.transform.position.z;
        transform.position += Vector3.back * BoardController.Instance.gameObject.transform.position.z;
        if (_gameObjects.Count > 0)
        {
            foreach(GameObject go in _gameObjects)
            {
                go.transform.SetParent(null);
            }
            _gameObjects.Clear();
        }
        if (_player)
        {
            DontDestroyOnLoad(_player);
            _player = null;
        }
        if (PlayerMovement.Instance.GetComponentInParent<BoardController>())
        {
            //BoardController.Instance.GetComponentInChildren<DriverSeatTrigger>().FixPosition();
            //Debug.Log(_playerAddPos);
        }

    }
}
