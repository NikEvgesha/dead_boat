using UnityEngine;
using UnityEngine.UI;

public class BoardUI : MonoBehaviour
{
    [SerializeField] private Text _time;
    [SerializeField] private Text _speed;
    [SerializeField] private Text _distance;
    [SerializeField] private Text _fuel;
    [SerializeField] private Scrollbar _fuelDisplay;

    private void Start()
    {
        BoardController boardController = GetComponentInParent<BoardController>();
        boardController.SwitchDistance += SwitchDistance;
        boardController.SwitchSpeed += SwitchSpeed;
        boardController.SwitchFuel += SwitchFuel;
        DayTime.instanse.GetTime += SwitcTime;

    }

    private void SwitchDistance(float distance)
    {
        _distance.text = (int)distance + "ì";
    }
    private void SwitchSpeed(float speed)
    {
        _speed.text = (int)speed + "ì/ñ";
    }
    private void SwitchFuel(float fuel,float maxFuel)
    {
        float fuelPercent = (fuel / maxFuel);
        _fuel.text = (int)(fuelPercent*100) + " %";
        _fuelDisplay.size = fuelPercent;
    }
    private void SwitcTime(int hour, int minute)
    {
        string hourTime;
        string minuteTime;
        hourTime = hour >= 10 ? hour.ToString() : "0" + hour.ToString();
        minuteTime = minute >= 10 ? minute.ToString() : "0" + minute.ToString();
        _time.text = hourTime + ":" + minuteTime;
    }
}
