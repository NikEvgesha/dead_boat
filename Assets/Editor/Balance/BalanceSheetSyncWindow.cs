using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEngine;

public sealed class BalanceSheetSyncWindow : EditorWindow
{
    private const string DefaultCredentialsPath = "UserSettings/Google/credentials.json";
    private const string DefaultSheetId = "1HSS4jCpcI94jL0jDs6EZqboiEkLthXGt8ib7YOywqYI";
    private const string EggCatalogPath = "Assets/Resources/Eggs/EggHatchingCatalog.asset";
    private const string ProfessionCatalogPath = "Assets/Resources/Professions/ProfessionCatalog.asset";
    private const string BoostersFolder = "Assets/Scripts/LevelStat/Boosters";

    private const string SheetIdPrefsKey = "DeadBoat.BalanceSheetSync.SheetId";
    private const string CredentialsPrefsKey = "DeadBoat.BalanceSheetSync.CredentialsPath";

    private string _sheetId;
    private string _credentialsPath;
    private bool _syncEggs = true;
    private bool _syncProfessions = true;
    private bool _syncBoosters = true;

    [MenuItem("Tools/Balance/Google Sheets Sync")]
    private static void Open()
    {
        GetWindow<BalanceSheetSyncWindow>("Balance Sync");
    }

    private void OnEnable()
    {
        _sheetId = EditorPrefs.GetString(SheetIdPrefsKey, DefaultSheetId);
        _credentialsPath = EditorPrefs.GetString(CredentialsPrefsKey, DefaultCredentialsPath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Google Sheets Balance Sync", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Editor-only sync. Runtime continues to read local ScriptableObject assets.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _sheetId = EditorGUILayout.TextField("Sheet Id", _sheetId);
        using (new EditorGUILayout.HorizontalScope())
        {
            _credentialsPath = EditorGUILayout.TextField("Credentials Path", _credentialsPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Google Service Account Credentials", string.Empty, "json");
                if (!string.IsNullOrWhiteSpace(selectedPath))
                    _credentialsPath = selectedPath;
            }
        }
        _syncEggs = EditorGUILayout.ToggleLeft("Eggs", _syncEggs);
        _syncProfessions = EditorGUILayout.ToggleLeft("Professions", _syncProfessions);
        _syncBoosters = EditorGUILayout.ToggleLeft("Boosters", _syncBoosters);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(SheetIdPrefsKey, _sheetId ?? string.Empty);
            EditorPrefs.SetString(CredentialsPrefsKey, _credentialsPath ?? string.Empty);
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Export Assets -> Sheet", GUILayout.Height(34)))
                ExportSelected();

            if (GUILayout.Button("Import Sheet -> Assets", GUILayout.Height(34)))
                ImportSelected();
        }

        if (GUILayout.Button("Open Sheet In Browser"))
        {
            if (!string.IsNullOrWhiteSpace(_sheetId))
                Application.OpenURL($"https://docs.google.com/spreadsheets/d/{_sheetId}");
        }
    }

    private void ExportSelected()
    {
        SheetsService service = CreateSheetsService(readOnly: false);
        if (service == null)
            return;

        try
        {
            if (_syncEggs)
                WriteSheet(service, BalanceSheetNames.Eggs, BuildEggRows());

            if (_syncProfessions)
                WriteSheet(service, BalanceSheetNames.Professions, BuildProfessionRows());

            if (_syncBoosters)
                WriteSheet(service, BalanceSheetNames.Boosters, BuildBoosterRows());

            Debug.Log("Balance export to Google Sheets finished.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Balance export failed: {exception.Message}");
        }
    }

    private void ImportSelected()
    {
        SheetsService service = CreateSheetsService(readOnly: true);
        if (service == null)
            return;

        try
        {
            if (_syncEggs)
                ImportEggs(ReadSheet(service, BalanceSheetNames.Eggs));

            if (_syncProfessions)
                ImportProfessions(ReadSheet(service, BalanceSheetNames.Professions));

            if (_syncBoosters)
                ImportBoosters(ReadSheet(service, BalanceSheetNames.Boosters));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Balance import from Google Sheets finished.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Balance import failed: {exception.Message}");
        }
    }

    private SheetsService CreateSheetsService(bool readOnly)
    {
        if (string.IsNullOrWhiteSpace(_sheetId))
        {
            Debug.LogError("Balance sync failed: Sheet Id is empty.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_credentialsPath))
        {
            Debug.LogError("Balance sync failed: credentials path is empty.");
            return null;
        }

        try
        {
            string[] scopes = readOnly
                ? new[] { SheetsService.Scope.SpreadsheetsReadonly }
                : new[] { SheetsService.Scope.Spreadsheets };

            GoogleCredential credential = GoogleCredential.FromFile(_credentialsPath).CreateScoped(scopes);
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DeadBoatBalanceSync"
            });
        }
        catch (Exception exception)
        {
            Debug.LogError($"Balance sync failed to initialize Google Sheets service: {exception.Message}");
            return null;
        }
    }

    private void WriteSheet(SheetsService service, string sheetName, IList<IList<object>> rows)
    {
        EnsureSheetExists(service, sheetName);
        service.Spreadsheets.Values.Clear(new ClearValuesRequest(), _sheetId, $"{sheetName}!A:ZZ").Execute();

        ValueRange valueRange = new ValueRange { Values = rows };
        SpreadsheetsResource.ValuesResource.UpdateRequest updateRequest =
            service.Spreadsheets.Values.Update(valueRange, _sheetId, $"{sheetName}!A1");
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        UpdateValuesResponse response = updateRequest.Execute();

        Debug.Log($"Balance sheet '{sheetName}' exported. Rows={rows.Count}, UpdatedCells={response?.UpdatedCells ?? 0}");
    }

    private IList<IList<object>> ReadSheet(SheetsService service, string sheetName)
    {
        ValueRange response = service.Spreadsheets.Values.Get(_sheetId, $"{sheetName}!A1:ZZ").Execute();
        return response.Values ?? new List<IList<object>>();
    }

    private void EnsureSheetExists(SheetsService service, string sheetName)
    {
        Spreadsheet spreadsheet = service.Spreadsheets.Get(_sheetId).Execute();
        bool exists = spreadsheet.Sheets != null &&
            spreadsheet.Sheets.Any(sheet => string.Equals(sheet.Properties?.Title, sheetName, StringComparison.Ordinal));

        if (exists)
            return;

        BatchUpdateSpreadsheetRequest request = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request>
            {
                new Request
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties { Title = sheetName }
                    }
                }
            }
        };

        service.Spreadsheets.BatchUpdate(request, _sheetId).Execute();
    }

    private IList<IList<object>> BuildEggRows()
    {
        List<IList<object>> rows = new List<IList<object>>
        {
            new List<object>
            {
                "eggAssetPath", "eggId", "title", "incubationSeconds", "skipCostGems", "hatchResults"
            }
        };

        EggHatchingCatalog catalog = AssetDatabase.LoadAssetAtPath<EggHatchingCatalog>(EggCatalogPath);
        if (catalog == null)
            return rows;

        foreach (EggDefinition definition in catalog.Definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.eggId))
                continue;

            rows.Add(new List<object>
            {
                AssetDatabase.GetAssetPath(definition),
                definition.eggId,
                definition.title,
                definition.incubationSeconds,
                definition.skipCostGems,
                FormatHatchResults(definition)
            });
        }

        return rows;
    }

    private static string FormatHatchResults(EggDefinition definition)
    {
        if (definition?.hatchResults == null || definition.hatchResults.Count == 0)
            return string.Empty;

        return string.Join(";",
            definition.hatchResults
                .Where(result => result != null && !string.IsNullOrWhiteSpace(result.AnimalId))
                .Select(result => $"{result.AnimalId}:{Mathf.Max(0, result.weight)}"));
    }

    private IList<IList<object>> BuildProfessionRows()
    {
        List<IList<object>> rows = new List<IList<object>>
        {
            new List<object>
            {
                "professionId", "title", "description", "defaultUnlocked",
                "availableInRandomUnlockPool", "randomUnlockWeight", "directSoftCurrencyCost",
                "directSoftCurrencyType", "purchaseProductId", "allowSoftCurrencyFallbackWhenPurchasesUnavailable",
                "startCoinsBonus", "startGemsBonus",
                "perkLines",
                "maxHealthFlat", "moveSpeedFlat", "moveSpeedMultiplier", "experienceMultiplier",
                "saleRewardMultiplier", "maxFuelFlat", "fuelConsumptionMultiplier", "fuelFillMultiplier",
                "boatSpeedFlat", "meleeDamageFlat", "meleeAttackSpeedMultiplier", "rangedDamageFlat",
                "rangedAttackSpeedMultiplier", "rangedReloadSpeedMultiplier"
            }
        };

        ProfessionCatalog catalog = AssetDatabase.LoadAssetAtPath<ProfessionCatalog>(ProfessionCatalogPath);
        if (catalog == null)
            return rows;

        foreach (ProfessionDefinition definition in catalog.Definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            ProfessionPassiveBonuses bonuses = definition.passiveBonuses ?? new ProfessionPassiveBonuses();
            rows.Add(new List<object>
            {
                definition.professionId,
                definition.title,
                definition.description,
                definition.defaultUnlocked ? "TRUE" : "FALSE",
                definition.availableInRandomUnlockPool ? "TRUE" : "FALSE",
                definition.randomUnlockWeight,
                definition.directSoftCurrencyCost,
                definition.directSoftCurrencyType.ToString(),
                definition.purchaseProductId,
                definition.allowSoftCurrencyFallbackWhenPurchasesUnavailable ? "TRUE" : "FALSE",
                definition.startCoinsBonus,
                definition.startGemsBonus,
                JoinLines(definition.perkLines),
                F(bonuses.maxHealthFlat),
                F(bonuses.moveSpeedFlat),
                F(bonuses.moveSpeedMultiplier),
                F(bonuses.experienceMultiplier),
                F(bonuses.saleRewardMultiplier),
                F(bonuses.maxFuelFlat),
                F(bonuses.fuelConsumptionMultiplier),
                F(bonuses.fuelFillMultiplier),
                F(bonuses.boatSpeedFlat),
                F(bonuses.meleeDamageFlat),
                F(bonuses.meleeAttackSpeedMultiplier),
                F(bonuses.rangedDamageFlat),
                F(bonuses.rangedAttackSpeedMultiplier),
                F(bonuses.rangedReloadSpeedMultiplier)
            });
        }

        return rows;
    }

    private IList<IList<object>> BuildBoosterRows()
    {
        List<IList<object>> rows = new List<IList<object>>
        {
            new List<object> { "assetPath", "assetName", "boostType", "chance", "percentage", "rare", "reward", "visualReward" }
        };

        foreach (string guid in AssetDatabase.FindAssets("t:BoostItem", new[] { BoostersFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BoostItem boostItem = AssetDatabase.LoadAssetAtPath<BoostItem>(path);
            if (boostItem == null)
                continue;

            IEnumerable<RareReward> rareRewards = boostItem.RareRewards ?? Enumerable.Empty<RareReward>();
            foreach (RareReward reward in rareRewards)
            {
                rows.Add(new List<object>
                {
                    path,
                    boostItem.name,
                    boostItem.BoostType.ToString(),
                    F(boostItem.Chance),
                    boostItem.Percentage ? "TRUE" : "FALSE",
                    reward.Rare.ToString(),
                    F(reward.Reward),
                    reward.VisualReward
                });
            }
        }

        return rows;
    }

    private void ImportEggs(IList<IList<object>> rows)
    {
        EggHatchingCatalog catalog = AssetDatabase.LoadAssetAtPath<EggHatchingCatalog>(EggCatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning("Egg import skipped: EggHatchingCatalog.asset not found.");
            return;
        }

        List<Row> dataRows = ParseRows(rows);
        foreach (Row row in dataRows)
        {
            string eggId = row.Get("eggId");
            if (string.IsNullOrWhiteSpace(eggId))
                continue;

            if (!catalog.TryGet(eggId, out EggDefinition definition) || definition == null)
            {
                Debug.LogWarning($"Egg import skipped missing egg definition asset: {eggId}");
                continue;
            }

            SerializedObject serializedEgg = new SerializedObject(definition);
            serializedEgg.FindProperty("title").stringValue = row.Get("title");
            serializedEgg.FindProperty("incubationSeconds").intValue = row.GetInt("incubationSeconds", 300);
            serializedEgg.FindProperty("skipCostGems").intValue = row.GetInt("skipCostGems", 0);
            SetHatchResults(serializedEgg.FindProperty("hatchResults"), row.Get("hatchResults"), catalog);
            serializedEgg.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
        }
        Debug.Log($"Egg balance imported. Rows={dataRows.Count}");
    }

    private void ImportProfessions(IList<IList<object>> rows)
    {
        ProfessionCatalog catalog = AssetDatabase.LoadAssetAtPath<ProfessionCatalog>(ProfessionCatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning("Profession import skipped: ProfessionCatalog.asset not found.");
            return;
        }

        List<Row> dataRows = ParseRows(rows);
        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty definitions = serializedCatalog.FindProperty("_definitions");

        foreach (Row row in dataRows)
        {
            string professionId = row.Get("professionId");
            if (string.IsNullOrWhiteSpace(professionId))
                continue;

            SerializedProperty entry = FindArrayElement(definitions, "professionId", professionId);
            if (entry == null)
            {
                entry = AddArrayElement(definitions);
                ClearProfessionObjectReferences(entry);
            }

            SetString(entry, "professionId", professionId);
            SetString(entry, "title", row.Get("title"));
            SetString(entry, "description", row.Get("description"));
            SetBool(entry, "defaultUnlocked", row.GetBool("defaultUnlocked", false));
            SetBool(entry, "availableInRandomUnlockPool", row.GetBool("availableInRandomUnlockPool", true));
            SetInt(entry, "randomUnlockWeight", row.GetInt("randomUnlockWeight", 1));
            SetInt(entry, "directSoftCurrencyCost", row.GetInt("directSoftCurrencyCost", 500));
            SetEnum(entry, "directSoftCurrencyType", row.Get("directSoftCurrencyType"), CurrencyType.Coins);
            SetString(entry, "purchaseProductId", row.Get("purchaseProductId"));
            SetBool(entry, "allowSoftCurrencyFallbackWhenPurchasesUnavailable", row.GetBool("allowSoftCurrencyFallbackWhenPurchasesUnavailable", true));
            SetInt(entry, "startCoinsBonus", row.GetInt("startCoinsBonus", 0));
            SetInt(entry, "startGemsBonus", row.GetInt("startGemsBonus", 0));
            SetStringArray(entry.FindPropertyRelative("perkLines"), SplitLines(row.Get("perkLines")));

            SerializedProperty bonuses = entry.FindPropertyRelative("passiveBonuses");
            SetFloat(bonuses, "maxHealthFlat", row.GetFloat("maxHealthFlat", 0f));
            SetFloat(bonuses, "moveSpeedFlat", row.GetFloat("moveSpeedFlat", 0f));
            SetFloat(bonuses, "moveSpeedMultiplier", row.GetFloat("moveSpeedMultiplier", 1f));
            SetFloat(bonuses, "experienceMultiplier", row.GetFloat("experienceMultiplier", 1f));
            SetFloat(bonuses, "saleRewardMultiplier", row.GetFloat("saleRewardMultiplier", 1f));
            SetFloat(bonuses, "maxFuelFlat", row.GetFloat("maxFuelFlat", 0f));
            SetFloat(bonuses, "fuelConsumptionMultiplier", row.GetFloat("fuelConsumptionMultiplier", 1f));
            SetFloat(bonuses, "fuelFillMultiplier", row.GetFloat("fuelFillMultiplier", 1f));
            SetFloat(bonuses, "boatSpeedFlat", row.GetFloat("boatSpeedFlat", 0f));
            SetFloat(bonuses, "meleeDamageFlat", row.GetFloat("meleeDamageFlat", 0f));
            SetFloat(bonuses, "meleeAttackSpeedMultiplier", row.GetFloat("meleeAttackSpeedMultiplier", 1f));
            SetFloat(bonuses, "rangedDamageFlat", row.GetFloat("rangedDamageFlat", 0f));
            SetFloat(bonuses, "rangedAttackSpeedMultiplier", row.GetFloat("rangedAttackSpeedMultiplier", 1f));
            SetFloat(bonuses, "rangedReloadSpeedMultiplier", row.GetFloat("rangedReloadSpeedMultiplier", 1f));
        }

        serializedCatalog.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
        Debug.Log($"Profession balance imported. Rows={dataRows.Count}");
    }

    private void ImportBoosters(IList<IList<object>> rows)
    {
        List<Row> dataRows = ParseRows(rows);
        Dictionary<string, List<Row>> rowsByAssetPath = dataRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Get("assetPath")))
            .GroupBy(row => row.Get("assetPath"))
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (KeyValuePair<string, List<Row>> pair in rowsByAssetPath)
        {
            BoostItem boostItem = AssetDatabase.LoadAssetAtPath<BoostItem>(pair.Key);
            if (boostItem == null)
            {
                Debug.LogWarning($"Booster import skipped missing asset: {pair.Key}");
                continue;
            }

            SerializedObject serializedBoost = new SerializedObject(boostItem);
            Row first = pair.Value[0];
            SerializedProperty boostType = serializedBoost.FindProperty("_boostType");
            if (Enum.TryParse(first.Get("boostType"), out BoostType parsedBoostType))
                boostType.enumValueIndex = (int)parsedBoostType;

            serializedBoost.FindProperty("_chance").floatValue = first.GetFloat("chance", 1f);
            serializedBoost.FindProperty("_percentage").boolValue = first.GetBool("percentage", false);

            SerializedProperty rareRewards = serializedBoost.FindProperty("_Rare");
            if (rareRewards == null)
            {
                Debug.LogWarning($"Booster import skipped asset without _Rare list: {pair.Key}");
                continue;
            }

            rareRewards.arraySize = pair.Value.Count;

            for (int i = 0; i < pair.Value.Count; i++)
            {
                Row row = pair.Value[i];
                SerializedProperty reward = rareRewards.GetArrayElementAtIndex(i);

                if (Enum.TryParse(row.Get("rare"), out RareType parsedRare))
                    reward.FindPropertyRelative("Rare").enumValueIndex = (int)parsedRare;

                reward.FindPropertyRelative("Reward").floatValue = row.GetFloat("reward", 0f);
                reward.FindPropertyRelative("VisualReward").stringValue = row.Get("visualReward");
            }

            serializedBoost.ApplyModifiedProperties();
            EditorUtility.SetDirty(boostItem);
        }

        Debug.Log($"Booster balance imported. Assets={rowsByAssetPath.Count}, Rows={dataRows.Count}");
    }

    private static List<Row> ParseRows(IList<IList<object>> rows)
    {
        List<Row> result = new List<Row>();
        if (rows == null || rows.Count < 2)
            return result;

        List<string> header = rows[0].Select(CellToString).ToList();
        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i] == null || rows[i].Count == 0)
                continue;

            result.Add(new Row(header, rows[i]));
        }

        return result;
    }

    private static SerializedProperty FindArrayElement(SerializedProperty array, string relativeKey, string key)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            SerializedProperty keyProperty = element.FindPropertyRelative(relativeKey);
            if (keyProperty != null && string.Equals(keyProperty.stringValue, key, StringComparison.Ordinal))
                return element;
        }

        return null;
    }

    private static SerializedProperty AddArrayElement(SerializedProperty array)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        return array.GetArrayElementAtIndex(index);
    }

    private static void ClearProfessionObjectReferences(SerializedProperty entry)
    {
        SerializedProperty icon = entry.FindPropertyRelative("icon");
        if (icon != null)
            icon.objectReferenceValue = null;

        SerializedProperty starterItems = entry.FindPropertyRelative("starterItems");
        if (starterItems != null)
            starterItems.arraySize = 0;
    }

    private static void SetHatchResults(SerializedProperty array, string value, EggHatchingCatalog catalog)
    {
        if (array == null || catalog == null)
            return;

        List<(AnimalDefinition animal, int weight)> results = new List<(AnimalDefinition animal, int weight)>();
        if (!string.IsNullOrWhiteSpace(value))
        {
            string[] parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                string[] pair = part.Split(new[] { ':' }, 2);
                string animalId = pair[0].Trim();
                int weight = 1;
                if (pair.Length > 1)
                    int.TryParse(pair[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out weight);

                if (!catalog.TryGetAnimal(animalId, out AnimalDefinition animal) || animal == null)
                {
                    Debug.LogWarning($"Egg import skipped missing animal definition asset: {animalId}");
                    continue;
                }

                results.Add((animal, Mathf.Max(0, weight)));
            }
        }

        array.arraySize = results.Count;
        for (int i = 0; i < results.Count; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("animal").objectReferenceValue = results[i].animal;
            element.FindPropertyRelative("weight").intValue = results[i].weight;
        }
    }

    private static void SetString(SerializedProperty root, string relativePath, string value)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        if (property != null)
            property.stringValue = value ?? string.Empty;
    }

    private static void SetInt(SerializedProperty root, string relativePath, int value)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        if (property != null)
            property.intValue = value;
    }

    private static void SetFloat(SerializedProperty root, string relativePath, float value)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        if (property != null)
            property.floatValue = value;
    }

    private static void SetBool(SerializedProperty root, string relativePath, bool value)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetEnum<TEnum>(SerializedProperty root, string relativePath, string value, TEnum fallback)
        where TEnum : struct, Enum
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        if (property == null)
            return;

        TEnum parsed = fallback;
        if (!string.IsNullOrWhiteSpace(value))
            Enum.TryParse(value, true, out parsed);

        property.enumValueIndex = Convert.ToInt32(parsed, CultureInfo.InvariantCulture);
    }

    private static void SetStringArray(SerializedProperty array, List<string> values)
    {
        if (array == null)
            return;

        array.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            array.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static string JoinLines(List<string> lines)
    {
        if (lines == null || lines.Count == 0)
            return string.Empty;

        return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static List<string> SplitLines(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Replace("\\n", "\n")
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string CellToString(object value)
    {
        return value?.ToString() ?? string.Empty;
    }

    private static class BalanceSheetNames
    {
        public const string Eggs = "Eggs";
        public const string Professions = "Professions";
        public const string Boosters = "Boosters";
    }

    private sealed class Row
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Row(IReadOnlyList<string> header, IList<object> cells)
        {
            for (int i = 0; i < header.Count; i++)
            {
                string key = header[i];
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                _values[key] = i < cells.Count ? CellToString(cells[i]) : string.Empty;
            }
        }

        public string Get(string key)
        {
            return _values.TryGetValue(key, out string value) ? value : string.Empty;
        }

        public int GetInt(string key, int fallback)
        {
            string value = Get(key);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        public float GetFloat(string key, float fallback)
        {
            string value = Get(key);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        public bool GetBool(string key, bool fallback)
        {
            string value = Get(key);
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            if (bool.TryParse(value, out bool parsed))
                return parsed;

            if (string.Equals(value, "1", StringComparison.Ordinal))
                return true;

            if (string.Equals(value, "0", StringComparison.Ordinal))
                return false;

            return fallback;
        }
    }
}
