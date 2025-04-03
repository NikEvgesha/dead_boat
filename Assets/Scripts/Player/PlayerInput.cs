using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector3 Movement { get; private set; }
    public Vector2 Rotation { get; private set; }

    private TouchControls _touchControls;

    public bool Sprint { get {
            return _sprint;
        } private set { } }

    public bool JumpTriggered
    {
        get
        {
            return _jump;
            /*var tmp = _jump;
            _jump = false;
            return tmp;*/
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
           return _interaction;
            //_interaction = false;
            //return tmp;
        }
        private set { }
    }

    public bool InteractionHold
    {
        get
        {
            return _interactionHold;
            //_interaction = false;
            //return tmp;
        }
        private set { }
    }



    private bool _jump;
    private bool _sprint;
    private bool _interaction;
    private bool _interactionHold;
    private bool _pickUp;
    private bool _inTrain;

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
        if (ControlManager.Instance.UseTouchControl)
        {
            _jump = _touchControls.jumpButton.IsTriggered;
            _pickUp = _touchControls.pickUpButton.IsTriggered;
            _interaction = _touchControls.putToInventoryButton.IsTriggered;
            _interactionHold = _touchControls.putToInventoryButton.IsHolded;
            _sprint = _touchControls.sprintButton.IsHolded;
        }
        else
        {
            _jump = Input.GetKeyDown(KeyCode.Space);
            _pickUp = Input.GetMouseButtonDown(0);
            _interaction = Input.GetKeyDown(KeyCode.E);
            _interactionHold = Input.GetKey(KeyCode.E);
            _sprint = Input.GetKey(KeyCode.LeftShift);
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

        if (ControlManager.Instance.UseTouchControl)
        {
            Movement = new Vector3(_touchControls.moveJoystick.Horizontal(), 0f, _touchControls.moveJoystick.Vertical());
        } else
        {
            Movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        }
    }


    public void UpdateRotation()
    {
        if (ControlManager.Instance.UseTouchControl)
        {
            Rotation = _touchControls.cameraTouchController.GetRotationInput();
        }
        else
        {
            Rotation = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
    }

    public void SitTrain(bool inTrain)
    {
        _inTrain = inTrain;
    }
}
