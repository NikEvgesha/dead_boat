using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] InteractType _interactType;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerStatsManager>())
            GameEvents.OnNPCInteracted(_interactType);
    }
}
