using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EggHatchResult
{
    public string animalId;
    [Min(0)] public int weight = 1;
}

[Serializable]
public class AnimalRunBuffs
{
    [Header("Player")]
    public float maxHealthFlat;
    public float moveSpeedFlat;
    public float moveSpeedMultiplier = 1f;
    public float experienceMultiplier = 1f;

    [Header("Economy")]
    public float saleRewardMultiplier = 1f;

    [Header("Boat")]
    public float maxFuelFlat;
    public float fuelConsumptionMultiplier = 1f;
    public float fuelFillMultiplier = 1f;
    public float boatSpeedFlat;

    [Header("Combat")]
    public float meleeDamageFlat;
    public float meleeAttackSpeedMultiplier = 1f;
    public float rangedDamageFlat;
    public float rangedAttackSpeedMultiplier = 1f;
    public float rangedReloadSpeedMultiplier = 1f;

    public float SafeMoveSpeedMultiplier => moveSpeedMultiplier > 0f ? moveSpeedMultiplier : 1f;
    public float SafeExperienceMultiplier => experienceMultiplier > 0f ? experienceMultiplier : 1f;
    public float SafeSaleRewardMultiplier => saleRewardMultiplier > 0f ? saleRewardMultiplier : 1f;
    public float SafeFuelConsumptionMultiplier => fuelConsumptionMultiplier > 0f ? fuelConsumptionMultiplier : 1f;
    public float SafeFuelFillMultiplier => fuelFillMultiplier > 0f ? fuelFillMultiplier : 1f;
    public float SafeMeleeAttackSpeedMultiplier => meleeAttackSpeedMultiplier > 0f ? meleeAttackSpeedMultiplier : 1f;
    public float SafeRangedAttackSpeedMultiplier => rangedAttackSpeedMultiplier > 0f ? rangedAttackSpeedMultiplier : 1f;
    public float SafeRangedReloadSpeedMultiplier => rangedReloadSpeedMultiplier > 0f ? rangedReloadSpeedMultiplier : 1f;

    public void Add(AnimalRunBuffs other)
    {
        if (other == null)
            return;

        maxHealthFlat += other.maxHealthFlat;
        moveSpeedFlat += other.moveSpeedFlat;
        moveSpeedMultiplier *= other.SafeMoveSpeedMultiplier;
        experienceMultiplier *= other.SafeExperienceMultiplier;
        saleRewardMultiplier *= other.SafeSaleRewardMultiplier;
        maxFuelFlat += other.maxFuelFlat;
        fuelConsumptionMultiplier *= other.SafeFuelConsumptionMultiplier;
        fuelFillMultiplier *= other.SafeFuelFillMultiplier;
        boatSpeedFlat += other.boatSpeedFlat;
        meleeDamageFlat += other.meleeDamageFlat;
        meleeAttackSpeedMultiplier *= other.SafeMeleeAttackSpeedMultiplier;
        rangedDamageFlat += other.rangedDamageFlat;
        rangedAttackSpeedMultiplier *= other.SafeRangedAttackSpeedMultiplier;
        rangedReloadSpeedMultiplier *= other.SafeRangedReloadSpeedMultiplier;
    }
}

[Serializable]
public class AnimalStageDefinition
{
    [Min(1)] public int stage = 1;
    public GameObject animalPrefab;
    public Color tint = Color.white;
    public GameObject mergeParticlesPrefab;
    public AnimalRunBuffs buffs = new();
}

[Serializable]
public class AnimalDefinition
{
    public string animalId;
    public string title;
    [Min(1)] public int maxStage = 3;
    public List<AnimalStageDefinition> stages = new();

    public AnimalStageDefinition GetStage(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        AnimalStageDefinition fallback = null;

        for (int i = 0; i < stages.Count; i++)
        {
            AnimalStageDefinition definition = stages[i];
            if (definition == null)
                continue;

            if (definition.stage == safeStage)
                return definition;

            if (fallback == null || definition.stage < fallback.stage)
                fallback = definition;
        }

        return fallback;
    }
}

[Serializable]
public class EggHatchingDefinition
{
    public string eggId;
    public string title;
    [Min(1)] public int incubationSeconds = 300;
    [Min(0)] public int skipCostGems = 10;
    [Header("Legacy animal data. Kept for save/catalog compatibility.")]
    [Min(0)] public int passiveIncomeCoins = 0;
    [Min(1)] public int passiveIncomeIntervalSeconds = 60;
    public GameObject animalPrefab;
    public GameObject eggPreviewPrefab;
    [Header("V2 hatch results")]
    public List<EggHatchResult> hatchResults = new();
}

[CreateAssetMenu(fileName = "EggHatchingCatalog", menuName = "ScriptableObject/Eggs/EggHatchingCatalog")]
public class EggHatchingCatalog : ScriptableObject
{
    [SerializeField] private List<EggHatchingDefinition> _definitions = new();
    [SerializeField] private List<AnimalDefinition> _animals = new();

    private Dictionary<string, EggHatchingDefinition> _cache;
    private Dictionary<string, AnimalDefinition> _animalCache;

    public IReadOnlyList<EggHatchingDefinition> Definitions => _definitions;
    public IReadOnlyList<AnimalDefinition> Animals => _animals;

    public bool TryGet(string eggId, out EggHatchingDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(eggId))
            return false;

        BuildCacheIfNeeded();
        return _cache.TryGetValue(eggId, out definition);
    }

    public bool TryGetAnimal(string animalId, out AnimalDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(animalId))
            return false;

        BuildCacheIfNeeded();
        return _animalCache.TryGetValue(animalId, out definition);
    }

    public bool TryGetAnimalStage(string animalId, int stage, out AnimalDefinition animal, out AnimalStageDefinition stageDefinition)
    {
        animal = null;
        stageDefinition = null;

        if (!TryGetAnimal(animalId, out animal))
            return false;

        stageDefinition = animal.GetStage(stage);
        return stageDefinition != null;
    }

    public string RollAnimalId(EggHatchingDefinition egg)
    {
        if (egg == null)
            return string.Empty;

        int totalWeight = 0;
        if (egg.hatchResults != null)
        {
            for (int i = 0; i < egg.hatchResults.Count; i++)
            {
                EggHatchResult result = egg.hatchResults[i];
                if (result == null || string.IsNullOrWhiteSpace(result.animalId))
                    continue;

                totalWeight += Mathf.Max(0, result.weight);
            }
        }

        if (totalWeight > 0)
        {
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cursor = 0;

            for (int i = 0; i < egg.hatchResults.Count; i++)
            {
                EggHatchResult result = egg.hatchResults[i];
                if (result == null || string.IsNullOrWhiteSpace(result.animalId))
                    continue;

                cursor += Mathf.Max(0, result.weight);
                if (roll < cursor)
                    return result.animalId;
            }
        }

        return egg.eggId;
    }

    public GameObject ResolveAnimalPrefab(string animalId, int stage)
    {
        if (TryGetAnimalStage(animalId, stage, out _, out AnimalStageDefinition stageDefinition) &&
            stageDefinition.animalPrefab != null)
        {
            return stageDefinition.animalPrefab;
        }

        if (TryGet(animalId, out EggHatchingDefinition legacyDefinition))
            return legacyDefinition.animalPrefab;

        return null;
    }

    public string GetAnimalTitle(string animalId)
    {
        if (TryGetAnimal(animalId, out AnimalDefinition animal) && !string.IsNullOrWhiteSpace(animal.title))
            return animal.title;

        if (TryGet(animalId, out EggHatchingDefinition legacyDefinition) && !string.IsNullOrWhiteSpace(legacyDefinition.title))
            return legacyDefinition.title;

        return animalId;
    }

    public int GetMaxStage(string animalId)
    {
        if (TryGetAnimal(animalId, out AnimalDefinition animal))
            return Mathf.Max(1, animal.maxStage);

        return 1;
    }

    public AnimalRunBuffs GetAnimalBuffs(string animalId, int stage)
    {
        if (TryGetAnimalStage(animalId, stage, out _, out AnimalStageDefinition stageDefinition) &&
            stageDefinition.buffs != null)
        {
            return stageDefinition.buffs;
        }

        if (TryGet(animalId, out _))
            return new AnimalRunBuffs();

        return null;
    }

    public GameObject GetMergeParticlesPrefab(string animalId, int stage)
    {
        if (TryGetAnimalStage(animalId, stage, out _, out AnimalStageDefinition stageDefinition))
            return stageDefinition.mergeParticlesPrefab;

        return null;
    }

    private void BuildCacheIfNeeded()
    {
        if (_cache != null && _animalCache != null)
            return;

        _cache = new Dictionary<string, EggHatchingDefinition>();
        _animalCache = new Dictionary<string, AnimalDefinition>();

        foreach (EggHatchingDefinition definition in _definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.eggId))
                continue;

            _cache[definition.eggId] = definition;
        }

        foreach (AnimalDefinition definition in _animals)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.animalId))
                continue;

            _animalCache[definition.animalId] = definition;
        }
    }

    private void OnValidate()
    {
        _cache = null;
        _animalCache = null;
    }
}
