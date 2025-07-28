using UnityEngine;

public class InAppCheck : MonoBehaviour
{
    [SerializeField] private GameObject _rewardObj;
    [SerializeField] private GameObject _storePoints;
    private void Start()
    {
        bool purchasesAvailable = PurchasesManager.Instance.PurchasesAvailable();
        _rewardObj.SetActive(!purchasesAvailable);
        //_storePoints.SetActive(!purchasesAvailable);
    }
}
