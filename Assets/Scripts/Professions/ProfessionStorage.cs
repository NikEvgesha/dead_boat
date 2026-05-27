using System;
using System.Collections.Generic;
using MirraGames.SDK;
using UnityEngine;

public static class ProfessionStorage
{
    private const string StorageKey = "ProfessionState_v1";

    public static ProfessionState Load(IReadOnlyList<ProfessionDefinition> definitions, string fallbackDefaultProfessionId)
    {
        string json = LoadRaw();

        ProfessionState state;
        if (string.IsNullOrWhiteSpace(json))
        {
            state = new ProfessionState();
        }
        else
        {
            try
            {
                state = JsonUtility.FromJson<ProfessionState>(json);
                state ??= new ProfessionState();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ProfessionStorage: load failed ({exception.Message})");
                state = new ProfessionState();
            }
        }

        state.Normalize(definitions, fallbackDefaultProfessionId);
        return state;
    }

    public static void Save(ProfessionState state, IReadOnlyList<ProfessionDefinition> definitions, string fallbackDefaultProfessionId)
    {
        state ??= new ProfessionState();
        state.Normalize(definitions, fallbackDefaultProfessionId);

        string json = JsonUtility.ToJson(state);
        SaveRaw(json);
    }

    public static void Reset()
    {
        SaveRaw(string.Empty);
        PlayerPrefs.DeleteKey(StorageKey);
        PlayerPrefs.Save();
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
            Debug.LogWarning($"ProfessionStorage: Mirra load fallback to PlayerPrefs ({exception.Message})");
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
            Debug.LogWarning($"ProfessionStorage: Mirra save fallback to PlayerPrefs ({exception.Message})");
        }

        PlayerPrefs.SetString(StorageKey, value ?? string.Empty);
        PlayerPrefs.Save();
    }
}
