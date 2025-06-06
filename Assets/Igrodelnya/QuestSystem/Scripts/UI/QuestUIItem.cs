using UnityEngine;
using UnityEngine.UI;
public class QuestUIItem : MonoBehaviour
{
    [Header("Ссылки на UI-элементы (заполните через Inspector)")]
    public Text titleText;
    public Text descriptionText;
    public Slider progressBar;
    public Button claimButton;
    public Text claimButtonText;

    private QuestInstance boundQuest;

    /// <summary>
    /// Вызывается сразу после Instantiate(prefab) — привязываем QuestInstance
    /// и настраиваем UI.
    /// </summary>
    public void Bind(QuestInstance quest)
    {
        boundQuest = quest;

        // Установим локализованный текст (из QuestDefinition)
        titleText.text = quest.questDefinition.Title;
        if (descriptionText != null)
            descriptionText.text = quest.questDefinition.Description;

        // Прячем кнопку «Забрать» до готовности квеста
        claimButton.gameObject.SetActive(false);

        // Устанавливаем первоначальный прогресс
        float p = boundQuest.GetCurrentProgress();
        if (progressBar != null)
            progressBar.value = p;

        // Подписываемся на события QuestInstance
        boundQuest.OnProgressChanged += OnProgressChanged;
        boundQuest.OnReadyToClaim += OnReadyToClaim;
        boundQuest.OnQuestClaimed += OnBoundQuestDestroyed;
        boundQuest.OnDestroyed += OnBoundQuestDestroyed;

        // Навешиваем клик на кнопку «Забрать»
        claimButton.onClick.AddListener(() =>
        {
            boundQuest.ClaimReward();
        });

        // Если квест уже завершён (IsCompleted) к этому моменту — сразу показать «Забрать»
        if (boundQuest.IsCompleted)
        {
            OnReadyToClaim();
        }
    }

    private void OnProgressChanged(float normalized)
    {
        // Обновляем шкалу, если кнопка «Забрать» ещё скрыта
        if (!claimButton.gameObject.activeSelf && progressBar != null)
            progressBar.value = normalized;
    }

    private void OnReadyToClaim()
    {
        // Скрываем полоску прогресса и показываем кнопку «Забрать награду»
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);

        claimButton.gameObject.SetActive(true);
        claimButtonText.text = "Забрать награду";
    }

    private void OnBoundQuestDestroyed(QuestInstance _)
    {
        // Когда QuestInstance удаляется (Destroy) или ClaimReward  уничтожаем UI
        Destroy(gameObject);
    }
    private void OnBoundQuestDestroyed()
    {
        // Когда QuestInstance удаляется (Destroy) или ClaimReward  уничтожаем UI
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (boundQuest != null)
        {
            boundQuest.OnProgressChanged -= OnProgressChanged;
            boundQuest.OnReadyToClaim -= OnReadyToClaim;
            boundQuest.OnQuestClaimed -= OnBoundQuestDestroyed;
            boundQuest.OnDestroyed -= OnBoundQuestDestroyed;
        }
        claimButton.onClick.RemoveAllListeners();
    }
}
