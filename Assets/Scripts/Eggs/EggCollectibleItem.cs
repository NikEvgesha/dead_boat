using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PickableItem))]
public class EggCollectibleItem : MonoBehaviour
{
    [SerializeField] private string _eggId;
    [SerializeField] private int _amount = 1;

    public string EggId => _eggId;
    public int Amount => _amount;

    public bool TryCollect()
    {
        if (string.IsNullOrWhiteSpace(_eggId))
        {
            Debug.LogWarning($"EggCollectibleItem on '{name}' has empty eggId");
            return false;
        }

        int amount = _amount > 0 ? _amount : 1;
        EggSpawnRuntimeState.OnEggCollected();
        EggHatchingManager.RegisterEggPickup(_eggId, amount);
        return true;
    }
}
