using UnityEngine;

public class SellableItem : MonoBehaviour, ISellable
{
    [SerializeField] private int _cost;

    public int GetReward() => _cost;

}
