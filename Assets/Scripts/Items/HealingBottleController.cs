using System.Collections;
using UnityEngine;

public class HealingBottleController : MonoBehaviour
{
    [Header("Настройки бутылки эликсира")]
    [SerializeField] private UsableItem _usableItem;       // Система использования (без StopUse)
    [SerializeField] private Animator _animator;           // Аниматор для анимаций питья
    [SerializeField] private float healingPerSecond = 5f;    // Сколько здоровья восстанавливается в секунду
    [SerializeField] private float maxDrinkTime = 3f;        // Максимальное время питья (бутылка полностью выпита за это время)
    [SerializeField] private int healingAmount = 20;         // Максимальное общее восстановление

    // Внутренние переменные
    private bool _isDrinking = false;
    private Coroutine _drinkRoutine;
    //private float _lastUseTime;
    //private float _useThreshold = 0.1f; // Если событие Use не вызвано 0.1 сек, считаем, что кнопка отпущена

    private void OnEnable()
    {
        // Подписываемся на событие Use при активации
        _usableItem.Active += SetActiveUse;
        //SetActiveUse(_usableItem.IsActive);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isDrinking = false;
        _usableItem.Active -= SetActiveUse;
        _usableItem.Use -= StartDrinking;
        _usableItem.StopUse -= StopDrinking;
        ControlUI.Instance.ShowUseButton(false);
    }

    private void Start()
    {
        SetActiveUse(_usableItem.IsActive);
    }

    // Подписка/отписка от события Use в зависимости от активности
    private void SetActiveUse(bool active)
    {
        ControlUI.Instance.ShowUseButton(active);
        if (active)
        {
            _animator.enabled = true;
            _usableItem.Use += StartDrinking;
            _usableItem.StopUse += StopDrinking;
        }
        else
        {
            _animator.enabled = false;
            _usableItem.Use -= StartDrinking;
            _usableItem.StopUse -= StopDrinking;
        }
    }
    // Вызывается, когда кнопку начали удерживать
    private void StartDrinking()
    {
        if (!_isDrinking)
        {
            Debug.Log("Начал пить");
            _isDrinking = true;
            if (_animator != null)
                _animator.SetTrigger("Drink");  // Запуск анимации питья
            _drinkRoutine = StartCoroutine(Drink());
        }
    }
    // Вызывается, когда кнопку отпускают
    private void StopDrinking()
    {
        if (_isDrinking)
        {
            Debug.Log("Бросил пить");

            _isDrinking = false;
            if (_animator != null)
                _animator.SetTrigger("StopDrink");  // Остановка анимации питья
            if (_drinkRoutine != null)
            {
                StopCoroutine(_drinkRoutine);
                _drinkRoutine = null;
            }
        }
    }



    // Сопрограмма, постепенно восстанавливающая здоровье
    private IEnumerator Drink()
    {
        float elapsedTime = 0f;
        int totalHealed = 0;

        while (_isDrinking && elapsedTime < maxDrinkTime && totalHealed < healingAmount)
        {
            float delta = Time.deltaTime;
            elapsedTime += delta;
            // Расчет восстановления за кадр
            int healThisFrame = Mathf.RoundToInt(healingPerSecond * delta);
            // Не превышаем максимальное значение
            if (totalHealed + healThisFrame > healingAmount)
                healThisFrame = healingAmount - totalHealed;
            totalHealed += healThisFrame;

            // Здесь предполагается, что у вас есть синглтон или ссылка на компонент здоровья игрока
            // Например, PlayerHealth.Instance.AddHealth(healThisFrame);

            yield return null;
        }
        _isDrinking = false;
        if (_animator != null)
            _animator.SetTrigger("StopDrink");
        ConsumeBottle();
    }


    // Логика "расходования" бутылки (например, удаление объекта или обновление инвентаря)
    private void ConsumeBottle()
    {
        PickableItem item = gameObject.GetComponentInParent<PickableItem>();
        item.Useble();
        PlayerStatsManager.Instance.AddHealth(healingAmount);
        Debug.Log("Бутылка эликсира использована");

        //Destroy(gameObject.GetComponentInParent<PickableItem>());
    }
}
