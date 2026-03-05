using System;
using System.Collections.Generic;

[Serializable]
public class ProfessionState
{
    public int version = 1;
    public string selectedProfessionId;
    public List<string> unlockedProfessionIds = new();

    public void Normalize(IReadOnlyList<ProfessionDefinition> definitions, string fallbackDefaultProfessionId)
    {
        unlockedProfessionIds ??= new List<string>();

        HashSet<string> validIds = new();
        for (int i = 0; i < definitions.Count; i++)
        {
            ProfessionDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            validIds.Add(definition.professionId);
        }

        HashSet<string> uniqueUnlocked = new();
        List<string> normalizedUnlocked = new();
        for (int i = 0; i < unlockedProfessionIds.Count; i++)
        {
            string id = unlockedProfessionIds[i];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!validIds.Contains(id))
                continue;

            if (!uniqueUnlocked.Add(id))
                continue;

            normalizedUnlocked.Add(id);
        }

        string defaultProfessionId = ResolveDefaultProfessionId(definitions, fallbackDefaultProfessionId);
        if (!string.IsNullOrWhiteSpace(defaultProfessionId) && !uniqueUnlocked.Contains(defaultProfessionId))
        {
            uniqueUnlocked.Add(defaultProfessionId);
            normalizedUnlocked.Insert(0, defaultProfessionId);
        }

        unlockedProfessionIds = normalizedUnlocked;

        if (string.IsNullOrWhiteSpace(selectedProfessionId) || !uniqueUnlocked.Contains(selectedProfessionId))
            selectedProfessionId = defaultProfessionId;
    }

    private static string ResolveDefaultProfessionId(IReadOnlyList<ProfessionDefinition> definitions, string fallbackDefaultProfessionId)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            ProfessionDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            if (definition.defaultUnlocked)
                return definition.professionId;
        }

        if (!string.IsNullOrWhiteSpace(fallbackDefaultProfessionId))
            return fallbackDefaultProfessionId;

        for (int i = 0; i < definitions.Count; i++)
        {
            ProfessionDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            return definition.professionId;
        }

        return string.Empty;
    }
}
