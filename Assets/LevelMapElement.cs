using UnityEngine;
using UnityEngine.UI;

public class LevelMapElement : MonoBehaviour
{
    [SerializeField] private Image _lvlIMG;
    [SerializeField] private GameObject _lockIMG;
    [SerializeField] private Text _title;
    [SerializeField] private Text _requierementInfo;
    [SerializeField] private Image _progressBar;
    [SerializeField] private Text _levelPrice;
    [SerializeField] private GameObject _unlockButton;
   
    private LevelData _levelData;
    private LevelMapUI _levelMapUI;


    public void Init(LevelData lvlData, LevelMapUI mapUI)
    {
        _levelData = lvlData;
        _levelMapUI = mapUI;
        _lvlIMG.sprite = _levelData.IMG;
        _title.text = lvlData.Title;

        _title.GetComponent<LocalizedText>().SelectedKey = LocalizationKeyType.Level.ToString() + "/" + _levelData.Name;
        if (_levelData.Requirement != null)
            _requierementInfo.GetComponent<LocalizedText>().SelectedKey = LocalizationKeyType.Achievement.ToString() + "/" + _levelData.Requirement.id + "_Description";

        if (lvlData.Unlocked)
        {
            Destroy(_lockIMG);
            Destroy(_requierementInfo.gameObject);
            Destroy(_progressBar.transform.parent.gameObject);
            Destroy(_unlockButton);
        } else
        {
            int currentProgress = AchievementManager.Instance.GetCurrentProgress(lvlData.Requirement.type);
            int requirement = lvlData.Requirement.requirement;
            
            _requierementInfo.text = string.Format("{0}\n{1}/{2}", lvlData.Requirement.description, currentProgress, requirement);
            _progressBar.fillAmount = (float)currentProgress / requirement;
            _levelPrice.text = _levelData.Price.ToString();
        }
    }

    public void OnClick()
    {
        if (_levelData.Unlocked)
        {
            ControlManager.Instance.CursorActive = false;
            LoadingManager.Instance.LoadLocation(Location.Game, _levelData.Scene);
        }
    }


    public void UnlockLevel()
    {
        if (CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Gems, _levelData.Price) && CurrencyManager.Instance.RemoveCurrency(CurrencyType.Gems, _levelData.Price))
        {
            Destroy(_lockIMG);
            Destroy(_requierementInfo.gameObject);
            Destroy(_progressBar.transform.parent.gameObject);
            Destroy(_unlockButton);
            _levelData.SetUnlock(true, true);
        }
    }
}
