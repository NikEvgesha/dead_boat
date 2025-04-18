using System;
using UnityEngine;

public class DayTime : MonoBehaviour
{
    public static DayTime instanse;

    [SerializeField, Range(0, 24)] private float _timeOfDay;
    [SerializeField] private float _orbitSpeed;
    [SerializeField] private float _axisOffset;
    [SerializeField] private Gradient _nightLight;
    [SerializeField] private AnimationCurve _sunCurve;
    [SerializeField] private float _intensityMultiplier;

    [SerializeField] private float _cycleDuration = 24f; // Длительность полного цикла в реальных часах
    [SerializeField] private float _transitionSpeed = 10f; // Скорость перехода восхода/захода
    [SerializeField] private float _nightLightIntensity = 0.15f; // интенсивность света ночью

    [SerializeField] private float _dayAngle = 90f;
    [SerializeField] private float _nightAngle = -90f;
    [SerializeField] private Color _dayFogColor;
    [SerializeField] private Color _nightFogColor;
    [SerializeField] private int _startDayTimeDistance = 100;

    [Header("Transition Timing")]
    [SerializeField] private float _sunriseStart = 5.5f; // Начало восхода
    [SerializeField] private float _sunriseEnd = 6.5f;   // Конец восхода
    [SerializeField] private float _sunsetStart = 17.5f; // Начало заката
    [SerializeField] private float _sunsetEnd = 18.5f;   // Конец заката

    [Header("Objects")]
    [SerializeField] private Light _sun;
    [SerializeField] private Material _skybox;

    [Header("Time")]
    private int _hour;
    private int _minute;
    private bool _isDay;
    private bool _isNight;
    private float _dayDuration; // Длительность в игровых единицах (24 часа = 12 реальных часов)
    private bool _isTransitioning;
    private float _currentOrbitSpeed;
    private BoardController _boardController;


    private static readonly int _rotation = Shader.PropertyToID("_Rotation");
    private static readonly int _exposure = Shader.PropertyToID("_Exposure");

    public Action<int, int> GetTime;
    public Action DayNightCycle;

    private void Awake()
    {
        if (instanse != null)
        {
            Destroy(this);
            return;
        }
        instanse = this;

        if (_sun == null)
        {
            _sun = GameObject.FindGameObjectWithTag("Sun").GetComponent<Light>();
        }

        _dayDuration = _cycleDuration * 3600f / 24f;
        _skybox = RenderSettings.skybox;
        _currentOrbitSpeed = 0;
    }

    private void Start()
    {
        _boardController = FindAnyObjectByType<BoardController>();
        _boardController.SwitchDistance += CheckDistanceToStart;
    }

    private void OnValidate()
    {
        if (_sun)
            ProgressTime();
    }

    private void OnDisable()
    {
        _skybox.SetFloat(_rotation, 0);
        _skybox.SetFloat(_exposure, 1);
        RenderSettings.ambientIntensity = 1;
    }

    private void Update()
    {
        _timeOfDay += (Time.deltaTime * _currentOrbitSpeed / _dayDuration) * 24f;
        _timeOfDay %= 24f;
        ProgressTime();
        UpdateLightning();
    }

    private void CheckDistanceToStart(float distance)
    {
        if (distance >= _startDayTimeDistance)
        {
            _currentOrbitSpeed = _orbitSpeed;
            _boardController.SwitchDistance -= CheckDistanceToStart;
        }
    }


    private void ProgressTime()
    {
        int oldMinute = _minute;
        int oldHour = _hour;
        float sunAngle;

        if (_timeOfDay >= _sunriseStart && _timeOfDay <= _sunriseEnd)
        {
            // Быстрый восход
            float t = (_timeOfDay - _sunriseStart) / (_sunriseEnd - _sunriseStart);
            sunAngle = Mathf.Lerp(-90f, 90f, t);
            _isTransitioning = true;
            _skybox.SetFloat(_exposure, Mathf.Lerp(_nightLightIntensity, 1f, t));
            RenderSettings.fogColor = Color.Lerp(_nightFogColor, _dayFogColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(_nightLightIntensity * 3f, 1, t);
            _sun.intensity = Mathf.Lerp(_nightLightIntensity, 1f, t);
        }
        else if (_timeOfDay >= _sunsetStart && _timeOfDay <= _sunsetEnd)
        {
            // Быстрый закат
            float t = (_timeOfDay - _sunsetStart) / (_sunsetEnd - _sunsetStart);
            sunAngle = Mathf.Lerp(90f, 270f, t);
            _isTransitioning = true;
            _skybox.SetFloat(_exposure, Mathf.Lerp(1f, _nightLightIntensity, t));
            RenderSettings.fogColor = Color.Lerp(_dayFogColor, _nightFogColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(1, _nightLightIntensity * 3f, t);
            _sun.intensity = Mathf.Lerp(1f, _nightLightIntensity, t);
        }
        else
        {
            if (IsNight() != _isNight)
            {
                _isNight = !_isNight;
                DayNightCycle?.Invoke();
            }
            // Неподвижное положение
            _isTransitioning = false;
            sunAngle = _isNight ? -90f : 90f;
        }

        _sun.transform.rotation = Quaternion.Euler(sunAngle, _axisOffset, 0);

        _hour = Mathf.FloorToInt(_timeOfDay);
        _minute = Mathf.FloorToInt((_timeOfDay / (24f / 1440f) % 60));


        if (oldHour != _hour)
            SetNewTime();
    }
    private void SetNewTime()
    {
        GetTime?.Invoke(_hour, _minute);
    }


    public bool IsNight()
    {
        return (_timeOfDay < _sunriseEnd || _timeOfDay > _sunsetEnd);
    }


    private void UpdateLightning()
    {
        float timeNormalized = _timeOfDay / 24f;
        //RenderSettings.ambientLight = _nightLight.Evaluate(timeNormalized);
        //_sun.intensity = _sunCurve.Evaluate(timeNormalized) * _intensityMultiplier;
        //RenderSettings.fogColor = IsNight() ? _nightFogColor : _dayFogColor;
        _skybox.SetFloat(_rotation, 180 + _timeOfDay);

    }


}
