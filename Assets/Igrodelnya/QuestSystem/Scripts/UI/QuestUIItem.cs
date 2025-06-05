using UnityEngine;
using UnityEngine.UI;

public class QuestUIItem : MonoBehaviour
{
    [Header("Ссылки на UI-элементы")]
    public Text titleText;
    public Text descriptionText;
    public Slider progressBar;
    public Button claimButton;      // Кнопка «Забрать награду»
    public Text claimButtonText; // Текст на кнопке (например, "Забрать")

    private QuestInstance boundQuest;

    /// <summary>
    /// Привязываем QuestInstance и сразу заполняем UI.
    /// </summary>
    public void Bind(QuestInstance quest)
    {
        boundQuest = quest;

        // Заполнить заголовок/описание (локализовано через QuestDefinition)
        titleText.text = quest.questDefinition.Title;
        if (descriptionText != null)
            descriptionText.text = quest.questDefinition.Description;

        // Прячем кнопку «Забрать» до тех пор, пока квест не станет готов
        claimButton.gameObject.SetActive(false);

        // Сразу отрисовать прогресс 
        float p = quest.GetCurrentProgress();
        if (progressBar != null)
            progressBar.value = p;

        // Подписываемся на прогресс
        quest.OnProgressChanged += OnProgressChanged;
        // Подписываемся на «квест готов к получению»
        quest.OnReadyToClaim += OnReadyToClaim;

        // Навешиваем событие на кнопку «Забрать»
        claimButton.onClick.AddListener(OnClaimButtonClicked);
    }

    private void OnProgressChanged(float normalized)
    {
        // Если ещё не готов к получению  показываем прогресс-бар
        if (progressBar != null && !claimButton.gameObject.activeSelf)
            progressBar.value = normalized;
    }

    private void OnReadyToClaim()
    {
        // Скрываем прогресс-бар и показываем кнопку «Забрать»
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);

        claimButton.gameObject.SetActive(true);
        claimButtonText.text = "Забрать награду";
    }

    private void OnClaimButtonClicked()
    {
        // Когда игрок нажал «Забрать», вызываем ClaimReward у QuestInstance
        boundQuest.ClaimReward();
    }

    private void OnDestroy()
    {
        if (boundQuest != null)
        {
            boundQuest.OnProgressChanged -= OnProgressChanged;
            boundQuest.OnReadyToClaim -= OnReadyToClaim;
            claimButton.onClick.RemoveAllListeners();
        }
    }
}
