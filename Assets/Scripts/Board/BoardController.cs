using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardController : MonoBehaviour
{
    public static BoardController Instance { get; private set; }
    [Header("Настройки поезда")]
    [Tooltip("Текущее количество топлива")]
    public float currentFuel = 100f;

    [Tooltip("Максимальное количество топлива")]
    [SerializeField]
    private float _maxFuel = 1000f;
    public float MaxFuel 
    { 
        get 
        {
            float maxFuel = _maxFuel;
            if (LevelStatManager.Instance)
                maxFuel += LevelStatManager.Instance.Stats.MaxFuel;
            maxFuel = ProfessionService.ApplyMaxFuel(maxFuel);
            return EggAnimalBuffService.ApplyMaxFuel(maxFuel);
        }
        set { _maxFuel = value; } 
    }
    [Tooltip("Коэффициент расхода топлива – расход топлива пропорционален текущей скорости")]
    [SerializeField] private float _fuelConsumptionRate = 0.1f;

    public float FuelConsumptionRate 
    { 
        get
        {
            float rate = _fuelConsumptionRate;
            if (LevelStatManager.Instance)
                rate *= LevelStatManager.Instance.Stats.ConsumptionFuel;
            rate = ProfessionService.ApplyFuelConsumption(rate);
            return EggAnimalBuffService.ApplyFuelConsumption(rate);
        }
        set 
        { 
            _fuelConsumptionRate = value;
        }
    }

    [Tooltip("Ускорение поезда (м/с)")]
    public float acceleration = 5f;
    [Tooltip("Максимальная скорость поезда (м/с)")]
    [SerializeField] private float _maxSpeed = 85f;

    public float MaxSpeed
    {
        get
        {
            float maxSpeed = _maxSpeed;
            if (LevelStatManager.Instance)
                maxSpeed += LevelStatManager.Instance.Stats.MaxSpeedBoard;
            maxSpeed = ProfessionService.ApplyBoatMaxSpeed(maxSpeed);
            return EggAnimalBuffService.ApplyBoatMaxSpeed(maxSpeed);
        }
        set
        {
            _maxSpeed = value;
        }
    }

    [Header("Параметры замедления")]
    [Tooltip("Замедление (фрикционное) поезда при отсутствии ввода ускорения (м/с)")]
    public float coastDeceleration = 2f;
    [Tooltip("Тормозное замедление поезда при нажатии на тормоз (м/с)")]
    public float brakeDeceleration = 10f;

    [Header("Настройки водителя")]
    [Tooltip("Находится ли игрок на водительском месте")]
    private bool playerOnSeat = false;

    public bool PlayerOnSeat {
        get {return playerOnSeat;}
        set {
            NoFuel?.Invoke(currentFuel <= 0);
            playerOnSeat = value;
        }
        }

    [SerializeField] private int _levels = 10;
    private int _level = 1;

    [SerializeField] private Transform _loadedItemsSpawnPoint;

    private float _levelDistance;

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
    //private float _preTotalDistanceTraveled = 0f;
    public float SaveDistanceTraveled = 10000f;
    public bool StartSpawn;

    [SerializeField] private float _endTutorialDistance = 50f;

    private bool _endGame = false;
    private bool _endTutorial = false;
    public bool StartGame;
    private string _sceneName;
    private int _levelID;
    //private int _nextSavePoint = 10000;

    public Action<float> SwitchDistance;
    public Action<float> SwitchSpeed;
    public Action<float,float> SwitchFuel;
    public Action EndGame;
    public Action<bool> NoFuel;
    [SerializeField] private AudioSource _audioSource;
    //[SerializeField] private bool _test = false;
    //[SerializeField] private int _rewardForWin = 20;
    public bool WaitFixUpdate;
    private void Awake()
    {
        if (Instance ==null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        // Если на объекте есть Rigidbody, переводим его в кинематический режим,
        // чтобы не зависеть от гравитации и столкновений
        //GameManager.Instance.GameResume += SetStartDistance;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        /*
        transform.position += transform.forward * 1000;
        TotalDistanceTraveled += 1000;
        PlayerMovement.Instance.Teleport(transform);    
        */
        
    }

    private void Start()
    {
        _sceneName = SceneManager.GetActiveScene().name;
        _levelID = LevelManager.Instance.GetLevelId(_sceneName);

        GameManager.Instance.SetBoard(this);
        float saveDustance = SaveManager.Instance.LoadGameProgress().Item1;
        SaveDistanceTraveled = saveDustance >= 0 ? saveDustance - FixCoordinate.Instance.BoardAddPos : 0;
        StartCoroutine(SetSavePosition(SaveDistanceTraveled));
    }
    private IEnumerator SetSavePosition(float pos)
    {
        float startPos = pos > 1000 ? pos - 1600 : pos;
        Vector3 oldPos = transform.position + transform.forward * startPos;
        TotalDistanceTraveled = startPos + FixCoordinate.Instance.BoardAddPos;
        Vector3 newPos = transform.position + transform.forward * pos;
        float time = 0;
        StartSpawn = true;
        LoadingProgressBarUI.Instance?.EndProgress(1);
        while (time < 1)
        {
            time += Time.deltaTime;

            if (time > 1)
                time = 1;

            transform.position = Vector3.Lerp(oldPos, newPos, time);
            TotalDistanceTraveled = time * pos + FixCoordinate.Instance.BoardAddPos;
            PlayerMovement.Instance.Teleport(transform);
            yield return null;
        }
        StartGame = true;
        StartPlay();
    }
/*    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameResume -= SetStartDistance;
        }
    }*/
    private void StartPlay()
    {
        currentFuel = SaveManager.Instance.LoadFuel();
        _endPoint = GameManager.Instance.PlayDistance;
        _levelDistance = _endPoint / _levels;
        StartCoroutine(UpdateUiDistance());
        //StartCoroutine(CheckProgressSave());
        SwitchSpeed?.Invoke(currentSpeed);
        SwitchFuel?.Invoke(currentFuel, MaxFuel);
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
            _level = (int)Math.Ceiling(TotalDistanceTraveled / _levelDistance);
            SwitchDistance?.Invoke(TotalDistanceTraveled);
            SaveProgress();
            yield return new WaitForSeconds(1);
        }
        yield return null;
    }
    private void SaveProgress()
    {
        if (GameManager.Instance.isEndGame)
            return;
        SaveManager.Instance.SaveGameProgress((int)TotalDistanceTraveled, Inventory.Instance.GetItems(), _levelID);
        SaveManager.Instance.SaveFuel((int)currentFuel);
    }
/*    private IEnumerator CheckProgressSave()
    {
        while (this.enabled)
        {
            if (TotalDistanceTraveled >= _nextSavePoint)
            {
                SaveManager.Instance.SaveGameProgress(_nextSavePoint, Inventory.Instance.GetInventoryList());
                _nextSavePoint += 10000;
                yield return new WaitForSeconds(30);
            } else
            {
                yield return new WaitForSeconds(1);
            }
        }
    }*/


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
        if (_endGame || !StartGame)
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
                currentSpeed = Mathf.Clamp(currentSpeed, 0, MaxSpeed);
                float consumption = FuelConsumptionRate * Time.fixedDeltaTime;
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
            //EndGameUIManager.EndGame(EndGameState.Win);
            //CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, _rewardForWin);
        }

        if (speed != currentSpeed)
            SwitchSpeed?.Invoke(currentSpeed);
        //if (currentSpeed > 0)
        //SwitchDistance?.Invoke(TotalDistanceTraveled);

        if (_audioSource)
            _audioSource.volume = (currentSpeed / 2) / MaxSpeed;
        if (WaitFixUpdate && currentSpeed <= 0)
        {
            WaitFixUpdate = false;
            FixCoordinate.Instance.FixPosition();
        }
    }
    // Метод для расхода топлива
    void ConsumeFuel(float amount)
    {
        currentFuel -= amount;
        if (currentFuel <= 0)
        {
            currentFuel = 0;
            NoFuel?.Invoke(true);
        }
            

        SwitchFuel?.Invoke(currentFuel, MaxFuel);
    }

    // Метод для добавления топлива
    public void AddFuel(float amount)
    {
        if (LevelStatManager.Instance)
            amount *= LevelStatManager.Instance.Stats.AddMultFuel;
        amount = ProfessionService.ApplyFuelFill(amount);
        amount = EggAnimalBuffService.ApplyFuelFill(amount);
        currentFuel += amount;
        if (currentFuel > MaxFuel)
        {
            currentFuel = MaxFuel;
        }
        if (currentFuel > 0) NoFuel?.Invoke(false);
        SwitchFuel?.Invoke(currentFuel, MaxFuel);
    }

    // Внешний метод для установки режима водителя
    public void SetPlayerOnSeat(bool onSeat)
    {
        PlayerOnSeat = onSeat;
        if (onSeat)
        {
            // При входе обнуляем ввод и запускаем таймер игнорирования остаточного ввода,
            // чтобы избежать резкого ускорения
            ignoreInputTime = ignoreInputDuration;
            inputValue = 0;
            NoFuel?.Invoke(currentFuel <= 0);

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
    public int GetLevel()
    {
        return _level;
    }

/*    public void SetStartDistance(int distance)
    {
        TotalDistanceTraveled = distance;
        currentFuel = SaveManager.Instance.LoadFuel();

        List<string> attachedItems = SaveManager.Instance.LoadAttachedItems();
        //SetStartItems(_starterPack.GetStartItems());
        foreach (string id in attachedItems)
        {
            PickableItem item = ItemsManager.Instance.GetItem(id);
            if (item != null)
            {
                PickableItem itemObj = Instantiate(item, transform);
                itemObj.transform.position = _loadedItemsSpawnPoint.position;
            }
                
        }
        //SaveManager.Instance.ResetAttachedItems();
    }*/
}
