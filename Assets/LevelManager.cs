using System.Collections.ObjectModel;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelList _levels;
    [SerializeField] private bool _resetProgress;


    private LevelMapUI _mapUI;
    private static LevelManager _instance;
    public static LevelManager Instance { get { return _instance; } }


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


    private void Start()
    {
        foreach (LevelData lvl in _levels.Levels)
        {
            lvl.SetUnlock(_resetProgress ? false : (lvl.Requirement == null || SaveManager.Instance.GetLevelStatus(lvl.Name)), false);
        }
    }

    public void SetMapUI(LevelMapUI ui)
    {
        _mapUI = ui;

        foreach (LevelData lvl in _levels.Levels)
        {
            if (!lvl.Unlocked) 
                lvl.SetUnlock(AchievementManager.Instance.CheckAchievementProgress(lvl.Requirement));
        }

            _mapUI.Init(_levels.Levels);
    }


}
