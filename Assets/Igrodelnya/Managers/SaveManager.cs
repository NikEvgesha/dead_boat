using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MirraGames.SDK;
using System;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;
    public SaveProvider PersistentProvider => saveProvider;
    private SaveProvider ActiveProvider => DeadBoat.Online.SharedRunContext.Save != null
        ? DeadBoat.Online.SharedRunContext.Save : saveProvider;

    [SerializeField] private SaveProvider saveProvider; // Назначаем в инспекторе нужный провайдер (YG2SaveProvider, DebugSaveProvider и т.д.)
    [SerializeField] private bool _newPlayer;
    public bool IsNewPlayer => ActiveProvider.CheckProgress() == false;

    private void Awake()
    {

        if (_newPlayer)
        {
            MirraSDK.WaitForProviders(() =>
            {
                MirraSDK.Data.DeleteAll();
            });
        }

        if (_instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
            ActiveProvider.Initialize();
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
            ActiveProvider.SaveProgress();
        }
    }

    public void SetSave(bool haveSave)
    {
        ActiveProvider.SetSave(haveSave);
    }

    public void FlushProgress()
    {
        ActiveProvider.SaveProgress();
    }

    // Пример методов, которые делегируют работу провайдеру:
    public float[] GetVolume()
    {
        return ActiveProvider.LoadVolume();
    }
    public void SaveQuestProgress(int step = 0)
    {
        ActiveProvider.SaveQuestProgress(step);
    }
    public int LoadQuestProgress() 
    {
        return ActiveProvider.LoadQuestProgress();
    }
    public void SaveMusicVolume(float volume)
    {
        var volumes = ActiveProvider.LoadVolume();
        ActiveProvider.SaveVolume(volume, volumes[1]);
    }

    public void SaveSoundVolume(float volume)
    {
        var volumes = ActiveProvider.LoadVolume();
        ActiveProvider.SaveVolume(volumes[0], volume);
    }


    public void SaveSensivity(float sens)
    {
        ActiveProvider.SaveSensivity(sens);
    }

    public float LoadSensivity()
    {
        return ActiveProvider.LoadSensivity();
    }

    public void SaveScore(float score, int levelId)
    {
        ActiveProvider.SaveScore(score, levelId);
    }

    public float GetLevelScore(int levelId)
    {
        return ActiveProvider.LoadScore(levelId);
    }

    public bool GetTutorialProgress()
    {
        return ActiveProvider.GetTutorialProgress();
    }
    public void SaveTutorialProgress(bool endTutorial)
    {
        ActiveProvider.SaveTutorialProgress(endTutorial);
    }
    public void SaveGems(int amount)
    {
        ActiveProvider.SaveGems(amount);
        if (!DeadBoat.Online.SharedRunContext.Active && LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.SaveScore(LBName.gems.ToString(), amount);
    }

    public int GetGems()
    {
        return ActiveProvider.LoadGems();
    }


    public void SaveAchiementTypeProgress(AchievementType achievementType, int progress)
    {
        ActiveProvider.SaveAchievementProgress(achievementType, progress);
    }

    public int GetAchievementTypeProgress(AchievementType achievementType)
    {
        return ActiveProvider.LoadAchievementProgress(achievementType);
    }

    public void SaveAchiementStatus(string achievementID, bool progress)
    {
        ActiveProvider.SaveAchievementStatus(achievementID, progress);
    }

    public bool GetAchievementStatus(string achievementID)
    {
        return ActiveProvider.LoadAchievementStatus(achievementID);
    }

    public void SaveLevelStatus(string lvlName, bool unlocked)
    {
        ActiveProvider.SaveLevelStatus("LevelStatus_" + lvlName, unlocked);
    }

    public bool GetLevelStatus(string lvlName)
    {
        return ActiveProvider.LoadLevelStatus("LevelStatus_" + lvlName);
    }

    public void SaveLobbyItem(string id)
    {
        ActiveProvider.SaveLobbyItem(id);
    }

    public List<string> LoadLobbyItems()
    {
        return ActiveProvider.LoadLobbyItems();
    }

    public void ResetLobbyItems()
    {
        ActiveProvider.ResetLobbyItems();
    }


    public void SaveGameProgress(int distance = -1, List<PickableItem> items = null, int lvlId = -1)
    { 
        ActiveProvider.SaveDistance(distance);
        ActiveProvider.SaveLevelId(lvlId);
        if (items != null)
            ActiveProvider.SaveInventory(items);

        //Debug.Log("Progress Saved");
       
    }
    public void SaveBoardItem(List<PickableItem> items = null)
    {
            if (items != null)
                ActiveProvider.SaveBoardItem(items);
        //Debug.Log(items.Count);
    }
    public void SavePlayerStats(int coin, float hp)
    {
        ActiveProvider.SavePlayerStats(coin, hp);
    }
    public void SaveGameCoin(int coin)
    {
        ActiveProvider.SaveGameCoin(coin);
    }
    public int LoadGameCoin()
    {
        return ActiveProvider.LoadGameCoin();
    }
    public void SavePlayerHealth(float health)
    {
        ActiveProvider.SavePlayerHealth(health);
    }
    public float LoadPlayerHealth()
    {
        return ActiveProvider.LoadPlayerHealth();
    }
    public void SavePlayerExperience(int exp , int level)
    {
        ActiveProvider.SavePlayerExperience(exp, level);
    }
    public (int, int) LoadPlayerExperience()
    {
        return ActiveProvider.LoadPlayerExperience();
    }
    public (int, float) LoadPlayerStats()
    {
        return ActiveProvider.LoadPlayerStats();
    }
    public void ResetGameProgress() => SaveGameProgress();

    public (int, List<string>) LoadGameProgress()
    {
        return (ActiveProvider.LoadDistance(), ActiveProvider.LoadInventory());
    }
    public List<SavedItem> LoadBoardItem()
    {
        return ActiveProvider.LoadBoardItem();
    }

    public int LoadLevelId() {
        return ActiveProvider.LoadLevelId();
    }


    public void SaveFuel(int fuel)
    {
        ActiveProvider.SaveFuel(fuel);
    }
    public int LoadFuel()
    {
        return ActiveProvider.LoadFuel();
    }

    public void SaveAmmo(WeaponType type, int amount) {
        ActiveProvider.SaveAmmo(type, amount);
    }

    public int LoadAmmo(WeaponType type) {
        return ActiveProvider.LoadAmmo(type);
    }

    public void SaveWin()
    {
        int wins = ActiveProvider.LoadWins();
        ActiveProvider.SaveWins(wins+1);
        if (!DeadBoat.Online.SharedRunContext.Active && LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.SaveScore(LBName.wins.ToString(), wins+1);
    }

    public void SaveRouletteDate(DateTime date)
    {
        ActiveProvider.SaveRouletteDate(date);
    }

    public DateTime LoadRouletteDate()
    {
        return ActiveProvider.LoadRouletteDate();
    }
    public void SavePlayerFixPos(float posZ)
    {
        ActiveProvider.SavePlayerFixPos(posZ);
    }

    public float LoadPlayerFixPos()
    {
        return ActiveProvider.LoadPlayerFixPos();
    }
    public void SaveBoardFixPos(float posZ)
    {
        ActiveProvider.SaveBoardFixPos(posZ);
    }
    public float LoadBoardFixPos()
    {
        return ActiveProvider.LoadBoardFixPos();
    }
    public void SaveLevelUpdate(Stats stats)
    {
        ActiveProvider.SaveLevelUpdate(stats);
    }
    public Stats LoadLevelUpdate()
    {
        return ActiveProvider.LoadLevelUpdate();
    }
    public void SaveLevelUp(int count)
    {
        ActiveProvider.SaveLevelUp(count);
    }
    public int LoadLevelUp()
    {
        return ActiveProvider.LoadLevelUp();
    }

    /*    public void SaveAttachedItem(string id)
        {
            ActiveProvider.SaveAttachedItem(id);
        }

        public List<string> LoadAttachedItems()
        {
            return ActiveProvider.LoadAttachedItems();
        }

        public void ResetAttachedItems()
        {
            ActiveProvider.ResetAttachedItems();
        }

        public void DeleteAttachedItem(string id)
        {
            List<string> current = ActiveProvider.LoadAttachedItems();
            current.Remove(id);
            ActiveProvider.SaveAllAttachedItems(current);
        }*/

    /*    public void SaveInventory()
        {
            ActiveProvider.SaveInventory(Inventory.Instance.GetInventoryList());
        }*/

    // Остальные методы аналогично делегируют работу провайдеру...
}
