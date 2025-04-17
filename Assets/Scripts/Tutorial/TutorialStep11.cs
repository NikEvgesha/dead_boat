using UnityEngine;

public class TutorialStep11 : TutorialStep
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private FuelDeposit _item;
    [SerializeField] private ArrowLine _line;

    private void Awake()
    {
        _canvas.SetActive(false);
    }
    public override void ActivateStep()
    {
        base.ActivateStep();
        _canvas.SetActive(true);
        _player = _player ? _player : PlayerStatsManager.Instance.transform;
        if (!_goal)
        {
            Debug.LogError("Ќет конечной точки дл€ стрелки в тутере");
            DeactivateStep();
            return;
        }
        _line.StartArrowLine(_player, _goal);
        _item.AddFuel += DeactivateStep;
    }
    public override void DeactivateStep()
    {
        _item.AddFuel -= DeactivateStep;
        _canvas.SetActive(false);
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
}
