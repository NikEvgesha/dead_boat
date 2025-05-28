using MirraGames.SDK;
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
    public override void SaveScore(float score, int levelId) { }
    public override float LoadScore(int levelId) => 0;
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
}
