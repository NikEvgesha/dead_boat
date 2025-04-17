using UnityEngine;

public class TutorialStep2 : TutorialStep
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private SellPoint _sellPoint;
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
        _sellPoint.SellItem += DeactivateStep;
    }
    public override void DeactivateStep()
    {
        _sellPoint.SellItem -= DeactivateStep;
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
    private void DeactivateStep(StoreType storeType)
    {
        DeactivateStep();
    }
}
