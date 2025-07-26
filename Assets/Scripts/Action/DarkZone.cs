using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
public class DarkZone : MonoBehaviour
{
    [Header("Настройки тьмы")]
    [SerializeField] private Color ambientInZone = Color.black;
    [Range(0, 1)][SerializeField] private float reflectionIntensityInZone = 0f;
    [SerializeField] private Light sunlightInZone = null;

    private Color prevAmbient;
    private AmbientMode prevMode;
    private float prevReflectionIntensity;
    private void Start()
    {
        if (sunlightInZone == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var item in lights)
            {
                if (item.tag == "Sun")
                {
                    sunlightInZone = item;
                    break;
                }
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // сохраняем прежние настройки
        prevMode = RenderSettings.ambientMode;
        prevAmbient = RenderSettings.ambientLight;
        prevReflectionIntensity = RenderSettings.reflectionIntensity;
        sunlightInZone.gameObject.SetActive(false);
        // делаем темно
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientInZone;
        RenderSettings.reflectionIntensity = reflectionIntensityInZone;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // возвращаем назад
        RenderSettings.ambientMode = prevMode;
        RenderSettings.ambientLight = prevAmbient;
        RenderSettings.reflectionIntensity = prevReflectionIntensity;
        sunlightInZone.gameObject.SetActive(true);
    }
}
