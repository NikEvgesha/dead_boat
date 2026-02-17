using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EggHatchingManager : MonoBehaviour
{
    private static EggHatchingManager _instance;
    public static EggHatchingManager Instance => _instance;

    [SerializeField] private EggHatchingCatalog _catalog;
    [SerializeField] private List<EggNestPoint> _nests = new();
    [SerializeField] private Transform _animalsRoot;
    [SerializeField] private bool _autoLoadOnStart = true;

    public event Action StateChanged;

    private EggFeatureState _state;
    private readonly Dictionary<string, GameObject> _spawnedAnimals = new();
    private readonly HashSet<string> _readyNotified = new();
    private float _nextTick;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (_animalsRoot == null)
            _animalsRoot = transform;

        if (_nests == null || _nests.Count == 0)
            _nests = FindObjectsByType<EggNestPoint>(FindObjectsSortMode.None).ToList();
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

        bool changed = false;
        long now = GetNowUnix();

        foreach (EggNestState nest in _state.nests)
        {
            if (nest == null)
                continue;

            if (nest.finishAtUnix <= now)
            {
                if (_readyNotified.Add(nest.nestId))
                    changed = true;
            }
            else
            {
                _readyNotified.Remove(nest.nestId);
            }
        }

        if (changed)
            StateChanged?.Invoke();
    }

    public void LoadState()
    {
        _state = EggFeatureStorage.Load();
        CleanupInvalidData();
        RebuildReadyNotified();
        RestoreSpawnedAnimals();
        RefreshNestVisuals();
        StateChanged?.Invoke();
    }

    public int GetEggAmount(string eggId)
    {
        EnsureStateLoaded();
        return _state.GetEggAmount(eggId);
    }

    public IReadOnlyList<EggInventoryEntry> GetOwnedEggs()
    {
        EnsureStateLoaded();
        return _state.ownedEggs;
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
        SaveAndNotify();
        return true;
    }

    public bool TryPlaceReadyAnimal(string nestId)
    {
        EnsureStateLoaded();

        EggNestState nest = GetNestState(nestId);
        if (nest == null)
            return false;

        if (GetRemainingSeconds(nestId) > 0)
            return false;

        EggNestPoint nestPoint = _nests.Find(x => x != null && x.NestId == nestId);
        if (nestPoint == null)
            return false;

        if (_catalog == null || !_catalog.TryGet(nest.eggId, out EggHatchingDefinition definition))
            return false;

        if (definition.animalPrefab == null)
            return false;

        int spawnPoint = FindFreeSpawnPoint(nestPoint);
        if (spawnPoint < 0)
            return false;

        _state.placedAnimals.Add(new PlacedAnimalState
        {
            nestId = nestId,
            eggId = nest.eggId,
            pointIndex = spawnPoint
        });

        _state.nests.Remove(nest);

        SpawnAnimal(nestPoint, definition, spawnPoint);
        SaveAndNotify();
        return true;
    }

    public static void RegisterEggPickup(string eggId, int amount = 1)
    {
        EggFeatureStorage.AddEgg(eggId, amount);

        if (_instance != null)
        {
            _instance.LoadState();
        }
    }

    private void SaveAndNotify()
    {
        EggFeatureStorage.Save(_state);
        RebuildReadyNotified();
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

        if (_catalog == null)
            return;

        _state.ownedEggs = _state.ownedEggs
            .Where(x => _catalog.TryGet(x.eggId, out _))
            .ToList();

        _state.nests = _state.nests
            .Where(x => _catalog.TryGet(x.eggId, out _))
            .ToList();

        _state.placedAnimals = _state.placedAnimals
            .Where(x => _catalog.TryGet(x.eggId, out _))
            .ToList();

        HashSet<string> knownNestIds = new HashSet<string>(_nests.Where(x => x != null).Select(x => x.NestId));
        _state.nests = _state.nests.Where(x => knownNestIds.Contains(x.nestId)).ToList();
        _state.placedAnimals = _state.placedAnimals.Where(x => knownNestIds.Contains(x.nestId)).ToList();
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
            if (!_catalog.TryGet(placed.eggId, out EggHatchingDefinition definition))
                continue;

            EggNestPoint nestPoint = _nests.Find(x => x != null && x.NestId == placed.nestId);
            if (nestPoint == null)
                continue;

            SpawnAnimal(nestPoint, definition, placed.pointIndex);
        }
    }

    private void SpawnAnimal(EggNestPoint nestPoint, EggHatchingDefinition definition, int pointIndex)
    {
        if (!nestPoint.TryGetSpawnPoint(pointIndex, out Transform point) || point == null)
            return;

        string key = MakeAnimalKey(nestPoint.NestId, pointIndex);

        if (_spawnedAnimals.TryGetValue(key, out GameObject oldAnimal) && oldAnimal != null)
            Destroy(oldAnimal);

        GameObject animal = Instantiate(definition.animalPrefab, point.position, point.rotation, _animalsRoot);
        _spawnedAnimals[key] = animal;
    }

    private int FindFreeSpawnPoint(EggNestPoint nestPoint)
    {
        int total = nestPoint.GetSpawnPointCount();
        if (total <= 0)
            return -1;

        HashSet<int> occupied = new HashSet<int>(
            _state.placedAnimals
                .Where(x => x.nestId == nestPoint.NestId)
                .Select(x => x.pointIndex));

        for (int i = 0; i < total; i++)
        {
            if (!occupied.Contains(i))
                return i;
        }

        return -1;
    }

    private void RefreshNestVisuals()
    {
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

    private static string MakeAnimalKey(string nestId, int pointIndex)
    {
        return $"{nestId}::{pointIndex}";
    }

    private void RebuildReadyNotified()
    {
        _readyNotified.Clear();

        long now = GetNowUnix();
        foreach (EggNestState nest in _state.nests)
        {
            if (nest != null && nest.finishAtUnix <= now)
                _readyNotified.Add(nest.nestId);
        }
    }

    private static long GetNowUnix()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
