using System.Collections.Generic;
using UnityEngine;

public class LevelMapUI : MonoBehaviour
{
    [SerializeField] private GameObject _lockedLevelWindow;
    [SerializeField] private LevelMapElement _levelPrefab;

    private void Start()
    {
        LevelManager.Instance.SetMapUI(this);
    }

    public void Init(List<LevelData> levels)
    {
       foreach (LevelData lvlData in levels)
        {
            LevelMapElement lvl = Instantiate(_levelPrefab, transform);
            lvl.Init(lvlData, this);
        }
    }
}
