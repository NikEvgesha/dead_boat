using UnityEngine;

public class SellableItem : MonoBehaviour
{
    [SerializeField] private int _cost;

    public int Cost { get { return _cost; } }

    public void OnSell()
    {

    }

}
