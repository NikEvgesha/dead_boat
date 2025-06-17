using UnityEngine;

public class InteractionAreaIndicator : MonoBehaviour
{
    [SerializeField] private InteractionArea _type;
    [SerializeField] private Sprite _icon;

    public InteractionArea Type => _type;

    public Sprite GetIcon()
    {
        return _icon;
    }

    public Transform GetInteractionPoint()
    {
        return transform;
    }
}
