using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ActivateZone : MonoBehaviour
{
    public UnityEvent PlayerOnTriggerEnter;
    public UnityEvent PlayerOnTriggerExit;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerOnTriggerEnter?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {

        if (!other.CompareTag("Player")) return;
        PlayerOnTriggerExit?.Invoke();
    }
}
