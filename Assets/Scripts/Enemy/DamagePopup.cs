using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private Text textUI;         // ваш UI Text
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float fadeDuration = 1f;

    private Color originalColor;
    private float lifetime;

    private void Awake()
    {
        originalColor = textUI.color;
    }

    /// <summary>
    /// Создать всплывающую цифру с рандомным смещением
    /// </summary>
    public static DamagePopup Create(Transform target, int damage)
    {
        // Загружаем префаб (или храните ссылку в менеджере)
        var prefab = Resources.Load<DamagePopup>("DamagePopup");
        // Базовая позиция над головой
        Vector3 basePos = target.position + Vector3.up * 2f;
        // Рандомное смещение по X и Y (можно настроить диапазон)
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.2f, 0.2f),
            0f
        );

        // Итоговая позиция
        Vector3 spawnPos = basePos + randomOffset;

        // Инстансим и настраиваем цифру
        var popup = Instantiate(prefab, spawnPos, Quaternion.identity);
        popup.Setup(damage);
        return popup;
    }

    private void Setup(int damage)
    {
        textUI.text = damage.ToString();
        lifetime = 0f;
    }

    private void Update()
    {
        // Поднимаем вверх
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Фейдим
        lifetime += Time.deltaTime;
        float alpha = Mathf.Lerp(originalColor.a, 0, lifetime / fadeDuration);
        textUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        // Удаляем после завершения
        if (lifetime >= fadeDuration)
            Destroy(gameObject);
    }
}
