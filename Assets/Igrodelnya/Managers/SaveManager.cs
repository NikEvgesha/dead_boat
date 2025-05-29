using UnityEngine;
using System.Collections;
using MirraGames.SDK.Common;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    [SerializeField] private SaveProvider saveProvider; // Назначаем в инспекторе нужный провайдер (YG2SaveProvider, DebugSaveProvider и т.д.)
    public bool IsNewPlayer => saveProvider.CheckProgress() == false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
            saveProvider.Initialize();
            StartCoroutine(ProgressSavingRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ProgressSavingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            saveProvider.SaveProgress();
        }
    }

    // Пример методов, которые делегируют работу провайдеру:
    public float[] GetVolume()
    {
        return saveProvider.LoadVolume();
    }

    public void SaveMusicVolume(float volume)
    {
        var volumes = saveProvider.LoadVolume();
        saveProvider.SaveVolume(volume, volumes[1]);
    }

    public void SaveSoundVolume(float volume)
    {
        var volumes = saveProvider.LoadVolume();
        saveProvider.SaveVolume(volumes[0], volume);
    }

    public void SaveScore(float score, int levelId)
    {
        saveProvider.SaveScore(score, levelId);
    }

    public float GetLevelScore(int levelId)
    {
        return saveProvider.LoadScore(levelId);
    }


    public void SaveGems(int amount)
    {
        saveProvider.SaveGems(amount);
        LeaderboardManager.Instance.SaveScore(LBName.gems.ToString(), amount);
    }

    public int GetGems()
    {
        return saveProvider.LoadGems();
    }


    public void SaveAchiementTypeProgress(AchievementType achievementType, int progress)
    {
        saveProvider.SaveAchievementProgress(achievementType, progress);
    }

    public int GetAchievementTypeProgress(AchievementType achievementType)
    {
        return saveProvider.LoadAchievementProgress(achievementType);
    }

    public void SaveAchiementStatus(string achievementID, bool progress)
    {
        saveProvider.SaveAchievementStatus(achievementID, progress);
    }

    public bool GetAchievementStatus(string achievementID)
    {
        return saveProvider.LoadAchievementStatus(achievementID);
    }

    public void SaveLevelStatus(string lvlName, bool unlocked)
    {
        saveProvider.SaveLevelStatus("LevelStatus_" + lvlName, unlocked);
    }

    public bool GetLevelStatus(string lvlName)
    {
        return saveProvider.LoadLevelStatus("LevelStatus_" + lvlName);
    }

    public void SaveLobbyItem(string id)
    {
        saveProvider.SaveLobbyItem(id);
    }

    public List<string> LoadLobbyItems()
    {
        return saveProvider.LoadLobbyItems();
    }

    public void ResetLobbyItems()
    {
        saveProvider.ResetLobbyItems();
    }


    public void SaveGameProgress(int distance = -1, List<PickableItem> items = null)
    { 
        saveProvider.SaveDistance(distance);
        if (items != null)
            saveProvider.SaveInventory(items);
       
    }

    public void ResetGameProgress() => SaveGameProgress();

    public (int, List<string>) LoadGameProgress()
    {
        return (saveProvider.LoadDistance(), saveProvider.LoadInventory());
    }

    // Остальные методы аналогично делегируют работу провайдеру...
}
