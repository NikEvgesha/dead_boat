using UnityEngine;
using UnityEngine.UI;


public class LevelUpButtomUI : MonoBehaviour
{
    [SerializeField] BoostItem _item;
    [SerializeField] RareType _rare;
    [SerializeField] Button _button;
    [SerializeField] Text _title;
    [SerializeField] Image _icon;
    [SerializeField] Text _rareText;
    [SerializeField] Image _rareBack;
    [SerializeField] Text _boostText;
    [SerializeField] Text _description;


    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(UseButton);
    }
    private void UseButton()
    {
            _item.AddBoost(_rare);
        LevelUpUi.Instance.Close();
    }
    public void AddItemInButton(BoostItem item,RareType rare)
    {
        _item = item;
        _rare = rare;
        _title.text = GetText(item.BoostType.ToString(), BoostUI.Title) ;
        _icon.sprite = item.BoostIcon;
        RareBack rareBack = LevelUpUi.Instance.GetRareBackUI(rare);
        _rareText.text = GetText(rare);
        _rareBack.sprite = rareBack.sprite;
        _boostText.text = item.GetReward(rare) +" "+ GetText(item.BoostType.ToString(), BoostUI.Useble);
        _description.text = GetText(item.BoostType.ToString(), BoostUI.Discription);
    }
    private string GetText(string key, BoostUI boostUI)
    {
        LocalizationData lData = LocalizationManager.Instance.LocalizationData;

        return lData.GetTranslation(lData.GetTranslation(BoostType.Boost.ToString()) + key +"/"+ boostUI.ToString());
    }

    private string GetText(RareType boostUI)
    {
        LocalizationData lData = LocalizationManager.Instance.LocalizationData;

        return lData.GetTranslation(lData.GetTranslation(BoostType.Boost.ToString()) + lData.GetTranslation(RareType.RareType.ToString()) + boostUI.ToString());
    }
}
