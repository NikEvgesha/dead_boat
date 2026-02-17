using System;
using System.Collections.Generic;
using UnityEngine;

public class EggNestPoint : MonoBehaviour
{
    [SerializeField] private string _nestId;
    [SerializeField] private Transform _eggVisualAnchor;
    [SerializeField] private List<Transform> _animalSpawnPoints = new();
    [SerializeField] private EggNestSelectionPanel _selectionPanel;

    private GameObject _eggPreviewInstance;

    public string NestId => _nestId;
    public Transform EggVisualAnchor => _eggVisualAnchor != null ? _eggVisualAnchor : transform;
    public IReadOnlyList<Transform> AnimalSpawnPoints => _animalSpawnPoints;

    public event Action<EggNestPoint> NestActionRequested;

    public void RequestAction()
    {
        NestActionRequested?.Invoke(this);
    }

    public void PlaceEggAction()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
            return;

        if (manager.GetNestState(_nestId) != null)
            return;

        EggNestSelectionPanel panel = _selectionPanel != null ? _selectionPanel : EggNestSelectionPanel.Instance;
        if (panel != null)
        {
            panel.OpenForNest(_nestId);
            return;
        }

        manager.TryStartIncubationWithAnyEgg(_nestId);
    }

    public void SkipAction()
    {
        EggHatchingManager.Instance?.TrySkipIncubation(_nestId);
    }

    public void PlaceAnimalAction()
    {
        EggHatchingManager.Instance?.TryPlaceReadyAnimal(_nestId);
    }

    public void SetEggPreview(GameObject previewPrefab)
    {
        ClearEggPreview();

        if (previewPrefab == null)
            return;

        _eggPreviewInstance = Instantiate(previewPrefab, EggVisualAnchor);
        _eggPreviewInstance.transform.localPosition = Vector3.zero;
        _eggPreviewInstance.transform.localRotation = Quaternion.identity;
    }

    public void ClearEggPreview()
    {
        if (_eggPreviewInstance != null)
            Destroy(_eggPreviewInstance);

        _eggPreviewInstance = null;
    }

    public bool TryGetSpawnPoint(int index, out Transform point)
    {
        point = null;

        if (index < 0 || index >= _animalSpawnPoints.Count)
            return false;

        point = _animalSpawnPoints[index];
        return point != null;
    }

    public int GetSpawnPointCount()
    {
        return _animalSpawnPoints != null ? _animalSpawnPoints.Count : 0;
    }
}
