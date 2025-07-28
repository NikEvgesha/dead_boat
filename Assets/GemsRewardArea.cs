using UnityEngine;

public class GemsRewardArea : MonoBehaviour
{
    private bool _inTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (!_inTrigger && other.CompareTag("Player"))
        {
            _inTrigger = true;
            if (!GemsShop.Instance.Opened)
                GemsShop.Instance.ToggleOpen();
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (_inTrigger && other.CompareTag("Player"))
        {
            _inTrigger = false;
            if (GemsShop.Instance.Opened)
                GemsShop.Instance.ToggleOpen();
        }
    }
}
