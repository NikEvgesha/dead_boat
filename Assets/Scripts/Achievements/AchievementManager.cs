using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Scripting;

public class AchievementManager : MonoBehaviour
{
    private static AchievementManager _instance;
    public static AchievementManager Instance => _instance;

    [SerializeField] private AchievementPanel _panel;
    [SerializeField] private AchievementsList _achievements;
    [SerializeField] private List<AchievementType> achievementTypes;
    [SerializeField] private bool _resetProgress;

    private Dictionary<AchievementType, int> progress;

    private bool updated;

    void Awake()
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
        _panel = FindObjectOfType<AchievementPanel>();
        LoadAchievements();
        StartCoroutine(UpdateAchievements());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }


    private void LoadAchievements()
    {
        progress = new();
        foreach (AchievementType type in achievementTypes) {
            progress.Add(type, _resetProgress ? 0 : SaveManager.Instance.GetAchievementTypeProgress(type));
            Debug.Log(type.ToString() + ": " + progress[type]);
        }

        foreach (Achievement achievement in _achievements.Achievements)
        {
                achievement.StartUnlock(achievement.requirement <= progress[achievement.type]);
                Debug.Log(achievement.id + ": " + achievement.Unlocked);
        }
    }

    public void UpdateData(AchievementType type, int data = 0)
    {
        if (data == 0)
        {
            progress[type]++;
            updated = true;
        } else if (progress[type] != data)
        {
            switch (type)
            {
                case AchievementType.TotalDistance:
                    {
                        progress[type] += data;
                        break;
                    }
                default:
                    {
                        progress[type] = data;
                        break;
                    }
            }
            
            updated = true;
        }
        
    }

    private IEnumerator UpdateAchievements()
    {
        while (true)
        {
            if (updated)
            {
                foreach (var achievement in _achievements.Achievements)
                {
                        if (!achievement.Unlocked && progress[achievement.type] >= achievement.requirement)
                        {
                            achievement.Unlock();
                            _panel.ShowAchievement(achievement);
                        }

                        SaveManager.Instance.SaveAchiementTypeProgress(achievement.type, progress[achievement.type]);
                }
                updated = false;
            }
            
            yield return new WaitForSeconds(2);
        }
        
        
    }


    public bool CheckAchievementProgress(Achievement achievement)
    {
        if (achievement == null) 
            return true;
        return (progress[achievement.type] >= achievement.requirement);
    }

    public int GetCurrentProgress(AchievementType type)
    {
        return progress[type];
    }

}
