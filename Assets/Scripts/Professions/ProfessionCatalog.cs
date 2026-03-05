using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProfessionStarterItem
{
    public PickableItem itemPrefab;
    [Min(1)] public int amount = 1;
}

[Serializable]
public class ProfessionDefinition
{
    public string professionId;
    public string title;
    [TextArea(2, 8)] public string description;
    public Sprite icon;
    [Min(0)] public int startCoinsBonus;
    [Min(0)] public int startGemsBonus;
    public bool defaultUnlocked;
    public List<ProfessionStarterItem> starterItems = new();
    public List<string> perkLines = new();
}

[CreateAssetMenu(fileName = "ProfessionCatalog", menuName = "ScriptableObject/Professions/ProfessionCatalog")]
public class ProfessionCatalog : ScriptableObject
{
    [SerializeField] private List<ProfessionDefinition> _definitions = new();

    private Dictionary<string, ProfessionDefinition> _cache;

    public IReadOnlyList<ProfessionDefinition> Definitions => _definitions;

    public bool TryGet(string professionId, out ProfessionDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(professionId))
            return false;

        BuildCacheIfNeeded();
        return _cache.TryGetValue(professionId, out definition);
    }

    public string GetDefaultProfessionId()
    {
        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            if (definition.defaultUnlocked)
                return definition.professionId;
        }

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            return definition.professionId;
        }

        return string.Empty;
    }

    public List<ProfessionDefinition> GetValidDefinitions()
    {
        List<ProfessionDefinition> result = new();

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            result.Add(definition);
        }

        return result;
    }

    private void BuildCacheIfNeeded()
    {
        if (_cache != null)
            return;

        _cache = new Dictionary<string, ProfessionDefinition>();

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            _cache[definition.professionId] = definition;
        }
    }

    private void OnValidate()
    {
        _cache = null;
    }
}
