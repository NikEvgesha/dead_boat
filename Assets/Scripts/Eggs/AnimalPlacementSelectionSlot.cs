using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimalPlacementSelectionSlot : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _incomeText;
    [SerializeField] private Button _button;

    private string _animalId;
    private int _stage = 1;
    private Action<string, int> _onSelect;

    public void BindTemporaryReferences(Text titleText, Text countText, Text incomeText, Button button)
    {
        _titleText = titleText;
        _countText = countText;
        _incomeText = incomeText;
        _button = button;
    }

    public void Init(EggDefinition definition, int amount, Action<string> onSelect)
    {
        string animalId = definition != null ? definition.eggId : string.Empty;
        Init(animalId, 1, definition != null ? definition.title : animalId, "No income", amount,
            (selectedAnimalId, _) => onSelect?.Invoke(selectedAnimalId));
    }

    public void Init(string animalId, int stage, string title, string detail, int amount, Action<string, int> onSelect)
    {
        _animalId = animalId;
        _stage = Mathf.Max(1, stage);
        _onSelect = onSelect;

        if (_titleText != null)
        {
            string safeTitle = EggFeatureLocalization.AnimalTitle(_animalId, string.IsNullOrWhiteSpace(title) ? _animalId : title);
            _titleText.text = $"{safeTitle} {EggFeatureLocalization.StageShort(_stage)}";
        }

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_incomeText != null)
        {
            _incomeText.text = string.IsNullOrWhiteSpace(detail)
                ? EggFeatureLocalization.Text("Eggs/ReadyToCollect", "Готово", "Ready")
                : detail;
        }

        if (_button == null)
            return;

        _button.onClick.RemoveListener(OnButtonClicked);
        _button.onClick.AddListener(OnButtonClicked);
        _button.interactable = amount > 0 && !string.IsNullOrWhiteSpace(_animalId);
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

    private static string FormatSeconds(int totalSeconds)
    {
        int seconds = Mathf.Max(0, totalSeconds);
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int secs = seconds % 60;
        return $"{hours:00}:{minutes:00}:{secs:00}";
    }
}
