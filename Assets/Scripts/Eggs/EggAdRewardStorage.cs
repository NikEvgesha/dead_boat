using System;
using MirraGames.SDK;
using UnityEngine;

public static class EggAdRewardStorage
{
    private const string NextAvailableUnixKey = "EggAdRewardNextAvailableUnix_v1";

    public static long LoadNextAvailableUnix()
    {
        try
        {
            if (MirraSDK.IsInitialized)
            {
                string stored = MirraSDK.Data.GetString(NextAvailableUnixKey, "0");
                return long.TryParse(stored, out long mirraUnix) ? mirraUnix : 0;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggAdRewardStorage: Mirra load fallback to PlayerPrefs ({exception.Message})");
        }

        string value = PlayerPrefs.GetString(NextAvailableUnixKey, "0");
        return long.TryParse(value, out long unix) ? unix : 0;
    }

    public static void SaveNextAvailableUnix(long nextAvailableUnix)
    {
        long safeValue = Math.Max(0, nextAvailableUnix);

        try
        {
            if (MirraSDK.IsInitialized)
            {
                MirraSDK.Data.SetString(NextAvailableUnixKey, safeValue.ToString());
                MirraSDK.Data.Save();
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggAdRewardStorage: Mirra save fallback to PlayerPrefs ({exception.Message})");
        }

        PlayerPrefs.SetString(NextAvailableUnixKey, safeValue.ToString());
        PlayerPrefs.Save();
    }
}
