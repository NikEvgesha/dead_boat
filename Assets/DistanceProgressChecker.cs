using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceProgressChecker : AchievementProgressChecker
{
    [SerializeField] private BoardController _board;

    protected new void Start()
    {
        _board = GetComponent<BoardController>();
        base.Start();
    }

    protected override void CheckValue()
    {
        int currentValue = (int)_board.TotalDistanceTraveled;
        if (_value != currentValue)
        {
            _value = currentValue;
            AchievementManager.Instance.UpdateData(_achievementType, _value);
        }
    }
}
