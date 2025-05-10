using UnityEngine;

public class TriggerBossFight : MonoBehaviour
{
    [SerializeField] private BoardController _board;
    private void Awake()
    {
        if (!_board)
            _board = FindAnyObjectByType<BoardController>();
        _board.EndGame += StartBossFight;
    }
    public virtual void StartBossFight()
    {
        
    }
}