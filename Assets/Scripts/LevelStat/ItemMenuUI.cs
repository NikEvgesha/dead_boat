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
        ProfessionService.StateChanged += UpdateCount;

        if (EggHatchingManager.Instance != null)
            EggHatchingManager.Instance.StateChanged += UpdateCount;

    }
    private void OnDisable()
    {
        if (_item == null) return;
        _item.Change -= UpdateCount;
        LocalizationManager.Instance.OnLanguageChanged -= Localization;
        ProfessionService.StateChanged -= UpdateCount;

        if (EggHatchingManager.Instance != null)
            EggHatchingManager.Instance.StateChanged -= UpdateCount;
    }
    private void Localization(string l = "")
    {
        _name.text = GetText(_item.BoostType.ToString(), BoostUI.Stats);
    }
    private void UpdateCount(BoostType boost,float c) { UpdateCount(); }
    private void UpdateCount()
    {
        if (_count == null)
            return;

        Stats stats = LevelStatManager.Instance != null ? LevelStatManager.Instance.Stats : BuildFallbackStats();
        ProfessionPassiveBonuses profession = ProfessionService.GetCurrentPassiveBonuses();
        AnimalRunBuffs animals = EggAnimalBuffService.GetCurrentBuffsSnapshot();

        string text = "";
        switch (_item.BoostType)
        {
            case BoostType.HP:
                text = FormatFlatAndMultiplier(
                    stats.HP + profession.maxHealthFlat + animals.maxHealthFlat,
                    profession.SafeMaxHealthMultiplier);
                break;
            case BoostType.MultExp:
                text = FormatMultiplier(SafeMultiplier(stats.MultExp) * profession.SafeExperienceMultiplier * animals.SafeExperienceMultiplier);
                break;
            case BoostType.MoveSpeedMult:
                text = FormatFlatAndMultiplier(
                    profession.moveSpeedFlat + animals.moveSpeedFlat,
                    SafeMultiplier(stats.MoveSpeedMult) * profession.SafeMoveSpeedMultiplier * animals.SafeMoveSpeedMultiplier,
                    true);
                break;
            case BoostType.MoneyMultSale:
                text = FormatMultiplier(SafeMultiplier(stats.MoneyMultSale) * profession.SafeSaleRewardMultiplier * animals.SafeSaleRewardMultiplier);
                break;
            case BoostType.MaxFuel:
                text = FormatFlatAndMultiplier(
                    stats.MaxFuel + profession.maxFuelFlat + animals.maxFuelFlat,
                    profession.SafeMaxFuelMultiplier);
                break;
            case BoostType.ConsumptionFuel:
                text = FormatMultiplier(SafeMultiplier(stats.ConsumptionFuel) * profession.SafeFuelConsumptionMultiplier * animals.SafeFuelConsumptionMultiplier, false);
                break;
            case BoostType.AddMultFuel:
                text = FormatMultiplier(SafeMultiplier(stats.AddMultFuel) * profession.SafeFuelFillMultiplier * animals.SafeFuelFillMultiplier);
                break;
            case BoostType.MaxSpeedBoard:
                text = FormatFlatAndMultiplier(
                    stats.MaxSpeedBoard + profession.boatSpeedFlat + animals.boatSpeedFlat,
                    profession.SafeBoatSpeedMultiplier);
                break;
            case BoostType.MeleDamage:
                text = FormatFlatAndMultiplier(
                    stats.MeleDamage + profession.meleeDamageFlat + animals.meleeDamageFlat,
                    profession.SafeOutgoingDamageMultiplier);
                break;
            case BoostType.MeleAttackSpeed:
                text = FormatMultiplier(SafeMultiplier(stats.MeleAttackSpeed) * profession.SafeMeleeAttackSpeedMultiplier * animals.SafeMeleeAttackSpeedMultiplier);
                break;
            case BoostType.RangeDamage:
                text = FormatFlatAndMultiplier(
                    stats.RangeDamage + profession.rangedDamageFlat + animals.rangedDamageFlat,
                    profession.SafeOutgoingDamageMultiplier);
                break;
            case BoostType.RangeAttackSpeed:
                text = FormatMultiplier(SafeMultiplier(stats.RangeAttackSpeed) * profession.SafeRangedAttackSpeedMultiplier * animals.SafeRangedAttackSpeedMultiplier);
                break;
            case BoostType.RangeReloadSpeed:
                text = FormatMultiplier(SafeMultiplier(stats.RangeReloadSpeed) * profession.SafeRangedReloadSpeedMultiplier * animals.SafeRangedReloadSpeedMultiplier);
                break;
            default:
                Debug.LogWarning($"Неизвестный тип BoostType: {_item.BoostType}");
                break;
        }
        _count.text = text;
    }

    private static Stats BuildFallbackStats()
    {
        return new Stats
        {
            Save = true,
            MultExp = 1f,
            MoveSpeedMult = 1f,
            MoneyMultSale = 1f,
            ConsumptionFuel = 1f,
            AddMultFuel = 1f,
            MeleAttackSpeed = 1f,
            RangeAttackSpeed = 1f,
            RangeReloadSpeed = 1f
        };
    }

    private static string FormatFlatAndMultiplier(float flat, float multiplier, bool preferPercentWhenNoFlat = false)
    {
        float safeMultiplier = SafeMultiplier(multiplier);
        bool hasFlat = !IsZero(flat);
        bool hasMultiplier = !Mathf.Approximately(safeMultiplier, 1f);

        if (hasFlat && hasMultiplier)
            return $"{FormatSignedNumber(flat)} {FormatMultiplier(safeMultiplier)}";

        if (hasMultiplier || preferPercentWhenNoFlat)
            return FormatMultiplier(safeMultiplier);

        return FormatSignedNumber(flat);
    }

    private static string FormatMultiplier(float multiplier, bool plusForZero = true)
    {
        float percent = (SafeMultiplier(multiplier) - 1f) * 100f;
        return $"{FormatSignedNumber(percent, plusForZero)}%";
    }

    private static string FormatSignedNumber(float value, bool plusForZero = true)
    {
        if (IsZero(value))
            value = 0f;

        string sign = value > 0f || (plusForZero && Mathf.Approximately(value, 0f)) ? "+" : string.Empty;
        return sign + FormatNumber(value);
    }

    private static string FormatNumber(float value)
    {
        float rounded = Mathf.Round(value);
        if (Mathf.Abs(value - rounded) < 0.05f)
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.#");
    }

    private static float SafeMultiplier(float value)
    {
        return value > 0f ? value : 1f;
    }

    private static bool IsZero(float value)
    {
        return Mathf.Abs(value) < 0.005f;
    }

    private string GetText(string key, BoostUI boostUI)
    {
        LocalizationData lData = LocalizationManager.Instance.LocalizationData;

        return lData.GetTranslation(lData.GetTranslation(BoostType.Boost.ToString()) + key + "/" + boostUI.ToString());
    }
}
