using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EggHatchingDefinition
{
    public string eggId;
    public string title;
    [Min(1)] public int incubationSeconds = 300;
    [Min(0)] public int skipCostGems = 10;
    public GameObject animalPrefab;
    public GameObject eggPreviewPrefab;
}

[CreateAssetMenu(fileName = "EggHatchingCatalog", menuName = "ScriptableObject/Eggs/EggHatchingCatalog")]
public class EggHatchingCatalog : ScriptableObject
{
    [SerializeField] private List<EggHatchingDefinition> _definitions = new();

    private Dictionary<string, EggHatchingDefinition> _cache;

    public IReadOnlyList<EggHatchingDefinition> Definitions => _definitions;

    public bool TryGet(string eggId, out EggHatchingDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(eggId))
            return false;

        BuildCacheIfNeeded();
        return _cache.TryGetValue(eggId, out definition);
    }

    private void BuildCacheIfNeeded()
    {
        if (_cache != null)
            return;

        _cache = new Dictionary<string, EggHatchingDefinition>();

        foreach (EggHatchingDefinition definition in _definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.eggId))
                continue;

            _cache[definition.eggId] = definition;
        }
    }

    private void OnValidate()
    {
        _cache = null;
    }
}
