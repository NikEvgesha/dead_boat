using UnityEngine;

[System.Serializable]
public class LevelData
{
    [SerializeField] private string _name;
    [SerializeField] private string _sceneName;
    [SerializeField] private Sprite _lvlImg;
    [SerializeField] private Achievement _requierement;
    [SerializeField] private int _gemsPrice;

    private bool _unlocked;

    public string Name => _name;
    public string Scene => _sceneName;

    public Sprite IMG => _lvlImg;

    public Achievement Requirement => _requierement;

    public int Price => _gemsPrice;

    public bool Unlocked => _unlocked;

    public string Title => LocalizationManager.Instance.LocalizationData.GetTranslation(_name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Level.ToString());

    public void SetUnlock(bool unlocked, bool toSave = true)
    {
        _unlocked = unlocked;
        if (toSave)
        {
            SaveManager.Instance.SaveLevelStatus(_name, unlocked);
        }
    }
}