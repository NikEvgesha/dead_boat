using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private GameObject _camera;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _maxSpeed = 10f;
    [SerializeField] private float _damping = 0.995f;
    [SerializeField] private float _jumpPower = 5;
    [SerializeField] private float _gravity = 9.8f;
    [SerializeField] private float _fallSpeedMultiplier = 2f; // Adjusted naming for clarity
    [SerializeField] private float _spaceSpeedMultiplier = 2f;
    [SerializeField] private float _verticalSpaceSpeed = 2f;
    [SerializeField] private float _groundDamping = 10f; // New: Smooths ground movement
    [SerializeField] private LayerMask _groundMask;

    [SerializeField] private float _YRotationLimitMax = 80f;
    [SerializeField] private float _YRotationLimitMin = -80f;

    [SerializeField] private bool _onPlatform;
    [SerializeField] private bool _isGrounded;

    private PlayerInput _input;

    private CharacterController _controller;
    private Rigidbody _rb;

    private Vector3 _velocity;
    private Vector3 _moveDirection;
    private float _currentXRotation = 0f;
    private float _currentYRotation = 0f;
    private ControlUI _controlUI;
    private GravityChecker _gravityChecker;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerInput>();
        _controlUI = FindAnyObjectByType<ControlUI>();
        _controlUI.UseMobileSetup(ControlManager.Instance.UseTouchControl);
    }


    void FixedUpdate()
    {
        Move();
        if (!ControlManager.Instance.CursorActive || ControlManager.Instance.UseTouchControl)
            CameraRotation();
    }
    private void Move()
    {
        Vector3 movement = _input.Movement;
        Vector3 moveDirection = transform.TransformDirection(movement);
        moveDirection.y = 0f;
        moveDirection = moveDirection.normalized;

        _isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.1f, _groundMask);

        Vector3 targetVelocity = moveDirection * _moveSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        Vector3 velocityChange = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, _groundDamping * Time.fixedDeltaTime);
        _rb.velocity = new Vector3(velocityChange.x, _rb.velocity.y, velocityChange.z);

        if (_isGrounded && _input.JumpTriggered)
        {
            _rb.AddForce(Vector3.up * _jumpPower, ForceMode.Impulse);
        }

        if (!_isGrounded)
        {
            _rb.AddForce(Vector3.down * _gravity * _fallSpeedMultiplier, ForceMode.Acceleration);
        }
    }


    private void CameraRotation()
    {
        Vector2 rotationInput = _input.Rotation * _rotationSpeed * Time.deltaTime;

        _currentYRotation += rotationInput.x;

        _currentXRotation -= rotationInput.y;
        _currentXRotation = Mathf.Clamp(_currentXRotation, _YRotationLimitMin, _YRotationLimitMax);
        transform.rotation = Quaternion.Euler(0f, _currentYRotation, 0f);
        _camera.transform.localRotation = Quaternion.Euler(_currentXRotation, 0f, 0f);
    }

    private void OnGravityChanged(bool inGravitySource)
    {
        _onPlatform = inGravitySource;
        _rb.useGravity = inGravitySource;
        _controlUI.SwitchPlatformControls(_onPlatform);
    }

}
