using UnityEngine;

public class TutorialStep12 : TutorialStep
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _mobile;
    [SerializeField] private GameObject _desctop;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private ArrowLine _line;
    private void Awake()
    {
        _canvas.SetActive(false);
    }
    public override void ActivateStep()
    {
        base.ActivateStep();
        _canvas.SetActive(true);
        bool isMobile = ControlManager.Instance.UseTouchControl;
        _mobile.SetActive(isMobile);
        _desctop.SetActive(!isMobile);
        _player = _player ? _player : PlayerStatsManager.Instance.transform;
        if (!_goal)
            Debug.LogError("Ќет конечной точки дл€ стрелки в тутере");
        _line.StartArrowLine(_player, _goal);
    }
    public override void DeactivateStep()
    {
        _canvas.SetActive(false);
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
}
