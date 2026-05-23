using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AnimalLoadoutBoardSpawner : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Eggs/EggHatchingCatalog";
    private const string AnimalLoadoutPointPrefix = "animal_loadout_";
    private const int SlotCount = 3;

    [SerializeField] private EggHatchingCatalog _catalog;
    [SerializeField] private string _catalogResourcePath = DefaultCatalogResourcePath;
    [SerializeField] private Transform[] _spawnPoints = new Transform[SlotCount];
    [SerializeField] private bool _resetLocalPose = true;
    [SerializeField, Min(0.1f)] private float _refreshInterval = 1f;

    private readonly Dictionary<int, SpawnedAnimal> _spawnedAnimals = new();
    private float _nextRefreshTime;

    private void Awake()
    {
        EnsureCatalogAssigned();
        EnsureSpawnPointsAssigned();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        RefreshNow();
    }

    private void OnDisable()
    {
        ClearSpawnedAnimals();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + _refreshInterval;
        RefreshNow();
    }

    public void RefreshNow()
    {
        EnsureCatalogAssigned();
        EnsureSpawnPointsAssigned();
        RefreshFromState(EggFeatureStorage.Load());
    }

    private void RefreshFromState(EggFeatureState state)
    {
        if (_catalog == null)
        {
            ClearSpawnedAnimals();
            return;
        }

        state ??= new EggFeatureState();
        state.Normalize();

        for (int i = 0; i < SlotCount; i++)
        {
            PlacedAnimalState placed = state.placedAnimals
                .FirstOrDefault(x => x != null && x.pointId == MakeLoadoutPointId(i));

            RefreshSlot(i, placed);
        }
    }

    private void RefreshSlot(int slotIndex, PlacedAnimalState placed)
    {
        if (!IsValidSlot(slotIndex))
            return;

        Transform anchor = _spawnPoints != null && slotIndex < _spawnPoints.Length
            ? _spawnPoints[slotIndex]
            : null;

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

        GameObject prefab = _catalog.ResolveAnimalPrefab(placed.animalId, safeStage);
        if (prefab == null)
        {
            RemoveSpawnedAnimal(slotIndex);
            return;
        }

        RemoveSpawnedAnimal(slotIndex);

        GameObject animal = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
        if (_resetLocalPose)
        {
            animal.transform.localPosition = Vector3.zero;
            animal.transform.localRotation = Quaternion.identity;
        }

        ApplyStageTint(animal, placed.animalId, safeStage);

        _spawnedAnimals[slotIndex] = new SpawnedAnimal
        {
            AnimalId = placed.animalId,
            Stage = safeStage,
            GameObject = animal
        };
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

    private void EnsureSpawnPointsAssigned()
    {
        if (_spawnPoints == null || _spawnPoints.Length != SlotCount)
            _spawnPoints = new Transform[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            if (_spawnPoints[i] != null)
                continue;

            _spawnPoints[i] = FindChildByName(transform, $"Point{i + 1}");
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

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
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
