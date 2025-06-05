using System.Collections.ObjectModel;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelList _levels;
    [SerializeField] private bool _resetProgress;


    private LevelMapUI _mapUI;
    private static LevelManager _instance;
    public static LevelManager Instance { get { return _instance; } }

    private LevelData _currentLevel;


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

    public int GetLevelId(LevelData lvl)
    {
        return _levels.Levels.IndexOf(lvl);
    }

    public int GetLevelId(string sceneName)
    {
        for (int i = 0; i < _levels.Levels.Count; i++)
        {
            if (_levels.Levels[i].Scene == sceneName)
            {
                return i;
            }
        }
        return 0;
        
    }

    public LevelData GetLevel(int id)
    {
        return _levels.Levels[id];
    }

    public LevelData GetLevel(string sceneName)
    {
        return _levels.Levels.Find(x => x.Scene == sceneName);
    }


}
