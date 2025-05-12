
[System.Serializable]
public class Achievement
{
    public string id;
    public AchievementType type;
    public int requirement;

    // доабвить current progress ? сохранять тип + текущий прогресс и на старте загружать и проходиться по списку,
    // если прогресс больше требуемого - разблокировать достижение

    private bool unlocked = false;
    private bool rewarded;

    public bool Unlocked => unlocked;

    public void StartUnlock(bool unlock)
    {
        unlocked = unlock;
    }

    public void Unlock()
    {
        unlocked = true;
        GiveReward();
        rewarded = true;
        SaveManager.Instance.SaveAchiementStatus(id, rewarded);
    }

    private void GiveReward()
    {

    }
}