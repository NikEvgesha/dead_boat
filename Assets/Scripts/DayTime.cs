using System;
using System.Collections;
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

    [Header("Objects")]
    [SerializeField] private Light _sun;

    [Header("Time")]
    private int _hour;
    private int _minute;

    public Action<int, int> GetTime;

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
    }

    private void OnValidate()
    {
        if (_sun)
            ProgressTime();
    }

    private void Update()
    {
        _timeOfDay += Time.deltaTime * _orbitSpeed;
        ProgressTime();
    }

    private void ProgressTime()
    {
        int oldMinute = _minute;
        float currentTime = _timeOfDay / 24;
        float sunRotation = Mathf.Lerp(-90, 270, currentTime);

        _sun.transform.rotation = Quaternion.Euler(sunRotation, _axisOffset, 0);

        _hour = Mathf.FloorToInt(_timeOfDay);
        _minute = Mathf.FloorToInt((_timeOfDay / (24f / 1440f) % 60));

        RenderSettings.ambientLight = _nightLight.Evaluate(currentTime);
        _sun.intensity = _sunCurve.Evaluate(currentTime) * _intensityMultiplier;

        _timeOfDay %= 24;
        if (oldMinute != _minute)
            SetNewTime();
    }
    private void SetNewTime()
    {
        GetTime?.Invoke(_hour, _minute);
    }
}
