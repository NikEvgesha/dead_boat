using UnityEngine;

public class TutorialStep3 : TutorialStep
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private MoneyBag _moneyBag;
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
        _moneyBag = _goal.GetComponentInChildren<MoneyBag>();
        _moneyBag.TakeBag += DeactivateStep;
    }
    public override void DeactivateStep()
    {
        _moneyBag.TakeBag -= DeactivateStep;
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
}
