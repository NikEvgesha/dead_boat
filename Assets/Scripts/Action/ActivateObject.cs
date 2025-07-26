using UnityEngine;

public class ActivateObject : MonoBehaviour
{
    [SerializeField] Animator animator;
   public void Activate()
    {
        animator.SetTrigger("Start");
    }
}
