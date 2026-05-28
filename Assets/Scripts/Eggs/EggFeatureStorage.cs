using System;
using System.Collections.Generic;
using System.Linq;
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

    public static void Reset()
    {
        SaveRaw(string.Empty);
        PlayerPrefs.DeleteKey(StorageKey);
        PlayerPrefs.Save();
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
        string prefsValue = PlayerPrefs.GetString(StorageKey, string.Empty);

        try
        {
            if (MirraSDK.IsInitialized)
            {
                string mirraValue = MirraSDK.Data.GetString(StorageKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(mirraValue) && !string.IsNullOrWhiteSpace(prefsValue) && mirraValue != prefsValue)
                {
                    string mergedValue = MergeRawStates(mirraValue, prefsValue);
                    if (!string.IsNullOrWhiteSpace(mergedValue))
                    {
                        SaveRaw(mergedValue);
                        return mergedValue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(mirraValue))
                    return mirraValue;

                if (!string.IsNullOrWhiteSpace(prefsValue))
                {
                    MirraSDK.Data.SetString(StorageKey, prefsValue);
                    MirraSDK.Data.Save();
                    return prefsValue;
                }

                return string.Empty;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggFeatureStorage: Mirra load fallback to PlayerPrefs ({exception.Message})");
        }

        return prefsValue;
    }

    private static void SaveRaw(string value)
    {
        string safeValue = value ?? string.Empty;
        PlayerPrefs.SetString(StorageKey, safeValue);
        PlayerPrefs.Save();

        try
        {
            if (MirraSDK.IsInitialized)
            {
                MirraSDK.Data.SetString(StorageKey, safeValue);
                MirraSDK.Data.Save();
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"EggFeatureStorage: Mirra save fallback to PlayerPrefs ({exception.Message})");
        }
    }

    private static string MergeRawStates(string mirraValue, string prefsValue)
    {
        if (!TryParseState(mirraValue, out EggFeatureState mirraState))
            return prefsValue;

        if (!TryParseState(prefsValue, out EggFeatureState prefsState))
            return mirraValue;

        EggFeatureState merged = new EggFeatureState
        {
            hasDiscoveredEggs = mirraState.hasDiscoveredEggs || prefsState.hasDiscoveredEggs,
            hasHatchedAnimal = mirraState.hasHatchedAnimal || prefsState.hasHatchedAnimal,
            hasUnlockedAnimalMerge = mirraState.hasUnlockedAnimalMerge || prefsState.hasUnlockedAnimalMerge,
            hasUsedAnimalLoadout = mirraState.hasUsedAnimalLoadout || prefsState.hasUsedAnimalLoadout,
            ownedEggs = MergeEggs(mirraState.ownedEggs, prefsState.ownedEggs),
            ownedAnimals = MergeAnimals(mirraState.ownedAnimals, prefsState.ownedAnimals),
            nests = MergeNests(mirraState.nests, prefsState.nests),
            placedAnimals = MergePlacedAnimals(mirraState.placedAnimals, prefsState.placedAnimals),
            collectedOneShotEggIds = mirraState.collectedOneShotEggIds
                .Concat(prefsState.collectedOneShotEggIds)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList(),
            animalMerge = SelectAnimalMerge(mirraState.animalMerge, prefsState.animalMerge)
        };

        merged.Normalize();
        return JsonUtility.ToJson(merged);
    }

    private static bool TryParseState(string raw, out EggFeatureState state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            state = JsonUtility.FromJson<EggFeatureState>(raw);
            state ??= new EggFeatureState();
            state.Normalize();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<EggInventoryEntry> MergeEggs(List<EggInventoryEntry> first, List<EggInventoryEntry> second)
    {
        return first.Concat(second)
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.eggId) && x.amount > 0)
            .GroupBy(x => x.eggId)
            .Select(g => new EggInventoryEntry { eggId = g.Key, amount = g.Max(x => x.amount) })
            .ToList();
    }

    private static List<AnimalInventoryEntry> MergeAnimals(List<AnimalInventoryEntry> first, List<AnimalInventoryEntry> second)
    {
        return first.Concat(second)
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.animalId) && x.amount > 0)
            .GroupBy(x => $"{x.animalId}::{Mathf.Max(1, x.stage)}")
            .Select(g =>
            {
                AnimalInventoryEntry entry = g.First();
                return new AnimalInventoryEntry
                {
                    animalId = entry.animalId,
                    stage = Mathf.Max(1, entry.stage),
                    amount = g.Max(x => x.amount)
                };
            })
            .ToList();
    }

    private static List<EggNestState> MergeNests(List<EggNestState> first, List<EggNestState> second)
    {
        return first.Concat(second)
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.nestId) && !string.IsNullOrWhiteSpace(x.eggId))
            .GroupBy(x => x.nestId)
            .Select(g => CopyNest(g.OrderByDescending(x => x.finishAtUnix).First()))
            .ToList();
    }

    private static List<PlacedAnimalState> MergePlacedAnimals(List<PlacedAnimalState> first, List<PlacedAnimalState> second)
    {
        return first.Concat(second)
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.pointId) && !string.IsNullOrWhiteSpace(x.animalId))
            .GroupBy(x => x.pointId)
            .Select(g => CopyPlacedAnimal(g.OrderByDescending(x => x.lastIncomeUnix).First()))
            .ToList();
    }

    private static EggNestState CopyNest(EggNestState source)
    {
        return new EggNestState
        {
            nestId = source.nestId,
            eggId = source.eggId,
            hatchedAnimalId = source.hatchedAnimalId,
            hatchedStage = Mathf.Max(1, source.hatchedStage),
            finishAtUnix = source.finishAtUnix,
            durationSeconds = source.durationSeconds,
            isReady = source.isReady
        };
    }

    private static PlacedAnimalState CopyPlacedAnimal(PlacedAnimalState source)
    {
        return new PlacedAnimalState
        {
            pointId = source.pointId,
            animalId = source.animalId,
            stage = Mathf.Max(1, source.stage),
            lastIncomeUnix = source.lastIncomeUnix
        };
    }

    private static AnimalMergeState SelectAnimalMerge(AnimalMergeState first, AnimalMergeState second)
    {
        AnimalMergeState selected = first;
        if (selected == null || second != null && second.finishAtUnix > selected.finishAtUnix)
            selected = second;

        if (selected == null)
            return null;

        return new AnimalMergeState
        {
            animalId = selected.animalId,
            stage = Mathf.Max(1, selected.stage),
            finishAtUnix = selected.finishAtUnix,
            durationSeconds = selected.durationSeconds,
            isReady = selected.isReady,
            resultAnimationShown = selected.resultAnimationShown
        };
    }
}
