using UnityEngine;

public sealed class ProfessionShortcutListener : MonoBehaviour
{
    [SerializeField] private KeyCode _keyboardShortcut = KeyCode.T;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_keyboardShortcut))
            ProfessionTemporaryUIBootstrap.TogglePanelFromShortcut();
    }
}
