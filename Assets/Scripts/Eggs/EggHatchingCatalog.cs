using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EggHatchingCatalog", menuName = "ScriptableObject/Eggs/Egg Catalog")]
public class EggHatchingCatalog : ScriptableObject
{
    [SerializeField] private List<EggDefinition> _eggs = new();
    [SerializeField] private List<AnimalDefinition> _animals = new();

    private Dictionary<string, EggDefinition> _eggCache;
    private Dictionary<string, AnimalDefinition> _animalCache;

    public IReadOnlyList<EggDefinition> Definitions => _eggs;
    public IReadOnlyList<AnimalDefinition> Animals => _animals;

    public bool TryGet(string eggId, out EggDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(eggId))
            return false;

        BuildCacheIfNeeded();
        return _eggCache.TryGetValue(eggId, out definition);
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

    public string RollAnimalId(EggDefinition egg)
    {
        if (egg == null)
            return string.Empty;

        int totalWeight = 0;
        if (egg.hatchResults != null)
        {
            for (int i = 0; i < egg.hatchResults.Count; i++)
            {
                EggHatchResult result = egg.hatchResults[i];
                if (result == null || string.IsNullOrWhiteSpace(result.AnimalId))
                    continue;

                totalWeight += Mathf.Max(0, result.weight);
            }
        }

        if (totalWeight > 0)
        {
            int roll = Random.Range(0, totalWeight);
            int cursor = 0;

            for (int i = 0; i < egg.hatchResults.Count; i++)
            {
                EggHatchResult result = egg.hatchResults[i];
                if (result == null || string.IsNullOrWhiteSpace(result.AnimalId))
                    continue;

                cursor += Mathf.Max(0, result.weight);
                if (roll < cursor)
                    return result.AnimalId;
            }
        }

        return string.Empty;
    }

    public GameObject ResolveAnimalPrefab(string animalId, int stage)
    {
        if (TryGetAnimalStage(animalId, stage, out _, out AnimalStageDefinition stageDefinition))
            return stageDefinition.animalPrefab;

        return null;
    }

    public string GetAnimalTitle(string animalId)
    {
        if (TryGetAnimal(animalId, out AnimalDefinition animal) && !string.IsNullOrWhiteSpace(animal.title))
            return animal.title;

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
        if (TryGetAnimalStage(animalId, stage, out _, out AnimalStageDefinition stageDefinition))
            return stageDefinition.buffs;

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
        if (_eggCache != null && _animalCache != null)
            return;

        _eggCache = new Dictionary<string, EggDefinition>();
        _animalCache = new Dictionary<string, AnimalDefinition>();

        foreach (EggDefinition definition in _eggs)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.eggId))
                continue;

            _eggCache[definition.eggId] = definition;
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
        _eggCache = null;
        _animalCache = null;
    }
}
