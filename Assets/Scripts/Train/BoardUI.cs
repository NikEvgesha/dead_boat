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

    }

    private void SwitchDistance(float distance)
    {
        _distance.text = (int)distance + "Ì";
    }
    private void SwitchSpeed(float speed)
    {
        _speed.text = (int)speed + "Ì/C";
    }
    private void SwitchFuel(float fuel,float maxFuel)
    {
        float fuelPercent = (fuel / maxFuel);
        _fuel.text = (int)(fuelPercent*100) + " %";
        _fuelDisplay.size = fuelPercent;
    }
}
