using UnityEngine;
using UnityEngine.UI;

public class AnimalInventorySlot : MonoBehaviour
{
    [SerializeField] private LocalizationData _localizationData;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _detailText;
    [SerializeField] private Text _stageText;
    [SerializeField] private Image _animalIconImage;
    [SerializeField] private GameObject _actionRoot;
    [SerializeField] private Button _button;

    public void Init(AnimalDefinition definition, int stage, int amount, string detail)
    {
        int safeStage = Mathf.Max(1, stage);

        if (_titleText != null)
        {
            string titleKey = definition != null && !string.IsNullOrWhiteSpace(definition.title)
                ? definition.title
                : definition != null ? definition.animalId : string.Empty;
            _titleText.text = GetLocalizedText(titleKey, titleKey);
        }

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_stageText != null)
            _stageText.text = GetLocalizedText("Eggs/AnimalStage", "Stage") + $" {safeStage}";

        if (_detailText != null)
            _detailText.text = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail;

        if (_animalIconImage != null)
        {
            _animalIconImage.sprite = definition != null ? definition.icon : null;
            _animalIconImage.enabled = _animalIconImage.sprite != null;
        }

        if (_actionRoot != null)
            _actionRoot.SetActive(false);

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.interactable = false;
        }
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (_localizationData == null || string.IsNullOrWhiteSpace(key))
            return fallback;

        string language = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage
            : "Ru";

        return _localizationData.TryGetTranslation(key, language, out string value)
            ? value
            : fallback;
    }
}
