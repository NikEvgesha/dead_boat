using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MirraGames.SDK;
using System;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    [SerializeField] private SaveProvider saveProvider; // Назначаем в инспекторе нужный провайдер (YG2SaveProvider, DebugSaveProvider и т.д.)
    [SerializeField] private bool _newPlayer;
    public bool IsNewPlayer => saveProvider.CheckProgress() == false;

    private void Awake()
    {

        if (_newPlayer)
        {
            MirraSDK.Data.DeleteAll();
        }

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

    public void SetSave(bool haveSave)
    {
        saveProvider.SetSave(haveSave);
    }

    // Пример методов, которые делегируют работу провайдеру:
    public float[] GetVolume()
    {
        return saveProvider.LoadVolume();
    }
    public void SaveQuestProgress(int step = 0)
    {
        saveProvider.SaveQuestProgress(step);
    }
    public int LoadQuestProgress() 
    {
        return saveProvider.LoadQuestProgress();
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


    public void SaveSensivity(float sens)
    {
        saveProvider.SaveSensivity(sens);
    }

    public float LoadSensivity()
    {
        return saveProvider.LoadSensivity();
    }

    public void SaveScore(float score, int levelId)
    {
        saveProvider.SaveScore(score, levelId);
    }

    public float GetLevelScore(int levelId)
    {
        return saveProvider.LoadScore(levelId);
    }

    public bool GetTutorialProgress()
    {
        return saveProvider.GetTutorialProgress();
    }
    public void SaveTutorialProgress(bool endTutorial)
    {
        saveProvider.SaveTutorialProgress(endTutorial);
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


    public void SaveGameProgress(int distance = -1, List<PickableItem> items = null, int lvlId = -1)
    { 
        saveProvider.SaveDistance(distance);
        saveProvider.SaveLevelId(lvlId);
        if (items != null)
            saveProvider.SaveInventory(items);

        //Debug.Log("Progress Saved");
       
    }
    public void SaveBoardItem(List<PickableItem> items = null)
    {
            if (items != null)
                saveProvider.SaveBoardItem(items);
        //Debug.Log(items.Count);
    }
    public void SavePlayerStats(int coin, float hp)
    {
        saveProvider.SavePlayerStats(coin, hp);
    }
    public void SaveGameCoin(int coin)
    {
        saveProvider.SaveGameCoin(coin);
    }
    public void SavePlayerHealth(float health)
    {
        saveProvider.SavePlayerHealth(health);
    }
    public int LoadGameCoin()
    {
        return saveProvider.LoadGameCoin();
    }
    public float LoadPlayerHealth()
    {
        return saveProvider.LoadPlayerHealth();
    }
    public (int, float) LoadPlayerStats()
    {
        return saveProvider.LoadPlayerStats();
    }
    public void ResetGameProgress() => SaveGameProgress();

    public (int, List<string>) LoadGameProgress()
    {
        return (saveProvider.LoadDistance(), saveProvider.LoadInventory());
    }
    public List<SavedItem> LoadBoardItem()
    {
        return saveProvider.LoadBoardItem();
    }

    public int LoadLevelId() {
        return saveProvider.LoadLevelId();
    }


    public void SaveFuel(int fuel)
    {
        saveProvider.SaveFuel(fuel);
    }
    public int LoadFuel()
    {
        return saveProvider.LoadFuel();
    }

    public void SaveAmmo(WeaponType type, int amount) {
        saveProvider.SaveAmmo(type, amount);
    }

    public int LoadAmmo(WeaponType type) {
        return saveProvider.LoadAmmo(type);
    }

    public void SaveWin()
    {
        int wins = saveProvider.LoadWins();
        saveProvider.SaveWins(wins+1);
        LeaderboardManager.Instance.SaveScore(LBName.wins.ToString(), wins+1);
    }

    public void SaveRouletteDate(DateTime date)
    {
        saveProvider.SaveRouletteDate(date);
    }

    public DateTime LoadRouletteDate()
    {
        return saveProvider.LoadRouletteDate();
    }

    /*    public void SaveAttachedItem(string id)
        {
            saveProvider.SaveAttachedItem(id);
        }

        public List<string> LoadAttachedItems()
        {
            return saveProvider.LoadAttachedItems();
        }

        public void ResetAttachedItems()
        {
            saveProvider.ResetAttachedItems();
        }

        public void DeleteAttachedItem(string id)
        {
            List<string> current = saveProvider.LoadAttachedItems();
            current.Remove(id);
            saveProvider.SaveAllAttachedItems(current);
        }*/

    /*    public void SaveInventory()
        {
            saveProvider.SaveInventory(Inventory.Instance.GetInventoryList());
        }*/

    // Остальные методы аналогично делегируют работу провайдеру...
}
