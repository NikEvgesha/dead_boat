using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private GameObject _camera;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 2f;
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _jumpPower = 5;
    [SerializeField] private float _gravity = 9.8f;
    [SerializeField] private float _fallSpeed = 2f;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _waterForce= 4f;
    [SerializeField] private float _waterHeightCof = 0.2f;

    [SerializeField] private float _YRotationLimitMax = 80f;
    [SerializeField] private float _YRotationLimitMin = -80f;

    [SerializeField] private bool _onPlatform;
    [SerializeField] private bool _isGrounded;

    private PlayerStatsManager _playerStats;
    private CharacterController _controller;
    private Rigidbody _rb;

    private Vector3 _velocity;
    private float _currentXRotation = 0f;
    private float _currentYRotation = 0f;
    private ControlUI _controlUI;

    

    void Start()
    {
        _playerStats = GetComponent<PlayerStatsManager>();
        _controller = GetComponent<CharacterController>();
        _rb = GetComponent<Rigidbody>();
        _controlUI = FindAnyObjectByType<ControlUI>();
        _controlUI.UseMobileSetup(ControlManager.Instance.UseTouchControl);
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            // Определяем уровень воды по верхней границе триггера
            float waterSurfaceY = other.bounds.max.y;

            // Предположим, что центр игрока + половина высоты контроллера - это точка, где располагается верхняя часть торса
            float playerUpperY = transform.position.y + _controller.height * _waterHeightCof; // можно настроить коэффициент под нужный уровень

            // Если верхняя часть торса находится ниже уровня воды, применяем силу для подъёма
            if (playerUpperY < waterSurfaceY)
            {

                if (PlayerInput.Instance.JumpTriggered)
                {
                    _velocity.y = _jumpPower;
                    return;
                }
                // Пример: добавляем силу вверх, можно настроить множитель для нужного эффекта
                _velocity.y = _waterForce;
            }
        }
    }


    void Update()
    {
        _isGrounded = _controller.isGrounded;
        Move();
        if (!ControlManager.Instance.CursorActive || ControlManager.Instance.UseTouchControl)
            CameraRotation();
    }
    private void Move()
    {
        Vector3 movement = PlayerInput.Instance.Movement;
        Vector3 moveDirection = transform.TransformDirection(movement);
        moveDirection.y = 0f;

        Vector3 horizontalMovement = moveDirection * _moveSpeed;

        if (PlayerInput.Instance.Sprint && _playerStats.Stamina > 0)
        {
            horizontalMovement *= _sprintMultiplier;
        }

        if (_controller.isGrounded)
        {
            _velocity.y = -0.5f;

            if (PlayerInput.Instance.JumpTriggered)
            {
                _velocity.y = _jumpPower;
            }

        }
        else
        {
            _velocity.y -= _gravity * Time.deltaTime * _fallSpeed;
        }
        Vector3 finalMovement = horizontalMovement + new Vector3(0f, _velocity.y, 0f);
        _controller.Move(finalMovement * Time.deltaTime);
    }


    private void CameraRotation()
    {
        Vector2 rotationInput = PlayerInput.Instance.Rotation * _rotationSpeed * Time.deltaTime;

        _currentYRotation += rotationInput.x;

        _currentXRotation -= rotationInput.y;
        _currentXRotation = Mathf.Clamp(_currentXRotation, _YRotationLimitMin, _YRotationLimitMax);
        transform.rotation = Quaternion.Euler(0f, _currentYRotation, 0f);
        _camera.transform.localRotation = Quaternion.Euler(_currentXRotation, 0f, 0f);
    }

}
