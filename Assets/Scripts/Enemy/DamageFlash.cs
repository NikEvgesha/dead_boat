using UnityEngine;
using System.Collections.Generic;

public class DamageFlash : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 1f); // красный
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private bool affectChildren = true;
    [Tooltip("Плавное возвращение к исходному цвету")]
    [SerializeField] private bool smoothFadeOut = true;

    private static MaterialPropertyBlock s_Block;
    private readonly List<Renderer> _renderers = new List<Renderer>();

    // Поддерживаемые имена цветовых свойств в разных шейдерах Built-in
    private static readonly int[] kColorProps = new int[]
    {
        // Самые частые:
        Shader.PropertyToID("_Color"),        // Standard, Diffuse, Mobile/Diffuse, Unlit/Color
        Shader.PropertyToID("_TintColor"),    // Particle/Additive и пр.
        Shader.PropertyToID("_MainColor"),    // иногда встречается
        // На случай URP-проекта, где всё же есть BaseColor:
        Shader.PropertyToID("_BaseColor")
    };

    // Для каждого рендера храним, какое свойство у него сработало и исходный цвет
    private struct PerRenderer
    {
        public Renderer r;
        public int colorPropId;
        public Color originalColor;
    }

    private PerRenderer[] _per;
    private float _tEnd = -1f;

    void Awake()
    {
        if (s_Block == null) s_Block = new MaterialPropertyBlock();

        _renderers.Clear();
        if (affectChildren) GetComponentsInChildren(true, _renderers);
        else
        {
            var r = GetComponent<Renderer>();
            if (r != null) _renderers.Add(r);
        }

        _per = new PerRenderer[_renderers.Count];
        for (int i = 0; i < _renderers.Count; i++)
        {
            var r = _renderers[i];
            int foundProp = -1;

            // Поищем доступное цветовое свойство по материалу
            var mat = r ? r.sharedMaterial : null;
            if (mat != null)
            {
                for (int p = 0; p < kColorProps.Length; p++)
                {
                    int propId = kColorProps[p];
                    if (mat.HasProperty(propId)) { foundProp = propId; break; }
                }
            }

            // Если не нашли свойство — будем считать, что цвета нет (пропустим такой рендерер)
            if (foundProp == -1)
            {
                _per[i] = new PerRenderer { r = r, colorPropId = -1, originalColor = Color.white };
                continue;
            }

            // Прочитать исходный цвет можно из sharedMaterial — это не создаёт копию
            Color baseCol = Color.white;
            try { baseCol = mat.GetColor(foundProp); } catch { baseCol = Color.white; }

            _per[i] = new PerRenderer { r = r, colorPropId = foundProp, originalColor = baseCol };
        }
    }

    void Update()
    {
        if (_tEnd < 0f) return;

        float remain = _tEnd - Time.unscaledTime;
        if (remain <= 0f)
        {
            // Снять override и вернуть исходный цвет
            for (int i = 0; i < _per.Length; i++)
            {
                if (_per[i].r == null || _per[i].colorPropId == -1) continue;
                s_Block.Clear();
                s_Block.SetColor(_per[i].colorPropId, _per[i].originalColor);
                _per[i].r.SetPropertyBlock(s_Block);
            }
            _tEnd = -1f;
            return;
        }

        if (smoothFadeOut)
        {
            // Линейно назад к оригиналу
            float t = 1f - (remain / duration); // 0..1
            for (int i = 0; i < _per.Length; i++)
            {
                if (_per[i].r == null || _per[i].colorPropId == -1) continue;
                s_Block.Clear();
                Color c = Color.Lerp(flashColor, _per[i].originalColor, t);
                s_Block.SetColor(_per[i].colorPropId, c);
                _per[i].r.SetPropertyBlock(s_Block);
            }
        }
    }

    /// <summary>Вызывай при получении урона</summary>
    public void Trigger()
    {
        for (int i = 0; i < _per.Length; i++)
        {
            if (_per[i].r == null || _per[i].colorPropId == -1) continue;
            s_Block.Clear();
            s_Block.SetColor(_per[i].colorPropId, flashColor);
            _per[i].r.SetPropertyBlock(s_Block);
        }
        _tEnd = Time.unscaledTime + duration;
    }
}
