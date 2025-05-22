using UnityEngine;
using UnityEngine.UI;

public class TriggerBossFight : MonoBehaviour
{
    [SerializeField] protected BoardController _board;
    [SerializeField] protected Scrollbar hpBar;
    private void Awake()
    {
        _board = FindAnyObjectByType<BoardController>();
        if (hpBar == null)
            hpBar = GetComponentInChildren<Scrollbar>();
        if (hpBar)
            hpBar.gameObject.SetActive(true);
    }
    public virtual void StartBossFight()
    {   
        
    }
}