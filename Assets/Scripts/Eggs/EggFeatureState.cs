using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EggInventoryEntry
{
    public string eggId;
    public int amount;
}

[Serializable]
public class AnimalInventoryEntry
{
    public string animalId;
    public int stage = 1;
    public int amount;
}

[Serializable]
public class EggNestState
{
    public string nestId;
    public string eggId;
    public string hatchedAnimalId;
    public int hatchedStage = 1;
    public long finishAtUnix;
    public int durationSeconds;
    public bool isReady;
}

[Serializable]
public class PlacedAnimalState
{
    public string pointId;
    public string animalId;
    public int stage = 1;
    public long lastIncomeUnix;
}

[Serializable]
public class AnimalMergeState
{
    public string animalId;
    public int stage = 1;
    public long finishAtUnix;
    public int durationSeconds;
    public bool isReady;
    public bool resultAnimationShown;
}

[Serializable]
public class EggFeatureState
{
    public bool hasDiscoveredEggs;
    public bool hasHatchedAnimal;
    public List<EggInventoryEntry> ownedEggs = new();
    public List<AnimalInventoryEntry> ownedAnimals = new();
    public List<EggNestState> nests = new();
    public List<PlacedAnimalState> placedAnimals = new();
    public List<string> collectedOneShotEggIds = new();
    public AnimalMergeState animalMerge;

    public int GetEggAmount(string eggId)
    {
        EggInventoryEntry entry = ownedEggs.Find(x => x.eggId == eggId);
        return entry != null ? entry.amount : 0;
    }

    public int GetAnimalAmount(string animalId)
    {
        return GetAnimalAmount(animalId, 1);
    }

    public int GetAnimalAmount(string animalId, int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        AnimalInventoryEntry entry = ownedAnimals.Find(x =>
            x.animalId == animalId &&
            Mathf.Max(1, x.stage) == safeStage);
        return entry != null ? entry.amount : 0;
    }

    public void AddEgg(string eggId, int amount)
    {
        if (string.IsNullOrWhiteSpace(eggId) || amount <= 0)
            return;

        hasDiscoveredEggs = true;

        EggInventoryEntry entry = ownedEggs.Find(x => x.eggId == eggId);
        if (entry == null)
        {
            ownedEggs.Add(new EggInventoryEntry { eggId = eggId, amount = amount });
            return;
        }

        entry.amount += amount;
    }

    public void AddAnimal(string animalId, int amount)
    {
        AddAnimal(animalId, 1, amount);
    }

    public void AddAnimal(string animalId, int stage, int amount)
    {
        if (string.IsNullOrWhiteSpace(animalId) || amount <= 0)
            return;

        hasHatchedAnimal = true;

        int safeStage = Mathf.Max(1, stage);
        AnimalInventoryEntry entry = ownedAnimals.Find(x =>
            x.animalId == animalId &&
            Mathf.Max(1, x.stage) == safeStage);
        if (entry == null)
        {
            ownedAnimals.Add(new AnimalInventoryEntry
            {
                animalId = animalId,
                stage = safeStage,
                amount = amount
            });
            return;
        }

        entry.amount += amount;
    }

    public bool TryConsumeEgg(string eggId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(eggId) || amount <= 0)
            return false;

        EggInventoryEntry entry = ownedEggs.Find(x => x.eggId == eggId);
        if (entry == null || entry.amount < amount)
            return false;

        entry.amount -= amount;
        if (entry.amount <= 0)
            ownedEggs.Remove(entry);

        return true;
    }

    public bool TryConsumeAnimal(string animalId, int amount = 1)
    {
        return TryConsumeAnimal(animalId, 1, amount);
    }

    public bool TryConsumeAnimal(string animalId, int stage, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(animalId) || amount <= 0)
            return false;

        int safeStage = Mathf.Max(1, stage);
        AnimalInventoryEntry entry = ownedAnimals.Find(x =>
            x.animalId == animalId &&
            Mathf.Max(1, x.stage) == safeStage);
        if (entry == null || entry.amount < amount)
            return false;

        entry.amount -= amount;
        if (entry.amount <= 0)
            ownedAnimals.Remove(entry);

        return true;
    }

    public void Normalize()
    {
        ownedEggs ??= new List<EggInventoryEntry>();
        ownedAnimals ??= new List<AnimalInventoryEntry>();
        nests ??= new List<EggNestState>();
        placedAnimals ??= new List<PlacedAnimalState>();
        collectedOneShotEggIds ??= new List<string>();
        if (animalMerge != null)
        {
            animalMerge.stage = Mathf.Max(1, animalMerge.stage);
            if (string.IsNullOrWhiteSpace(animalMerge.animalId))
                animalMerge = null;
        }

        ownedEggs = ownedEggs
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.eggId) && x.amount > 0)
            .GroupBy(x => x.eggId)
            .Select(g => new EggInventoryEntry
            {
                eggId = g.Key,
                amount = g.Sum(x => x.amount)
            })
            .ToList();

        ownedAnimals = ownedAnimals
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.animalId) && x.amount > 0)
            .GroupBy(x => $"{x.animalId}::{Mathf.Max(1, x.stage)}")
            .Select(g =>
            {
                AnimalInventoryEntry first = g.First();
                return new AnimalInventoryEntry
                {
                    animalId = first.animalId,
                    stage = Mathf.Max(1, first.stage),
                    amount = g.Sum(x => x.amount)
                };
            })
            .ToList();

        nests = nests
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.nestId) && !string.IsNullOrWhiteSpace(x.eggId))
            .GroupBy(x => x.nestId)
            .Select(g =>
            {
                EggNestState nest = g.OrderByDescending(x => x.finishAtUnix).First();
                nest.hatchedStage = Mathf.Max(1, nest.hatchedStage);
                return nest;
            })
            .ToList();

        placedAnimals = placedAnimals
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.pointId) && !string.IsNullOrWhiteSpace(x.animalId))
            .GroupBy(x => x.pointId)
            .Select(g =>
            {
                PlacedAnimalState state = g.OrderByDescending(x => x.lastIncomeUnix).First();
                state.stage = Mathf.Max(1, state.stage);
                return state;
            })
            .ToList();

        collectedOneShotEggIds = collectedOneShotEggIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (ownedEggs.Count > 0 || nests.Count > 0 || ownedAnimals.Count > 0 || placedAnimals.Count > 0)
            hasDiscoveredEggs = true;

        if (ownedAnimals.Count > 0 || placedAnimals.Count > 0 || animalMerge != null)
            hasHatchedAnimal = true;
    }
}
