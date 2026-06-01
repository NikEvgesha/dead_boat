using UnityEngine;

public class EggFeatureUnlockVisibility : MonoBehaviour
{
    [SerializeField] private GameObject[] _eggNestObjects;
    [SerializeField] private GameObject[] _animalPlacementObjects;
    [SerializeField] private GameObject[] _animalMergeObjects;
    [SerializeField] private GameObject[] _eggNestTutorialObjects;
    [SerializeField] private GameObject[] _animalPlacementTutorialObjects;

    private EggHatchingManager _manager;

    private void OnEnable()
    {
        TryBindManager();
        Refresh();
    }

    private void OnDisable()
    {
        if (_manager != null)
            _manager.StateChanged -= Refresh;
    }

    private void Update()
    {
        if (_manager == null)
        {
            TryBindManager();
            Refresh();
        }
    }

    public void Refresh()
    {
        EggFeatureState storedState = EggFeatureStorage.Load();
        bool showEggNests = HasDiscoveredEggs(storedState);
        bool showAnimalMeta = HasHatchedAnimal(storedState);
        bool showAnimalMerge = HasUnlockedAnimalMerge(storedState);
        bool showEggNestTutorial = showEggNests && !showAnimalMeta;
        bool showAnimalPlacementTutorial = showAnimalMeta && !HasUsedAnimalLoadout(storedState);

        SetActive(_eggNestObjects, showEggNests);
        SetActive(_animalPlacementObjects, showAnimalMeta);
        SetActive(_animalMergeObjects, showAnimalMerge);
        SetActive(_eggNestTutorialObjects, showEggNestTutorial);
        SetActive(_animalPlacementTutorialObjects, showAnimalPlacementTutorial);
    }

    private bool HasDiscoveredEggs(EggFeatureState storedState)
    {
        return _manager != null && _manager.HasDiscoveredEggs() ||
               storedState != null && storedState.hasDiscoveredEggs;
    }

    private bool HasHatchedAnimal(EggFeatureState storedState)
    {
        return _manager != null && _manager.HasHatchedAnimal() ||
               storedState != null && storedState.hasHatchedAnimal;
    }

    private bool HasUnlockedAnimalMerge(EggFeatureState storedState)
    {
        return _manager != null && _manager.HasUnlockedAnimalMerge() ||
               storedState != null && storedState.hasUnlockedAnimalMerge;
    }

    private bool HasUsedAnimalLoadout(EggFeatureState storedState)
    {
        return _manager != null && _manager.HasUsedAnimalLoadout() ||
               storedState != null && storedState.hasUsedAnimalLoadout;
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
            _manager.StateChanged -= Refresh;

        _manager = manager;
        _manager.StateChanged += Refresh;
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject target = objects[i];
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
