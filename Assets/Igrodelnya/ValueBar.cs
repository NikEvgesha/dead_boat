using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ValueBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Text minText;
    [SerializeField] private Text maxText;
    [SerializeField] private Text valueText;

    [Header("Config")]
    [SerializeField] private float min = 0f;
    [SerializeField] private float max = 100f;
    [SerializeField] private float value = 100f;
    [SerializeField] private bool animate = false;
    [SerializeField] private float animateSpeed = 8f;
    [SerializeField] private string numberFormat = "0";

    // NEW: какой край опасный
    public enum DangerEnd { LowIsDanger, HighIsDanger, BothEnds }
    [Header("Alert / Shake")]
    [SerializeField] private DangerEnd dangerEnd = DangerEnd.LowIsDanger;
    [SerializeField, Range(0f, 1f)] private float shakeThreshold = 0.2f; // когда ближе чем 20% к опасному краю
    [SerializeField] private float maxShakePixels = 8f;        // амплитуда дрожи в пикселях при danger=1
    [SerializeField] private float shakeFrequency = 18f;        // Гц
    [SerializeField] private AnimationCurve shakeIntensityByDanger = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private bool pulseScale = false;           // опциональный пульс масштаба
    [SerializeField] private float maxScalePulse = 0.06f;       // ±6% при danger=1

    // Внутреннее целевое значение для анимации
    private float targetValue;

    // NEW: кеш исходных трансформов, чтобы возвращаться в базу
    private RectTransform rt;
    private Vector2 baseAnchoredPos;
    private Vector3 baseScale;

    public float Min
    {
        get => min;
        set { min = value; ClampAll(); RefreshTexts(); RefreshFillImmediate(); }
    }

    public float Max
    {
        get => max;
        set { max = Mathf.Max(value, min + Mathf.Epsilon); ClampAll(); RefreshTexts(); RefreshFillImmediate(); }
    }

    public float Value
    {
        get => this.value;
        set
        {
            targetValue = Mathf.Clamp(value, min, max);
            if (!animate)
            {
                this.value = targetValue;
                RefreshFillImmediate();
                RefreshValueText();
            }
        }
    }

    private void Awake()
    {
        if (fillImage != null && fillImage.type != Image.Type.Filled)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        targetValue = Mathf.Clamp(value, min, max);
        ClampAll();
        RefreshAllImmediate();

        // NEW: кешируем базовую позу/масштаб
        rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            baseAnchoredPos = rt.anchoredPosition;
            baseScale = rt.localScale;
        }
    }

    private void Update()
    {
        if (animate)
        {
            if (!Mathf.Approximately(value, targetValue))
            {
                value = Mathf.Lerp(value, targetValue, Time.deltaTime * animateSpeed);
                if (Mathf.Abs(value - targetValue) < 0.0001f)
                    value = targetValue;

                RefreshFillImmediate();
                RefreshValueText();
            }
        }

        // NEW: дрожь каждый кадр (независимо от animate заполнения)
        UpdateShake();
    }

    /// <summary>Полная инициализация.</summary>
    public void SetRange(float min, float max)
    {
        this.min = min;
        this.max = Mathf.Max(max, min + Mathf.Epsilon);
        ClampAll();
        RefreshAllImmediate();
    }

    /// <summary>Единовременное обновление всего (мин/макс/значение).</summary>
    public void Set(float value, float min = 0, float max = 100, bool animateToValue = false)
    {
        this.min = min;
        this.max = Mathf.Max(max, min + Mathf.Epsilon);
        animate = animateToValue;
        Value = value; // пройдет через сеттер с учетом animate
        RefreshTexts();
        if (!animate) RefreshAllImmediate();
    }

    /// <summary>Установить формат чисел для текстов, например "0" или "0.##".</summary>
    public void SetNumberFormat(string format)
    {
        numberFormat = string.IsNullOrEmpty(format) ? "0" : format;
        RefreshTexts();
        RefreshValueText();
    }

    private void ClampAll()
    {
        value = Mathf.Clamp(value, min, max);
        targetValue = Mathf.Clamp(targetValue, min, max);
    }

    private void RefreshAllImmediate()
    {
        RefreshTexts();
        RefreshFillImmediate();
        RefreshValueText();
    }

    private void RefreshTexts()
    {
        if (minText) minText.text = min.ToString(numberFormat);
        if (maxText) maxText.text = max.ToString(numberFormat);
    }

    private void RefreshValueText()
    {
        if (valueText) valueText.text = value.ToString(numberFormat)+ "/"+ max.ToString(numberFormat);
    }

    private void RefreshFillImmediate()
    {
        if (!fillImage || max <= min) return;
        float t = Mathf.InverseLerp(min, max, value);
        fillImage.fillAmount = t;
    }

    // NEW: универсальный расчет близости к опасному краю
    private float GetDanger01()
    {
        if (max <= min) return 0f;
        float t = Mathf.InverseLerp(min, max, value); // 0..1

        switch (dangerEnd)
        {
            case DangerEnd.LowIsDanger: return 1f - t;                   // ближе к 0 — опаснее
            case DangerEnd.HighIsDanger: return t;                         // ближе к 1 — опаснее
            case DangerEnd.BothEnds: return 1f - Mathf.Abs(2f * t - 1f); // края опасны, середина безопасна
            default: return 0f;
        }
    }

    // NEW: дрожь по позиции + опциональный пульс масштаба
    private void UpdateShake()
    {
        if (rt == null) return;

        float danger = Mathf.Clamp01(GetDanger01());
        if (danger < shakeThreshold)
        {
            // Вернуть в базу, если не шатаем
            rt.anchoredPosition = baseAnchoredPos;
            if (pulseScale) rt.localScale = baseScale;
            return;
        }

        // Нормализуем интенсивность с учетом порога
        float k = Mathf.InverseLerp(shakeThreshold, 1f, danger);
        k = shakeIntensityByDanger.Evaluate(k); // кривая настраивает рост интенсивности

        // Псевдослучайная дрожь: Perlin по времени, чтобы не дергалось резко
        float t = Time.unscaledTime * shakeFrequency; // unscaled — дрожит и на паузе Time.timeScale
        float nx = Mathf.PerlinNoise(0.123f, t) * 2f - 1f;
        float ny = Mathf.PerlinNoise(42.42f, t + 10f) * 2f - 1f;

        Vector2 offset = new Vector2(nx, ny) * (maxShakePixels * k);
        rt.anchoredPosition = baseAnchoredPos + offset;

        if (pulseScale)
        {
            float pulse = (Mathf.PerlinNoise(7.7f, t * 0.5f) * 2f - 1f) * (maxScalePulse * k);
            float s = 1f + pulse;
            rt.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
        }
    }
}
