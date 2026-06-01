using UnityEngine;

public class ControlManager : MonoBehaviour
{
    private static ControlManager _instance;
    public static ControlManager Instance { get { return _instance; } private set { } }

    [SerializeField] private bool _useTouchControls;
    [SerializeField] private DeviceProvider _provider;
    private bool _cursorActive;
    private bool _moveActive = true;
    public bool UseTouchControl { get { return _useTouchControls; } private set { } }

    private int _activeWindows = 0;
    public bool CursorActive
    {
        get
        {
            if (_provider && _provider.IsInitialized())
                return _provider.IsCursorVisible();

            return _cursorActive;
        }

        set
        {
            if (_useTouchControls)
                return;

            if (value)
            {
                _activeWindows++;
            }
            else
            {
                _activeWindows = _activeWindows > 0 ? _activeWindows - 1 : 0 ;
                if (_activeWindows > 0) return;
            }

            if (_provider && _provider.IsInitialized())
            {
                _cursorActive = value;
                CursorLockMode lockState = value ? CursorLockMode.None : GetGameplayCursorLockMode();
                _provider.SetCursorLockState(lockState);
                _provider.SetCursorVisible(value);
            } 
            else
            {
                _cursorActive = value;
                CursorLockMode lockState = value ? CursorLockMode.None : GetGameplayCursorLockMode();
                Cursor.lockState = lockState;
                Cursor.visible = value;
            }

            /*if (_moveActive)
                InventoryUI.Instance.ToggleOpen(false);*/

        }
    }
    public bool MoveActive
    {
        get
        {
            return _moveActive;
        }

        set
        {
            _moveActive = value;
        }
    }

    private static CursorLockMode GetGameplayCursorLockMode()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return CursorLockMode.None;
#else
        return CursorLockMode.Locked;
#endif
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
    }

    private void Start()
    {
        if (_provider && _provider.IsInitialized())
        {
            if (_provider.IsMobileDevice())
            {
                _useTouchControls = true;
            }
        }
        else
        {
            if (Application.isMobilePlatform)
            {
                _useTouchControls = true;
            }
        }
        if (!_useTouchControls)
        {
            CursorActive = false;  
        }
    }



}
