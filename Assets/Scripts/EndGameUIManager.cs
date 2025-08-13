using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using MirraGames.SDK;

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
    public static Action<EndGameState> EndGame;
    private void Awake()
    {
        EndGame += ShowEndGameUIAction;
    }
    private void OnDestroy()
    {
        EndGame -= ShowEndGameUIAction;
    }
    private void Start()
    {
        ShowEndGameUI(EndGameState.None);
    }
    // Текущее состояние конца игры
    private EndGameState currentState = EndGameState.None;
    private void ShowEndGameUIAction(EndGameState state)
    {
        MirraSDK.Analytics.GameplayStop();
        //Debug.Log("GameplayStop");
        ShowEndGameUI(state);
    }
        /// <summary>
        /// Метод для отображения UI в конце игры.
        /// </summary>
        /// <param name="state">Состояние окончания игры: Win, Lose или Faint</param>
        /// <param name="distance">Пройденная дистанция (применяется для состояний Win и Lose)</param>
        /// <param name="duration">Длительность таймера (в секундах)</param>
        public void ShowEndGameUI(EndGameState state, int distance = 0, float duration = 10f)
    {
        bool isStart = currentState == state && state == EndGameState.None;
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

        PlayerStatsManager.Instance.InGame = true;

        // Кнопка перехода в лобби показывается всегда
        if (state == EndGameState.None)
        {
            ControlManager.Instance.MoveActive = true;
            if (!isStart)
                ControlManager.Instance.CursorActive = false;
            lobbyButton.gameObject.SetActive(false);
            timerSlider.gameObject.SetActive(false);
            _panel.gameObject.SetActive(false);
        } 
        else 
        {
            if (state != EndGameState.Lose)
            {
                ControlManager.Instance.MoveActive = false;
                ControlManager.Instance.CursorActive = true;
                _panel.gameObject.SetActive(true);
            }
            lobbyButton.gameObject.SetActive(true);
            timerSlider.gameObject.SetActive(true);
        }

        if (state == EndGameState.Faint)
        {
            timerMessageTextFaint.gameObject.SetActive(true);
            // Состояние обморока: не показываем заголовок и информацию о дистанции.
            string tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/SecLeft", LocalizationManager.Instance.CurrentLanguage);
            timerMessageTextFaint.text = string.Format(tagText, Mathf.Ceil(timerRemaining));
            reviveButton.gameObject.SetActive(true); // Возрождение за монеты
            StartCoroutine(FaintStateTimer());
        }
        else if (state == EndGameState.Win)
        {
            // Состояние победы: показываем заголовок, дистанцию, кнопку "Сыграть снова"
            CurrencyManager.Instance.ShowGems?.Invoke(true);
            titleObject.SetActive(true);
            titleTextWin.gameObject.SetActive(true); 
            distanceObject.SetActive(true);
            timerMessageText.gameObject.SetActive(true);
            string tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/Traveled", LocalizationManager.Instance.CurrentLanguage);
            distanceText.text = string.Format(tagText, GameManager.Instance.CurrentDistance);
            playAgainButton.gameObject.SetActive(true);
            SaveManager.Instance.SaveWin();
            // Текст таймера для победы/поражения

            GameManager.Instance.AddReward();
            PlayerStatsManager.Instance.InGame = false;

            tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/AutoLeft", LocalizationManager.Instance.CurrentLanguage);
            timerMessageText.text = string.Format(tagText, Mathf.Ceil(timerRemaining));
            StartCoroutine(WinLoseStateTimer());
        }
        else if (state == EndGameState.Lose)
        {
            // Состояние поражения: показываем заголовок, дистанцию, кнопку "Сыграть снова", а также кнопку возрождения (с рекламой)
            timerMessageText.gameObject.SetActive(true);
            titleObject.SetActive(true);
            titleTextLose.gameObject.SetActive(true);
            distanceObject.SetActive(true);
            string tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/Traveled", LocalizationManager.Instance.CurrentLanguage);
            distanceText.text = string.Format(tagText, GameManager.Instance.CurrentDistance);
            playAgainButton.gameObject.SetActive(true);
            //reviveButton.gameObject.SetActive(true);
            tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/AutoLeft", LocalizationManager.Instance.CurrentLanguage);
            timerMessageText.text = string.Format(tagText, Mathf.Ceil(timerRemaining));
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
            string tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/SecLeft", LocalizationManager.Instance.CurrentLanguage);
            timerMessageTextFaint.text = string.Format(tagText, Mathf.Ceil(timerRemaining));
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
            string tagText = LocalizationManager.Instance.LocalizationData.GetTranslation("Game/AutoLeft", LocalizationManager.Instance.CurrentLanguage);
            timerMessageText.text = string.Format(tagText, Mathf.Ceil(timerRemaining));
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
        StopAllCoroutines();
        GameManager.Instance.EndGame(false);
        ShowEndGameUI(EndGameState.None);
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Метод, привязанный к кнопке возрождения
    public void OnReviveButtonClicked()
    {
        /*        if (currentState == EndGameState.Faint)
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
                }*/

        ShowAdAndRevive();

    }

    #endregion

    // Метод для загрузки сцены лобби
    private void LoadLobby()
    {
        StopAllCoroutines();
        if (PurchasesManager.Instance.PurchasesAvailable())
        {
            GameManager.Instance.EndGame(true, currentState != EndGameState.Faint);
        }
        else
        {
            GameManager.Instance.EndGame(true);
        }
        ShowEndGameUI(EndGameState.None);
        // Замените "LobbyScene" на имя вашей сцены лобби
        //SceneManager.LoadScene("SampleScene");
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
        StopAllCoroutines();
        Debug.Log("Игрок возрожден");
        PlayerStatsManager.Instance.Revive();
        // Спрячьте UI конца игры и восстановите игровое состояние
        ShowEndGameUI(EndGameState.None);
        //gameObject.SetActive(false);
        // Дополнительные процедуры возрождения персонажа
    }

    // Метод для показа рекламы и последующего возрождения
    private void ShowAdAndRevive()
    {
        Debug.Log("Показ рекламы для возрождения");
        // Реализуйте здесь вызов показа рекламы.
        // После завершения рекламы вызовите RevivePlayer()
        AdsManager.Instance.ShowRewardedAd(
            "revive",
            (success) =>
            {
                if (success)
                    RevivePlayer();
            });
        
    }
    public EndGameState GetState()
    {
        return currentState;
    }
}
