using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private bool _useTouchControls = false;

/*    public bool UseTouchControl { get { return _useTouchControls; } private set { } }
    public bool IsCursorVisible { get; private set; }*/
    public bool SpaceUp { get; private set; }
    public bool SpaceDown { get; private set; }

    public Vector3 Movement { get; private set; }
    public Vector2 Rotation { get; private set; }

    private TouchControls _touchControls;

    public bool JumpTriggered
    {
        get
        {
            var tmp = _jump;
            _jump = false;
            return tmp;
        }
        private set { }
    }

    public bool PickUp 
    { 
        get 
        {
            var tmp = _pickUp;
            _pickUp = false;
            return tmp;
        }
        private set { } 
    }


    public bool Interaction
    {
        get
        {
            var tmp = _interaction;
            _interaction = false;
            return tmp;
        }
        private set { }
    }

    private bool _jump;
    private bool _interaction;
    private bool _pickUp;
    private bool _inTrain;
    private void Awake()
    {
/*        if (!_useTouchControls)
        {
            IsCursorVisible = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }*/
    }

    private void Start()
    {
        _touchControls = FindAnyObjectByType<ControlUI>().GetTouchControls();
    }

    private void Update()
    {
        CheckControls();
        UpdateMovement();
        UpdateRotation();
    }

    private void CheckControls()
    {
        if (_useTouchControls)
        {
            SpaceUp = _touchControls.upButton.IsHolded;
            SpaceDown = _touchControls.downButton.IsHolded;
            _jump = _touchControls.jumpButton.IsTriggered;
            _pickUp = _touchControls.pickUpButton.IsTriggered;
            _interaction = _touchControls.putToInventoryButton.IsTriggered;
        }
        else
        {
            SpaceUp = Input.GetKey(KeyCode.Space);
            SpaceDown = Input.GetKey(KeyCode.LeftControl);
            _jump = SpaceUp;
            _pickUp = Input.GetMouseButtonDown(0);
            _interaction = Input.GetKeyDown(KeyCode.E);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //ShowCursor(!IsCursorVisible);
            ControlManager.Instance.CursorActive = !ControlManager.Instance.CursorActive;
        }
    }

    private void UpdateMovement()
    {
        if (_inTrain)
        {
            Movement = new Vector3(0f, 0f, 0f);
            return;
        }

        if (_useTouchControls)
        {
            Movement = new Vector3(_touchControls.moveJoystick.Horizontal(), 0f, _touchControls.moveJoystick.Vertical());
        } else
        {
            Movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        }
    }


    public void UpdateRotation()
    {
        if (_useTouchControls)
        {
            Rotation = _touchControls.cameraTouchController.GetRotationInput();
        }
        else
        {
            Rotation = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
    }


/*    public void ShowCursor(bool visible)
    {
        Debug.Log("Set cursor visibility to: " + visible);
        IsCursorVisible = visible;
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }*/
    public void SitTrain(bool inTrain)
    {
        _inTrain = inTrain;
    }
}
