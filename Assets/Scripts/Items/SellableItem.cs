using UnityEngine;

public class SellableItem : MonoBehaviour, ISellable
{
    [SerializeField] private int _cost;

    public int GetReward() 
    {
        float reward = _cost;

        if (LevelStatManager.Instance)
            reward *= LevelStatManager.Instance.Stats.MoneyMultSale;

        int professionReward = ProfessionService.ApplySaleReward(Mathf.RoundToInt(reward));
        return EggAnimalBuffService.ApplySaleReward(professionReward);
    }

}
