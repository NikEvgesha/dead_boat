using UnityEngine;

public class SellableItem : MonoBehaviour, ISellable
{
    [SerializeField] private int _cost;

    public int GetReward() 
    {

        if (LevelStatManager.Instance)
            return (int)(_cost * LevelStatManager.Instance.Stats.MoneyMultSale);
        return _cost;
    }

}
