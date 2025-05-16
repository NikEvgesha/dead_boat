
using UnityEngine;

[CreateAssetMenu(fileName = "Achievement", menuName ="ScriptableObject/Achievement")]
public class Achievement : ScriptableObject
{
    public string id;
    public AchievementType type;
    public int requirement;
    public string title => LocalizationManager.Instance.LocalizationData.GetTranslation(id + "_Title", LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Achievement.ToString());
    public string description => LocalizationManager.Instance.LocalizationData.GetTranslation(id + "_Description", LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Achievement.ToString());

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