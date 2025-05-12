using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class AchievementProgressChecker : MonoBehaviour
{
    [SerializeField] protected AchievementType _achievementType;
    [SerializeField] protected float _updateTime;


    protected int _value;

/*    protected void Start()
    {
        StartCoroutine(CheckUpdate());
    }

    protected IEnumerator CheckUpdate()
    {
        while (true)
        {
            CheckValue();
            yield return new WaitForSeconds(_updateTime);
        }
    } */

    //protected abstract void OnValueChange();

/*    private void OnEventTriggered(int value)
    {
        if (_value != value)
        {
            AchievementManager.Instance.UpdateData(_achievementType, value);
        }
    }*/
}
