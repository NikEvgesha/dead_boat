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
            _value = currentValue;
            AchievementManager.Instance.UpdateData(_achievementType, _value);
        }
    }
}
