using UnityEngine;

public class TutorialStep4 : TutorialStep
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private PickableItem _item;
    [SerializeField] private ArrowLine _line;
    public override void ActivateStep()
    {
        base.ActivateStep();
        _player = _player ? _player : PlayerStatsManager.Instance.transform;

        if (!_goal)
        {
            Debug.LogError("Ќет конечной точки дл€ стрелки в тутере");
            DeactivateStep();
            return;
        }
        _line.StartArrowLine(_player, _goal);
        _item.PickUpItem += DeactivateStep;
        _item.PutItemToInventory += DeactivateStep;
    }
    public override void DeactivateStep()
    {
        _item.PickUpItem -= DeactivateStep;
        _item.PutItemToInventory -= DeactivateStep;
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
}
