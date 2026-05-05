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
    // Legacy field. Existing saves used eggId as the animal key.
    public string eggId;
    public string animalId;
    public int stage = 1;
    public int amount;

    public string EffectiveAnimalId => !string.IsNullOrWhiteSpace(animalId) ? animalId : eggId;
    public int EffectiveStage => Mathf.Max(1, stage);
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
    public string nestId;
    // Legacy field. Existing saves used eggId as the animal key.
    public string eggId;
    public string animalId;
    public int stage = 1;
    public int pointIndex;
    public long lastIncomeUnix;

    public string EffectiveAnimalId => !string.IsNullOrWhiteSpace(animalId) ? animalId : eggId;
    public int EffectiveStage => Mathf.Max(1, stage);
}

[Serializable]
public class EggFeatureState
{
    public int version = 2;
    public List<EggInventoryEntry> ownedEggs = new();
    public List<AnimalInventoryEntry> ownedAnimals = new();
    public List<EggNestState> nests = new();
    public List<PlacedAnimalState> placedAnimals = new();

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
            x.EffectiveAnimalId == animalId &&
            x.EffectiveStage == safeStage);
        return entry != null ? entry.amount : 0;
    }

    public void AddEgg(string eggId, int amount)
    {
        if (string.IsNullOrWhiteSpace(eggId) || amount <= 0)
            return;

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

        int safeStage = Mathf.Max(1, stage);
        AnimalInventoryEntry entry = ownedAnimals.Find(x =>
            x.EffectiveAnimalId == animalId &&
            x.EffectiveStage == safeStage);
        if (entry == null)
        {
            ownedAnimals.Add(new AnimalInventoryEntry
            {
                eggId = animalId,
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
            x.EffectiveAnimalId == animalId &&
            x.EffectiveStage == safeStage);
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
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.EffectiveAnimalId) && x.amount > 0)
            .GroupBy(x => $"{x.EffectiveAnimalId}::{x.EffectiveStage}")
            .Select(g =>
            {
                AnimalInventoryEntry first = g.First();
                string animalId = first.EffectiveAnimalId;
                int stage = first.EffectiveStage;
                return new AnimalInventoryEntry
                {
                    eggId = animalId,
                    animalId = animalId,
                    stage = stage,
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
                if (string.IsNullOrWhiteSpace(nest.hatchedAnimalId))
                    nest.hatchedAnimalId = nest.eggId;
                nest.hatchedStage = Mathf.Max(1, nest.hatchedStage);
                return nest;
            })
            .ToList();

        placedAnimals = placedAnimals
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.EffectiveAnimalId))
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.pointId) ||
                (!string.IsNullOrWhiteSpace(x.nestId) && x.pointIndex >= 0))
            .GroupBy(MakePlacementKey)
            .Select(g =>
            {
                PlacedAnimalState state = g.OrderByDescending(x => x.lastIncomeUnix).First();
                string animalId = state.EffectiveAnimalId;
                int stage = state.EffectiveStage;
                state.eggId = animalId;
                state.animalId = animalId;
                state.stage = stage;
                return state;
            })
            .ToList();

        version = Mathf.Max(2, version);
    }

    private static string MakePlacementKey(PlacedAnimalState state)
    {
        if (!string.IsNullOrWhiteSpace(state.pointId))
            return $"point::{state.pointId}";

        return $"legacy::{state.nestId}::{state.pointIndex}";
    }
}
