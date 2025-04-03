using UnityEngine;

public class EnemyReward : MonoBehaviour, ISellable
{
    [SerializeField] private int _reward;
    public int GetReward() => _reward;
}
