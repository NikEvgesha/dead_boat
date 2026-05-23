using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimalMergeInventorySlot : MonoBehaviour
{
    [SerializeField] private LocalizationData _localizationData;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _stageText;
    [SerializeField] private Text _detailText;
    [SerializeField] private Image _animalIconImage;
    [SerializeField] private Button _button;

    private string _animalId;
    private int _stage = 1;
    private Action<string, int> _onSelect;

    public void BindReferences(Text titleText, Text countText, Text stageText, Text detailText, Image animalIconImage, Button button)
    {
        _titleText = titleText;
        _countText = countText;
        _stageText = stageText;
        _detailText = detailText;
        _animalIconImage = animalIconImage;
        _button = button;
    }

    public void Init(AnimalDefinition definition, int stage, int amount, string detail, Action<string, int> onSelect)
    {
        _animalId = definition != null ? definition.animalId : string.Empty;
        _stage = Mathf.Max(1, stage);
        _onSelect = onSelect;

        if (_titleText != null)
            _titleText.text = EggFeatureLocalization.AnimalTitle(definition);

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_stageText != null)
            _stageText.text = EggFeatureLocalization.StageLong(_stage);

        if (_detailText != null)
            _detailText.text = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail;

        if (_animalIconImage != null)
        {
            _animalIconImage.sprite = definition != null ? definition.icon : null;
            _animalIconImage.enabled = _animalIconImage.sprite != null;
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(OnButtonClicked);
            _button.onClick.AddListener(OnButtonClicked);
            _button.interactable = amount > 0 && !string.IsNullOrWhiteSpace(_animalId);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(_animalId))
            return;

        _onSelect?.Invoke(_animalId, _stage);
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
