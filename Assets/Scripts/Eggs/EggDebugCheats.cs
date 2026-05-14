using UnityEngine;

public class EggDebugCheats : MonoBehaviour
{
    [SerializeField] private bool _enabled = true;
    [SerializeField] private KeyCode _grantRandomEggKey = KeyCode.BackQuote;
    [SerializeField] private int _amount = 1;

    private void Update()
    {
        if (!_enabled)
            return;

        if (!Input.GetKeyDown(_grantRandomEggKey))
            return;

        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
            return;

        manager.TryGrantRandomEgg(_amount);
    }
}
