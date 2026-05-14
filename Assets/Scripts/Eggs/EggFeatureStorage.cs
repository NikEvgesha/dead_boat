using System;
using MirraGames.SDK;
using UnityEngine;

public static class EggFeatureStorage
{
    private const string StorageKey = "EggFeatureState_v1";

    public static EggFeatureState Load()
    {
        string json = LoadRaw();

        if (string.IsNullOrWhiteSpace(json))
            return new EggFeatureState();

        try
        {
            EggFeatureState state = JsonUtility.FromJson<EggFeatureState>(json);
            state ??= new EggFeatureState();
            state.Normalize();
            return state;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggFeatureStorage: load failed ({exception.Message})");
            return new EggFeatureState();
        }
    }

    public static void Save(EggFeatureState state)
    {
        if (state == null)
            state = new EggFeatureState();

        state.Normalize();

        string json = JsonUtility.ToJson(state);
        SaveRaw(json);
    }

    public static void AddEgg(string eggId, int amount)
    {
        EggFeatureState state = Load();
        state.AddEgg(eggId, amount);
        Save(state);
    }

    public static bool IsOneShotEggCollected(string collectibleId)
    {
        if (string.IsNullOrWhiteSpace(collectibleId))
            return false;

        EggFeatureState state = Load();
        return state.collectedOneShotEggIds.Contains(collectibleId);
    }

    public static bool TryAddOneShotEgg(string collectibleId, string eggId, int amount)
    {
        if (string.IsNullOrWhiteSpace(collectibleId) || string.IsNullOrWhiteSpace(eggId))
            return false;

        EggFeatureState state = Load();
        if (state.collectedOneShotEggIds.Contains(collectibleId))
            return false;

        state.collectedOneShotEggIds.Add(collectibleId);
        state.AddEgg(eggId, amount);
        Save(state);
        return true;
    }

    private static string LoadRaw()
    {
        try
        {
            if (MirraSDK.IsInitialized)
                return MirraSDK.Data.GetString(StorageKey, string.Empty);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggFeatureStorage: Mirra load fallback to PlayerPrefs ({exception.Message})");
        }

        return PlayerPrefs.GetString(StorageKey, string.Empty);
    }

    private static void SaveRaw(string value)
    {
        try
        {
            if (MirraSDK.IsInitialized)
            {
                MirraSDK.Data.SetString(StorageKey, value ?? string.Empty);
                MirraSDK.Data.Save();
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggFeatureStorage: Mirra save fallback to PlayerPrefs ({exception.Message})");
        }

        PlayerPrefs.SetString(StorageKey, value ?? string.Empty);
        PlayerPrefs.Save();
    }
}
