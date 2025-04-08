using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

// Перечисление состояний конца игры
public enum EndGameState
{
    None,
    Faint, // обморок
    Win,   // победа
    Lose   // поражение
}

public class EndGameUIManager : MonoBehaviour
{
    [Header("Общие элементы")]

    [SerializeField] private GameObject _panel;   // Родительский объект заголовка (например, панель)
    // Слайдер для отсчёта времени (визуальная индикация оставшегося времени)
    [SerializeField] private Slider timerSlider;
    // Текст, который будет выводить сообщение об оставшемся времени
    [SerializeField] private Text timerMessageText;
    [SerializeField] private Text timerMessageTextFaint;
    // Кнопка перехода в лобби
    [SerializeField] private Button lobbyButton;

    [Header("Элементы для победы и поражения")]
    [SerializeField] private GameObject titleObject;   // Родительский объект заголовка (например, панель)
    [SerializeField] private Text titleTextLose;           // Текст заголовка ("Вы проиграли")
    [SerializeField] private Text titleTextWin;           // Текст заголовка ("Вы выиграли")
    [SerializeField] private GameObject distanceObject;// Панель с информацией о дистанции
    [SerializeField] private Text distanceText;        // Текст для отображения дистанции (например, "Пройденный путь: {0}м")
    [SerializeField] private Button playAgainButton;   // Кнопка "Сыграть снова"

    [Header("Элементы возрождения")]
    [SerializeField] private Button reviveButton;      // Кнопка возрождения (поведение зависит от состояния: оплата монетами или через рекламу)

    // Время отсчёта
    private float timerDuration;
    private float timerRemaining;
    private void Start()
    {
        ShowEndGameUI(EndGameState.None);
    }
    // Текущее состояние конца игры
    private EndGameState currentState = EndGameState.None;

    /// <summary>
    /// Метод для отображения UI в конце игры.
    /// </summary>
    /// <param name="state">Состояние окончания игры: Win, Lose или Faint</param>
    /// <param name="distance">Пройденная дистанция (применяется для состояний Win и Lose)</param>
    /// <param name="duration">Длительность таймера (в секундах)</param>
    public void ShowEndGameUI(EndGameState state, int distance = 0, float duration = 10f)
    {
        currentState = state;
        timerDuration = duration;
        timerRemaining = duration;

        // Настройка слайдера: задаём максимальное значение и устанавливаем текущее значение
        timerSlider.maxValue = timerDuration;
        timerSlider.value = timerDuration;

        // Скрываем все элементы, чтобы затем активировать только нужные
        titleObject.SetActive(false);
        distanceObject.SetActive(false);
        playAgainButton.gameObject.SetActive(false);
        lobbyButton.gameObject.SetActive(false);
        reviveButton.gameObject.SetActive(false);
        titleTextLose.gameObject.SetActive(false);
        titleTextWin.gameObject.SetActive(false);
        timerMessageText.gameObject.SetActive(false);
        timerMessageTextFaint.gameObject.SetActive(false);

        // Кнопка перехода в лобби показывается всегда
        if (state == EndGameState.None)
        {
            lobbyButton.gameObject.SetActive(false);
            timerSlider.gameObject.SetActive(false);
            _panel.gameObject.SetActive(false);
        } 
        else
        {
            _panel.gameObject.SetActive(true);
            lobbyButton.gameObject.SetActive(true);
            timerSlider.gameObject.SetActive(true);
        }

        if (state == EndGameState.Faint)
        {
            timerMessageTextFaint.gameObject.SetActive(true);
            // Состояние обморока: не показываем заголовок и информацию о дистанции.
            timerMessageTextFaint.text = string.Format("Осталось {0} секунд", Mathf.Ceil(timerRemaining));
            reviveButton.gameObject.SetActive(true); // Возрождение за монеты
            StartCoroutine(FaintStateTimer());
        }
        else if (state == EndGameState.Win)
        {
            // Состояние победы: показываем заголовок, дистанцию, кнопку "Сыграть снова"
            titleObject.SetActive(true);
            titleTextWin.gameObject.SetActive(true); 
            distanceObject.SetActive(true);
            timerMessageText.gameObject.SetActive(true);
            distanceText.text = string.Format("Пройденный путь: {0}м", distance);
            playAgainButton.gameObject.SetActive(true);
            // Текст таймера для победы/поражения
            timerMessageText.text = string.Format("Возвращаемся в лобби через {0} секунд", Mathf.Ceil(timerRemaining));
            StartCoroutine(WinLoseStateTimer());
        }
        else if (state == EndGameState.Lose)
        {
            // Состояние поражения: показываем заголовок, дистанцию, кнопку "Сыграть снова", а также кнопку возрождения (с рекламой)
            timerMessageText.gameObject.SetActive(true);
            titleObject.SetActive(true);
            titleTextLose.gameObject.SetActive(true);
            distanceObject.SetActive(true);
            distanceText.text = string.Format("Пройденный путь: {0}м", distance);
            playAgainButton.gameObject.SetActive(true);
            reviveButton.gameObject.SetActive(true);
            timerMessageText.text = string.Format("Возвращаемся в лобби через {0} секунд", Mathf.Ceil(timerRemaining));
            StartCoroutine(WinLoseStateTimer());
        }
    }

    // Обратный отсчёт для состояния обморока (Faint)
    private IEnumerator FaintStateTimer()
    {
        while (timerRemaining > 0)
        {
            // Обновление слайдера и текстового сообщения
            timerSlider.value = timerRemaining;
            timerMessageTextFaint.text = string.Format("Осталось {0} секунд", Mathf.Ceil(timerRemaining));
            yield return new WaitForSeconds(1f);
            timerRemaining--;
        }
        // По окончании отсчёта переключаем состояние на "Поражение"
        ShowEndGameUI(EndGameState.Lose, distance: 0, duration: timerDuration);
    }

    // Обратный отсчёт для состояний Победы и Поражения
    private IEnumerator WinLoseStateTimer()
    {
        while (timerRemaining > 0)
        {
            // Обновление слайдера и текстового сообщения
            timerSlider.value = timerRemaining;
            timerMessageText.text = string.Format("Возвращаемся в лобби через {0} секунд", Mathf.Ceil(timerRemaining));
            yield return new WaitForSeconds(1f);
            timerRemaining--;
        }
        // Автоматически переходим в сцену лобби по окончании отсчёта
        LoadLobby();
    }

    #region Методы для кнопок

    // Метод, привязанный к кнопке "Лобби"
    public void OnLobbyButtonClicked()
    {
        LoadLobby();
    }

    // Метод, привязанный к кнопке "Сыграть снова"
    public void OnPlayAgainClicked()
    {
        // Перезапуск текущей сцены
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Метод, привязанный к кнопке возрождения
    public void OnReviveButtonClicked()
    {
        if (currentState == EndGameState.Faint)
        {
            // При обмороке возрождение за монеты
            bool coinsDeducted = DeductCoinsForRevive();
            if (coinsDeducted)
            {
                RevivePlayer();
            }
            else
            {
                Debug.Log("Недостаточно монет для возрождения!");
                // Здесь можно вывести сообщение об ошибке
            }
        }
        else if (currentState == EndGameState.Lose)
        {
            // При поражении: показать рекламу перед возрождением
            ShowAdAndRevive();
        }
    }

    #endregion

    // Метод для загрузки сцены лобби
    private void LoadLobby()
    {
        // Замените "LobbyScene" на имя вашей сцены лобби
        SceneManager.LoadScene("LobbyScene");
    }

    // Пример метода списания монет для возрождения
    private bool DeductCoinsForRevive()
    {
        // Реализуйте логику проверки и списания монет
        // Например:
        // if (GameManager.Instance.Coins >= coinCost) { GameManager.Instance.Coins -= coinCost; return true; }
        return true;
    }

    // Метод возрождения игрока
    private void RevivePlayer()
    {
        Debug.Log("Игрок возрожден");
        // Спрячьте UI конца игры и восстановите игровое состояние
        gameObject.SetActive(false);
        // Дополнительные процедуры возрождения персонажа
    }

    // Метод для показа рекламы и последующего возрождения
    private void ShowAdAndRevive()
    {
        Debug.Log("Показ рекламы для возрождения");
        // Реализуйте здесь вызов показа рекламы.
        // После завершения рекламы вызовите RevivePlayer()
        RevivePlayer();
    }
}
