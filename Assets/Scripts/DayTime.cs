using UnityEngine;

public class DayTime : MonoBehaviour
{

    [SerializeField, Range(0, 24)] private float _timeOfDay;
    [SerializeField] private float _orbitSpeed;
    [SerializeField] private float _axisOffset;
    [SerializeField] private Gradient _nightLight;
    [SerializeField] private AnimationCurve _sunCurve;

    [Header("Objects")]
    [SerializeField] private Light _sun;

    [Header("Time")]
    private int _hour;
    private int _minute;

    private void OnValidate()
    {
        ProgressTime();
    }

    private void Update()
    {
        _timeOfDay += Time.deltaTime * _orbitSpeed;
        ProgressTime();
    }

    private void ProgressTime()
    {

        float currentTime = _timeOfDay / 24;
        float sunRotation = Mathf.Lerp(-90, 270, currentTime);

        _sun.transform.rotation = Quaternion.Euler(sunRotation, _axisOffset, 0);

        _hour = Mathf.FloorToInt(_timeOfDay);
        _minute = Mathf.FloorToInt((_timeOfDay / (24f / 1440f) % 60));

        RenderSettings.ambientLight = _nightLight.Evaluate(currentTime);
        _sun.intensity = _sunCurve.Evaluate(currentTime);

        _timeOfDay %= 24;
    }
}
