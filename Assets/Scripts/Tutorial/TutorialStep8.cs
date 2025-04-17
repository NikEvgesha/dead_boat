using UnityEngine;

public class TutorialStep8 : TutorialStep
{
    // проверка на моб, и 2 текста. и передать стрелке параметры

    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _mobile;
    [SerializeField] private GameObject _desctop;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _goal;
    [SerializeField] private PickableItem _item;
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
        {
            Debug.LogError("Нет конечной точки для стрелки в тутере");
            DeactivateStep();
            return;
        }
        _line.StartArrowLine(_player, _goal);
        _item = _goal.GetComponentInChildren<PickableItem>();
        _item.PickUpItem += DeactivateStep;
        _item.PutItemToInventory += DeactivateStep;
    }
    public override void DeactivateStep()
    {
        _item.PickUpItem -= DeactivateStep;
        _item.PutItemToInventory -= DeactivateStep;
        _canvas.SetActive(false);
        _line.ActiveArrowLine(false);
        base.DeactivateStep();
    }
}
