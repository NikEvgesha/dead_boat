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
    public string eggId;
    public int amount;
}

[Serializable]
public class EggNestState
{
    public string nestId;
    public string eggId;
    public long finishAtUnix;
    public int durationSeconds;
}

[Serializable]
public class PlacedAnimalState
{
    public string pointId;
    public string nestId;
    public string eggId;
    public int pointIndex;
    public long lastIncomeUnix;
}

[Serializable]
public class EggFeatureState
{
    public int version = 1;
    public List<EggInventoryEntry> ownedEggs = new();
    public List<AnimalInventoryEntry> ownedAnimals = new();
    public List<EggNestState> nests = new();
    public List<PlacedAnimalState> placedAnimals = new();

    public int GetEggAmount(string eggId)
    {
        EggInventoryEntry entry = ownedEggs.Find(x => x.eggId == eggId);
        return entry != null ? entry.amount : 0;
    }

    public int GetAnimalAmount(string eggId)
    {
        AnimalInventoryEntry entry = ownedAnimals.Find(x => x.eggId == eggId);
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

    public void AddAnimal(string eggId, int amount)
    {
        if (string.IsNullOrWhiteSpace(eggId) || amount <= 0)
            return;

        AnimalInventoryEntry entry = ownedAnimals.Find(x => x.eggId == eggId);
        if (entry == null)
        {
            ownedAnimals.Add(new AnimalInventoryEntry { eggId = eggId, amount = amount });
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

    public bool TryConsumeAnimal(string eggId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(eggId) || amount <= 0)
            return false;

        AnimalInventoryEntry entry = ownedAnimals.Find(x => x.eggId == eggId);
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
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.eggId) && x.amount > 0)
            .GroupBy(x => x.eggId)
            .Select(g => new AnimalInventoryEntry
            {
                eggId = g.Key,
                amount = g.Sum(x => x.amount)
            })
            .ToList();

        nests = nests
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.nestId) && !string.IsNullOrWhiteSpace(x.eggId))
            .GroupBy(x => x.nestId)
            .Select(g => g.OrderByDescending(x => x.finishAtUnix).First())
            .ToList();

        placedAnimals = placedAnimals
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.eggId))
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.pointId) ||
                (!string.IsNullOrWhiteSpace(x.nestId) && x.pointIndex >= 0))
            .GroupBy(MakePlacementKey)
            .Select(g => g.OrderByDescending(x => x.lastIncomeUnix).First())
            .ToList();
    }

    private static string MakePlacementKey(PlacedAnimalState state)
    {
        if (!string.IsNullOrWhiteSpace(state.pointId))
            return $"point::{state.pointId}";

        return $"legacy::{state.nestId}::{state.pointIndex}";
    }
}
