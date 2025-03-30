using UnityEngine;

public class ControlManager : MonoBehaviour
{
    private static ControlManager _instance;
    public static ControlManager Instance { get { return _instance; } private set { } }

    [SerializeField] private bool _useTouchControls;
    private bool _cursorActive;

    public bool UseTouchControl { get { return _useTouchControls; } private set { } }
    public bool CursorActive
    {
        get
        {
            return _cursorActive;
        }

        set
        {
            _cursorActive = value;
            Cursor.visible = value;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;

        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (Application.isMobilePlatform)
        {
            _useTouchControls = true;
        }
    }

    private void Start()
    {

        if (!_useTouchControls)
        {
            CursorActive = false;  
        }
    }




}
