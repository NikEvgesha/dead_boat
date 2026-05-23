using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // Автоматически требует компонент Button
public class UISound : MonoBehaviour
{
    private Button _button; // Ссылка на кнопку

    void Awake()
    {
        Rebind();
    }

    void OnEnable()
    {
        Rebind();
    }

    public void Rebind()
    {
        // Получаем компонент кнопки с того же объекта
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button == null)
            return;

        // Добавляем обработчик нажатия кнопки
        _button.onClick.RemoveListener(PlaySound);
        _button.onClick.AddListener(PlaySound);
    }

    // Метод для воспроизведения звука
    private void PlaySound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUIClick();
    }

    // Очистка слушателя при уничтожении объекта
    void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlaySound);
        }
    }
}