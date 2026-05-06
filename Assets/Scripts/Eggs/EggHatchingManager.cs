using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EggHatchingManager : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Eggs/EggHatchingCatalog";

    private static EggHatchingManager _instance;
    public static EggHatchingManager Instance => _instance;

    [SerializeField] private EggHatchingCatalog _catalog;
    [SerializeField] private string _catalogResourcePath = DefaultCatalogResourcePath;
    [SerializeField] private List<EggNestPoint> _nests = new();
    [SerializeField] private List<AnimalPlacementPoint> _animalPoints = new();
    [SerializeField] private Transform _animalsRoot;
    [SerializeField] private bool _parentAnimalsToAnchor = true;
    [SerializeField] private bool _resetAnimalLocalPoseWhenParented = true;
    [SerializeField] private bool _autoLoadOnStart = true;
    [SerializeField] private bool _autoCollectFinishedEggs = false;

    public event Action StateChanged;

    private EggFeatureState _state;
    private readonly Dictionary<string, GameObject> _spawnedAnimals = new();
    private float _nextTick;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        EnsureCatalogAssigned();

        if (_animalsRoot == null)
            _animalsRoot = transform;

        if (_nests == null || _nests.Count == 0)
            _nests = FindObjectsByType<EggNestPoint>(FindObjectsSortMode.None).ToList();
        else
            _nests = _nests.Where(x => x != null).Distinct().ToList();

        if (_animalPoints == null || _animalPoints.Count == 0)
            _animalPoints = FindObjectsByType<AnimalPlacementPoint>(FindObjectsSortMode.None).ToList();
        else
            _animalPoints = _animalPoints.Where(x => x != null).Distinct().ToList();
    }

    private void Start()
    {
        if (!_autoLoadOnStart)
            return;

        LoadState();
    }

    private void Update()
    {
        if (_state == null)
            return;

        if (Time.unscaledTime < _nextTick)
            return;

        _nextTick = Time.unscaledTime + 1f;

        bool stateChanged = false;

        stateChanged = MarkFinishedNestsReadyInternal();

        if (stateChanged)
            SaveAndNotify();

        TryRestoreMissingSpawnedAnimals();
    }

    public void LoadState()
    {
        EnsureCatalogAssigned();
        RefreshSceneReferencesIfNeeded();
        _state = EggFeatureStorage.Load();
        CleanupInvalidData();
        RestoreSpawnedAnimals();
        RefreshNestVisuals();

        if (MarkFinishedNestsReadyInternal())
        {
            SaveAndNotify();
            return;
        }

        StateChanged?.Invoke();
    }

    public int GetEggAmount(string eggId)
    {
        EnsureStateLoaded();
        return _state.GetEggAmount(eggId);
    }

    public int GetAnimalAmount(string eggId)
    {
        EnsureStateLoaded();
        return _state.GetAnimalAmount(eggId);
    }

    public int GetAnimalAmount(string animalId, int stage)
    {
        EnsureStateLoaded();
        return _state.GetAnimalAmount(animalId, stage);
    }

    public IReadOnlyList<EggInventoryEntry> GetOwnedEggs()
    {
        EnsureStateLoaded();
        return _state.ownedEggs;
    }

    public IReadOnlyList<AnimalInventoryEntry> GetOwnedAnimals()
    {
        EnsureStateLoaded();
        return _state.ownedAnimals;
    }

    public bool TryGetDefinition(string eggId, out EggDefinition definition)
    {
        definition = null;
        if (_catalog == null)
            return false;

        return _catalog.TryGet(eggId, out definition);
    }

    public bool TryGetAnimalDetails(string animalId, int stage, out string title, out string detail)
    {
        title = string.Empty;
        detail = string.Empty;

        if (_catalog == null || string.IsNullOrWhiteSpace(animalId))
            return false;

        int safeStage = Mathf.Max(1, stage);
        if (_catalog.ResolveAnimalPrefab(animalId, safeStage) == null)
            return false;

        title = _catalog.GetAnimalTitle(animalId);

        AnimalRunBuffs buffs = _catalog.GetAnimalBuffs(animalId, safeStage);
        detail = FormatBuffSummary(buffs);
        return true;
    }

    public EggNestState GetNestState(string nestId)
    {
        EnsureStateLoaded();
        return _state.nests.Find(x => x.nestId == nestId);
    }

    public bool IsNestReady(string nestId)
    {
        EggNestState nest = GetNestState(nestId);
        if (nest == null)
            return false;

        return GetRemainingSeconds(nestId) <= 0;
    }

    public int GetRemainingSeconds(string nestId)
    {
        EggNestState nest = GetNestState(nestId);
        if (nest == null)
            return 0;

        long remaining = nest.finishAtUnix - GetNowUnix();
        return remaining > 0 ? (int)remaining : 0;
    }

    public bool TryStartIncubationWithAnyEgg(string nestId)
    {
        EnsureStateLoaded();

        EggInventoryEntry first = _state.ownedEggs.FirstOrDefault(x => _catalog != null && _catalog.TryGet(x.eggId, out _));
        if (first == null)
            return false;

        return TryStartIncubation(nestId, first.eggId);
    }

    public bool TryStartIncubation(string nestId, string eggId)
    {
        EnsureStateLoaded();

        if (string.IsNullOrWhiteSpace(nestId) || string.IsNullOrWhiteSpace(eggId))
            return false;

        if (_catalog == null || !_catalog.TryGet(eggId, out EggDefinition definition))
            return false;

        if (!_state.TryConsumeEgg(eggId, 1))
            return false;

        EggNestState nest = GetNestState(nestId);
        if (nest != null)
            _state.nests.Remove(nest);

        _state.nests.Add(new EggNestState
        {
            nestId = nestId,
            eggId = eggId,
            hatchedAnimalId = _catalog.RollAnimalId(definition),
            hatchedStage = 1,
            durationSeconds = Mathf.Max(1, definition.incubationSeconds),
            finishAtUnix = GetNowUnix() + Mathf.Max(1, definition.incubationSeconds)
        });

        SaveAndNotify();
        return true;
    }

    public bool TrySkipIncubation(string nestId)
    {
        EnsureStateLoaded();

        EggNestState nest = GetNestState(nestId);
        if (nest == null)
            return false;

        if (GetRemainingSeconds(nestId) <= 0)
            return true;

        if (_catalog == null || !_catalog.TryGet(nest.eggId, out EggDefinition definition))
            return false;

        int price = Mathf.Max(0, definition.skipCostGems);
        if (price > 0)
        {
            if (CurrencyManager.Instance == null)
                return false;

            if (!CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Gems, price))
                return false;

            if (!CurrencyManager.Instance.RemoveCurrency(CurrencyType.Gems, price))
                return false;
        }

        nest.finishAtUnix = GetNowUnix();
        nest.isReady = true;

        SaveAndNotify();
        return true;
    }

    public bool TryPlaceReadyAnimal(string nestId)
    {
        return TryCollectReadyAnimal(nestId);
    }

    public bool TryCollectReadyAnimal(string nestId)
    {
        EnsureStateLoaded();

        EggNestState nest = GetNestState(nestId);
        if (nest == null)
            return false;

        if (GetRemainingSeconds(nestId) > 0)
            return false;

        if (string.IsNullOrWhiteSpace(nest.hatchedAnimalId))
        {
            if (_catalog != null && _catalog.TryGet(nest.eggId, out EggDefinition definition))
                nest.hatchedAnimalId = _catalog.RollAnimalId(definition);
        }

        if (string.IsNullOrWhiteSpace(nest.hatchedAnimalId))
            return false;

        int stage = Mathf.Max(1, nest.hatchedStage);
        _state.AddAnimal(nest.hatchedAnimalId, stage, 1);
        _state.nests.Remove(nest);

        SaveAndNotify();
        return true;
    }

    public bool TryPlaceAnyOwnedAnimal(string pointId)
    {
        EnsureStateLoaded();

        AnimalInventoryEntry first = _state.ownedAnimals.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.animalId) &&
            x.amount > 0 &&
            _catalog != null &&
            _catalog.ResolveAnimalPrefab(x.animalId, Mathf.Max(1, x.stage)) != null);

        if (first == null)
            return false;

        return TryPlaceAnimalToPoint(pointId, first.animalId, Mathf.Max(1, first.stage));
    }

    public bool TryPlaceAnimalToPoint(string pointId, string eggId)
    {
        return TryPlaceAnimalToPoint(pointId, eggId, 1);
    }

    public bool TryPlaceAnimalToPoint(string pointId, string animalId, int stage)
    {
        EnsureStateLoaded();

        if (string.IsNullOrWhiteSpace(pointId) || string.IsNullOrWhiteSpace(animalId))
            return false;

        if (_catalog == null)
            return false;

        int safeStage = Mathf.Max(1, stage);
        GameObject animalPrefab = _catalog.ResolveAnimalPrefab(animalId, safeStage);
        if (animalPrefab == null)
            return false;

        AnimalPlacementPoint point = FindAnimalPoint(pointId);
        if (point == null)
            return false;

        if (IsAnimalPointOccupied(pointId))
            return false;

        if (!_state.TryConsumeAnimal(animalId, safeStage, 1))
            return false;

        PlacedAnimalState placed = new PlacedAnimalState
        {
            pointId = pointId,
            animalId = animalId,
            stage = safeStage,
            lastIncomeUnix = GetNowUnix()
        };

        _state.placedAnimals.Add(placed);
        SpawnAnimal(animalId, safeStage, animalPrefab, point.SpawnAnchor, MakePointAnimalKey(pointId));

        SaveAndNotify();
        return true;
    }

    public bool TryRemovePlacedAnimal(string pointId)
    {
        EnsureStateLoaded();

        if (string.IsNullOrWhiteSpace(pointId))
            return false;

        PlacedAnimalState placed = _state.placedAnimals.Find(x => x != null && x.pointId == pointId);
        if (placed == null)
            return false;

        _state.AddAnimal(placed.animalId, Mathf.Max(1, placed.stage), 1);
        _state.placedAnimals.Remove(placed);

        string key = MakePointAnimalKey(pointId);
        if (_spawnedAnimals.TryGetValue(key, out GameObject oldAnimal) && oldAnimal != null)
            Destroy(oldAnimal);

        _spawnedAnimals.Remove(key);

        SaveAndNotify();
        return true;
    }

    public bool CanMergeAnimal(string animalId, int stage)
    {
        EnsureStateLoaded();

        if (string.IsNullOrWhiteSpace(animalId) || _catalog == null)
            return false;

        int safeStage = Mathf.Max(1, stage);
        if (safeStage >= _catalog.GetMaxStage(animalId))
            return false;

        return _state.GetAnimalAmount(animalId, safeStage) >= 2;
    }

    public bool TryMergeAnimal(string animalId, int stage)
    {
        EnsureStateLoaded();

        if (!CanMergeAnimal(animalId, stage))
            return false;

        int safeStage = Mathf.Max(1, stage);
        if (!_state.TryConsumeAnimal(animalId, safeStage, 2))
            return false;

        _state.AddAnimal(animalId, safeStage + 1, 1);
        SpawnMergeFeedback(animalId, safeStage + 1);
        SaveAndNotify();
        return true;
    }

    public bool TryMergeAnyAvailableAnimal()
    {
        EnsureStateLoaded();

        for (int i = 0; i < _state.ownedAnimals.Count; i++)
        {
            AnimalInventoryEntry entry = _state.ownedAnimals[i];
            if (entry == null)
                continue;

            if (TryMergeAnimal(entry.animalId, Mathf.Max(1, entry.stage)))
                return true;
        }

        return false;
    }

    public bool IsAnimalPointOccupied(string pointId)
    {
        return TryGetPlacedAnimal(pointId, out _);
    }

    public bool TryGetPlacedAnimal(string pointId, out PlacedAnimalState placed)
    {
        EnsureStateLoaded();
        placed = _state.placedAnimals.Find(x => x != null && x.pointId == pointId);
        return placed != null;
    }

    public static void RegisterEggPickup(string eggId, int amount = 1)
    {
        EggFeatureStorage.AddEgg(eggId, amount);

        if (_instance != null)
            _instance.LoadState();
    }

    private void SaveAndNotify()
    {
        _state.Normalize();
        EggFeatureStorage.Save(_state);
        EggAnimalBuffService.MarkDirty();
        RefreshNestVisuals();
        StateChanged?.Invoke();
    }

    private void EnsureStateLoaded()
    {
        if (_state == null)
            LoadState();
    }

    private void CleanupInvalidData()
    {
        _state.Normalize();
    }

    private void RestoreSpawnedAnimals()
    {
        foreach (GameObject value in _spawnedAnimals.Values)
        {
            if (value != null)
                Destroy(value);
        }

        _spawnedAnimals.Clear();

        foreach (PlacedAnimalState placed in _state.placedAnimals)
        {
            if (placed == null)
                continue;

            if (_catalog == null)
                continue;

            GameObject animalPrefab = _catalog.ResolveAnimalPrefab(placed.animalId, Mathf.Max(1, placed.stage));
            if (animalPrefab == null)
                continue;

            if (!TryResolvePlacementAnchor(placed, out Transform anchor, out string key))
                continue;

            SpawnAnimal(placed.animalId, Mathf.Max(1, placed.stage), animalPrefab, anchor, key);
        }
    }

    private void TryRestoreMissingSpawnedAnimals()
    {
        if (_state == null || _state.placedAnimals == null || _state.placedAnimals.Count == 0)
            return;

        foreach (PlacedAnimalState placed in _state.placedAnimals)
        {
            if (placed == null)
                continue;

            if (_catalog == null)
                continue;

            GameObject animalPrefab = _catalog.ResolveAnimalPrefab(placed.animalId, Mathf.Max(1, placed.stage));
            if (animalPrefab == null)
                continue;

            if (!TryResolvePlacementAnchor(placed, out Transform anchor, out string key))
                continue;

            if (_spawnedAnimals.TryGetValue(key, out GameObject existing) && existing != null)
                continue;

            SpawnAnimal(placed.animalId, Mathf.Max(1, placed.stage), animalPrefab, anchor, key);
        }
    }

    private void SpawnAnimal(string animalId, int stage, GameObject animalPrefab, Transform anchor, string key)
    {
        if (anchor == null || animalPrefab == null || string.IsNullOrWhiteSpace(key))
            return;

        if (_spawnedAnimals.TryGetValue(key, out GameObject oldAnimal) && oldAnimal != null)
            Destroy(oldAnimal);

        Transform parent = _parentAnimalsToAnchor
            ? anchor
            : (_animalsRoot != null ? _animalsRoot : transform);

        GameObject animal = Instantiate(animalPrefab, anchor.position, anchor.rotation, parent);

        if (_parentAnimalsToAnchor && _resetAnimalLocalPoseWhenParented)
        {
            animal.transform.localPosition = Vector3.zero;
            animal.transform.localRotation = Quaternion.identity;
        }

        ApplyStageTint(animal, animalId, stage);

        _spawnedAnimals[key] = animal;
    }

    private void ApplyStageTint(GameObject animal, string animalId, int stage)
    {
        if (animal == null || _catalog == null)
            return;

        if (!_catalog.TryGetAnimalStage(animalId, stage, out _, out AnimalStageDefinition stageDefinition))
            return;

        if (stageDefinition.tint == Color.white)
            return;

        Renderer[] renderers = animal.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null || !material.HasProperty("_Color"))
                    continue;

                material.color *= stageDefinition.tint;
            }
        }
    }

    private void SpawnMergeFeedback(string animalId, int resultStage)
    {
        if (_catalog == null)
            return;

        GameObject particlesPrefab = _catalog.GetMergeParticlesPrefab(animalId, resultStage);
        if (particlesPrefab == null)
            return;

        Transform root = _animalsRoot != null ? _animalsRoot : transform;
        Instantiate(particlesPrefab, root.position, root.rotation);
    }

    private bool TryResolvePlacementAnchor(PlacedAnimalState placed, out Transform anchor, out string key)
    {
        anchor = null;
        key = string.Empty;

        if (placed == null)
            return false;

        if (!string.IsNullOrWhiteSpace(placed.pointId))
        {
            AnimalPlacementPoint point = FindAnimalPoint(placed.pointId);
            if (point == null || point.SpawnAnchor == null)
                return false;

            anchor = point.SpawnAnchor;
            key = MakePointAnimalKey(placed.pointId);
            return true;
        }

        return false;
    }

    private bool MarkFinishedNestsReadyInternal()
    {
        if (_state == null || _state.nests == null || _state.nests.Count == 0)
            return false;

        long now = GetNowUnix();
        bool changed = false;

        for (int i = _state.nests.Count - 1; i >= 0; i--)
        {
            EggNestState nest = _state.nests[i];

            if (nest == null || string.IsNullOrWhiteSpace(nest.eggId))
            {
                _state.nests.RemoveAt(i);
                changed = true;
                continue;
            }

            if (nest.finishAtUnix > now || nest.isReady)
                continue;

            if (string.IsNullOrWhiteSpace(nest.hatchedAnimalId))
            {
                if (_catalog != null && _catalog.TryGet(nest.eggId, out EggDefinition definition))
                    nest.hatchedAnimalId = _catalog.RollAnimalId(definition);
            }

            nest.hatchedStage = Mathf.Max(1, nest.hatchedStage);
            nest.isReady = true;
            changed = true;
        }

        return changed;
    }

    private void RefreshNestVisuals()
    {
        if (_state == null)
            return;

        RefreshSceneReferencesIfNeeded();

        foreach (EggNestPoint nestPoint in _nests)
        {
            if (nestPoint == null)
                continue;

            EggNestState nest = _state.nests.Find(x => x.nestId == nestPoint.NestId);
            if (nest == null)
            {
                nestPoint.ClearEggPreview();
                continue;
            }

            if (nest.isReady && _catalog != null)
            {
                GameObject animalPreview = _catalog.ResolveAnimalPrefab(nest.hatchedAnimalId, nest.hatchedStage);
                if (animalPreview != null)
                    nestPoint.SetEggPreview(animalPreview);
                else if (_catalog.TryGet(nest.eggId, out EggDefinition readyDefinition))
                    nestPoint.SetEggPreview(readyDefinition.eggPreviewPrefab);
                else
                    nestPoint.ClearEggPreview();
            }
            else if (_catalog != null && _catalog.TryGet(nest.eggId, out EggDefinition definition))
            {
                nestPoint.SetEggPreview(definition.eggPreviewPrefab);
            }
            else
            {
                nestPoint.ClearEggPreview();
            }
        }
    }

    private AnimalPlacementPoint FindAnimalPoint(string pointId)
    {
        if (string.IsNullOrWhiteSpace(pointId))
            return null;

        RefreshSceneReferencesIfNeeded();
        return _animalPoints.Find(x => x != null && x.PointId == pointId);
    }

    private EggNestPoint FindNestPoint(string nestId)
    {
        if (string.IsNullOrWhiteSpace(nestId))
            return null;

        RefreshSceneReferencesIfNeeded();
        return _nests.Find(x => x != null && x.NestId == nestId);
    }

    private void RefreshSceneReferencesIfNeeded()
    {
        if (_nests == null || _nests.Count == 0 || _nests.Any(x => x == null))
            _nests = FindObjectsByType<EggNestPoint>(FindObjectsSortMode.None).ToList();
        else
            _nests = _nests.Where(x => x != null).Distinct().ToList();

        if (_animalPoints == null || _animalPoints.Count == 0 || _animalPoints.Any(x => x == null))
            _animalPoints = FindObjectsByType<AnimalPlacementPoint>(FindObjectsSortMode.None).ToList();
        else
            _animalPoints = _animalPoints.Where(x => x != null).Distinct().ToList();
    }

    private static string MakePointAnimalKey(string pointId)
    {
        return $"point::{pointId}";
    }

    private static long GetNowUnix()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static string FormatBuffSummary(AnimalRunBuffs buffs)
    {
        if (buffs == null)
            return "No buffs";

        List<string> parts = new();
        AppendFlat(parts, "HP", buffs.maxHealthFlat);
        AppendFlat(parts, "Speed", buffs.moveSpeedFlat);
        AppendMultiplier(parts, "Move", buffs.SafeMoveSpeedMultiplier);
        AppendMultiplier(parts, "XP", buffs.SafeExperienceMultiplier);
        AppendMultiplier(parts, "Sale", buffs.SafeSaleRewardMultiplier);
        AppendFlat(parts, "Fuel", buffs.maxFuelFlat);
        AppendMultiplier(parts, "Fuel use", buffs.SafeFuelConsumptionMultiplier, invertSign: true);
        AppendMultiplier(parts, "Fuel fill", buffs.SafeFuelFillMultiplier);
        AppendFlat(parts, "Boat", buffs.boatSpeedFlat);
        AppendFlat(parts, "Melee", buffs.meleeDamageFlat);
        AppendMultiplier(parts, "Melee spd", buffs.SafeMeleeAttackSpeedMultiplier);
        AppendFlat(parts, "Ranged", buffs.rangedDamageFlat);
        AppendMultiplier(parts, "Range spd", buffs.SafeRangedAttackSpeedMultiplier);
        AppendMultiplier(parts, "Reload", buffs.SafeRangedReloadSpeedMultiplier, invertSign: true);

        return parts.Count == 0 ? "No buffs" : string.Join(", ", parts);
    }

    private static void AppendFlat(List<string> parts, string label, float value)
    {
        if (Mathf.Approximately(value, 0f))
            return;

        string sign = value > 0f ? "+" : string.Empty;
        parts.Add($"{label} {sign}{Mathf.RoundToInt(value)}");
    }

    private static void AppendMultiplier(List<string> parts, string label, float multiplier, bool invertSign = false)
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        if (percent == 0)
            return;

        if (invertSign)
            percent *= -1;

        string sign = percent > 0 ? "+" : string.Empty;
        parts.Add($"{label} {sign}{percent}%");
    }

    private void EnsureCatalogAssigned()
    {
        if (_catalog != null)
            return;

        string path = string.IsNullOrWhiteSpace(_catalogResourcePath)
            ? DefaultCatalogResourcePath
            : _catalogResourcePath;

        _catalog = Resources.Load<EggHatchingCatalog>(path);
    }
}
