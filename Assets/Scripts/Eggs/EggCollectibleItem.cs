using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PickableItem))]
public class EggCollectibleItem : MonoBehaviour
{
    [SerializeField] private string _eggId;
    [SerializeField] private int _amount = 1;
    [Header("Optional one-shot meta pickup")]
    [SerializeField] private bool _collectOnce;
    [SerializeField] private string _uniqueCollectibleId;

    public string EggId => _eggId;
    public int Amount => _amount;

    private void Start()
    {
        if (!_collectOnce)
            return;

        string collectibleId = ResolveUniqueCollectibleId();
        if (EggFeatureStorage.IsOneShotEggCollected(collectibleId))
            gameObject.SetActive(false);
    }

    public bool TryCollect()
    {
        if (string.IsNullOrWhiteSpace(_eggId))
        {
            Debug.LogWarning($"EggCollectibleItem on '{name}' has empty eggId");
            return false;
        }

        int amount = _amount > 0 ? _amount : 1;
        bool collected;

        bool countInRun = LoadingManager.Instance == null ||
                          LoadingManager.Instance.CurrentLocation == Location.Game;

        if (_collectOnce)
        {
            collected = EggHatchingManager.RegisterOneShotEggPickup(ResolveUniqueCollectibleId(), _eggId, amount, countInRun);
        }
        else
        {
            EggHatchingManager.RegisterEggPickup(_eggId, amount, countInRun);
            collected = true;
        }

        return collected;
    }

    private string ResolveUniqueCollectibleId()
    {
        if (!string.IsNullOrWhiteSpace(_uniqueCollectibleId))
            return _uniqueCollectibleId;

        string scenePath = gameObject.scene.IsValid() ? gameObject.scene.path : "no_scene";
        return $"{scenePath}:{GetHierarchyPath(transform)}:{_eggId}";
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
