using UnityEngine;
using UnityEngine.UI;

public class ItemMenuUI : MonoBehaviour
{
    [SerializeField] private BoostItem _item;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _name;
    [SerializeField] private Text _count;

    private void Start()
    {
        if (_item == null) return;

        _icon.sprite = _item.BoostIcon;
        Localization();
        UpdateCount();
    }

    private void OnEnable()
    {
        if (_item == null) return;

        UpdateCount();
        Localization();
        _item.Change += UpdateCount;
        LocalizationManager.Instance.OnLanguageChanged += Localization;

    }
    private void OnDisable()
    {
        if (_item == null) return;
        _item.Change -= UpdateCount;
        LocalizationManager.Instance.OnLanguageChanged -= Localization;
    }
    private void Localization(string l = "")
    {
        _name.text = GetText(_item.BoostType.ToString(), BoostUI.Stats);
    }
    private void UpdateCount(BoostType boost,float c) { UpdateCount(); }
    private void UpdateCount()
    {
        Stats Stats = LevelStatManager.Instance.Stats;
        //_count.text = 
        string text = "";
            switch (_item.BoostType)
        {
            case BoostType.HP:
                text = "+" + Stats.HP;
                break;
            case BoostType.MultExp:
                text = "+" + (Stats.MultExp - 1) + "%";
                break;
            case BoostType.MoveSpeedMult:
                text = "+" + (Stats.MoveSpeedMult - 1) + "%";
                break;
            case BoostType.MoneyMultSale:
                text = "+" + (Stats.MoneyMultSale - 1) + "%";
                break;
            case BoostType.MaxFuel:
                text = "+" + Stats.MaxFuel;
                break;
            case BoostType.ConsumptionFuel:
                text = "-" + (Stats.ConsumptionFuel - 1) + "%";
                break;
            case BoostType.AddMultFuel:
                text = "+" + (Stats.AddMultFuel - 1) + "%";
                break;
            case BoostType.MaxSpeedBoard:
                text = "+" + Stats.MaxSpeedBoard;
                break;
            case BoostType.MeleDamage:
                text = "+" + Stats.MeleDamage;
                break;
            case BoostType.MeleAttackSpeed:
                text = "+" + (Stats.MeleAttackSpeed - 1) + "%";
                break;
            case BoostType.RangeDamage:
                text = "+" + Stats.RangeDamage;
                break;
            case BoostType.RangeAttackSpeed:
                text = "+" + (Stats.RangeAttackSpeed - 1) + "%";
                break;
            case BoostType.RangeReloadSpeed:
                text = "+" + (Stats.RangeReloadSpeed - 1) + "%";
                break;
            default:
                Debug.LogWarning($"Неизвестный тип BoostType: {_item.BoostType}");
                break;
        }
        _count.text = text;
    }

    private string GetText(string key, BoostUI boostUI)
    {
        LocalizationData lData = LocalizationManager.Instance.LocalizationData;

        return lData.GetTranslation(lData.GetTranslation(BoostType.Boost.ToString()) + key + "/" + boostUI.ToString());
    }
}
