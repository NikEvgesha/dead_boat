/// <summary>
/// Интрефейс для любого типа «условия» (Condition) в квесте.
/// Каждый компонент, реализующий IQuestCondition, 
/// должен сам подписываться на нужные игровые события в OnEnable/OnDisable.
/// </summary>
public interface IQuestCondition
{
    // Инициализация (может быть пустая, если вся логика в OnEnable/OnDisable)
    void Initialize();

    // Возвращает true, если условие выполнено (достигнут требуемый count или NPC отдан)
    bool IsSatisfied { get; }

    // Возвращает float от 0 до 1, показывающий прогресс условия (0–100%)
    // (на усмотрение: если условие «одноразовое» (Interact), можно вернуть 1 или 0).
    float GetProgressNormalized();

    // Обязательно отписаться от событий, чтобы не было «утечек» подписок.
    void Dispose();
}
