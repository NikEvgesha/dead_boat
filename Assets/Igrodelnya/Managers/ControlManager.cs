using UnityEngine;

public class ControlManager : MonoBehaviour
{
    private static ControlManager _instance;
    public static ControlManager Instance { get { return _instance; } private set { } }

    [SerializeField] private bool _useTouchControls;
    [SerializeField] private DeviceProvider _provider;
    private bool _cursorActive;
    private bool _moveActive = true;
    private bool _blockPrimaryActionUntilMouseUp;
    public bool UseTouchControl { get { return _useTouchControls; } private set { } }

    private int _activeWindows = 0;
    public bool CursorActive
    {
        get
        {
            return _cursorActive;
        }

        set
        {
            SetCursorActive(value, false);
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

    public bool BlocksPrimaryAction
    {
        get
        {
            if (_useTouchControls)
                return false;

            return _blockPrimaryActionUntilMouseUp ||
                   _cursorActive ||
                   GetCurrentCursorLockState() != GetGameplayCursorLockMode();
        }
    }

    private static CursorLockMode GetGameplayCursorLockMode()
    {
        return CursorLockMode.Locked;
    }

    private static bool IsWebGLPlayer()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    private CursorLockMode GetCurrentCursorLockState()
    {
        if (_provider && _provider.IsInitialized())
            return _provider.GetCursorLockState();

        return Cursor.lockState;
    }

    public void ForceGameplayCursor()
    {
        SetCursorActive(false, true);
    }

    private void SetCursorActive(bool value, bool force)
    {
        if (_useTouchControls)
            return;

        if (value)
        {
            if (force)
                _activeWindows = Mathf.Max(_activeWindows, 1);
            else
                _activeWindows++;
        }
        else
        {
            if (force)
            {
                _activeWindows = 0;
            }
            else
            {
                _activeWindows = _activeWindows > 0 ? _activeWindows - 1 : 0;
                if (_activeWindows > 0)
                {
                    _cursorActive = true;
                    ApplyCursorState(true);
                    return;
                }
            }
        }

        _cursorActive = value;
        ApplyCursorState(value);

        /*if (_moveActive)
            InventoryUI.Instance.ToggleOpen(false);*/
    }

    private void ApplyCursorState(bool active, bool requestLock = true)
    {
        CursorLockMode lockState = active || !requestLock ? CursorLockMode.None : GetGameplayCursorLockMode();

        if (!active && requestLock && Input.GetMouseButton(0))
            _blockPrimaryActionUntilMouseUp = true;

        if (_provider && _provider.IsInitialized())
        {
            _provider.SetCursorLockState(lockState);
            _provider.SetCursorVisible(active);
        }
        else
        {
            Cursor.lockState = lockState;
            Cursor.visible = active;
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
            _activeWindows = 0;
            _cursorActive = false;
            ApplyCursorState(false, !IsWebGLPlayer());
        }
    }

    private void Update()
    {
        if (_blockPrimaryActionUntilMouseUp && !Input.GetMouseButton(0))
            _blockPrimaryActionUntilMouseUp = false;

        if (_useTouchControls || _cursorActive)
            return;

        if (GetCurrentCursorLockState() == GetGameplayCursorLockMode())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            _blockPrimaryActionUntilMouseUp = true;
            ApplyCursorState(false);
        }
    }

}
