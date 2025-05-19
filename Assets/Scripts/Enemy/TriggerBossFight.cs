using UnityEngine;

public class TriggerBossFight : MonoBehaviour
{
    [SerializeField] private BoardController _board;
    private void Awake()
    {
        _board = FindAnyObjectByType<BoardController>();
        StartBossFight();
    }
    public virtual void StartBossFight()
    {   
        
    }
}