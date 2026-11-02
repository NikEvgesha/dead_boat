using System.Collections.Generic;
using UnityEngine;

public class LocationContentSpawner : MonoBehaviour
{
    private const string DefaultBalanceProfileResourcePath = "LocationBalanceProfile";

    [Header("Items")]
    [Tooltip("ScriptableObject with location item spawn entries")]
    public LocationItemSpawnCollection itemCollection;

    [Tooltip("Optional scene balance profile. If empty, Assets/Resources/LocationBalanceProfile is used.")]
    [SerializeField] private LocationBalanceProfile _balanceProfile;

    [Tooltip("Points where collectable items can appear")]
    public List<LocationSpawnPoint> itemSpawnPoints;

    [Header("Enemies")]
    [Tooltip("Enemy prefabs that can defend this location")]
    public List<ZombieController> _enemyPrefabs;

    [Tooltip("Points where enemies can appear")]
    public List<Transform> enemySpawnPoints;

    public float ZPosition;

    private int _level = 1;
    private bool contentInitialized, rehydrating;
    private float restoredLogicalZ;
    private IReadOnlyDictionary<ulong, string> restoredItemNames;
    public int GenerationLevel => _level;
    // Called only on an inactive staging root by the explicit residency transaction.
    public void RestoreGeneration(int index, float logicalZ, IReadOnlyDictionary<ulong, string> itemNames)
    {
        if (contentInitialized || gameObject.activeInHierarchy) throw new System.InvalidOperationException("Inactive fresh location required");
        rehydrating = true;
        restoredItemNames = itemNames;
        restoredLogicalZ = logicalZ;
        InitializeGeneration(index);
        Start();
    }
    private int generationIndex = -1;
    private string generationKey;
    private DeadBoat.Online.RunRandom itemRandom;
    private DeadBoat.Online.RunRandom enemyRandom;
    private List<(PickableItem item, string key)> authoredItems;
    public void InitializeGeneration(int index)
    {
        generationIndex = index;
        authoredItems = null;
        if (!DeadBoat.Online.SharedRunContext.GenerationActive) return;
        // Snapshot asset-authored pickups before Start can create store display models
        // or generated loot. Capture paths now, before sibling lists can change.
        foreach (var item in GetComponentsInChildren<PickableItem>(true))
        {
            if (item.GetComponent<DeadBoat.Online.WorldSpawnIdentity>() != null ||
                item.GetComponentInParent<EnemyCore>(true) != null || item.GetComponentInParent<StorePoint>(true) != null ||
                item.GetComponentInParent<Inventory>(true) != null) continue;
            authoredItems ??= new();
            authoredItems.Add((item, "authored:" + gameObject.scene.name + ":" + DeadBoat.Online.WorldSpawnIdentity.StablePath(item.transform)));
        }
    }
    private string stableGenerationKey;
    public void InitializeStableGeneration(string key) => stableGenerationKey = key;
    public string SharedGenerationKey => stableGenerationKey ?? (generationIndex >= 0
        ? "procedural:" + generationIndex : DeadBoat.Online.WorldSpawnIdentity.StableScenePath(transform));

    private void Start()
    {
        if (contentInitialized) return;
        contentInitialized = true;
        if (DeadBoat.Online.SharedRunContext.GenerationActive)
        {
            // Scene-authored locations use their stable hierarchy path; procedural ones use their index.
            generationKey = SharedGenerationKey;
            itemRandom = DeadBoat.Online.SharedRunContext.Random("items:" + generationKey, 0);
            enemyRandom = DeadBoat.Online.SharedRunContext.Random("enemies:" + generationKey, 0);
        }
        var membership = DeadBoat.Online.SharedRunContext.Membership;
        bool tracked = false;
        if (generationIndex >= 0 && membership != null)
        {
            float logicalZ = transform.position.z + (FixCoordinate.Instance != null ? FixCoordinate.Instance.PlayerAddPos : DeadBoat.Online.LobbyNetworkAvatar.Origin.z);
            var owner = GetComponentInParent<LocationSpawner>();
            if (owner != null && owner.LayoutManifest != null && owner.LayoutManifest.TryGet(generationIndex, out var step)) logicalZ = step.LogicalZ;
            tracked = membership.Begin(generationIndex, rehydrating ? restoredLogicalZ : logicalZ);
        }
        if (DeadBoat.Online.SharedRunContext.GenerationActive && authoredItems != null)
            foreach (var authored in authoredItems)
                if (authored.item != null && authored.item.GetComponent<DeadBoat.Online.WorldSpawnIdentity>() == null)
                {
                    if (rehydrating) DeadBoat.Online.WorldRegionRestore.InitializeItem(authored.item.gameObject, authored.key, restoredItemNames);
                    else authored.item.gameObject.AddComponent<DeadBoat.Online.WorldSpawnIdentity>().Key = authored.key;
                }
        LocationSceneBalance balance = ResolveBalance();
        SpawnItems(ResolveItemCollection());
        SpawnEnemies(balance);
        if (tracked) membership.Seal(generationIndex);
    }

    private void SpawnItems(LocationItemSpawnCollection activeItemCollection)
    {
        if (activeItemCollection == null || itemSpawnPoints == null)
            return;

        int guaranteedEggSpawnPointIndex = FindGuaranteedEggSpawnPointIndex(activeItemCollection);

        for (int i = 0; i < itemSpawnPoints.Count; i++)
        {
            LocationSpawnPoint spawnPoint = itemSpawnPoints[i];
            if (spawnPoint == null || spawnPoint.spawnTransform == null)
                continue;

            PickableItem itemPrefab = null;
            bool forceEgg = i == guaranteedEggSpawnPointIndex;
            if (forceEgg)
                activeItemCollection.TryGetRandomEggItem(spawnPoint.allowedType, spawnPoint.allowedSize, out itemPrefab, itemRandom);

            itemPrefab ??= activeItemCollection.GetRandomItem(spawnPoint.allowedType, spawnPoint.allowedSize, itemRandom);
            if (itemPrefab == null)
                continue;

            var item = Instantiate(itemPrefab.gameObject, spawnPoint.spawnTransform.position, spawnPoint.spawnTransform.rotation, transform);
            if (rehydrating) DeadBoat.Online.WorldRegionRestore.InitializeItem(item, generationKey + ":item:" + i, restoredItemNames);
            else if (DeadBoat.Online.SharedRunContext.GenerationActive)
                item.AddComponent<DeadBoat.Online.WorldSpawnIdentity>().Key = generationKey + ":item:" + i;

            if (!rehydrating && LocationItemSpawnCollection.IsEggPrefab(itemPrefab))
                EggSpawnRuntimeState.OnEggSpawned();
        }
    }

    private int FindGuaranteedEggSpawnPointIndex(LocationItemSpawnCollection activeItemCollection)
    {
        if (DeadBoat.Online.SharedRunContext.GenerationActive ? generationIndex != 0 : !EggSpawnRuntimeState.ShouldGuaranteeFirstEggSpawn)
            return -1;

        if (activeItemCollection == null || itemSpawnPoints == null || itemSpawnPoints.Count == 0)
            return -1;

        List<int> candidateIndexes = new List<int>();
        for (int i = 0; i < itemSpawnPoints.Count; i++)
        {
            LocationSpawnPoint spawnPoint = itemSpawnPoints[i];
            if (spawnPoint == null || spawnPoint.spawnTransform == null)
                continue;

            if (activeItemCollection.HasEggItem(spawnPoint.allowedType, spawnPoint.allowedSize))
                candidateIndexes.Add(i);
        }

        if (candidateIndexes.Count == 0)
            return -1;

        return candidateIndexes[itemRandom != null ? itemRandom.Range(0, candidateIndexes.Count) : Random.Range(0, candidateIndexes.Count)];
    }

    private void SpawnEnemies(LocationSceneBalance balance)
    {
        if (_enemyPrefabs == null || _enemyPrefabs.Count == 0 || enemySpawnPoints == null || enemySpawnPoints.Count == 0)
            return;

        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < enemySpawnPoints.Count; i++)
        {
            if (enemySpawnPoints[i] != null)
                spawnPoints.Add(enemySpawnPoints[i]);
        }

        Shuffle(spawnPoints, enemyRandom);

        int enemyLevel = balance != null ? balance.ResolveEnemyLevel(_level) : _level;
        int maxSpawns = balance != null ? balance.ResolveLocationEnemyLimit(spawnPoints.Count) : spawnPoints.Count;
        float spawnChance = balance != null ? balance.locationEnemySpawnChance : 1f;
        int spawnedCount = 0;

        for (int i = 0; i < spawnPoints.Count && spawnedCount < maxSpawns; i++)
        {
            if ((enemyRandom != null ? enemyRandom.Value : Random.value) > spawnChance)
                continue;

            ZombieController enemyPrefab = _enemyPrefabs[enemyRandom != null ? enemyRandom.Range(0, _enemyPrefabs.Count) : Random.Range(0, _enemyPrefabs.Count)];
            if (enemyPrefab == null)
                continue;

            if (rehydrating)
            {
                // Consume exactly the same RNG choices and spawn budget, even for
                // a dead or detached actor that must not be instantiated again.
                DeadBoat.Online.WorldRegionRestore.InitializeEnemy(enemyPrefab, generationKey + ":enemy:" + i, transform);
                spawnedCount++;
                continue;
            }
            Transform enemyPoint = spawnPoints[i];
            ZombieController enemy = Instantiate(enemyPrefab, enemyPoint.position, enemyPoint.rotation, transform);
            enemy.InitializeLevel(enemyLevel);
            if (DeadBoat.Online.SharedRunContext.GenerationActive)
            {
                var identity = enemy.gameObject.AddComponent<DeadBoat.Online.WorldSpawnIdentity>();
                identity.TemplateId = DeadBoat.Online.SharedEnemiesRuntime.RegisterTemplate(enemyPrefab);
                identity.Key = generationKey + ":enemy:" + i;
            }
            spawnedCount++;
        }
    }

    public void SetLevel(int level)
    {
        _level = level;
    }

    private LocationItemSpawnCollection ResolveItemCollection()
    {
        EnsureBalanceProfile();
        return _balanceProfile != null ? _balanceProfile.ResolveItemCollection(itemCollection) : itemCollection;
    }

    private LocationSceneBalance ResolveBalance()
    {
        EnsureBalanceProfile();
        return _balanceProfile != null ? _balanceProfile.GetCurrentSceneBalance() : null;
    }

    private void EnsureBalanceProfile()
    {
        if (_balanceProfile != null)
            return;

        _balanceProfile = Resources.Load<LocationBalanceProfile>(DefaultBalanceProfileResourcePath);
    }

    private static void Shuffle<T>(List<T> values, DeadBoat.Online.RunRandom random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random != null ? random.Range(0, i + 1) : Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
