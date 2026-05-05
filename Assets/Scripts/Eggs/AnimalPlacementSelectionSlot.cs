using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimalPlacementSelectionSlot : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _incomeText;
    [SerializeField] private Button _button;

    private string _eggId;
    private Action<string> _onSelect;

    public void BindTemporaryReferences(Text titleText, Text countText, Text incomeText, Button button)
    {
        _titleText = titleText;
        _countText = countText;
        _incomeText = incomeText;
        _button = button;
    }

    public void Init(EggHatchingDefinition definition, int amount, Action<string> onSelect)
    {
        _eggId = definition != null ? definition.eggId : string.Empty;
        _onSelect = onSelect;

        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrWhiteSpace(definition?.title)
                ? _eggId
                : definition.title;
        }

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_incomeText != null)
        {
            if (definition == null || definition.passiveIncomeCoins <= 0)
            {
                _incomeText.text = "No income";
            }
            else
            {
                int interval = Mathf.Max(1, definition.passiveIncomeIntervalSeconds);
                _incomeText.text = $"+{definition.passiveIncomeCoins} / {FormatSeconds(interval)}";
            }
        }

        if (_button == null)
            return;

        _button.onClick.RemoveListener(OnButtonClicked);
        _button.onClick.AddListener(OnButtonClicked);
        _button.interactable = definition != null && amount > 0 && !string.IsNullOrWhiteSpace(_eggId);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(_eggId))
            return;

        _onSelect?.Invoke(_eggId);
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
