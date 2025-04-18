using UnityEngine;

public class GameStartPoint : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _canvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _canvas.SetActive(false);
        }
    }

}
