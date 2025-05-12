using UnityEngine;
using MirraGames.SDK;  // доступ к MirraSDK.Data

public class MirraSDKSaveProvider : SaveProvider
{
    private bool isInitialize;
    public override void Initialize()
    {
        // Дождёмся полной готовности системы сохранений
        MirraSDK.WaitForProviders(() =>
        {
            Debug.Log("MirraSDKSaveProvider initialized");
            isInitialize = true;
        });
    }

    public override float[] LoadVolume()
    {
        if (!isInitialize)
            return new float[] { 0.5f, 0.5f }; ;
        // Достаём значения, с дефолтом 0.5f
        float music = MirraSDK.Data.GetFloat("MusicVolume", 0.5f);
        float sound = MirraSDK.Data.GetFloat("SoundVolume", 0.5f);
        return new float[] { music, sound };
    }

    public override void SaveVolume(float musicVolume, float soundVolume)
    {
        if (!isInitialize) return;
        MirraSDK.Data.SetFloat("MusicVolume", musicVolume);
        MirraSDK.Data.SetFloat("SoundVolume", soundVolume);
        Changed = true;
    }

    public override void SaveScore(float score, int levelId)
    {
        if (!isInitialize) return;
        // Ключ «Score_1», «Score_2» и т.д.
        MirraSDK.Data.SetFloat($"Score_{levelId}", score);
        Changed = true;
    }

    public override float LoadScore(int levelId)
    {
        if (!isInitialize)
            return 0f;
        return MirraSDK.Data.GetFloat($"Score_{levelId}", 0f);
    }

    public override void SaveLevelUnlock(int id, bool unlocked)
    {
        if (!isInitialize) return;
        MirraSDK.Data.SetBool($"LevelUnlock_{id}", unlocked);
        Changed = true;
    }

    public override void SaveLevelWin(int id, bool win)
    {
        if (!isInitialize) return;
        MirraSDK.Data.SetBool($"LevelWin_{id}", win);
        Changed = true;
    }

    public override void SaveGems(int amount)
    {
        if (!isInitialize) return;
        Changed = true;
        MirraSDK.Data.SetInt("Gems", amount);
    }

    public override int LoadGems()
    {
        if (!isInitialize)
            return 0;
        return MirraSDK.Data.GetInt("Gems", 0);
    }

    public override void SaveProgress()
    {
        if (!isInitialize) return;
        // Синхронизировать все изменения с провайдером (локальным или облачным)
        if (Changed)
        {
            MirraSDK.Data.Save();
            Changed = false;
        }
    }

    public override bool CheckProgress()
    {
        if (!isInitialize) return false;
        // Есть ли хоть что-то из основных ключей?
        return MirraSDK.Data.HasKey("MusicVolume")
            || MirraSDK.Data.HasKey("Gems");
    }


    public override void SaveAchievementProgress(AchievementType id, int progress)
    {
        if (!isInitialize) return;
        Changed = true;
        MirraSDK.Data.SetInt(id.ToString(), progress);
    }

    public override int LoadAchievementProgress(AchievementType id)
    {
        if (!isInitialize)
            return 0;
        return MirraSDK.Data.GetInt(id.ToString(), 0);
    }

    public override void SaveAchievementStatus(string id, bool rewarded)
    {
        if (!isInitialize) return;
        Changed = true;
        MirraSDK.Data.SetBool(id.ToString(), rewarded);
    }

    public override bool LoadAchievementStatus(string id)
    {
        if (!isInitialize)
            return false;
        return MirraSDK.Data.GetBool(id.ToString(), false);
    }
}
