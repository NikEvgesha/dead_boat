using UnityEngine;

public abstract class SaveProvider : MonoBehaviour
{
    public abstract void Initialize();

    // Методы для работы с громкостью
    public abstract float[] LoadVolume();
    public abstract void SaveVolume(float musicVolume, float soundVolume);

    // Методы для работы со счётом
    public abstract void SaveScore(float score, int levelId);
    public abstract float LoadScore(int levelId);

    // Прочие методы (например, сохранение статуса уровней)
    public abstract void SaveLevelUnlock(int id, bool unlocked);
    public abstract void SaveLevelWin(int id, bool win);

    // Общий метод сохранения прогресса
    public abstract void SaveProgress();
}
