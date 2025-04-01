using UnityEngine;

public class TrainMagnet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" || other.tag == "Enemy" || other.tag == "Item")
        other.transform.SetParent(this.gameObject.transform.parent);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" || other.tag == "Enemy" || other.tag == "Item")
            other.transform.SetParent(null);
    }
}
