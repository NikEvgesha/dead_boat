using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Список квестов (в порядке)")]
    [Tooltip("Перетащите сюда ваши префабы Quest_Kill5Rats, Quest_BuyCoal и т. д. " +
             "Они будут запускаться строго по порядку.")]
    public List<GameObject> questPrefabs = new List<GameObject>();

    [Header("UI")]
    public Transform questsUIContainer;   // Контейнер для QuestUIItem
    public GameObject questUIItemPrefab;  // Prefab QuestUIItem (UI-строка)

    private int currentIndex = 0;          // индекс в questPrefabs, который сейчас активен
    private QuestInstance currentQuest;    // ссылка на вновь созданный QuestInstance

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // если нужно
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Запускаем «очередной» квест на старте уровня
        
    }
    public void StartQuest()
    {
        StartNextQuest();
    }
    /// <summary>
    /// Создаёт новый QuestInstance из prefab questPrefabs[currentIndex].
    /// Если таких нет — просто ничего не делает (все квесты пройдены).
    /// </summary>
    private void StartNextQuest()
    {
        // Если мы вышли за пределы списка, значит квестов больше нет
        if (currentIndex < 0 || currentIndex >= questPrefabs.Count)
        {
            currentQuest = null;
            return;
        }

        // Инстанциируем следующий prefab
        GameObject prefab = questPrefabs[currentIndex];
        if (prefab == null)
        {
            Debug.LogError($"[QuestManager] questPrefabs[{currentIndex}] == null!");
            return;
        }

        // Создаём копию в сцене
        GameObject qGO = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        currentQuest = qGO.GetComponent<QuestInstance>();
        if (currentQuest == null)
        {
            Debug.LogWarning("Ошибка: prefab не содержит QuestInstance!");
            return;
        }

        // Регистрируем UI для этого нового квеста
        if (questUIItemPrefab != null && questsUIContainer != null)
        {
            var uiGO = Instantiate(questUIItemPrefab, questsUIContainer);
            var uiItem = uiGO.GetComponent<QuestUIItem>();
            uiItem.Bind(currentQuest);
        }

        // Подписываемся: когда игрок заберёт награду (ClaimReward),
        // запустить следующий квест:
        currentQuest.OnQuestClaimed += OnCurrentQuestClaimed;
    }

    /// <summary>
    /// Вызывается, когда текущий квест был окончательно «забран» (награда получена).
    /// Отключаем подписки, удаляем UI и запускаем следующий в списке.
    /// </summary>
    private void OnCurrentQuestClaimed()
    {
        if (currentQuest != null)
        {
            currentQuest.OnQuestClaimed -= OnCurrentQuestClaimed;
        }

        // Переходим к следующему квесту в очереди
        currentIndex++;
        StartNextQuest();
    }

    /// <summary>
    /// Регистрирует новый QuestInstance, чтобы QuestUIItem в Awake/Start его подхватил.
    /// (теперь это почти не нужно, так как мы сами создаём UI вручную)
    /// </summary>
    public void RegisterQuest(QuestInstance quest)
    {
        // Если хотим регистрировать «прочие» квесты, можно оставить пустым
    }

    /// <summary>
    /// Отменить регистрацию/закрыть квест (мы сами его уничтожаем по ClaimReward).
    /// </summary>
    public void UnregisterQuest(QuestInstance quest)
    {
        // Сюда можно добавить логику, если нужно еще что-то делать при завершении.
    }
}
