using UnityEngine;

public class TutorialStep7 : TutorialStep
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private StorePoint _storePoint;
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
        _storePoint.BuyItem += DeactivateStep;
    }
    public override void DeactivateStep()
    {
        _storePoint.BuyItem -= DeactivateStep;
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
}
