using UnityEngine;

[DisallowMultipleComponent]
public class AnimalMergePoint : MonoBehaviour
{
    [SerializeField] private AnimalMergeSelectionPanel _selectionPanel;

    public void RequestAction()
    {
        MergeAction();
    }

    public void MergeAction()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
            return;

        AnimalMergeSelectionPanel panel = _selectionPanel != null ? _selectionPanel : AnimalMergeSelectionPanel.Instance;
        if (panel != null)
        {
            panel.Open();
            return;
        }

        manager.TryMergeAnyAvailableAnimal();
    }
}
