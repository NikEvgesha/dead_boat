using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EggZooCatalogSync
{
    public const string DefaultAnimalPrefabFolder = "Assets/_models/ZOO/PrefabZoo";
    public const string DefaultEggPrefabFolder = "Assets/_models/ZOO/PrefabEgg";
    public const string DefaultAnimalDefinitionFolder = "Assets/Resources/Eggs/Definitions/Animals";
    public const string DefaultEggDefinitionFolder = "Assets/Resources/Eggs/Definitions/Eggs";
    public const string DefaultCatalogPath = "Assets/Resources/Eggs/EggHatchingCatalog.asset";

    [MenuItem("Tools/Eggs/Sync Zoo Prefabs To Catalog")]
    public static void SyncDefaultFolders()
    {
        Sync(DefaultCatalogPath, DefaultAnimalPrefabFolder, DefaultEggPrefabFolder);
    }

    public static void Sync(string catalogPath, string animalPrefabFolder, string eggPrefabFolder)
    {
        EnsureFolder(DefaultAnimalDefinitionFolder);
        EnsureFolder(DefaultEggDefinitionFolder);

        List<AnimalDefinition> animals = BuildAnimalDefinitions(animalPrefabFolder);
        AddExistingDefinition("condor", animals);

        List<EggDefinition> eggs = BuildEggDefinitions(eggPrefabFolder, animals);

        EggHatchingCatalog catalog = AssetDatabase.LoadAssetAtPath<EggHatchingCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<EggHatchingCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SetObjectArray(serializedCatalog.FindProperty("_eggs"), eggs);
        SetObjectArray(serializedCatalog.FindProperty("_animals"), animals);
        serializedCatalog.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Egg zoo catalog synced. Eggs={eggs.Count}, Animals={animals.Count}");
    }

    private static List<AnimalDefinition> BuildAnimalDefinitions(string folder)
    {
        List<AnimalDefinition> result = new List<AnimalDefinition>();
        foreach (string prefabPath in FindPrefabs(folder).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                continue;

            string id = BuildId(Path.GetFileNameWithoutExtension(prefabPath));
            if (string.IsNullOrWhiteSpace(id))
                continue;

            AnimalDefinition animal = GetOrCreateAsset<AnimalDefinition>($"{DefaultAnimalDefinitionFolder}/{id}.asset");
            SetGeneratedId(animal, id);
            animal.title = Path.GetFileNameWithoutExtension(prefabPath);
            animal.maxStage = 3;
            animal.stages = new List<AnimalStageDefinition>
            {
                CreateStage(1, prefab, Color.white, 1f),
                CreateStage(2, prefab, new Color(1f, 0.9f, 0.65f, 1f), 1.03f),
                CreateStage(3, prefab, new Color(1f, 0.72f, 0.35f, 1f), 1.06f)
            };

            EditorUtility.SetDirty(animal);
            result.Add(animal);
        }

        return result;
    }

    private static List<EggDefinition> BuildEggDefinitions(string folder, IReadOnlyList<AnimalDefinition> animals)
    {
        List<EggDefinition> result = new List<EggDefinition>();
        List<string> prefabs = FindPrefabs(folder)
            .OrderBy(GetTrailingNumber)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (prefabs.Count > 0)
        {
            AnimalDefinition chicken = animals.FirstOrDefault(animal => animal != null && animal.animalId == "chicken");
            result.Add(CreateEgg("egg_chicken", "Chicken Egg", prefabs[0], MakePool(chicken ?? animals.FirstOrDefault())));
        }

        if (prefabs.Count > 1)
        {
            AnimalDefinition condor = AssetDatabase.LoadAssetAtPath<AnimalDefinition>($"{DefaultAnimalDefinitionFolder}/condor.asset");
            result.Add(CreateEgg("egg_condor", "Condor Egg", prefabs[1], MakePool(condor ?? animals.FirstOrDefault())));
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            string number = GetTrailingNumber(prefabs[i]).ToString();
            string id = $"egg_{number}";
            string title = $"Egg {number}";
            result.Add(CreateEgg(id, title, prefabs[i], MakeRotatingPool(animals, i)));
        }

        return result
            .Where(egg => egg != null)
            .GroupBy(egg => egg.eggId)
            .Select(group => group.First())
            .ToList();
    }

    private static EggDefinition CreateEgg(string id, string title, string prefabPath, List<EggHatchResult> hatchResults)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return null;

        EggDefinition egg = GetOrCreateAsset<EggDefinition>($"{DefaultEggDefinitionFolder}/{id}.asset");
        SetGeneratedId(egg, id);
        egg.title = title;
        egg.incubationSeconds = 1800;
        egg.skipCostGems = 25;
        egg.eggPreviewPrefab = prefab;
        egg.hatchResults = hatchResults ?? new List<EggHatchResult>();
        EditorUtility.SetDirty(egg);
        return egg;
    }

    private static List<EggHatchResult> MakePool(AnimalDefinition animal)
    {
        if (animal == null)
            return new List<EggHatchResult>();

        return new List<EggHatchResult>
        {
            new EggHatchResult { animal = animal, weight = 100 }
        };
    }

    private static List<EggHatchResult> MakeRotatingPool(IReadOnlyList<AnimalDefinition> animals, int offset)
    {
        List<EggHatchResult> pool = new List<EggHatchResult>();
        if (animals == null || animals.Count == 0)
            return pool;

        pool.Add(new EggHatchResult { animal = animals[offset % animals.Count], weight = 70 });
        if (animals.Count > 1)
            pool.Add(new EggHatchResult { animal = animals[(offset + 1) % animals.Count], weight = 25 });
        if (animals.Count > 2)
            pool.Add(new EggHatchResult { animal = animals[(offset + 2) % animals.Count], weight = 5 });

        return pool;
    }

    private static AnimalStageDefinition CreateStage(int stage, GameObject prefab, Color tint, float saleRewardMultiplier)
    {
        return new AnimalStageDefinition
        {
            stage = stage,
            animalPrefab = prefab,
            tint = tint,
            buffs = new AnimalRunBuffs
            {
                saleRewardMultiplier = saleRewardMultiplier
            }
        };
    }

    private static void AddExistingDefinition(string id, List<AnimalDefinition> animals)
    {
        if (animals.Any(animal => animal != null && animal.animalId == id))
            return;

        AnimalDefinition existing = AssetDatabase.LoadAssetAtPath<AnimalDefinition>($"{DefaultAnimalDefinitionFolder}/{id}.asset");
        if (existing != null)
            animals.Add(existing);
    }

    private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void SetGeneratedId(UnityEngine.Object asset, string id)
    {
        SerializedObject serialized = new SerializedObject(asset);
        SerializedProperty property = serialized.FindProperty("_id");
        if (property != null)
            property.stringValue = id;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray<T>(SerializedProperty array, IReadOnlyList<T> values)
        where T : UnityEngine.Object
    {
        if (array == null)
            return;

        array.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static IEnumerable<string> FindPrefabs(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return Enumerable.Empty<string>();

        return AssetDatabase.FindAssets("t:Prefab", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrWhiteSpace(path));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static int GetTrailingNumber(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        StringBuilder digits = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsDigit(name[i]))
                digits.Append(name[i]);
        }

        return int.TryParse(digits.ToString(), out int value) ? value : int.MaxValue;
    }

    private static string BuildId(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        string trimmed = source.Trim();
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = char.ToLowerInvariant(trimmed[i]);
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                builder.Append(c);
                continue;
            }

            if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                builder.Append('_');
        }

        return builder.ToString().Trim('_');
    }
}
