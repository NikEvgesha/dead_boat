using System.Collections.Generic;
using UnityEngine;

public abstract class SaveProvider : MonoBehaviour
{
    public bool Changed;
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
    public abstract bool CheckProgress();

    public abstract void SaveGems(int amount);

    public abstract int LoadGems();

    public abstract void SaveAchievementProgress(AchievementType id, int progress);
    public abstract int LoadAchievementProgress(AchievementType id);

    public abstract void SaveAchievementStatus(string id, bool progress);
    public abstract bool LoadAchievementStatus(string id);

    public abstract void SaveLevelStatus(string lvlName, bool unlocked);
    public abstract bool LoadLevelStatus(string lvlName);

    public abstract void SaveLobbyItem(string id);
    public abstract List<string> LoadLobbyItems();

    public abstract void ResetLobbyItems();


    public abstract void SaveDistance(int distance);
    public abstract void SaveInventory(List<PickableItem> items);

    public abstract List<string> LoadInventory();
    public abstract int LoadDistance();

    public abstract void SaveFuel(int fuel);
    public abstract int LoadFuel();

    public abstract void SaveAttachedItem(string id);
    public abstract void SaveAllAttachedItems(List<string> items);
    public abstract List<string> LoadAttachedItems();

    public abstract void ResetAttachedItems();


    public abstract void SetSave(bool save);

}
