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
    [SerializeField] private bool _autoLoadOnStart = true;
    [SerializeField] private bool _autoCollectFinishedEggs = true;

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

        if (_autoCollectFinishedEggs)
            stateChanged = CollectFinishedNestsInternal();

        if (stateChanged)
            SaveAndNotify();

        TryRestoreMissingSpawnedAnimals();
        ProcessAnimalIncomeTick();
    }

    public void LoadState()
    {
        EnsureCatalogAssigned();
        RefreshSceneReferencesIfNeeded();
        _state = EggFeatureStorage.Load();
        CleanupInvalidData();
        RestoreSpawnedAnimals();
        RefreshNestVisuals();

        if (_autoCollectFinishedEggs && CollectFinishedNestsInternal())
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

    public bool TryGetDefinition(string eggId, out EggHatchingDefinition definition)
    {
        definition = null;
        if (_catalog == null)
            return false;

        return _catalog.TryGet(eggId, out definition);
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

        if (_catalog == null || !_catalog.TryGet(eggId, out EggHatchingDefinition definition))
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

        if (_catalog == null || !_catalog.TryGet(nest.eggId, out EggHatchingDefinition definition))
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

        if (_autoCollectFinishedEggs && CollectFinishedNestsInternal())
        {
            SaveAndNotify();
            return true;
        }

        SaveAndNotify();
        return true;
    }

    // Legacy name kept to avoid breaking UI bindings.
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

        _state.AddAnimal(nest.eggId, 1);
        _state.nests.Remove(nest);

        SaveAndNotify();
        return true;
    }

    public bool TryPlaceAnyOwnedAnimal(string pointId)
    {
        EnsureStateLoaded();

        AnimalInventoryEntry first = _state.ownedAnimals.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.eggId) &&
            x.amount > 0 &&
            _catalog != null &&
            _catalog.TryGet(x.eggId, out EggHatchingDefinition definition) &&
            definition.animalPrefab != null);

        if (first == null)
            return false;

        return TryPlaceAnimalToPoint(pointId, first.eggId);
    }

    public bool TryPlaceAnimalToPoint(string pointId, string eggId)
    {
        EnsureStateLoaded();

        if (string.IsNullOrWhiteSpace(pointId) || string.IsNullOrWhiteSpace(eggId))
            return false;

        if (_catalog == null || !_catalog.TryGet(eggId, out EggHatchingDefinition definition))
            return false;

        if (definition.animalPrefab == null)
            return false;

        AnimalPlacementPoint point = FindAnimalPoint(pointId);
        if (point == null)
            return false;

        if (IsAnimalPointOccupied(pointId))
            return false;

        if (!_state.TryConsumeAnimal(eggId, 1))
            return false;

        PlacedAnimalState placed = new PlacedAnimalState
        {
            pointId = pointId,
            eggId = eggId,
            lastIncomeUnix = GetNowUnix()
        };

        _state.placedAnimals.Add(placed);
        SpawnAnimal(definition, point.SpawnAnchor, MakePointAnimalKey(pointId));

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

        _state.AddAnimal(placed.eggId, 1);
        _state.placedAnimals.Remove(placed);

        string key = MakePointAnimalKey(pointId);
        if (_spawnedAnimals.TryGetValue(key, out GameObject oldAnimal) && oldAnimal != null)
            Destroy(oldAnimal);

        _spawnedAnimals.Remove(key);

        SaveAndNotify();
        return true;
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
        EggFeatureStorage.Save(_state);
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
        // Keep unknown ids to reduce risk of data loss if catalog/scene is temporarily incomplete.
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

            if (_catalog == null || !_catalog.TryGet(placed.eggId, out EggHatchingDefinition definition))
                continue;

            if (definition.animalPrefab == null)
                continue;

            if (!TryResolvePlacementAnchor(placed, out Transform anchor, out string key))
                continue;

            SpawnAnimal(definition, anchor, key);
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

            if (_catalog == null || !_catalog.TryGet(placed.eggId, out EggHatchingDefinition definition))
                continue;

            if (definition.animalPrefab == null)
                continue;

            if (!TryResolvePlacementAnchor(placed, out Transform anchor, out string key))
                continue;

            if (_spawnedAnimals.TryGetValue(key, out GameObject existing) && existing != null)
                continue;

            SpawnAnimal(definition, anchor, key);
        }
    }

    private void SpawnAnimal(EggHatchingDefinition definition, Transform anchor, string key)
    {
        if (anchor == null || definition == null || definition.animalPrefab == null || string.IsNullOrWhiteSpace(key))
            return;

        if (_spawnedAnimals.TryGetValue(key, out GameObject oldAnimal) && oldAnimal != null)
            Destroy(oldAnimal);

        GameObject animal = Instantiate(definition.animalPrefab, anchor.position, anchor.rotation, _animalsRoot);
        _spawnedAnimals[key] = animal;
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

        if (string.IsNullOrWhiteSpace(placed.nestId) || placed.pointIndex < 0)
            return false;

        EggNestPoint nestPoint = FindNestPoint(placed.nestId);
        if (nestPoint == null)
            return false;

        if (!nestPoint.TryGetSpawnPoint(placed.pointIndex, out Transform legacyAnchor) || legacyAnchor == null)
            return false;

        anchor = legacyAnchor;
        key = MakeLegacyAnimalKey(placed.nestId, placed.pointIndex);
        return true;
    }

    private void ProcessAnimalIncomeTick()
    {
        if (_state == null || _state.placedAnimals == null || _state.placedAnimals.Count == 0)
            return;

        CurrencyManager currency = CurrencyManager.Instance;
        if (currency == null)
            return;

        long now = GetNowUnix();
        long totalReward = 0;
        bool changed = false;

        foreach (PlacedAnimalState placed in _state.placedAnimals)
        {
            if (placed == null)
                continue;

            if (_catalog == null || !_catalog.TryGet(placed.eggId, out EggHatchingDefinition definition))
                continue;

            int income = Mathf.Max(0, definition.passiveIncomeCoins);
            if (income <= 0)
                continue;

            int interval = Mathf.Max(1, definition.passiveIncomeIntervalSeconds);

            if (placed.lastIncomeUnix <= 0)
            {
                placed.lastIncomeUnix = now;
                changed = true;
                continue;
            }

            long elapsed = now - placed.lastIncomeUnix;
            if (elapsed < interval)
                continue;

            long cycles = elapsed / interval;
            if (cycles <= 0)
                continue;

            placed.lastIncomeUnix += cycles * interval;
            totalReward += (long)income * cycles;
            changed = true;
        }

        if (totalReward > 0)
        {
            int reward = totalReward > int.MaxValue ? int.MaxValue : (int)totalReward;
            currency.AddCurrency(CurrencyType.Coins, reward);
        }

        if (changed)
            EggFeatureStorage.Save(_state);
    }

    private bool CollectFinishedNestsInternal()
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

            if (nest.finishAtUnix > now)
                continue;

            _state.AddAnimal(nest.eggId, 1);
            _state.nests.RemoveAt(i);
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

            if (_catalog != null && _catalog.TryGet(nest.eggId, out EggHatchingDefinition definition))
                nestPoint.SetEggPreview(definition.eggPreviewPrefab);
            else
                nestPoint.ClearEggPreview();
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

    private static string MakeLegacyAnimalKey(string nestId, int pointIndex)
    {
        return $"legacy::{nestId}::{pointIndex}";
    }

    private static long GetNowUnix()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
