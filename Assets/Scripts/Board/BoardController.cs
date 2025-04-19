using System;
using System.Collections;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [Header("Настройки поезда")]
    [Tooltip("Текущее количество топлива")]
    public float currentFuel = 100f;

    [Tooltip("Максимальное количество топлива")]
    [SerializeField]
    private float _maxFuel = 1000f;

    [Tooltip("Коэффициент расхода топлива – расход топлива пропорционален текущей скорости")]
    public float fuelConsumptionRate = 0.1f;
    [Tooltip("Ускорение поезда (м/с)")]
    public float acceleration = 5f;
    [Tooltip("Максимальная скорость поезда (м/с)")]
    public float maxSpeed = 20f;

    [Header("Параметры замедления")]
    [Tooltip("Замедление (фрикционное) поезда при отсутствии ввода ускорения (м/с)")]
    public float coastDeceleration = 2f;
    [Tooltip("Тормозное замедление поезда при нажатии на тормоз (м/с)")]
    public float brakeDeceleration = 10f;

    [Header("Настройки водителя")]
    [Tooltip("Находится ли игрок на водительском месте")]
    public bool playerOnSeat = false;

    private float _endPoint = 100000f;

    // Текущая скорость поезда (в м/с)
    private float currentSpeed = 0f;

    // Значение ввода (ось "Vertical"), получаемое в Update и используемое в FixedUpdate
    private float inputValue;

    // Таймер для игнорирования остаточного ввода сразу после входа в режим вождения
    private float ignoreInputTime = 0f;
    [Tooltip("Длительность игнорирования ввода после входа в поезд (сек.)")]
    public float ignoreInputDuration = 0.5f;

    // Общая пройденная дистанция (в метрах)
    public float TotalDistanceTraveled = 0f;

    [SerializeField] private float _endTutorialDistance = 50f;

    private bool _endGame = false;
    private bool _endTutorial = false;

    public Action<float> SwitchDistance;
    public Action<float> SwitchSpeed;
    public Action<float,float> SwitchFuel;
    public Action EndGame;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private int _rewardForWin = 20;

    private void Awake()
    {
        // Если на объекте есть Rigidbody, переводим его в кинематический режим,
        // чтобы не зависеть от гравитации и столкновений
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }
    private void Start()
    {
        _endPoint = GameManager.Instance.PlayDistance;
        StartCoroutine(UpdateUiDistance());
        SwitchSpeed?.Invoke(currentSpeed);
        SwitchFuel?.Invoke(currentFuel, _maxFuel);
        if (_audioSource)
        {
            _audioSource.volume = 0;
            _audioSource.Play();
        }

    }
    IEnumerator UpdateUiDistance()
    {   
        while (this.enabled)
        {
            if (TotalDistanceTraveled > _endTutorialDistance && !_endTutorial)
            {
                _endTutorial = true;
                TutorialManager.Instance.QuickStopTutorial();
            }
            SwitchDistance?.Invoke(TotalDistanceTraveled);
            yield return new WaitForSeconds(1);
        }
        yield return null;
    }
    
        private void Update()
    {
        // Обновляем ввод только если водитель за рулём
        if (playerOnSeat && !_endGame)
        {
            if (ignoreInputTime > 0)
            {
                inputValue = 0;
            }
            else
            {
                //inputValue = Input.GetAxis("Vertical");
                inputValue = PlayerInput.Instance.TrainMove;
            }
        }
        else
        {
            // При отсутствии водителя ввод не учитывается
            inputValue = 0;
        }
    }

    private void FixedUpdate()
    {
        if (_endGame)
            return;
        float speed = currentSpeed;
        // Если таймер игнорирования ввода активен, обнуляем ввод
       /* if (ignoreInputTime > 0)
        {
            ignoreInputTime -= Time.fixedDeltaTime;
            inputValue = 0;
        }*/

        if (playerOnSeat)
        {
            if (currentFuel > 0 && inputValue > 0)
            {
                // Ускорение: увеличиваем скорость
                currentSpeed += acceleration * inputValue * Time.fixedDeltaTime;
                currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
                float consumption = fuelConsumptionRate * Time.fixedDeltaTime;
                ConsumeFuel(consumption);
            }
            else if (inputValue < 0)
            {
                // Тормозное замедление
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Естественное (фрикционное) замедление при отсутствии ввода
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, coastDeceleration * Time.fixedDeltaTime);
            }
        }
        else
        {
            // Если водитель не за рулём, поезд всё равно замедляется естественным образом
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, coastDeceleration * Time.fixedDeltaTime);
        }
        // Перемещаем поезд по локальной оси X (transform.right)
        transform.position += transform.forward * currentSpeed * Time.fixedDeltaTime;
        // Обновляем пройденное расстояние
        TotalDistanceTraveled += currentSpeed * Time.fixedDeltaTime;

        if (!_endGame && TotalDistanceTraveled >= _endPoint)
        {
            _endGame = true;
            EndGame?.Invoke();
            currentSpeed = 0f;
            EndGameUIManager.EndGame(EndGameState.Win);
            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, _rewardForWin);
        }

        if (speed != currentSpeed)
            SwitchSpeed?.Invoke(currentSpeed);
        //if (currentSpeed > 0)
        //SwitchDistance?.Invoke(TotalDistanceTraveled);

        if (_audioSource)
            _audioSource.volume = (currentSpeed / 2) / maxSpeed;
    }
    // Метод для расхода топлива
    void ConsumeFuel(float amount)
    {
        currentFuel -= amount;
        if (currentFuel < 0)
            currentFuel = 0;

        SwitchFuel?.Invoke(currentFuel, _maxFuel);
    }

    // Метод для добавления топлива
    public void AddFuel(float amount)
    {
        currentFuel += amount;
        if (currentFuel > _maxFuel)
        {
            currentFuel = _maxFuel;
        }
        SwitchFuel?.Invoke(currentFuel, _maxFuel);
    }

    // Внешний метод для установки режима водителя
    public void SetPlayerOnSeat(bool onSeat)
    {
        playerOnSeat = onSeat;
        if (onSeat)
        {
            // При входе обнуляем ввод и запускаем таймер игнорирования остаточного ввода,
            // чтобы избежать резкого ускорения
            ignoreInputTime = ignoreInputDuration;
            inputValue = 0;
        }
    }

    // Свойство для получения скорости в км/ч (1 м/с = 3.6 км/ч)
    public float SpeedKmh
    {
        get { return currentSpeed * 3.6f; }
    }

    // Свойство для получения пройденного расстояния в км
    public float DistanceKm
    {
        get { return TotalDistanceTraveled / 1000f; }
    }
}
