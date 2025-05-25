using UnityEngine;

public class TrainMagnet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.isEndGame)
            return;
        if (other.tag == "Player" || other.tag == "Enemy" || other.tag == "Item")
            other.transform.SetParent(this.gameObject.transform.parent);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" || other.tag == "Enemy" || other.tag == "Item")
            other.transform.SetParent(null);
        if(other.tag == "Player")
        {
            DontDestroyOnLoad(other.gameObject);
        }
    }
}
