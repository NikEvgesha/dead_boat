using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocationSpawnCollection", menuName = "Spawning/LocationSpawnCollection")]
public class LocationSpawnCollection : ScriptableObject
{
    // Сериализованный список всех вариантов спавна (каждый с весом chance и диапазоном дистанций)
    [SerializeField] private List<LocationSpawnItem> spawnItems;

    public const int CurrentPoolRevision = 3;
    [SerializeField, Min(0)] private int _legacyPoolSize;
    [SerializeField, Min(0)] private int _revisionOnePoolSize;
    [SerializeField] private List<LocationSpawnItem> _revisionTwoSpawnItems;

    // Apply to an owned runtime copy. Seeded snapshots depend on the original pool order.
    public void ApplyPoolRevision(int revision)
    {
        if (revision >= CurrentPoolRevision) return;

        // Village consolidation changes order/count, so retain the complete old
        // pool for revision 2 before applying the earlier prefix migrations.
        bool changed = _revisionTwoSpawnItems != null && _revisionTwoSpawnItems.Count > 0;
        if (changed) spawnItems = new List<LocationSpawnItem>(_revisionTwoSpawnItems);
        if (revision <= 1)
        {
            int poolSize = revision <= 0 ? _legacyPoolSize : _revisionOnePoolSize;
            if (poolSize > 0 && poolSize < spawnItems.Count)
            {
                spawnItems.RemoveRange(poolSize, spawnItems.Count - poolSize);
                changed = true;
            }
        }
        if (changed) OnEnable();
    }

    // Рабочий список тех элементов, которые ещё не выпали в этом «цикле»
    private List<LocationSpawnItem> _unusedItems;

    // Временный список всех элементов, которые соответствуют текущей дистанции
    private List<LocationSpawnItem> _actualSpawnItems;

    private void OnEnable()
    {
        // При первом включении/заходе в память инициализируем _unusedItems как копию всех spawnItems.
        // Если ScriptableObject загружается заново между сессиями, OnEnable будет вызван снова.
        _unusedItems = new List<LocationSpawnItem>(spawnItems);
        _actualSpawnItems = new List<LocationSpawnItem>();
    }

    /// <summary>
    /// Возвращает один случайный префаб с учётом весов и с гарантией, 
    /// что один и тот же элемент не выпадет повторно, пока не будут использованы все доступные по дистанции.
    /// </summary>
    public LocationContentSpawner GetRandomSpawnPrefab(float distance, DeadBoat.Online.RunRandom random = null)
    {
        // Шаг 1: Собираем все элементы, которые сейчас подходят по distance
        _actualSpawnItems.Clear();
        foreach (var item in spawnItems)
        {
            bool okMin = !item.HaveMinSpawnDistence || (item.HaveMinSpawnDistence && item.MinSpawnDistence < distance);
            bool okMax = !item.HaveMaxSpawnDistence || (item.HaveMaxSpawnDistence && item.MaxSpawnDistence > distance);
            if (okMin && okMax)
            {
                _actualSpawnItems.Add(item);
            }
        }

        // Если вообще нет ни одного элемента, подходящего по дистанции — возвращаем null
        if (_actualSpawnItems.Count == 0)
            return null;

        // Шаг 2: Ищем пересечение между _actualSpawnItems и _unusedItems — это наши кандидаты
        List<LocationSpawnItem> candidates = new List<LocationSpawnItem>();
        foreach (var candidate in _actualSpawnItems)
        {
            if (_unusedItems.Contains(candidate))
                candidates.Add(candidate);
        }

        // Шаг 3: Если все подходящие элементы уже «использованы» (пересечение пусто),
        // то начинаем новый цикл: «сбрасываем» _unusedItems только элементами из _actualSpawnItems
        if (candidates.Count == 0)
        {
            // Очистить _unusedItems и добавить туда каждый элемент, подходящий по current distance
            _unusedItems.Clear();
            _unusedItems.AddRange(_actualSpawnItems);

            // И теперь кандидаты = все _actualSpawnItems, потому что мы только что сбросили список
            candidates = new List<LocationSpawnItem>(_actualSpawnItems);
        }

        // Шаг 4: Делаем взвешенный рандомный выбор из candidates
        float totalWeight = 0f;
        foreach (var item in candidates)
        {
            totalWeight += item.chance;
        }

        // Если по какой-то причине totalWeight получилось 0 или отрицательным, возвращаем первый элемент
        if (totalWeight <= 0f)
        {
            var fallback = candidates[0];
            // Удаляем из _unusedItems, чтобы он «отработался» в этом цикле
            _unusedItems.Remove(fallback);
            return ResolveBank(fallback, random);
        }

        float randomValue = (random != null ? random.Range(0f, totalWeight) : Random.Range(0f, totalWeight));
        LocationSpawnItem chosenItem = null;
        foreach (var item in candidates)
        {
            if (randomValue < item.chance)
            {
                chosenItem = item;
                break;
            }
            randomValue -= item.chance;
        }

        // На всякий случай, если loop ни разу не выбрал (из-за плавающей арифметики), берём последний
        if (chosenItem == null)
            chosenItem = candidates[candidates.Count - 1];

        // Шаг 5: Удаляем выбранный элемент из _unusedItems, чтобы он не повторился в этом цикле
        _unusedItems.Remove(chosenItem);

        // Возвращаем сам префаб
        return ResolveBank(chosenItem, random);
    }

    private static LocationContentSpawner ResolveBank(LocationSpawnItem item, DeadBoat.Online.RunRandom random)
    {
        // Existing entries consume no extra random value; only the unified
        // village selects between its two placements after the family is chosen.
        if (item.bankAlternativePrefab == null) return item.prefab;
        int bank = random != null ? random.Range(0, 2) : Random.Range(0, 2);
        return bank == 0 ? item.prefab : item.bankAlternativePrefab;
    }
}
