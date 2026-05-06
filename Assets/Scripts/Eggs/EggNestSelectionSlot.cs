using System;
using UnityEngine;
using UnityEngine.UI;

public class EggNestSelectionSlot : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _durationText;
    [SerializeField] private Button _button;

    private string _eggId;
    private Action<string> _onSelect;

    public void BindTemporaryReferences(Text titleText, Text countText, Text durationText, Button button)
    {
        _titleText = titleText;
        _countText = countText;
        _durationText = durationText;
        _button = button;
    }

    public void Init(EggDefinition definition, int amount, Action<string> onSelect)
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

        if (_durationText != null)
        {
            _durationText.text = definition == null
                ? string.Empty
                : FormatSeconds(Mathf.Max(1, definition.incubationSeconds));
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
