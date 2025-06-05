using System.Collections.Generic;
using UnityEngine;

public class QuestInstance : MonoBehaviour
{
    [Header("Ссылка на данные квеста (ScriptableObject)")]
    public QuestDefinition questDefinition;

    // Состояние:
    private bool isCompleted = false;       // true, когда все условия выполнены
    private bool isClaimed = false;       // true, когда награда забрана

    // Собранные Condition-компоненты (IQuestCondition)
    private List<IQuestCondition> conditions = new List<IQuestCondition>();

    // Событие «квест готов к получению награды»
    public event System.Action OnReadyToClaim;

    // Событие «игрок нажал кнопку забрать награду»
    public event System.Action OnQuestClaimed;

    private void Awake()
    {
        // Собираем все компоненты условия на этом GameObject (и потомках)
        var all = GetComponentsInChildren<MonoBehaviour>();
        foreach (var mb in all)
        {
            if (mb is IQuestCondition cond)
            {
                conditions.Add(cond);
                cond.Initialize();
            }
        }

        // Регистрируемся в QuestManager
        QuestManager.Instance.RegisterQuest(this);

        // Если условия уже выполнены (например, игрок сделал их раньше),
        // сразу же помечаем квест как «готов получить награду»
        CheckImmediateCompletion();
    }

    private void Update()
    {
        if (isCompleted || isClaimed)
            return;

        // Если ещё не выполнены все условия, проверяем каждую секунду/кадр
        bool allTrue = true;
        foreach (var cond in conditions)
        {
            if (!cond.IsSatisfied)
            {
                allTrue = false;
                break;
            }
        }

        if (allTrue)
        {
            // Помечаем: все условия достигнуты  квест завершён (но не выдали награду)
            isCompleted = true;
            // Отпишем все условия, чтобы не получать лишние события после готовности
            foreach (var cond in conditions)
                cond.Dispose();

            // Оповещаем UI: «квест можно забрать»
            OnReadyToClaim?.Invoke();
        }
        else
        {
            // Пока условия не все выполнены, можно оповестить UI о прогрессе
            float progress = CalculateNormalizedProgress();
            OnProgressChanged?.Invoke(progress);
        }
    }

    /// <summary>
    /// Если все условия (IQuestCondition) до этого момента уже выполнялись,
    /// сразу вызываем OnReadyToClaim, чтобы UI показал кнопку «забрать» сразу.
    /// </summary>
    private void CheckImmediateCompletion()
    {
        if (isCompleted || isClaimed) return;

        bool allTrue = true;
        foreach (var cond in conditions)
        {
            if (!cond.IsSatisfied)
            {
                allTrue = false;
                break;
            }
        }

        if (allTrue)
        {
            isCompleted = true;
            foreach (var cond in conditions)
                cond.Dispose();
            OnReadyToClaim?.Invoke();
        }
    }

    // Метод для UI, чтобы получить текущий прогресс (0..1).
    public float GetCurrentProgress()
    {
        return CalculateNormalizedProgress();
    }

    private float CalculateNormalizedProgress()
    {
        if (conditions.Count == 0) return 1f;
        float sum = 0f;
        foreach (var cond in conditions)
            sum += cond.GetProgressNormalized();
        return sum / conditions.Count;
    }

    /// <summary>
    /// Вызывается по клику кнопки «Забрать награду».
    /// Здесь выдаём игроку награду, запускаем UnityEvent onQuestCompleted,
    /// и оповещаем об окончательном закрытии квеста.
    /// </summary>
    public void ClaimReward()
    {
        if (!isCompleted || isClaimed)
            return;

        isClaimed = true;

        // Выдать награду
        RewardManager.Instance.GiveReward(questDefinition.reward);
        // Дополнительный UnityEvent (например, открыть дверь и т. д.)
        questDefinition.onQuestCompleted?.Invoke();

        // Оповестить, что квест окончательно «забран»
        OnQuestClaimed?.Invoke();

        // Удалить/деактивировать квест из сцены
        QuestManager.Instance.UnregisterQuest(this);
        Destroy(gameObject);
    }

    // Событие прогресса, которое UI может слушать (от 0 до 1)
    public event System.Action<float> OnProgressChanged;

    // Для отладки в редакторе
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && questDefinition != null)
        {
            var loc = LocalizationManager.Instance;
            string label = questDefinition.questId.ToString(); // или questDefinition.questId.ToString()
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, label);
        }
#endif
    }
}
