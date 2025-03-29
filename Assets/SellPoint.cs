using UnityEngine;

public class SellPoint : MonoBehaviour
{

    // TODO: spawn money bag

    private void OnTriggerEnter(Collider other)
    {

        if (other.TryGetComponent<SellableItem>(out SellableItem item))
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, item.Cost);
            Destroy(other.gameObject);
        }
    }

}
