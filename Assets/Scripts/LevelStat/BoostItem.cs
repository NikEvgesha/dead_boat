using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RareReward
{
    public RareType Rare;
    public float Reward;
    public string VisualReward;
}

[CreateAssetMenu(fileName = "BoostItem", menuName = "ScriptableObject/BoostItems")]
public class BoostItem : ScriptableObject
{
    [SerializeField] private BoostType _boostType;
    [SerializeField] private List<RareReward> _Rare;
    [SerializeField] private float _chance = 1;
    [SerializeField] private Sprite _icon;

    public List<RareReward> RareRewards { get { return _Rare; } }

    public BoostType BoostType { get { return _boostType; } }
    public string GetReward(RareType rare)
    {
        return _Rare.Find(r => r.Rare == rare).VisualReward;
    }
    public float Chance { get { return _chance; } }
    public Sprite BoostIcon { get { return _icon; } }
    /*[SerializeField] private bool _isLeveling; // на будущее если будем делать у умений уровни
    [SerializeField] private int _levels;*/

    public Action<BoostType,float> Change;

    public void AddBoost(RareType rare)
    {
        Change?.Invoke(_boostType, _Rare.Find(r => r.Rare == rare).Reward);
    }

}
