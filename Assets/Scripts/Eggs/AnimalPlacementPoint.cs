using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AnimalPlacementPoint : MonoBehaviour
{
    [SerializeField] private string _pointId;
    [SerializeField] private Transform _spawnAnchor;
    [SerializeField] private AnimalPlacementSelectionPanel _selectionPanel;

    public string PointId => _pointId;
    public Transform SpawnAnchor => _spawnAnchor != null ? _spawnAnchor : transform;

    public event Action<AnimalPlacementPoint> ActionRequested;

    public void RequestAction()
    {
        ActionRequested?.Invoke(this);
    }

    public void PlaceAnimalAction()
    {
        if (string.IsNullOrWhiteSpace(_pointId))
            return;

        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
            return;

        if (manager.IsAnimalPointOccupied(_pointId))
            return;

        AnimalPlacementSelectionPanel panel = _selectionPanel != null ? _selectionPanel : AnimalPlacementSelectionPanel.Instance;
        if (panel != null)
        {
            panel.OpenForPoint(_pointId);
            return;
        }

        manager.TryPlaceAnyOwnedAnimal(_pointId);
    }

    public void RemoveAnimalAction()
    {
        if (string.IsNullOrWhiteSpace(_pointId))
            return;

        EggHatchingManager.Instance?.TryRemovePlacedAnimal(_pointId);
    }
}
