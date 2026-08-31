using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SafeEventSystemInput : BaseInput
{
    private Vector2 _lastMousePosition;
    private bool _hasLastMousePosition;
    private bool _mousePositionIsValid;

    public bool MousePositionIsValid => _mousePositionIsValid;

    public override Vector2 mousePosition
    {
        get
        {
            Vector2 position = base.mousePosition;
            _mousePositionIsValid = IsFinite(position);

            if (_mousePositionIsValid)
            {
                _lastMousePosition = position;
                _hasLastMousePosition = true;
                return position;
            }

            return _hasLastMousePosition
                ? _lastMousePosition
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
    }

    public override Vector2 mouseScrollDelta
    {
        get
        {
            Vector2 delta = base.mouseScrollDelta;
            return IsFinite(delta) ? delta : Vector2.zero;
        }
    }

    public override bool GetMouseButtonDown(int button)
    {
        return MousePositionIsValid && base.GetMouseButtonDown(button);
    }

    public override bool GetMouseButtonUp(int button)
    {
        return MousePositionIsValid && base.GetMouseButtonUp(button);
    }

    public override bool GetMouseButton(int button)
    {
        return MousePositionIsValid && base.GetMouseButton(button);
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsNaN(value.y)
            && !float.IsInfinity(value.x)
            && !float.IsInfinity(value.y);
    }
}
