using UnityEngine;
using MirraGames.SDK;  // доступ к MirraSDK.Data

public class MirraSDKSaveProvider : SaveProvider
{
    public override void Initialize()
    {
        // Дождёмся полной готовности системы сохранений
        MirraSDK.Data.WaitForProviders(() =>
        {
            Debug.Log("MirraSDKSaveProvider initialized");
        });
    }

    public override float[] LoadVolume()
    {
        // Достаём значения, с дефолтом 0.5f
        float music = MirraSDK.Data.GetFloat("MusicVolume", 0.5f);
        float sound = MirraSDK.Data.GetFloat("SoundVolume", 0.5f);
        return new float[] { music, sound };
    }

    public override void SaveVolume(float musicVolume, float soundVolume)
    {
        MirraSDK.Data.SetFloat("MusicVolume", musicVolume);
        MirraSDK.Data.SetFloat("SoundVolume", soundVolume);
    }

    public override void SaveScore(float score, int levelId)
    {
        // Ключ «Score_1», «Score_2» и т.д.
        MirraSDK.Data.SetFloat($"Score_{levelId}", score);
    }

    public override float LoadScore(int levelId)
    {
        return MirraSDK.Data.GetFloat($"Score_{levelId}", 0f);
    }

    public override void SaveLevelUnlock(int id, bool unlocked)
    {
        MirraSDK.Data.SetBool($"LevelUnlock_{id}", unlocked);
    }

    public override void SaveLevelWin(int id, bool win)
    {
        MirraSDK.Data.SetBool($"LevelWin_{id}", win);
    }

    public override void SaveGems(int amount)
    {
        MirraSDK.Data.SetInt("Gems", amount);
    }

    public override int LoadGems()
    {
        return MirraSDK.Data.GetInt("Gems", 0);
    }

    public override void SaveProgress()
    {
        // Синхронизировать все изменения с провайдером (локальным или облачным)
        MirraSDK.Data.Save();
    }

    public override bool CheckProgress()
    {
        // Есть ли хоть что-то из основных ключей?
        return MirraSDK.Data.HasKey("MusicVolume")
            || MirraSDK.Data.HasKey("Gems");
    }
}
