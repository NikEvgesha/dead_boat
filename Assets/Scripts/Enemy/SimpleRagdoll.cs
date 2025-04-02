using UnityEngine;

public class SimpleRagdoll : MonoBehaviour
{
    private Rigidbody[] rigidbodies; // Для 3D используй Rigidbody

    void Start()
    {
        // Находим все Rigidbody в мобе
        rigidbodies = GetComponentsInChildren<Rigidbody>();

        // Отключаем физику при старте
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
        }
    }

    public void EnableRagdoll()
    {
        // Включаем регдолл при смерти
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
        }
    }
}