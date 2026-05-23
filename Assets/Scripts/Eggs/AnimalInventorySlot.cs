using UnityEngine;
using UnityEngine.UI;

public class AnimalInventorySlot : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _detailText;
    [SerializeField] private Text _stageText;
    [SerializeField] private Text _buffsLabelText;
    [SerializeField] private Image _animalIconImage;
    [SerializeField] private Button _button;

    public void Init(AnimalDefinition definition, int stage, int amount, string detail)
    {
        int safeStage = Mathf.Max(1, stage);

        if (_titleText != null)
            _titleText.text = EggFeatureLocalization.AnimalTitle(definition);

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_stageText != null)
            _stageText.text = EggFeatureLocalization.StageLong(safeStage);

        if (_buffsLabelText != null)
            _buffsLabelText.text = EggFeatureLocalization.Text("UI/AnimalInventory/Buffs", "Умения", "Abilities");

        if (_detailText != null)
            _detailText.text = FormatBuffText(detail);

        if (_animalIconImage != null)
        {
            _animalIconImage.sprite = definition != null ? definition.icon : null;
            _animalIconImage.enabled = _animalIconImage.sprite != null;
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.interactable = false;
        }
    }

    private static string FormatBuffText(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return EggFeatureLocalization.Text("UI/AnimalBuff/None", "Нет бонусов", "No buffs");

        return detail.Replace(", ", "\n");
    }
}
