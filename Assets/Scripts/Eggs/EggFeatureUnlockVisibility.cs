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
        bool showEggNests = _manager != null && _manager.HasDiscoveredEggs();
        bool showAnimalMeta = _manager != null && _manager.HasHatchedAnimal();
        bool showAnimalMerge = _manager != null && _manager.HasUnlockedAnimalMerge();
        bool showEggNestTutorial = showEggNests && !showAnimalMeta;
        bool showAnimalPlacementTutorial = showAnimalMeta && _manager != null && !_manager.HasUsedAnimalLoadout();

        SetActive(_eggNestObjects, showEggNests);
        SetActive(_animalPlacementObjects, showAnimalMeta);
        SetActive(_animalMergeObjects, showAnimalMerge);
        SetActive(_eggNestTutorialObjects, showEggNestTutorial);
        SetActive(_animalPlacementTutorialObjects, showAnimalPlacementTutorial);
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
