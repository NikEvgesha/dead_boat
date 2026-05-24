using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AnimalPlacementPoints : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Eggs/EggHatchingCatalog";
    private const string AnimalLoadoutPointPrefix = "animal_loadout_";
    private const int DefaultSlotCount = 3;

    [SerializeField] private EggHatchingCatalog _catalog;
    [SerializeField] private string _catalogResourcePath = DefaultCatalogResourcePath;
    [SerializeField] private Transform[] _slotAnchors = new Transform[DefaultSlotCount];
    [SerializeField] private Transform _spawnRoot;
    [SerializeField] private bool _parentSpawnedToAnchor;
    [SerializeField] private bool _resetLocalPoseWhenParented = true;
    [SerializeField] private Vector3 _spawnLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 _spawnLocalEulerOffset = Vector3.zero;
    [SerializeField, Min(0.1f)] private float _refreshInterval = 0.5f;

    private readonly Dictionary<int, SpawnedAnimal> _spawnedAnimals = new();
    private EggHatchingManager _manager;
    private float _nextRefreshTime;

    private void Awake()
    {
        EnsureCatalogAssigned();
        EnsureSlotAnchorsAssigned();
    }

    private void OnEnable()
    {
        BindManager();
        RefreshNow();
    }

    private void OnDisable()
    {
        UnbindManager();
        ClearSpawnedAnimals();
    }

    private void Update()
    {
        BindManager();

        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + _refreshInterval;
        RefreshNow();
    }

    public void RefreshNow()
    {
        EnsureCatalogAssigned();
        EnsureSlotAnchorsAssigned();

        int slotCount = GetSlotCount();
        for (int i = 0; i < slotCount; i++)
            RefreshSlot(i, GetPlacedLoadoutAnimal(i));

        RemoveExtraSpawnedAnimals(slotCount);
    }

    private void BindManager()
    {
        EggHatchingManager current = EggHatchingManager.Instance;
        if (current == _manager)
            return;

        UnbindManager();

        _manager = current;
        if (_manager != null)
            _manager.StateChanged += HandleManagerStateChanged;
    }

    private void UnbindManager()
    {
        if (_manager != null)
            _manager.StateChanged -= HandleManagerStateChanged;

        _manager = null;
    }

    private void HandleManagerStateChanged()
    {
        RefreshNow();
    }

    private void RefreshSlot(int slotIndex, PlacedAnimalState placed)
    {
        Transform anchor = GetSlotAnchor(slotIndex);
        if (anchor == null || placed == null || string.IsNullOrWhiteSpace(placed.animalId))
        {
            RemoveSpawnedAnimal(slotIndex);
            return;
        }

        int safeStage = Mathf.Max(1, placed.stage);
        if (_spawnedAnimals.TryGetValue(slotIndex, out SpawnedAnimal existing) &&
            existing.GameObject != null &&
            existing.AnimalId == placed.animalId &&
            existing.Stage == safeStage)
        {
            return;
        }

        GameObject prefab = _catalog != null
            ? _catalog.ResolveAnimalPrefab(placed.animalId, safeStage)
            : null;

        if (prefab == null)
        {
            RemoveSpawnedAnimal(slotIndex);
            return;
        }

        RemoveSpawnedAnimal(slotIndex);

        Vector3 position = anchor.TransformPoint(_spawnLocalOffset);
        Quaternion rotation = anchor.rotation * Quaternion.Euler(_spawnLocalEulerOffset);
        Transform parent = _parentSpawnedToAnchor
            ? anchor
            : (_spawnRoot != null ? _spawnRoot : transform);

        GameObject animal = Instantiate(prefab, position, rotation, parent);
        if (_parentSpawnedToAnchor && _resetLocalPoseWhenParented)
        {
            animal.transform.localPosition = _spawnLocalOffset;
            animal.transform.localRotation = Quaternion.Euler(_spawnLocalEulerOffset);
        }

        ApplyStageTint(animal, placed.animalId, safeStage);

        _spawnedAnimals[slotIndex] = new SpawnedAnimal
        {
            AnimalId = placed.animalId,
            Stage = safeStage,
            GameObject = animal
        };
    }

    private PlacedAnimalState GetPlacedLoadoutAnimal(int slotIndex)
    {
        if (_manager != null)
            return _manager.GetAnimalLoadoutSlot(slotIndex);

        EggFeatureState state = EggFeatureStorage.Load();
        if (state == null || state.placedAnimals == null)
            return null;

        string pointId = MakeLoadoutPointId(slotIndex);
        for (int i = 0; i < state.placedAnimals.Count; i++)
        {
            PlacedAnimalState placed = state.placedAnimals[i];
            if (placed != null && placed.pointId == pointId)
                return placed;
        }

        return null;
    }

    private Transform GetSlotAnchor(int slotIndex)
    {
        if (_slotAnchors == null || slotIndex < 0 || slotIndex >= _slotAnchors.Length)
            return null;

        return _slotAnchors[slotIndex];
    }

    private int GetSlotCount()
    {
        if (_manager != null)
            return Mathf.Max(0, _manager.AnimalLoadoutSlotCount);

        return _slotAnchors != null && _slotAnchors.Length > 0
            ? _slotAnchors.Length
            : DefaultSlotCount;
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

    private void EnsureSlotAnchorsAssigned()
    {
        int slotCount = GetSlotCount();
        if (_slotAnchors == null || _slotAnchors.Length != slotCount)
        {
            Transform[] resized = new Transform[slotCount];
            if (_slotAnchors != null)
            {
                int copyCount = Mathf.Min(_slotAnchors.Length, resized.Length);
                for (int i = 0; i < copyCount; i++)
                    resized[i] = _slotAnchors[i];
            }

            _slotAnchors = resized;
        }

        for (int i = 0; i < _slotAnchors.Length; i++)
        {
            if (_slotAnchors[i] != null)
                continue;

            _slotAnchors[i] = FindChildByName(transform, $"Animal_{i + 1:00}");
            if (_slotAnchors[i] == null)
                _slotAnchors[i] = FindChildByName(transform, $"Point{i + 1}");
        }
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.name == targetName)
                return child;

            Transform nested = FindChildByName(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
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
            Renderer animalRenderer = renderers[i];
            if (animalRenderer == null)
                continue;

            Material[] materials = animalRenderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null || !material.HasProperty("_Color"))
                    continue;

                material.color *= stageDefinition.tint;
            }
        }
    }

    private void RemoveExtraSpawnedAnimals(int slotCount)
    {
        List<int> extraSlots = null;
        foreach (int slotIndex in _spawnedAnimals.Keys)
        {
            if (slotIndex < slotCount)
                continue;

            extraSlots ??= new List<int>();
            extraSlots.Add(slotIndex);
        }

        if (extraSlots == null)
            return;

        for (int i = 0; i < extraSlots.Count; i++)
            RemoveSpawnedAnimal(extraSlots[i]);
    }

    private void ClearSpawnedAnimals()
    {
        foreach (SpawnedAnimal spawned in _spawnedAnimals.Values)
            DestroySpawnedAnimal(spawned.GameObject);

        _spawnedAnimals.Clear();
    }

    private void RemoveSpawnedAnimal(int slotIndex)
    {
        if (!_spawnedAnimals.TryGetValue(slotIndex, out SpawnedAnimal spawned))
            return;

        DestroySpawnedAnimal(spawned.GameObject);
        _spawnedAnimals.Remove(slotIndex);
    }

    private void DestroySpawnedAnimal(GameObject animal)
    {
        if (animal == null)
            return;

        if (Application.isPlaying)
            Destroy(animal);
        else
            DestroyImmediate(animal);
    }

    private static string MakeLoadoutPointId(int slotIndex)
    {
        return $"{AnimalLoadoutPointPrefix}{slotIndex}";
    }

    private sealed class SpawnedAnimal
    {
        public string AnimalId;
        public int Stage;
        public GameObject GameObject;
    }
}
