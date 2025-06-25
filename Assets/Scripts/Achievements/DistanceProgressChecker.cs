using UnityEngine;

public class DistanceProgressChecker : AchievementProgressChecker
{
    [SerializeField] private BoardController _board;

    protected void Start()
    {
        _board = GetComponent<BoardController>();
        _board.SwitchDistance += OnValueChange;
    }

    private void OnDisable()
    {
        _board.SwitchDistance -= OnValueChange;
    }

    private void OnValueChange(float value)
    {
        int currentValue = (int)value;
        if (_value != currentValue)
        {
            AchievementManager.Instance.UpdateData(AchievementType.TotalDistance, currentValue - _value);
            _value = currentValue;
            AchievementManager.Instance.UpdateData(AchievementType.Distance, _value);
        }
    }
}
