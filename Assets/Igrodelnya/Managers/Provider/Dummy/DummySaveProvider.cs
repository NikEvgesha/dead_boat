using MirraGames.SDK;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DummySaveProvider : SaveProvider
{
    public override void Initialize() { Debug.Log("DummySaveProvider initialized"); }
    public override float[] LoadVolume() {
        float[] volumes = new float[] { 0.5f, 0.5f };
        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            volumes[0] = PlayerPrefs.GetFloat("MusicVolume");
        }
        if (PlayerPrefs.HasKey("SoundVolume"))
        {
            volumes[1] = PlayerPrefs.GetFloat("SoundVolume");
        }
        return volumes;
    }
    public override void SaveGems(int amount) {
        PlayerPrefs.SetInt("Gems", amount);
    }

    public override int LoadGems()
    {
        int gems = 0;
        if (PlayerPrefs.HasKey("Gems"))
        {
            gems = PlayerPrefs.GetInt("Gems");
        }
        return gems;
    }
    public override void SaveVolume(float musicVolume, float soundVolume)
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SoundVolume", soundVolume);
    }

    public override void SaveSensivity(float sens) {
        PlayerPrefs.SetFloat("Sensivity", sens);
    }

    public override float LoadSensivity()
    {
        float sens = 0.5f;
        if (PlayerPrefs.HasKey("Sensivity"))
        {
            sens = PlayerPrefs.GetFloat("Sensivity");
        }
        return sens;
    }
    public override void SaveScore(float score, int levelId) { }
    public override float LoadScore(int levelId) => 0;
    public override bool GetTutorialProgress() => false;
    public override void SaveTutorialProgress(bool endTutorial) { }
    public override void SaveQuestProgress(int step) { }
    public override int LoadQuestProgress() => 0;
    public override void SaveLevelUnlock(int id, bool unlocked) { }
    public override void SaveLevelWin(int id, bool win) { }
    public override void SaveProgress() { }

    public override bool CheckProgress() { return false; }

    public override void SaveAchievementProgress(AchievementType id, int progress)
    {
        PlayerPrefs.SetInt(id.ToString(), progress);
    }

    public override int LoadAchievementProgress(AchievementType id)
    {
        int progress = 0;
        if (PlayerPrefs.HasKey(id.ToString()))
        {
            progress = PlayerPrefs.GetInt(id.ToString());
        }
        return progress;
    }


    public override void SaveAchievementStatus(string id, bool progress)
    {
        PlayerPrefs.SetInt(id, progress ? 1: 0);
    }

    public override bool LoadAchievementStatus(string id)
    {
        int progress = 0;
        if (PlayerPrefs.HasKey(id))
        {
            progress = PlayerPrefs.GetInt(id);
        }
        return progress == 1 ? true : false;
    }

    public override void SaveLevelStatus(string key, bool unlocked)
    {
        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
    }
    public override bool LoadLevelStatus(string key)
    {
        return PlayerPrefs.GetInt(key) == 1;
    }

    public override void SaveLobbyItem(string id)
    {
        
    }
    public override List<string> LoadLobbyItems()
    {
        return new List<string>();
    }

    public override void ResetLobbyItems()
    {

    }


    public override void SaveDistance(int distance) { }
    public override void SaveInventory(List<PickableItem> items) { }
    public override void SaveBoardItem(List<PickableItem> items) { }
    public override void SavePlayerStats(int coin, float hp) { }
    public override void SaveGameCoin(int coin)
    {
        PlayerPrefs.SetInt("GameCoin", coin);
    }
    public override void SavePlayerHealth(float health)
    {
        PlayerPrefs.SetFloat("PlayerHealth", health);
    }
    public override void SavePlayerExperience(int exp, int level)
    {
        PlayerPrefs.SetInt("PlayerExp", exp);
        PlayerPrefs.SetInt("PlayerLevel", level);
    }
    public override (int, int) LoadPlayerExperience()
    {
        return (PlayerPrefs.GetInt("PlayerExp", 0), PlayerPrefs.GetInt("PlayerLevel", 0));
    }
    public override int LoadGameCoin()
    {
        return PlayerPrefs.GetInt("GameCoin", 0);
    }
    public override float LoadPlayerHealth()
    {
        return PlayerPrefs.GetFloat("PlayerHealth", 100f);
    }
    public override (int, float) LoadPlayerStats()
    {
        return (0, 0);
    }

    public override List<string> LoadInventory() {
        return new List<string>();
    }
    public override List<SavedItem> LoadBoardItem()
    {
        return new List<SavedItem>();
    }
    public override int LoadDistance() {
        return 0;
    }


    public override void SaveFuel(int fuel)
    {
    }
    public override int LoadFuel()
    {
        return 0;
    }


    public override void SaveAttachedItem(string id)
    {

    }
    public override List<string> LoadAttachedItems()
    {
        return new List<string>();
    }

    public override void ResetAttachedItems()
    {
        
    }

    public override void SaveAllAttachedItems(List<string> items)
    {
    }

    public override void SetSave(bool save)
    {
    }

    public override void SaveAmmo(WeaponType type, int amount) { }
    public override int LoadAmmo(WeaponType type) {
        return 0;
    }

    public override void SaveWins(int wins)
    {

    }
    public override int LoadWins()
    {
        return 0;
    }

    public override void SaveLevelId(int id)
    {

    }

    public override int LoadLevelId()
    {
        return -1;
    }

    public override void SaveRouletteDate(DateTime date)
    {
    }

    public override DateTime LoadRouletteDate()
    {
        return DateTime.Today.AddDays(-1);
    }
    public override void SavePlayerFixPos(float posZ) {}
    public override float LoadPlayerFixPos()
    {
        return 0f;
    }
    public override void SaveBoardFixPos(float posZ)
    {
    }
    public override float LoadBoardFixPos()
    {
        return 0f;
    }
    public override void SaveLevelUpdate(Stats stats)
    {
        PlayerPrefs.SetString("LevelStats", JsonUtility.ToJson(stats));
    }
    public override Stats LoadLevelUpdate()
    {
        string json = PlayerPrefs.GetString("LevelStats", string.Empty);
        if (string.IsNullOrEmpty(json))
            return new Stats();

        return JsonUtility.FromJson<Stats>(json);
    }
    public override void SaveLevelUp(int count)
    {
        PlayerPrefs.SetInt("LevelUpCount", count);
    }
    public override int LoadLevelUp()
    {
        return PlayerPrefs.GetInt("LevelUpCount", 0);
    }
}
