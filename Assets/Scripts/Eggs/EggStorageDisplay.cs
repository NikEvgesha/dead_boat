using System.Collections.Generic;
using UnityEngine;

public class EggStorageDisplay : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Eggs/EggHatchingCatalog";

    [SerializeField] private EggHatchingCatalog _catalog;
    [SerializeField] private string _catalogResourcePath = DefaultCatalogResourcePath;
    [SerializeField] private Transform _displayRoot;
    [SerializeField] private List<Transform> _slots = new();
    [SerializeField] private int _maxDisplayed = 20;
    [SerializeField] private float _fallbackSpacing = 0.6f;

    private readonly List<GameObject> _instances = new();
    private EggHatchingManager _manager;

    private void Start()
    {
        TryBindManager();
        Rebuild();
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.StateChanged -= Rebuild;
    }

    public void Rebuild()
    {
        ClearInstances();

        TryBindManager();
        EnsureCatalogAssigned();

        if (_manager == null || _catalog == null)
            return;

        IReadOnlyList<EggInventoryEntry> eggs = _manager.GetOwnedEggs();
        if (eggs == null || eggs.Count == 0)
            return;

        int shown = 0;
        foreach (EggInventoryEntry egg in eggs)
        {
            if (!_catalog.TryGet(egg.eggId, out EggDefinition definition))
                continue;

            if (definition.eggPreviewPrefab == null)
                continue;

            int count = Mathf.Max(0, egg.amount);
            for (int i = 0; i < count; i++)
            {
                if (shown >= _maxDisplayed)
                    return;

                SpawnPreview(definition.eggPreviewPrefab, shown);
                shown++;
            }
        }
    }

    private void SpawnPreview(GameObject prefab, int index)
    {
        Transform root = _displayRoot != null ? _displayRoot : transform;

        Vector3 position;
        Quaternion rotation;

        if (_slots != null && index >= 0 && index < _slots.Count && _slots[index] != null)
        {
            position = _slots[index].position;
            rotation = _slots[index].rotation;
        }
        else
        {
            position = root.position + root.right * (_fallbackSpacing * index);
            rotation = root.rotation;
        }

        GameObject instance = Instantiate(prefab, position, rotation, root);
        _instances.Add(instance);
    }

    private void ClearInstances()
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null)
                Destroy(_instances[i]);
        }

        _instances.Clear();
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
            _manager.StateChanged -= Rebuild;

        _manager = manager;
        _manager.StateChanged += Rebuild;
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
