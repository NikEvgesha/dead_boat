using UnityEngine;
using UnityEngine.UI;

public class GameProgressBar : MonoBehaviour
{
    [SerializeField] private GameObject _castlePointPrefab;
    [SerializeField] private Transform _castlePointsParent;
    [SerializeField] private Slider _progressBar;

    private float _totalDistance;
    private float _currentDistance;
    private float _barWidth;
    private BoardController _board;
    private CitySpawner _citySpawner;
    private int _cityAmount;



    private void Start()
    {
        _totalDistance = GameManager.Instance.PlayDistance;
        _currentDistance = 0;
        _board = FindObjectOfType<BoardController>();
        _citySpawner = FindObjectOfType<CitySpawner>();
        _cityAmount = _citySpawner.Cities+1;

        for (int i = 0; i < _cityAmount; i++) {
            Instantiate(_castlePointPrefab, _castlePointsParent);
        }

        _board.SwitchDistance += UpdateProgress;


    }


    private void UpdateProgress(float distance) {
        _currentDistance = distance;

        _progressBar.value = _currentDistance / _totalDistance;
        //_progressBar.fillAmount = _currentDistance / _totalDistance;

    }
}
