using UnityEngine;

public class GameStartPoint : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private Transform _insidePoint;
    [SerializeField] private Transform _outsidePoint;

    private Transform _player;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement.Instance.Teleport(_insidePoint);
            _canvas.SetActive(true);
            ControlManager.Instance.CursorActive = true;
        }
    }

    public void Cancel()
    {
        PlayerMovement.Instance.Teleport(_outsidePoint);
        _canvas.SetActive(false);
        ControlManager.Instance.CursorActive = false;
    }

}
