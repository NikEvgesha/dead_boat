using System;
using System.Collections.Generic;

[Serializable]
public class ProfessionState
{
    public int version = 1;
    public string selectedProfessionId;
    public bool hasExplicitProfessionChoice;
    public List<string> unlockedProfessionIds = new();
    public List<string> purchasedProfessionIds = new();

    public void Normalize(IReadOnlyList<ProfessionDefinition> definitions, string fallbackDefaultProfessionId)
    {
        unlockedProfessionIds ??= new List<string>();
        purchasedProfessionIds ??= new List<string>();

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
        NormalizePurchased(validIds);

        if (!hasExplicitProfessionChoice || string.IsNullOrWhiteSpace(selectedProfessionId) || !uniqueUnlocked.Contains(selectedProfessionId))
        {
            selectedProfessionId = defaultProfessionId;

            if (string.IsNullOrWhiteSpace(selectedProfessionId) || !uniqueUnlocked.Contains(selectedProfessionId))
            {
                selectedProfessionId = string.Empty;
                hasExplicitProfessionChoice = false;
            }
            else
            {
                hasExplicitProfessionChoice = true;
            }
        }
    }

    private void NormalizePurchased(HashSet<string> validIds)
    {
        HashSet<string> uniquePurchased = new();
        List<string> normalizedPurchased = new();
        for (int i = 0; i < purchasedProfessionIds.Count; i++)
        {
            string id = purchasedProfessionIds[i];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!validIds.Contains(id))
                continue;

            if (!uniquePurchased.Add(id))
                continue;

            normalizedPurchased.Add(id);
        }

        purchasedProfessionIds = normalizedPurchased;
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
